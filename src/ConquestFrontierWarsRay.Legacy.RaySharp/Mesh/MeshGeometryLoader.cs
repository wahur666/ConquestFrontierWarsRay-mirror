using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using RaySharp.Particle;
using Raylib_cs;

namespace RaySharp.Mesh;

internal static class MeshGeometryLoader {
	private const int HiddenFace = 0x08;
	private const int FlatShadedFace = 0x02;
	private const float PositionScale = 0.01f;
	private const uint ChannelFloat = 1;
	private const uint ChannelVector = 2;
	private const uint ChannelQuaternion = 4;
	private static readonly Color ShieldTint = new(111, 169, 255, 92);

	public static MeshGeometry LoadXml(string path, MeshUvMode uvMode = MeshUvMode.Raw) {
		return path.EndsWith(".shield.xml", StringComparison.OrdinalIgnoreCase)
			? LoadShieldXml(path)
			: path.EndsWith(".cmp.xml", StringComparison.OrdinalIgnoreCase)
				? LoadCompoundXml(path, uvMode)
				: Load3DbXml(path, uvMode);
	}

	public static MeshGeometry Load3DbXml(string path, MeshUvMode uvMode = MeshUvMode.Raw) {
		UtfDocument document = UtfParser.Load(path);
		UtfNode root = FindRootNode(document) ?? throw new InvalidDataException("UTF XML has no root node.");
		UtfNode meshNode = FindDescendant(root, "openFLAME 3D N-mesh")
		                   ?? throw new InvalidDataException("No openFLAME 3D N-mesh node found.");
		Dictionary<string, UtfTextureImage> textureImages = LoadMergedTextureImages(root, meshNode);
		return LoadMeshNode(Path.GetFileName(path), meshNode, textureImages, uvMode, root);
	}

	private static MeshGeometry LoadMeshNode(
		string name,
		UtfNode meshNode,
		Dictionary<string, UtfTextureImage> textureImages,
		MeshUvMode uvMode,
		UtfNode? containerNode) {
		IReadOnlyDictionary<string, UtfNode> mesh = meshNode.Children;
		UtfNode verticesNode = RequiredChild(meshNode, "Vertices");
		UtfNode faceGroupsNode = RequiredChild(meshNode, "Face groups");
		UtfNode? normalsNode = FindChild(meshNode, "Normals");
		UtfNode? materialLibraryNode = FindChild(meshNode, "Material library");

		MeshLoadDiagnostics diagnostics = new();
		Dictionary<int, MeshMaterialInfo> materialsById = ReadMaterialMap(materialLibraryNode, textureImages);
		diagnostics.DecodedTextureCount = textureImages.Count;
		diagnostics.DecodedTextureNames.AddRange(textureImages.Keys);
		diagnostics.DiffuseMaterialMapCount = materialsById.Values.Count(material => !string.IsNullOrWhiteSpace(material.TextureName));
		diagnostics.DiffuseTextureNames.AddRange(materialsById.Values.Select(material => material.TextureName).Where(name => !string.IsNullOrWhiteSpace(name))!);
		Vector3[] objectVertices = ReadVector3Array(RequiredChild(verticesNode, "Object vertex list").Value, PositionScale);
		Vector2[] textureVertices = ReadVector2Array(FindChild(verticesNode, "Texture vertex list")?.Value, flipV: uvMode == MeshUvMode.FlipV);
		int[] vertexBatch = ReadInt32Array(RequiredChild(verticesNode, "Vertex batch list").Value);
		int[] textureBatch = ReadInt32Array(FindChild(verticesNode, "Texture batch list")?.Value);
		int[] textureBatch2 = ReadInt32Array(FindChild(verticesNode, "Texture batch list2")?.Value);
		Vector3[] normals = ReadVector3Array(FindChild(normalsNode, "Surface normal list")?.Value, 1.0f);
		int[] vertexNormals = ReadInt32Array(FindChild(verticesNode, "Vertex normal")?.Value);
		int materialCount = CountMaterials(materialLibraryNode);
		List<MeshTriangle> triangles = [];
		int sourceFaceCount = 0;

		foreach (UtfNode groupNode in faceGroupsNode.Children.Values
			         .Where(node => node.Name.StartsWith("Group", StringComparison.OrdinalIgnoreCase))
			         .OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase)) {
			int[] faceChain = ReadInt32Array(RequiredChild(groupNode, "Face vertex chain").Value);
			int[] faceNormals = ReadInt32Array(FindChild(groupNode, "Face normal")?.Value);
			int[] faceProperties = ReadInt32Array(FindChild(groupNode, "Face property")?.Value);
			int faceCount = ReadInt32(FindChild(groupNode, "Face count")?.Value) ?? faceChain.Length / 3;
			int materialId = ReadInt32(FindChild(groupNode, "Material")?.Value) ?? 0;
			MeshMaterialInfo material = materialsById.GetValueOrDefault(materialId, MeshMaterialInfo.Default);
			string? textureName = material.TextureName;
			bool textureAvailable = !string.IsNullOrWhiteSpace(textureName) && FindTextureImage(textureImages, textureName) is not null;
			bool textureHasAlpha = textureAvailable && FindTextureImage(textureImages, textureName!)?.HasAlpha == true;
			if (!string.IsNullOrWhiteSpace(textureName) && !textureAvailable) {
				diagnostics.MissingTextureNames.Add(textureName);
			}
			sourceFaceCount += faceCount;

			for (int faceIndex = 0; faceIndex < faceCount; faceIndex++) {
				int properties = faceIndex < faceProperties.Length ? faceProperties[faceIndex] : 0;
				if ((properties & HiddenFace) != 0) {
					continue;
				}

				int chainOffset = faceIndex * 3;
				if (chainOffset + 2 >= faceChain.Length) {
					continue;
				}

				Vector3 a = VertexFromBatch(faceChain[chainOffset], vertexBatch, objectVertices);
				Vector3 b = VertexFromBatch(faceChain[chainOffset + 1], vertexBatch, objectVertices);
				Vector3 c = VertexFromBatch(faceChain[chainOffset + 2], vertexBatch, objectVertices);
				if (IsDegenerate(a, b, c)) {
					continue;
				}

				Vector2 uvA = TextureFromBatch(faceChain[chainOffset], textureBatch, textureVertices);
				Vector2 uvB = TextureFromBatch(faceChain[chainOffset + 1], textureBatch, textureVertices);
				Vector2 uvC = TextureFromBatch(faceChain[chainOffset + 2], textureBatch, textureVertices);
				Vector2 uv2A = TextureFromBatch(faceChain[chainOffset], textureBatch2.Length > 0 ? textureBatch2 : textureBatch, textureVertices);
				Vector2 uv2B = TextureFromBatch(faceChain[chainOffset + 1], textureBatch2.Length > 0 ? textureBatch2 : textureBatch, textureVertices);
				Vector2 uv2C = TextureFromBatch(faceChain[chainOffset + 2], textureBatch2.Length > 0 ? textureBatch2 : textureBatch, textureVertices);
				Vector3 faceNormal = NormalFromIndex(faceIndex, faceNormals, normals, a, b, c);
				bool flat = (properties & FlatShadedFace) != 0;
				Vector3 normalA = flat ? faceNormal : NormalFromBatch(faceChain[chainOffset], vertexBatch, vertexNormals, normals, faceNormal);
				Vector3 normalB = flat ? faceNormal : NormalFromBatch(faceChain[chainOffset + 1], vertexBatch, vertexNormals, normals, faceNormal);
				Vector3 normalC = flat ? faceNormal : NormalFromBatch(faceChain[chainOffset + 2], vertexBatch, vertexNormals, normals, faceNormal);
				bool usesAlphaBlend = material.MaterialAlpha < 255 || textureHasAlpha;
				bool writesDepth = material.MaterialAlpha >= 255 && !textureHasAlpha;
				triangles.Add(new MeshTriangle(
					a,
					b,
					c,
					uvA,
					uvB,
					uvC,
					uv2A,
					uv2B,
					uv2C,
					normalA,
					normalB,
					normalC,
					textureName,
					material.TextureAddress,
					material.SecondDiffuseTextureName,
					material.SecondDiffuseTextureAddress,
					material.EmissiveTextureName,
					material.EmissiveTextureAddress,
					material.EmissiveBlend,
					material.Tint,
					usesAlphaBlend,
					writesDepth));
				if (textureAvailable) {
					diagnostics.TexturedTriangleCount++;
				} else {
					diagnostics.UntexturedTriangleCount++;
				}
			}
		}

		if (triangles.Count == 0) {
			throw new InvalidDataException("Mesh contains no drawable triangles.");
		}

		BoundingBox bounds = ComputeBounds(objectVertices);
		_ = mesh;
		_ = normals;
		_ = vertexNormals;
		List<MeshHardpoint> hardpoints = ReadHardpoints(FindChild(containerNode, "Hardpoints"), parentName: null);
		return new MeshGeometry(
			name,
			triangles,
			bounds,
			objectVertices.Length,
			sourceFaceCount,
			materialCount,
			textureImages,
			diagnostics,
			hardpoints: hardpoints);
	}

	private static MeshGeometry LoadCompoundXml(string path, MeshUvMode uvMode) {
		UtfDocument document = UtfParser.Load(path);
		UtfNode root = FindRootNode(document) ?? throw new InvalidDataException("UTF XML has no root node.");
		UtfNode cmpndNode = RequiredChild(root, "Cmpnd");
		List<CompoundPartInfo> parts = ReadCompoundParts(cmpndNode);
		List<MeshCompoundJoint> joints = ReadMeshCompoundJoints(FindChild(cmpndNode, "Cons"));
		List<MeshAnimationClip> animations = ReadAnimationClips(FindChild(root, "Animation"), joints);
		Dictionary<string, UtfNode> meshContainers = root.Children.Values
			.Where(node => FindChild(node, "openFLAME 3D N-mesh") is not null)
			.ToDictionary(node => node.Name, node => node, StringComparer.OrdinalIgnoreCase);

		if (parts.Count == 0) {
			parts = meshContainers.Values
				.Select((node, index) => new CompoundPartInfo(node.Name, node.Name, node.Name, index))
				.ToList();
		}

		Dictionary<string, Matrix4x4> partTransforms = ComputeCompoundPartTransforms(parts, joints);
		Dictionary<string, Matrix4x4> localPartTransforms = ComputeCompoundLocalPartTransforms(parts, joints);
		List<MeshPartHierarchyLink> hierarchy = BuildCompoundHierarchyLinks(joints);
		Dictionary<string, UtfTextureImage> mergedTextures = UtfTextureUtils.LoadTextureImages(root.Children);
		MeshLoadDiagnostics diagnostics = new();
		List<MeshTriangle> triangles = [];
		List<MeshPartGeometry> compoundParts = [];
		List<MeshHardpoint> hardpoints = [];
		List<MeshParticleEmitter> particleEmitters = [];
		int vertexCount = 0;
		int faceCount = 0;
		int materialCount = 0;

		foreach (CompoundPartInfo part in parts.OrderBy(part => part.Index < 0 ? int.MaxValue : part.Index)) {
			if (!meshContainers.TryGetValue(part.FileName, out UtfNode? containerNode)) {
				if (part.FileName.EndsWith(".pte", StringComparison.OrdinalIgnoreCase)) {
					Matrix4x4 particleTransform = localPartTransforms.GetValueOrDefault(part.ObjectName, Matrix4x4.Identity);
					particleEmitters.Add(new MeshParticleEmitter(
						part.ObjectName,
						part.FileName,
						ResolveUnifiedParticlePath(part.FileName),
						particleTransform,
						PositionScale));
				}

				continue;
			}

			UtfNode meshNode = RequiredChild(containerNode, "openFLAME 3D N-mesh");
			Dictionary<string, UtfTextureImage> partTextures = LoadMergedTextureImages(root, meshNode);
			MergeTextures(mergedTextures, partTextures);
			MeshGeometry partGeometry = LoadMeshNode(part.ObjectName, meshNode, partTextures, uvMode, containerNode);
			Matrix4x4 partTransform = partTransforms.GetValueOrDefault(part.ObjectName, Matrix4x4.Identity);
			Matrix4x4 localPartTransform = localPartTransforms.GetValueOrDefault(part.ObjectName, partTransform);
			compoundParts.Add(new MeshPartGeometry(
				part.ObjectName,
				part.FileName,
				localPartTransform,
				partGeometry.Triangles,
				partGeometry.Bounds));
			triangles.AddRange(partGeometry.Triangles.Select(triangle => TransformTriangle(triangle, partTransform)));
			hardpoints.AddRange(partGeometry.Hardpoints.Select(hardpoint => hardpoint with { ParentName = part.ObjectName }));
			vertexCount += partGeometry.VertexCount;
			faceCount += partGeometry.FaceCount;
			materialCount += partGeometry.MaterialCount;
			MergeDiagnostics(diagnostics, partGeometry.Diagnostics);
		}

		if (triangles.Count == 0) {
			throw new InvalidDataException("Compound contains no drawable mesh parts.");
		}

		diagnostics.DecodedTextureCount = mergedTextures.Count;
		diagnostics.DecodedTextureNames.Clear();
		diagnostics.DecodedTextureNames.AddRange(mergedTextures.Keys);
		return new MeshGeometry(
			Path.GetFileName(path),
			triangles,
			ComputeBoundsFromTriangles(triangles),
			vertexCount,
			faceCount,
			materialCount,
			mergedTextures,
			diagnostics,
			compoundParts,
			hierarchy,
			animations,
			hardpoints,
			particleEmitters);
	}

	private static MeshGeometry LoadShieldXml(string path) {
		UtfDocument document = UtfParser.Load(path);
		UtfNode root = FindRootNode(document) ?? throw new InvalidDataException("UTF XML has no root node.");
		UtfNode facesNode = RequiredChild(root, "Faces");
		UtfNode verticesNode = RequiredChild(root, "Vertices");
		ShieldVertex[] vertices = ReadShieldVertices(verticesNode.Value);
		int[] indices = ReadShieldIndices(facesNode.Value);
		List<MeshTriangle> triangles = [];

		for (int index = 0; index + 2 < indices.Length; index += 3) {
			int aIndex = indices[index];
			int bIndex = indices[index + 1];
			int cIndex = indices[index + 2];
			if (!TryGetShieldVertex(vertices, aIndex, out ShieldVertex a) ||
			    !TryGetShieldVertex(vertices, bIndex, out ShieldVertex b) ||
			    !TryGetShieldVertex(vertices, cIndex, out ShieldVertex c) ||
			    IsDegenerate(a.Position, b.Position, c.Position)) {
				continue;
			}

			triangles.Add(new MeshTriangle(
				a.Position,
				b.Position,
				c.Position,
				Vector2.Zero,
				Vector2.Zero,
				Vector2.Zero,
				Vector2.Zero,
				Vector2.Zero,
				Vector2.Zero,
				a.Normal,
				b.Normal,
				c.Normal,
				null,
				MeshTextureAddress.Repeat,
				null,
				MeshTextureAddress.Repeat,
				null,
				MeshTextureAddress.Repeat,
				255,
				ShieldTint,
				UsesAlphaBlend: true,
				WritesDepth: false));
		}

		if (triangles.Count == 0) {
			throw new InvalidDataException("Shield mesh contains no drawable triangles.");
		}

		MeshLoadDiagnostics diagnostics = new() {
			UntexturedTriangleCount = triangles.Count
		};

		return new MeshGeometry(
			Path.GetFileName(path),
			triangles,
			ComputeBounds(vertices.Select(vertex => vertex.Position).ToArray()),
			vertices.Length,
			indices.Length / 3,
			0,
			new Dictionary<string, UtfTextureImage>(StringComparer.OrdinalIgnoreCase),
			diagnostics);
	}

	private static List<MeshHardpoint> ReadHardpoints(UtfNode? hardpointsNode, string? parentName) {
		List<MeshHardpoint> hardpoints = [];
		if (hardpointsNode is null) {
			return hardpoints;
		}

		foreach (UtfNode groupNode in hardpointsNode.Children.Values) {
			foreach (UtfNode hardpointNode in groupNode.Children.Values) {
				Vector3? position = ReadVector3Value(FindChild(hardpointNode, "Position")?.Value, PositionScale);
				if (position is null) {
					continue;
				}

				Matrix3x3 orientation = ReadMatrix3Value(FindChild(hardpointNode, "Orientation")?.Value)
				                        ?? IdentityMatrix3x3();
				hardpoints.Add(new MeshHardpoint(
					hardpointNode.Name,
					groupNode.Name,
					parentName,
					position.Value,
					orientation));
			}
		}

		return hardpoints;
	}

	private static UtfTextureImage? FindTextureImage(
		IReadOnlyDictionary<string, UtfTextureImage> textures,
		string textureName) {
		return UtfTextureUtils.FindTextureImage(textures, textureName);
	}

	private static UtfNode? FindRootNode(UtfDocument document) {
		return document.RootNode ?? document.Roots.Values.FirstOrDefault(node => node.Children.Count > 0);
	}

	private static UtfNode? FindDescendant(UtfNode node, string name) {
		if (string.Equals(node.Name, name, StringComparison.OrdinalIgnoreCase)) {
			return node;
		}

		foreach (UtfNode child in node.Children.Values) {
			UtfNode? match = FindDescendant(child, name);
			if (match is not null) {
				return match;
			}
		}

		return null;
	}

	private static UtfNode RequiredChild(UtfNode node, string name) {
		return FindChild(node, name)
		       ?? throw new InvalidDataException($"Node '{node.Name}' is missing required child '{name}'.");
	}

	private static UtfNode? FindChild(UtfNode? node, string name) {
		return node?.Children.Values.FirstOrDefault(child => string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase));
	}

	private static int CountMaterials(UtfNode? materialLibraryNode) {
		if (materialLibraryNode is null) {
			return 0;
		}

		int? declared = ReadInt32(FindChild(materialLibraryNode, "Material count")?.Value);
		return declared ?? materialLibraryNode.Children.Values.Count(child =>
			!string.Equals(child.Name, "Material count", StringComparison.OrdinalIgnoreCase));
	}

	private static Dictionary<int, MeshMaterialInfo> ReadMaterialMap(
		UtfNode? materialLibraryNode,
		IReadOnlyDictionary<string, UtfTextureImage> textureImages) {
		Dictionary<int, MeshMaterialInfo> materials = [];
		if (materialLibraryNode is null) {
			return materials;
		}

		int fallbackIndex = 0;
		foreach (UtfNode materialNode in materialLibraryNode.Children.Values
			         .Where(child => !string.Equals(child.Name, "Material count", StringComparison.OrdinalIgnoreCase))) {
			int materialId = ReadInt32(FindChild(materialNode, "Material identifier")?.Value) ?? fallbackIndex;
			UtfNode? diffuseNode = FindChild(materialNode, "Diffuse");
			UtfNode? emissionNode = FindChild(materialNode, "Emission");
			UtfNode? bumpNode = FindChild(materialNode, "Bump");
			string? textureName = ReadString(FindChild(FindChild(diffuseNode, "Map"), "Name")?.Value);
			string? secondDiffuseTextureName = ReadString(FindChild(FindChild(bumpNode, "Map"), "Name")?.Value);
			string? emissiveTextureName = ReadString(FindChild(FindChild(emissionNode, "Map"), "Name")?.Value);
			int diffuseTextureFlags = ReadInt32(FindChild(FindChild(diffuseNode, "Map"), "Flags")?.Value) ?? 0;
			int secondDiffuseTextureFlags = ReadInt32(FindChild(FindChild(bumpNode, "Map"), "Flags")?.Value) ?? 0;
			int emissiveTextureFlags = ReadInt32(FindChild(FindChild(emissionNode, "Map"), "Flags")?.Value) ?? 0;
			float emissiveBlend = ReadSingleArray(FindChild(FindChild(emissionNode, "Map"), "Blend")?.Value).FirstOrDefault(1.0f);
			Color diffuse = ReadColor255(FindChild(diffuseNode, "Constant")?.Value, new Color(255, 255, 255, 255));
			Color emission = ReadColor255(FindChild(emissionNode, "Constant")?.Value, new Color(0, 0, 0, 255));
			float transparency = ReadSingleArray(FindChild(FindChild(materialNode, "Transparency"), "Constant")?.Value).FirstOrDefault(1.0f);
			byte alpha = ToByte(transparency);
			Color tint = new(
				Math.Min(255, diffuse.R + emission.R),
				Math.Min(255, diffuse.G + emission.G),
				Math.Min(255, diffuse.B + emission.B),
				alpha);
			materials[materialId] = new MeshMaterialInfo(
				textureName,
				TextureAddressFromFlags(diffuseTextureFlags),
				secondDiffuseTextureName,
				TextureAddressFromFlags(secondDiffuseTextureFlags),
				emissiveTextureName,
				TextureAddressFromFlags(emissiveTextureFlags),
				ToByte(emissiveBlend),
				tint,
				alpha);

			fallbackIndex++;
		}

		return materials;
	}

	private static Vector3 VertexFromBatch(int batchIndex, int[] vertexBatch, Vector3[] objectVertices) {
		if (batchIndex < 0 || batchIndex >= vertexBatch.Length) {
			return Vector3.Zero;
		}

		int vertexIndex = vertexBatch[batchIndex];
		return vertexIndex >= 0 && vertexIndex < objectVertices.Length ? objectVertices[vertexIndex] : Vector3.Zero;
	}

	private static Vector2 TextureFromBatch(int batchIndex, int[] textureBatch, Vector2[] textureVertices) {
		if (batchIndex < 0 || batchIndex >= textureBatch.Length) {
			return Vector2.Zero;
		}

		int textureIndex = textureBatch[batchIndex];
		return textureIndex >= 0 && textureIndex < textureVertices.Length ? textureVertices[textureIndex] : Vector2.Zero;
	}

	private static Vector3 NormalFromBatch(
		int batchIndex,
		int[] vertexBatch,
		int[] vertexNormals,
		Vector3[] normals,
		Vector3 fallback) {
		if (batchIndex < 0 || batchIndex >= vertexBatch.Length) {
			return fallback;
		}

		int vertexIndex = vertexBatch[batchIndex];
		if (vertexIndex < 0 || vertexIndex >= vertexNormals.Length) {
			return fallback;
		}

		int normalIndex = vertexNormals[vertexIndex];
		return NormalizeOrFallback(normalIndex >= 0 && normalIndex < normals.Length ? normals[normalIndex] : fallback, fallback);
	}

	private static Vector3 NormalFromIndex(int faceIndex, int[] faceNormals, Vector3[] normals, Vector3 a, Vector3 b, Vector3 c) {
		Vector3 fallback = NormalizeOrFallback(Vector3.Cross(b - a, c - a), Vector3.UnitY);
		if (faceIndex < 0 || faceIndex >= faceNormals.Length) {
			return fallback;
		}

		int normalIndex = faceNormals[faceIndex];
		return NormalizeOrFallback(normalIndex >= 0 && normalIndex < normals.Length ? normals[normalIndex] : fallback, fallback);
	}

	private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback) {
		return value.LengthSquared() > 0.000001f ? Vector3.Normalize(value) : fallback;
	}

	private static bool IsDegenerate(Vector3 a, Vector3 b, Vector3 c) {
		return Vector3.Cross(b - a, c - a).LengthSquared() < 0.000001f;
	}

	private static BoundingBox ComputeBounds(IReadOnlyList<Vector3> vertices) {
		if (vertices.Count == 0) {
			return new BoundingBox(Vector3.Zero, Vector3.One);
		}

		Vector3 min = vertices[0];
		Vector3 max = vertices[0];
		foreach (Vector3 vertex in vertices) {
			min = Vector3.Min(min, vertex);
			max = Vector3.Max(max, vertex);
		}

		return new BoundingBox(min, max);
	}

	private static BoundingBox ComputeBoundsFromTriangles(IReadOnlyList<MeshTriangle> triangles) {
		List<Vector3> vertices = new(triangles.Count * 3);
		foreach (MeshTriangle triangle in triangles) {
			vertices.Add(triangle.A);
			vertices.Add(triangle.B);
			vertices.Add(triangle.C);
		}

		return ComputeBounds(vertices);
	}

	private static Dictionary<string, UtfTextureImage> LoadMergedTextureImages(UtfNode root, UtfNode meshNode) {
		Dictionary<string, UtfTextureImage> textures = UtfTextureUtils.LoadTextureImages(root.Children);
		MergeTextures(textures, UtfTextureUtils.LoadTextureImages(meshNode.Children));
		return textures;
	}

	private static void MergeTextures(Dictionary<string, UtfTextureImage> target, IReadOnlyDictionary<string, UtfTextureImage> source) {
		foreach ((string name, UtfTextureImage image) in source) {
			target[name] = image;
		}
	}

	private static string? ResolveUnifiedParticlePath(string particleFileName) {
		string unifiedName = particleFileName.EndsWith(".unified.xml", StringComparison.OrdinalIgnoreCase)
			? particleFileName
			: $"{particleFileName}.unified.xml";
		string? resolved = ParticleSampleResolver.Resolve(unifiedName);
		if (resolved is not null) {
			return resolved;
		}

		string stemUnifiedName = $"{Path.GetFileNameWithoutExtension(particleFileName)}.pte.unified.xml";
		return ParticleSampleResolver.Resolve(stemUnifiedName);
	}

	private static void MergeDiagnostics(MeshLoadDiagnostics target, MeshLoadDiagnostics source) {
		target.DiffuseMaterialMapCount += source.DiffuseMaterialMapCount;
		target.TexturedTriangleCount += source.TexturedTriangleCount;
		target.UntexturedTriangleCount += source.UntexturedTriangleCount;
		target.DiffuseTextureNames.AddRange(source.DiffuseTextureNames);
		target.MissingTextureNames.AddRange(source.MissingTextureNames);
	}

	private static MeshTriangle TransformTriangle(MeshTriangle triangle, Matrix4x4 transform) {
		return triangle with {
			A = Vector3.Transform(triangle.A, transform),
			B = Vector3.Transform(triangle.B, transform),
			C = Vector3.Transform(triangle.C, transform),
			NormalA = TransformNormal(triangle.NormalA, transform),
			NormalB = TransformNormal(triangle.NormalB, transform),
			NormalC = TransformNormal(triangle.NormalC, transform)
		};
	}

	private static Vector3 TransformNormal(Vector3 normal, Matrix4x4 transform) {
		Vector3 transformed = Vector3.TransformNormal(normal, transform);
		return NormalizeOrFallback(transformed, normal);
	}

	private static List<MeshAnimationClip> ReadAnimationClips(UtfNode? animationNode, IReadOnlyList<MeshCompoundJoint> joints) {
		UtfNode? scriptRoot = FindChild(animationNode, "Script");
		if (scriptRoot is null) {
			return [];
		}

		Dictionary<string, MeshAnimationChannel> namedChannels = ReadNamedAnimationChannels(FindChild(animationNode, "Chnl"));
		Dictionary<string, MeshCompoundJoint> jointByPair = joints.ToDictionary(joint => JointKey(joint.Parent, joint.Child), joint => joint, StringComparer.OrdinalIgnoreCase);
		List<MeshAnimationClip> clips = [];

		foreach (UtfNode scriptNode in scriptRoot.Children.Values.Where(node => node.Children.Count > 0)) {
			List<MeshAnimationTrack> tracks = [];
			float duration = 0.0f;
			foreach (UtfNode mapNode in scriptNode.Children.Values.Where(node => node.Children.Count > 0)) {
				bool isJointMap = mapNode.Name.StartsWith("Joint map", StringComparison.OrdinalIgnoreCase);
				bool isObjectMap = mapNode.Name.StartsWith("Object map", StringComparison.OrdinalIgnoreCase);
				if (!isJointMap && !isObjectMap) {
					continue;
				}

				string? parent = ReadString(FindChild(mapNode, "Parent name")?.Value);
				string? child = ReadString(FindChild(mapNode, "Child name")?.Value);
				string? targetName = isJointMap ? child : parent;
				if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(targetName)) {
					continue;
				}

				string? channelName = ReadString(FindChild(mapNode, "Channel name")?.Value);
				MeshAnimationChannel? channel = !string.IsNullOrWhiteSpace(channelName)
					? namedChannels.GetValueOrDefault(channelName)
					: ReadAnimationChannel(FindChild(mapNode, "Channel"));
				if (channel is null) {
					continue;
				}

				if (isJointMap) {
					if (string.IsNullOrWhiteSpace(child) || !jointByPair.TryGetValue(JointKey(parent, child), out MeshCompoundJoint joint)) {
						continue;
					}

					tracks.Add(new MeshAnimationTrack(MeshAnimationTargetType.Joint, targetName, joint, channel));
				} else {
					tracks.Add(new MeshAnimationTrack(MeshAnimationTargetType.Object, targetName, null, channel));
				}

				duration = Math.Max(duration, channel.Duration);
			}

			if (tracks.Count > 0 && duration > 0.0f) {
				clips.Add(new MeshAnimationClip(scriptNode.Name, duration, tracks));
			}
		}

		return clips;
	}

	private static Dictionary<string, MeshAnimationChannel> ReadNamedAnimationChannels(UtfNode? channelRoot) {
		Dictionary<string, MeshAnimationChannel> channels = new(StringComparer.OrdinalIgnoreCase);
		if (channelRoot is null) {
			return channels;
		}

		foreach (UtfNode channelNode in channelRoot.Children.Values) {
			MeshAnimationChannel? channel = ReadAnimationChannel(channelNode);
			if (channel is not null) {
				channels[channelNode.Name] = channel;
			}
		}

		return channels;
	}

	private static MeshAnimationChannel? ReadAnimationChannel(UtfNode? channelNode) {
		UtfNode? headerNode = FindChild(channelNode, "Header");
		UtfNode? framesNode = FindChild(channelNode, "Frames");
		byte[]? headerBytes = headerNode?.Value;
		byte[]? frameBytes = framesNode?.Value;
		if (headerBytes is null || frameBytes is null || headerBytes.Length < 12) {
			return null;
		}

		uint frameCount = ReadUInt32(headerBytes, 0);
		float captureRate = ReadSingle(headerBytes, 4);
		uint type = ReadUInt32(headerBytes, 8);
		if (frameCount == 0) {
			return null;
		}

		if ((type & (ChannelVector | ChannelQuaternion)) == (ChannelVector | ChannelQuaternion)) {
			return ReadFullTransformChannel(frameBytes, frameCount, captureRate);
		}

		if ((type & ChannelQuaternion) != 0) {
			return ReadQuaternionChannel(frameBytes, frameCount, captureRate);
		}

		if ((type & ChannelVector) != 0) {
			return ReadVectorChannel(frameBytes, frameCount, captureRate);
		}

		return (type & ChannelFloat) != 0
			? ReadFloatChannel(frameBytes, frameCount, captureRate)
			: null;
	}

	private static MeshAnimationChannel? ReadFloatChannel(byte[] frameBytes, uint frameCount, float captureRate) {
		int frameSize = (captureRate < 0.0f ? 4 : 0) + 4;
		if (frameBytes.Length < frameSize * frameCount) {
			return null;
		}

		List<MeshFloatKeyframe> frames = [];
		for (int index = 0; index < frameCount; index++) {
			int offset = index * frameSize;
			int dataOffset = captureRate < 0.0f ? offset + 4 : offset;
			float time = captureRate < 0.0f ? ReadSingle(frameBytes, offset) : index * captureRate;
			frames.Add(new MeshFloatKeyframe(time, ReadSingle(frameBytes, dataOffset)));
		}

		return new MeshAnimationChannel(MeshAnimationChannelKind.Float, Math.Max(0.0f, frames[^1].Time), floatFrames: frames);
	}

	private static MeshAnimationChannel? ReadVectorChannel(byte[] frameBytes, uint frameCount, float captureRate) {
		int frameSize = (captureRate < 0.0f ? 4 : 0) + 12;
		if (frameBytes.Length < frameSize * frameCount) {
			return null;
		}

		List<MeshVectorKeyframe> frames = [];
		for (int index = 0; index < frameCount; index++) {
			int offset = index * frameSize;
			int dataOffset = captureRate < 0.0f ? offset + 4 : offset;
			float time = captureRate < 0.0f ? ReadSingle(frameBytes, offset) : index * captureRate;
			frames.Add(new MeshVectorKeyframe(time, ReadVector3(frameBytes, dataOffset, PositionScale)));
		}

		return new MeshAnimationChannel(MeshAnimationChannelKind.Vector, Math.Max(0.0f, frames[^1].Time), vectorFrames: frames);
	}

	private static MeshAnimationChannel? ReadQuaternionChannel(byte[] frameBytes, uint frameCount, float captureRate) {
		int frameSize = (captureRate < 0.0f ? 4 : 0) + 16;
		if (frameBytes.Length < frameSize * frameCount) {
			return null;
		}

		List<MeshQuaternionKeyframe> frames = [];
		for (int index = 0; index < frameCount; index++) {
			int offset = index * frameSize;
			int dataOffset = captureRate < 0.0f ? offset + 4 : offset;
			float time = captureRate < 0.0f ? ReadSingle(frameBytes, offset) : index * captureRate;
			frames.Add(new MeshQuaternionKeyframe(time, ReadQuaternionWxyz(frameBytes, dataOffset)));
		}

		return new MeshAnimationChannel(MeshAnimationChannelKind.Quaternion, Math.Max(0.0f, frames[^1].Time), quaternionFrames: frames);
	}

	private static MeshAnimationChannel? ReadFullTransformChannel(byte[] frameBytes, uint frameCount, float captureRate) {
		int frameSize = (captureRate < 0.0f ? 4 : 0) + 28;
		if (frameBytes.Length < frameSize * frameCount) {
			return null;
		}

		List<MeshTransformKeyframe> frames = [];
		for (int index = 0; index < frameCount; index++) {
			int offset = index * frameSize;
			int dataOffset = captureRate < 0.0f ? offset + 4 : offset;
			float time = captureRate < 0.0f ? ReadSingle(frameBytes, offset) : index * captureRate;
			Vector3 position = ReadVector3(frameBytes, dataOffset, PositionScale);
			Quaternion rotation = ReadQuaternionWxyz(frameBytes, dataOffset + 12);
			frames.Add(new MeshTransformKeyframe(time, position, rotation));
		}

		return new MeshAnimationChannel(MeshAnimationChannelKind.FullTransform, Math.Max(0.0f, frames[^1].Time), transformFrames: frames);
	}

	private static Quaternion ReadQuaternionWxyz(byte[] bytes, int offset) {
		float w = ReadSingle(bytes, offset);
		float x = ReadSingle(bytes, offset + 4);
		float y = ReadSingle(bytes, offset + 8);
		float z = ReadSingle(bytes, offset + 12);
		Quaternion q = new(x, y, z, w);
		return q.LengthSquared() > 0.0f ? Quaternion.Normalize(q) : Quaternion.Identity;
	}

	private static string JointKey(string parent, string child) {
		return $"{parent}\n{child}";
	}

	private static List<CompoundPartInfo> ReadCompoundParts(UtfNode cmpndNode) {
		return cmpndNode.Children.Values
			.Where(node => string.Equals(node.Name, "Root", StringComparison.OrdinalIgnoreCase)
			               || node.Name.StartsWith("Part", StringComparison.OrdinalIgnoreCase))
			.Select(node => new CompoundPartInfo(
				node.Name,
				ReadString(FindChild(node, "Object name")?.Value) ?? node.Name,
				ReadString(FindChild(node, "File name")?.Value) ?? string.Empty,
				ReadInt32(FindChild(node, "Index")?.Value) ?? -1))
			.Where(part => !string.IsNullOrWhiteSpace(part.FileName))
			.ToList();
	}

	private static List<MeshCompoundJoint> ReadMeshCompoundJoints(UtfNode? consNode) {
		if (consNode is null) {
			return [];
		}

		List<MeshCompoundJoint> joints = [];
		joints.AddRange(ReadFixedLikeJoints(FindChild(consNode, "Fix"), MeshCompoundJointType.Fixed));
		joints.AddRange(ReadFixedLikeJoints(FindChild(consNode, "Trans"), MeshCompoundJointType.Translational));
		joints.AddRange(ReadFixedLikeJoints(FindChild(consNode, "Loose"), MeshCompoundJointType.Loose));
		joints.AddRange(ReadRevLikeJoints(FindChild(consNode, "Rev"), MeshCompoundJointType.Revolute));
		joints.AddRange(ReadRevLikeJoints(FindChild(consNode, "Pris"), MeshCompoundJointType.Prismatic));
		joints.AddRange(ReadSphereJoints(FindChild(consNode, "Sphere")));
		return joints;
	}

	private static IEnumerable<MeshCompoundJoint> ReadFixedLikeJoints(UtfNode? node, MeshCompoundJointType type) {
		const int recordSize = 64 + 64 + 12 + 36;
		byte[]? bytes = node?.Value;
		if (bytes is null) {
			yield break;
		}

		for (int offset = 0; offset + recordSize <= bytes.Length; offset += recordSize) {
			yield return new MeshCompoundJoint(
				type,
				ReadFixedString(bytes, offset, 64),
				ReadFixedString(bytes, offset + 64, 64),
				ReadVector3(bytes, offset + 128, PositionScale),
				Vector3.Zero,
				ReadMatrix3(bytes, offset + 140),
				Vector3.Zero);
		}
	}

	private static IEnumerable<MeshCompoundJoint> ReadRevLikeJoints(UtfNode? node, MeshCompoundJointType type) {
		const int recordSize = 64 + 64 + 12 + 12 + 36 + 12 + 4 + 4;
		byte[]? bytes = node?.Value;
		if (bytes is null) {
			yield break;
		}

		for (int offset = 0; offset + recordSize <= bytes.Length; offset += recordSize) {
			yield return new MeshCompoundJoint(
				type,
				ReadFixedString(bytes, offset, 64),
				ReadFixedString(bytes, offset + 64, 64),
				ReadVector3(bytes, offset + 128, PositionScale),
				ReadVector3(bytes, offset + 140, PositionScale),
				ReadMatrix3(bytes, offset + 152),
				ReadVector3(bytes, offset + 188, 1.0f));
		}
	}

	private static IEnumerable<MeshCompoundJoint> ReadSphereJoints(UtfNode? node) {
		const int recordSize = 64 + 64 + 12 + 12 + 36 + 24;
		byte[]? bytes = node?.Value;
		if (bytes is null) {
			yield break;
		}

		for (int offset = 0; offset + recordSize <= bytes.Length; offset += recordSize) {
			yield return new MeshCompoundJoint(
				MeshCompoundJointType.Spherical,
				ReadFixedString(bytes, offset, 64),
				ReadFixedString(bytes, offset + 64, 64),
				ReadVector3(bytes, offset + 128, PositionScale),
				ReadVector3(bytes, offset + 140, PositionScale),
				ReadMatrix3(bytes, offset + 152),
				Vector3.Zero);
		}
	}

	private static Dictionary<string, Matrix4x4> ComputeCompoundPartTransforms(
		IReadOnlyList<CompoundPartInfo> parts,
		IReadOnlyList<MeshCompoundJoint> joints) {
		Dictionary<string, Matrix4x4> transforms = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, List<MeshCompoundJoint>> childrenByParent = new(StringComparer.OrdinalIgnoreCase);
		foreach (MeshCompoundJoint joint in joints) {
			if (!childrenByParent.TryGetValue(joint.Parent, out List<MeshCompoundJoint>? children)) {
				children = [];
				childrenByParent[joint.Parent] = children;
			}

			children.Add(joint);
		}

		void Attach(string partName, Matrix4x4 parentTransform) {
			if (transforms.ContainsKey(partName)) {
				return;
			}

			transforms[partName] = parentTransform;
			if (!childrenByParent.TryGetValue(partName, out List<MeshCompoundJoint>? childJoints)) {
				return;
			}

			foreach (MeshCompoundJoint joint in childJoints) {
				Attach(joint.Child, JointLocalTransform(joint) * parentTransform);
			}
		}

		Attach("Root", Matrix4x4.Identity);
		foreach (CompoundPartInfo part in parts) {
			transforms.TryAdd(part.ObjectName, Matrix4x4.Identity);
		}

		return transforms;
	}

	private static Dictionary<string, Matrix4x4> ComputeCompoundLocalPartTransforms(
		IReadOnlyList<CompoundPartInfo> parts,
		IReadOnlyList<MeshCompoundJoint> joints) {
		Dictionary<string, Matrix4x4> transforms = new(StringComparer.OrdinalIgnoreCase) {
			["Root"] = Matrix4x4.Identity
		};

		foreach (MeshCompoundJoint joint in joints) {
			transforms[joint.Child] = JointLocalTransform(joint);
		}

		foreach (CompoundPartInfo part in parts) {
			transforms.TryAdd(part.ObjectName, Matrix4x4.Identity);
		}

		return transforms;
	}

	private static List<MeshPartHierarchyLink> BuildCompoundHierarchyLinks(IReadOnlyList<MeshCompoundJoint> joints) {
		List<MeshPartHierarchyLink> links = [];
		foreach (MeshCompoundJoint joint in joints) {
			if (!string.IsNullOrWhiteSpace(joint.Parent) && !string.IsNullOrWhiteSpace(joint.Child)) {
				links.Add(new MeshPartHierarchyLink(joint.Parent, joint.Child));
			}
		}

		return links;
	}

	private static Matrix4x4 JointLocalTransform(MeshCompoundJoint joint) {
		if (joint.Type is MeshCompoundJointType.Fixed or MeshCompoundJointType.Translational or MeshCompoundJointType.Loose) {
			return MatrixFromRotationTranslation(joint.RelativeOrientation, joint.ParentPoint);
		}

		Vector3 transformedChildPoint = Vector3.Transform(joint.ChildPoint, MatrixFromRotationTranslation(joint.RelativeOrientation, Vector3.Zero));
		return MatrixFromRotationTranslation(joint.RelativeOrientation, joint.ParentPoint - transformedChildPoint);
	}

	private static Matrix4x4 MatrixFromRotationTranslation(Matrix3x3 rotation, Vector3 translation) {
		return new Matrix4x4(
			rotation.E00, rotation.E10, rotation.E20, 0.0f,
			rotation.E01, rotation.E11, rotation.E21, 0.0f,
			rotation.E02, rotation.E12, rotation.E22, 0.0f,
			translation.X, translation.Y, translation.Z, 1.0f);
	}

	private static Matrix3x3 ReadMatrix3(byte[] bytes, int offset) {
		return new Matrix3x3(
			ReadSingle(bytes, offset),
			ReadSingle(bytes, offset + sizeof(float)),
			ReadSingle(bytes, offset + sizeof(float) * 2),
			ReadSingle(bytes, offset + sizeof(float) * 3),
			ReadSingle(bytes, offset + sizeof(float) * 4),
			ReadSingle(bytes, offset + sizeof(float) * 5),
			ReadSingle(bytes, offset + sizeof(float) * 6),
			ReadSingle(bytes, offset + sizeof(float) * 7),
			ReadSingle(bytes, offset + sizeof(float) * 8));
	}

	private static Matrix3x3? ReadMatrix3Value(byte[]? bytes) {
		return bytes is { Length: >= sizeof(float) * 9 }
			? ReadMatrix3(bytes, 0)
			: null;
	}

	private static Matrix3x3 IdentityMatrix3x3() {
		return new Matrix3x3(
			1.0f, 0.0f, 0.0f,
			0.0f, 1.0f, 0.0f,
			0.0f, 0.0f, 1.0f);
	}

	private static Vector3 ReadVector3(byte[] bytes, int offset, float scale) {
		return new Vector3(
			ReadSingle(bytes, offset),
			ReadSingle(bytes, offset + sizeof(float)),
			ReadSingle(bytes, offset + sizeof(float) * 2)) * scale;
	}

	private static Vector3? ReadVector3Value(byte[]? bytes, float scale) {
		return bytes is { Length: >= sizeof(float) * 3 }
			? ReadVector3(bytes, 0, scale)
			: null;
	}

	private static string ReadFixedString(byte[] bytes, int offset, int maxLength) {
		int end = offset;
		int limit = Math.Min(bytes.Length, offset + maxLength);
		while (end < limit && bytes[end] != 0) {
			end++;
		}

		return Encoding.ASCII.GetString(bytes, offset, end - offset);
	}

	private static Vector3[] ReadVector3Array(byte[]? bytes, float scale) {
		if (bytes is null || bytes.Length < sizeof(float) * 3) {
			return [];
		}

		int count = bytes.Length / (sizeof(float) * 3);
		Vector3[] values = new Vector3[count];
		for (int index = 0; index < count; index++) {
			int offset = index * sizeof(float) * 3;
			values[index] = new Vector3(
				ReadSingle(bytes, offset),
				ReadSingle(bytes, offset + sizeof(float)),
				ReadSingle(bytes, offset + sizeof(float) * 2)) * scale;
		}

		return values;
	}

	private static ShieldVertex[] ReadShieldVertices(byte[]? bytes) {
		const int headerSize = sizeof(ushort);
		const int recordSize = sizeof(float) * 6;
		if (bytes is null || bytes.Length < headerSize) {
			throw new InvalidDataException("Shield Vertices data is missing or too short.");
		}

		int declaredCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(0, sizeof(ushort)));
		int availableCount = (bytes.Length - headerSize) / recordSize;
		int count = Math.Min(declaredCount, availableCount);
		if (count == 0) {
			throw new InvalidDataException("Shield Vertices data contains no vertices.");
		}

		ShieldVertex[] vertices = new ShieldVertex[count];
		for (int index = 0; index < count; index++) {
			int offset = headerSize + index * recordSize;
			Vector3 position = new Vector3(
				ReadSingle(bytes, offset),
				ReadSingle(bytes, offset + sizeof(float)),
				ReadSingle(bytes, offset + sizeof(float) * 2)) * PositionScale;
			Vector3 normal = NormalizeOrFallback(new Vector3(
				ReadSingle(bytes, offset + sizeof(float) * 3),
				ReadSingle(bytes, offset + sizeof(float) * 4),
				ReadSingle(bytes, offset + sizeof(float) * 5)), Vector3.UnitY);
			vertices[index] = new ShieldVertex(position, normal);
		}

		return vertices;
	}

	private static int[] ReadShieldIndices(byte[]? bytes) {
		const int headerSize = sizeof(ushort);
		const int recordSize = 20;
		const int firstIndexOffset = 12;
		if (bytes is null || bytes.Length < headerSize) {
			throw new InvalidDataException("Shield Faces data is missing or too short.");
		}

		int declaredCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(0, sizeof(ushort)));
		int availableCount = (bytes.Length - headerSize) / recordSize;
		int faceCount = Math.Min(declaredCount, availableCount);
		if (faceCount == 0) {
			throw new InvalidDataException("Shield Faces data contains no faces.");
		}

		int[] indices = new int[faceCount * 3];
		for (int faceIndex = 0; faceIndex < faceCount; faceIndex++) {
			int offset = headerSize + faceIndex * recordSize + firstIndexOffset;
			int indexOffset = faceIndex * 3;
			indices[indexOffset] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, sizeof(ushort)));
			indices[indexOffset + 1] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + sizeof(ushort), sizeof(ushort)));
			indices[indexOffset + 2] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + sizeof(ushort) * 2, sizeof(ushort)));
		}

		return indices;
	}

	private static bool TryGetShieldVertex(ShieldVertex[] vertices, int index, out ShieldVertex vertex) {
		if (index >= 0 && index < vertices.Length) {
			vertex = vertices[index];
			return true;
		}

		vertex = default;
		return false;
	}

	private static Vector2[] ReadVector2Array(byte[]? bytes, bool flipV) {
		if (bytes is null || bytes.Length < sizeof(float) * 2) {
			return [];
		}

		int count = bytes.Length / (sizeof(float) * 2);
		Vector2[] values = new Vector2[count];
		for (int index = 0; index < count; index++) {
			int offset = index * sizeof(float) * 2;
			float u = ReadSingle(bytes, offset);
			float v = ReadSingle(bytes, offset + sizeof(float));
			values[index] = new Vector2(u, flipV ? 1.0f - v : v);
		}

		return values;
	}

	private static int[] ReadInt32Array(byte[]? bytes) {
		if (bytes is null || bytes.Length < sizeof(int)) {
			return [];
		}

		int count = bytes.Length / sizeof(int);
		int[] values = new int[count];
		for (int index = 0; index < count; index++) {
			values[index] = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(index * sizeof(int), sizeof(int)));
		}

		return values;
	}

	private static float[] ReadSingleArray(byte[]? bytes) {
		if (bytes is null || bytes.Length < sizeof(float)) {
			return [];
		}

		int count = bytes.Length / sizeof(float);
		float[] values = new float[count];
		for (int index = 0; index < count; index++) {
			values[index] = ReadSingle(bytes, index * sizeof(float));
		}

		return values;
	}

	private static int? ReadInt32(byte[]? bytes) {
		return bytes is { Length: >= sizeof(int) }
			? BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0, sizeof(int)))
			: null;
	}

	private static uint ReadUInt32(byte[] bytes, int offset) {
		return BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, sizeof(uint)));
	}

	private static string? ReadString(byte[]? bytes) {
		if (bytes is null || bytes.Length == 0) {
			return null;
		}

		int end = 0;
		while (end < bytes.Length && bytes[end] != 0) {
			end++;
		}

		return Encoding.ASCII.GetString(bytes, 0, end);
	}

	private static MeshTextureAddress TextureAddressFromFlags(int textureFlags) {
		int wrapMode = (textureFlags & 0xf0) >> 4;
		int coordinateSet = wrapMode == 5 ? 1 : 0;
		return new MeshTextureAddress(textureFlags & 0x03, (textureFlags & 0x0c) >> 2, coordinateSet);
	}

	private static Color ReadColor255(byte[]? bytes, Color fallback) {
		if (bytes is null) {
			return fallback;
		}

		if (bytes.Length >= sizeof(float) * 3) {
			return new Color(
				(int)ToByte(ReadSingle(bytes, 0)),
				(int)ToByte(ReadSingle(bytes, sizeof(float))),
				(int)ToByte(ReadSingle(bytes, sizeof(float) * 2)),
				255);
		}

		return bytes.Length >= 3
			? new Color((int)bytes[0], (int)bytes[1], (int)bytes[2], 255)
			: fallback;
	}

	private static byte ToByte(float value) {
		return (byte)Math.Clamp((int)MathF.Round(value * 255.0f), 0, 255);
	}

	private static float ReadSingle(byte[] bytes, int offset) {
		int bits = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, sizeof(int)));
		return BitConverter.Int32BitsToSingle(bits);
	}

	private readonly record struct MeshMaterialInfo(
		string? TextureName,
		MeshTextureAddress TextureAddress,
		string? SecondDiffuseTextureName,
		MeshTextureAddress SecondDiffuseTextureAddress,
		string? EmissiveTextureName,
		MeshTextureAddress EmissiveTextureAddress,
		byte EmissiveBlend,
		Color Tint,
		byte MaterialAlpha) {
		public static MeshMaterialInfo Default { get; } = new(
			null,
			MeshTextureAddress.Repeat,
			null,
			MeshTextureAddress.Repeat,
			null,
			MeshTextureAddress.Repeat,
			255,
			Color.White,
			255);
	}

	private readonly record struct CompoundPartInfo(string PartDirectory, string ObjectName, string FileName, int Index);

	private readonly record struct ShieldVertex(Vector3 Position, Vector3 Normal);
}

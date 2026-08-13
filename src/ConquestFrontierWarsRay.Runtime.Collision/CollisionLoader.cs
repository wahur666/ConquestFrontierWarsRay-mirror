using System.Numerics;
using System.Text;
using DACOM;
using DOSFile;
using Math3D;

namespace ConquestFrontierWarsRay.Runtime.Collision;

internal static class CollisionLoader {
	public static ICollisionModel? TryLoad(IFileSystem fileSystem, IDacomRegistry registry) {
		ArgumentNullException.ThrowIfNull(fileSystem);
		ArgumentNullException.ThrowIfNull(registry);

		fileSystem = ResolveContainerRoot(fileSystem, registry);

		if (!TryGetChildDirectory(fileSystem, "Rigid body", registry, out var rigidBody) ||
		    !TryGetChildDirectory(rigidBody, "Extent tree", registry, out var extentTree)) {
			return null;
		}

		var rootEntry = extentTree.FindFiles("*").FirstOrDefault(static entry => entry.IsDirectory);
		if (rootEntry is null) {
			return null;
		}

		var rootDirectory = OpenChild(extentTree, rootEntry.Name, registry);
		var root = LoadExtent(rootDirectory, registry);
		if (root is null) {
			return null;
		}

		var mass = TryRead(rigidBody, @"Mass properties\Mass") is { Length: >= 4 } massBytes
			? BitConverter.ToSingle(massBytes, 0)
			: 0f;
		var centerOfMass = TryRead(rigidBody, @"Mass properties\Center of mass") is { Length: 12 } centerBytes
			? ReadVector3(centerBytes)
			: Vector3.Zero;
		var inertiaTensor = TryRead(rigidBody, @"Mass properties\Inertia tensor") is { Length: 36 } tensorBytes
			? ReadMatrix3(tensorBytes)
			: Matrix3.Identity;
		var (boundingCenter, boundingRadius) = ComputeBounds(root, Transform3.Identity);

		return new CollisionModel(
			fileSystem.FileName,
			root,
			mass,
			centerOfMass,
			inertiaTensor,
			boundingCenter,
			boundingRadius);
	}

	private static BaseExtent? LoadExtent(IFileSystem extentDirectory, IDacomRegistry registry) {
		var currentDirectory = extentDirectory.GetCurrentDirectory().TrimEnd('\\', '/');
		var separator = currentDirectory.LastIndexOfAny(['\\', '/']);
		var typeName = separator >= 0 ? currentDirectory[(separator + 1)..] : currentDirectory;
		var name = TryRead(extentDirectory, "Name") is { Length: > 0 } nameBytes ? ReadAsciiZ(nameBytes) : typeName;
		var transform = TryRead(extentDirectory, "Transform") is { Length: 48 } transformBytes
			? ReadTransform(transformBytes)
			: Transform3.Identity;

		BaseExtent? extent = typeName.StartsWith("Sphere", StringComparison.OrdinalIgnoreCase)
			? new SphereExtent(name, transform, new CollisionSphere(ReadSingle(extentDirectory, "Radius", 0f)))
			: typeName.StartsWith("Box", StringComparison.OrdinalIgnoreCase)
				? new BoxExtent(name, transform, new CollisionBox(
					ReadSingle(extentDirectory, "half x", 0f),
					ReadSingle(extentDirectory, "half y", 0f),
					ReadSingle(extentDirectory, "half z", 0f)))
				: typeName.StartsWith("Cylinder", StringComparison.OrdinalIgnoreCase)
					? new CylinderExtent(name, transform, new CollisionCylinder(
						ReadSingle(extentDirectory, "length", 0f),
						ReadSingle(extentDirectory, "radius", 0f)))
					: typeName.StartsWith("Tube", StringComparison.OrdinalIgnoreCase)
						? new TubeExtent(name, transform, new CollisionTube(
							ReadSingle(extentDirectory, "length", 0f),
							ReadSingle(extentDirectory, "radius", 0f)))
						: typeName.StartsWith("Convex mesh", StringComparison.OrdinalIgnoreCase)
							? new ConvexMeshExtent(name, transform, LoadMesh(extentDirectory))
							: typeName.StartsWith("Mesh", StringComparison.OrdinalIgnoreCase)
								? new MeshExtent(name, transform, LoadMesh(extentDirectory))
								: null;

		if (extent is null) {
			return null;
		}

		if (TryGetChildDirectory(extentDirectory, "Children", registry, out var childrenDirectory)) {
			foreach (var childEntry in childrenDirectory.FindFiles("*").Where(static entry => entry.IsDirectory)) {
				var childDirectory = OpenChild(childrenDirectory, childEntry.Name, registry);
				var child = LoadExtent(childDirectory, registry);
				if (child is not null) {
					extent.MutableChildren.Add(child);
				}
			}
		}

		return extent;
	}

	private static CollisionMesh LoadMesh(IFileSystem meshDirectory) {
		var vertices = ReadVector3Array(meshDirectory.ReadAllBytes("Vertex list"));
		var normals = ReadVector3Array(meshDirectory.ReadAllBytes("Normal list"));
		var triangleD = ReadSingleArray(meshDirectory.ReadAllBytes("Triangle D"));
		var centroid = TryRead(meshDirectory, "Centroid") is { Length: 12 } centroidBytes
			? ReadVector3(centroidBytes)
			: ComputeCentroid(vertices);

		var triangles = ParseTriangles(meshDirectory.ReadAllBytes("Face list"));
		var sphereCenter = vertices.Length == 0 ? centroid : centroid;
		var sphereRadius = vertices.Length == 0 ? 0f : vertices.Max(vertex => Vector3.Distance(vertex, sphereCenter));

		return new CollisionMesh {
			Vertices = vertices,
			Normals = normals,
			Triangles = triangles,
			TriangleD = triangleD,
			Centroid = centroid,
			SphereCenter = sphereCenter,
			SphereRadius = sphereRadius
		};
	}

	private static CollisionTriangle[] ParseTriangles(byte[] bytes) {
		if (bytes.Length == 0) {
			return [];
		}

		const int stride = sizeof(int) * 7;
		var count = bytes.Length / stride;
		var triangles = new CollisionTriangle[count];
		using var reader = new BinaryReader(new MemoryStream(bytes, writable: false));
		for (var index = 0; index < count; index++) {
			var v0 = reader.ReadInt32();
			var v1 = reader.ReadInt32();
			var v2 = reader.ReadInt32();
			_ = reader.ReadInt32();
			_ = reader.ReadInt32();
			_ = reader.ReadInt32();
			var normalIndex = reader.ReadInt32();
			triangles[index] = new CollisionTriangle {
				V0 = v0,
				V1 = v1,
				V2 = v2,
				NormalIndex = normalIndex
			};
		}

		return triangles;
	}

	private static (Vector3 Center, float Radius) ComputeBounds(BaseExtent extent, Transform3 parentTransform) {
		var world = Combine(parentTransform, extent.Transform);
		var bounds = EnumerateSpheres(extent, world).ToArray();
		if (bounds.Length == 0) {
			return (Vector3.Zero, 0f);
		}

		var min = bounds[0].Center - new Vector3(bounds[0].Radius);
		var max = bounds[0].Center + new Vector3(bounds[0].Radius);
		foreach (var bound in bounds[1..]) {
			min = Vector3.Min(min, bound.Center - new Vector3(bound.Radius));
			max = Vector3.Max(max, bound.Center + new Vector3(bound.Radius));
		}

		var center = 0.5f * (min + max);
		var radius = bounds.Max(bound => Vector3.Distance(bound.Center, center) + bound.Radius);
		return (center, radius);
	}

	private static IEnumerable<(Vector3 Center, float Radius)> EnumerateSpheres(BaseExtent extent, Transform3 worldTransform) {
		yield return CollisionMath.ComputeBoundingSphere(extent, worldTransform);
		foreach (var child in extent.Children) {
			foreach (var item in EnumerateSpheres(child, CollisionMath.Combine(worldTransform, extent.Transform))) {
				yield return item;
			}
		}
	}

	private static IFileSystem ResolveContainerRoot(IFileSystem fileSystem, IDacomRegistry registry) {
		if (fileSystem.IsDirectory || fileSystem.ParentSystem is null) {
			return fileSystem;
		}

		try {
			return fileSystem.ParentSystem.CreateInstance(new DAFILEDESC(Path.GetFileName(fileSystem.FileName)) {
				Implementation = "UTF",
				DesiredAccess = fileSystem.Access,
				CreationDisposition = FileMode.Open
			}, registry);
		} catch {
			return fileSystem;
		}
	}

	private static Transform3 Combine(Transform3 left, Transform3 right) =>
		Transform3.FromNumericsMatrix4x4(right.ToNumericsMatrix4x4() * left.ToNumericsMatrix4x4());

	private static IFileSystem OpenChild(IFileSystem parent, string name, IDacomRegistry registry) =>
		parent.CreateInstance(new DAFILEDESC(name), registry);

	private static bool TryGetChildDirectory(IFileSystem parent, string name, IDacomRegistry registry, out IFileSystem directory) {
		if (parent.TryGetEntry(name, out var entry) && entry.IsDirectory) {
			directory = OpenChild(parent, name, registry);
			return true;
		}

		directory = null!;
		return false;
	}

	private static byte[]? TryRead(IFileSystem fileSystem, string path) {
		try {
			return fileSystem.ReadAllBytes(path);
		} catch {
			return null;
		}
	}

	private static float ReadSingle(IFileSystem fileSystem, string path, float fallback) =>
		TryRead(fileSystem, path) is { Length: >= 4 } bytes ? BitConverter.ToSingle(bytes, 0) : fallback;

	private static float[] ReadSingleArray(byte[] bytes) {
		var values = new float[bytes.Length / sizeof(float)];
		Buffer.BlockCopy(bytes, 0, values, 0, values.Length * sizeof(float));
		return values;
	}

	private static Vector3[] ReadVector3Array(byte[] bytes) {
		var count = bytes.Length / (sizeof(float) * 3);
		var values = new Vector3[count];
		using var reader = new BinaryReader(new MemoryStream(bytes, writable: false));
		for (var index = 0; index < count; index++) {
			values[index] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		return values;
	}

	private static Vector3 ReadVector3(byte[] bytes) {
		using var reader = new BinaryReader(new MemoryStream(bytes, writable: false));
		return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	}

	private static Matrix3 ReadMatrix3(byte[] bytes) {
		using var reader = new BinaryReader(new MemoryStream(bytes, writable: false));
		return new Matrix3(
			reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
			reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
			reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	}

	private static Transform3 ReadTransform(byte[] bytes) {
		using var reader = new BinaryReader(new MemoryStream(bytes, writable: false));
		var orientation = new Matrix3(
			reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
			reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
			reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		var translation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		return new Transform3(orientation, translation);
	}

	private static string ReadAsciiZ(byte[] bytes) {
		var length = Array.IndexOf(bytes, (byte)0);
		if (length < 0) {
			length = bytes.Length;
		}

		return Encoding.ASCII.GetString(bytes, 0, length);
	}

	private static Vector3 ComputeCentroid(Vector3[] vertices) =>
		vertices.Length == 0 ? Vector3.Zero : vertices.Aggregate(Vector3.Zero, static (sum, value) => sum + value) / vertices.Length;

	private sealed record CollisionModel(
		string SourceName,
		BaseExtent RootExtent,
		float Mass,
		Vector3 CenterOfMass,
		Matrix3 InertiaTensor,
		Vector3 BoundingCenter,
		float BoundingRadius) : ICollisionModel;
}

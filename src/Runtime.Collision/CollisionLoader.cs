using System.Numerics;
using System.Text;
using ConquestFrontierWarsRay.Data.DosFile;
using Math3D;

namespace ConquestFrontierWarsRay.Runtime.Collision;

internal static class CollisionLoader {
	public static ICollisionModel? TryLoad(string sourcePath) => TryLoad(new DosFileReader(sourcePath));

	public static ICollisionModel? TryLoad(DosFileReader reader) {
		ArgumentNullException.ThrowIfNull(reader);

		const string rigidBodyPath = "Rigid body";
		const string extentTreePath = @"Rigid body\Extent tree";
		if (!TryGetFirstChildDirectory(reader, extentTreePath, out var rootDirectoryName)) {
			return null;
		}

		var root = LoadExtent(reader, Path.Combine(extentTreePath, rootDirectoryName));
		if (root is null) {
			return null;
		}

		var mass = TryRead(reader, Path.Combine(rigidBodyPath, @"Mass properties\Mass")) is { Length: >= 4 } massBytes
			? BitConverter.ToSingle(massBytes, 0)
			: 0f;
		var centerOfMass = TryRead(reader, Path.Combine(rigidBodyPath, @"Mass properties\Center of mass")) is { Length: 12 } centerBytes
			? ReadVector3(centerBytes)
			: Vector3.Zero;
		var inertiaTensor = TryRead(reader, Path.Combine(rigidBodyPath, @"Mass properties\Inertia tensor")) is { Length: 36 } tensorBytes
			? ReadMatrix3(tensorBytes)
			: Matrix3.Identity;
		var (boundingCenter, boundingRadius) = ComputeBounds(root, Transform3.Identity);

		return new CollisionModel(
			reader.SourcePath,
			root,
			mass,
			centerOfMass,
			inertiaTensor,
			boundingCenter,
			boundingRadius);
	}

	private static BaseExtent? LoadExtent(DosFileReader reader, string extentPath) {
		var typeName = Path.GetFileName(extentPath.TrimEnd('\\', '/'));
		var name = TryRead(reader, Path.Combine(extentPath, "Name")) is { Length: > 0 } nameBytes ? ReadAsciiZ(nameBytes) : typeName;
		var transform = TryRead(reader, Path.Combine(extentPath, "Transform")) is { Length: 48 } transformBytes
			? ReadTransform(transformBytes)
			: Transform3.Identity;

		BaseExtent? extent = typeName.StartsWith("Sphere", StringComparison.OrdinalIgnoreCase)
			? new SphereExtent(name, transform, new CollisionSphere(ReadSingle(reader, Path.Combine(extentPath, "Radius"), 0f)))
			: typeName.StartsWith("Box", StringComparison.OrdinalIgnoreCase)
				? new BoxExtent(name, transform, new CollisionBox(
					ReadSingle(reader, Path.Combine(extentPath, "half x"), 0f),
					ReadSingle(reader, Path.Combine(extentPath, "half y"), 0f),
					ReadSingle(reader, Path.Combine(extentPath, "half z"), 0f)))
				: typeName.StartsWith("Cylinder", StringComparison.OrdinalIgnoreCase)
					? new CylinderExtent(name, transform, new CollisionCylinder(
						ReadSingle(reader, Path.Combine(extentPath, "length"), 0f),
						ReadSingle(reader, Path.Combine(extentPath, "radius"), 0f)))
					: typeName.StartsWith("Tube", StringComparison.OrdinalIgnoreCase)
						? new TubeExtent(name, transform, new CollisionTube(
							ReadSingle(reader, Path.Combine(extentPath, "length"), 0f),
							ReadSingle(reader, Path.Combine(extentPath, "radius"), 0f)))
						: typeName.StartsWith("Convex mesh", StringComparison.OrdinalIgnoreCase)
							? new ConvexMeshExtent(name, transform, LoadMesh(reader, extentPath))
							: typeName.StartsWith("Mesh", StringComparison.OrdinalIgnoreCase)
								? new MeshExtent(name, transform, LoadMesh(reader, extentPath))
								: null;

		if (extent is null) {
			return null;
		}

		var childrenPath = Path.Combine(extentPath, "Children");
		foreach (var childEntry in TryEnumerateDirectories(reader, childrenPath)) {
			var child = LoadExtent(reader, Path.Combine(childrenPath, childEntry.Name));
			if (child is not null) {
				extent.MutableChildren.Add(child);
			}
		}

		return extent;
	}

	private static CollisionMesh LoadMesh(DosFileReader reader, string meshPath) {
		var vertices = ReadVector3Array(reader.ReadAllBytes(Path.Combine(meshPath, "Vertex list")));
		var normals = ReadVector3Array(reader.ReadAllBytes(Path.Combine(meshPath, "Normal list")));
		var triangleD = ReadSingleArray(reader.ReadAllBytes(Path.Combine(meshPath, "Triangle D")));
		var centroid = TryRead(reader, Path.Combine(meshPath, "Centroid")) is { Length: 12 } centroidBytes
			? ReadVector3(centroidBytes)
			: ComputeCentroid(vertices);

		var triangles = ParseTriangles(reader.ReadAllBytes(Path.Combine(meshPath, "Face list")));
		var sphereCenter = centroid;
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

	private static (Vector3 Center, float Radius) ComputeBounds(BaseExtent extent, Transform3 worldTransform) {
		var bounds = EnumerateSpheres(extent, worldTransform).ToArray();
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

	private static IEnumerable<(Vector3 Center, float Radius)> EnumerateSpheres(BaseExtent extent, Transform3 parentTransform) {
		var world = CollisionMath.Combine(parentTransform, extent.Transform);
		yield return CollisionMath.ComputeBoundingSphere(extent, world);
		foreach (var child in extent.Children) {
			foreach (var item in EnumerateSpheres(child, world)) {
				yield return item;
			}
		}
	}

	private static bool TryGetFirstChildDirectory(DosFileReader reader, string path, out string name) {
		name = TryEnumerateDirectories(reader, path).Select(static entry => entry.Name).FirstOrDefault() ?? string.Empty;
		return name.Length > 0;
	}

	private static IEnumerable<FileSystemEntry> TryEnumerateDirectories(DosFileReader reader, string path) {
		try {
			return reader.FindFiles(path).Where(static entry => entry.IsDirectory);
		} catch {
			return [];
		}
	}

	private static byte[]? TryRead(DosFileReader reader, string path) {
		try {
			return reader.ReadAllBytes(path);
		} catch {
			return null;
		}
	}

	private static float ReadSingle(DosFileReader reader, string path, float fallback) =>
		TryRead(reader, path) is { Length: >= 4 } bytes ? BitConverter.ToSingle(bytes, 0) : fallback;

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

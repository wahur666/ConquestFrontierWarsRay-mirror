using System.Numerics;
using ConquestSharp.Engine;
using ConquestSharp.SystemLayer;
using Math3D;

namespace ConquestFrontierWarsRay.Runtime.Collision;

public static class CollisionIdentifiers {
	public const string ComponentName = "Collision";
	public const string InterfaceName = "ICollision";
	public const string ModelInterfaceName = "ICollisionModel";
}

public enum ExtentType {
	LineSegment,
	InfinitePlane,
	Sphere,
	Cylinder,
	Box,
	ConvexMesh,
	GeneralMesh,
	Tube,
	None
}

public sealed class CollisionData {
	public BaseExtent? E1 { get; set; }

	public BaseExtent? E2 { get; set; }

	public float Coeff { get; set; } = 0.5f;

	public float Mu { get; set; } = 0.5f;

	public Vector3 Contact { get; set; }

	public Vector3 Normal { get; set; }
}

public sealed class CollisionStats {
	public int SphereSphere { get; set; }
	public int SphereBox { get; set; }
	public int SphereTube { get; set; }
	public int SphereCylinder { get; set; }
	public int SphereConvexMesh { get; set; }
	public int BoxBox { get; set; }
	public int BoxTube { get; set; }
	public int BoxCylinder { get; set; }
	public int BoxConvexMesh { get; set; }
	public int TubeTube { get; set; }
	public int TubeCylinder { get; set; }
	public int TubeConvexMesh { get; set; }
	public int CylinderCylinder { get; set; }
	public int CylinderConvexMesh { get; set; }
	public int ConvexMeshConvexMesh { get; set; }

	public CollisionStats Clone() => (CollisionStats)MemberwiseClone();
}

public readonly record struct CollisionSphere(float Radius);

public readonly record struct CollisionCylinder(float Length, float Radius);

public readonly record struct CollisionTube(float Length, float Radius);

public readonly record struct CollisionBox(float HalfX, float HalfY, float HalfZ);

public sealed class CollisionTriangle {
	public required int V0 { get; init; }
	public required int V1 { get; init; }
	public required int V2 { get; init; }
	public required int NormalIndex { get; init; }
}

public sealed class CollisionMesh {
	public required Vector3[] Vertices { get; init; }
	public required Vector3[] Normals { get; init; }
	public required CollisionTriangle[] Triangles { get; init; }
	public required float[] TriangleD { get; init; }
	public required Vector3 Centroid { get; init; }
	public required Vector3 SphereCenter { get; init; }
	public required float SphereRadius { get; init; }
}

public abstract class BaseExtent {
	private readonly List<BaseExtent> _children = [];

	protected BaseExtent(string? name, ExtentType type, Transform3 transform) {
		Name = name;
		Type = type;
		Transform = transform;
	}

	public string? Name { get; }

	public ExtentType Type { get; }

	public Transform3 Transform { get; }

	public IReadOnlyList<BaseExtent> Children => _children;

	public bool IsLeaf => _children.Count == 0;

	internal List<BaseExtent> MutableChildren => _children;
}

public sealed class SphereExtent(string? name, Transform3 transform, CollisionSphere sphere)
	: BaseExtent(name, ExtentType.Sphere, transform) {
	public CollisionSphere Sphere { get; } = sphere;
}

public sealed class CylinderExtent(string? name, Transform3 transform, CollisionCylinder cylinder)
	: BaseExtent(name, ExtentType.Cylinder, transform) {
	public CollisionCylinder Cylinder { get; } = cylinder;
}

public sealed class TubeExtent(string? name, Transform3 transform, CollisionTube tube)
	: BaseExtent(name, ExtentType.Tube, transform) {
	public CollisionTube Tube { get; } = tube;
}

public sealed class BoxExtent(string? name, Transform3 transform, CollisionBox box)
	: BaseExtent(name, ExtentType.Box, transform) {
	public CollisionBox Box { get; } = box;
}

public sealed class ConvexMeshExtent(string? name, Transform3 transform, CollisionMesh mesh)
	: BaseExtent(name, ExtentType.ConvexMesh, transform) {
	public CollisionMesh Mesh { get; } = mesh;
}

public sealed class MeshExtent(string? name, Transform3 transform, CollisionMesh mesh)
	: BaseExtent(name, ExtentType.GeneralMesh, transform) {
	public CollisionMesh Mesh { get; } = mesh;
}

public interface ICollisionModel {
	string SourceName { get; }

	BaseExtent RootExtent { get; }

	float Mass { get; }

	Vector3 CenterOfMass { get; }

	Matrix3 InertiaTensor { get; }

	Vector3 BoundingCenter { get; }

	float BoundingRadius { get; }
}

public interface ICollision : IAggregateComponent {
	bool IntersectRayWithExtent(
		out Vector3 pointOfIntersection,
		out Vector3 normal,
		Vector3 rayOrigin,
		Vector3 rayDirection,
		BaseExtent extent,
		Transform3 objectCenterOfMassFrame);

	bool IntersectRayWithExtentHierarchy(
		out Vector3 pointOfIntersection,
		out Vector3 normal,
		Vector3 rayOrigin,
		Vector3 rayDirection,
		BaseExtent rootExtent,
		Transform3 objectCenterOfMassFrame,
		bool findClosest = false);

	bool CollideExtents(
		BaseExtent root1,
		Transform3 transform1,
		BaseExtent root2,
		Transform3 transform2,
		float epsilon = 1e-3f);

	bool CollideExtentHierarchies(
		out BaseExtent? intersect1,
		out BaseExtent? intersect2,
		BaseExtent root1,
		Transform3 transform1,
		BaseExtent root2,
		Transform3 transform2,
		float epsilon = 1e-3f);

	void ComputeContact(
		CollisionData data,
		BaseExtent extent1,
		Transform3 transform1,
		BaseExtent extent2,
		Transform3 transform2);

	bool CollideExtents(
		CollisionData data,
		BaseExtent extent1,
		Transform3 transform1,
		BaseExtent extent2,
		Transform3 transform2);

	bool CollideExtentHierarchies(
		CollisionData data,
		BaseExtent root1,
		Transform3 transform1,
		BaseExtent root2,
		Transform3 transform2);

	CollisionStats GetCollisionStats();
}

public interface ICollisionComponent : ICollision, IEngineComponent {
}

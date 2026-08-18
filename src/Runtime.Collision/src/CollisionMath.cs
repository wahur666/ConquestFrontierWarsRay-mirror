using System.Numerics;
using Math3D;

namespace ConquestFrontierWarsRay.Runtime.Collision;

internal static class CollisionMath {
	public static (Vector3 Center, float Radius) ComputeBoundingSphere(BaseExtent extent, Transform3 objectTransform) {
		var world = Combine(objectTransform, extent.Transform);
		return extent switch {
			SphereExtent sphere => (world.Translation, sphere.Sphere.Radius),
			BoxExtent box => (world.Translation, MathF.Sqrt(
				(box.Box.HalfX * box.Box.HalfX) +
				(box.Box.HalfY * box.Box.HalfY) +
				(box.Box.HalfZ * box.Box.HalfZ))),
			CylinderExtent cylinder => (world.Translation, MathF.Sqrt(
				(cylinder.Cylinder.Radius * cylinder.Cylinder.Radius) +
				(0.5f * cylinder.Cylinder.Length * 0.5f * cylinder.Cylinder.Length))),
			TubeExtent tube => (world.Translation, tube.Tube.Radius + (0.5f * tube.Tube.Length)),
			ConvexMeshExtent mesh => (world.Transform(mesh.Mesh.SphereCenter), mesh.Mesh.SphereRadius),
			MeshExtent mesh => (world.Transform(mesh.Mesh.SphereCenter), mesh.Mesh.SphereRadius),
			_ => (world.Translation, 0f)
		};
	}

	public static Transform3 Combine(Transform3 left, Transform3 right) =>
		Transform3.FromNumericsMatrix4x4(right.ToNumericsMatrix4x4() * left.ToNumericsMatrix4x4());

	public static Vector3 ClosestPointOnBox(Vector3 point, CollisionBox box) => new(
		Math.Clamp(point.X, -box.HalfX, box.HalfX),
		Math.Clamp(point.Y, -box.HalfY, box.HalfY),
		Math.Clamp(point.Z, -box.HalfZ, box.HalfZ));

	public static Vector3 WorldToLocalPoint(Transform3 transform, Vector3 point) => transform.InverseTransform(point);

	public static Vector3 LocalToWorldPoint(Transform3 transform, Vector3 point) => transform.Transform(point);

	public static Vector3 LocalToWorldDirection(Transform3 transform, Vector3 direction) => transform.Rotate(direction);

	public static Vector3 SafeNormal(Vector3 value, Vector3 fallback) =>
		value.LengthSquared() > 1e-12f ? Vector3.Normalize(value) : fallback;
}

using System.Numerics;
using DACOM;
using DOSFile;
using ConquestSharp.Engine;
using ConquestSharp.SystemLayer;
using Math3D;

namespace ConquestFrontierWarsRay.Runtime.Collision;

internal sealed class CollisionFactory : IDacomFactory {
	public string InterfaceName => CollisionIdentifiers.ComponentName;

	public object CreateInstance(DacomDesc descriptor, IDacomRegistry registry) {
		ArgumentNullException.ThrowIfNull(registry);

		return descriptor switch {
			AggDesc aggregate => new CollisionService(aggregate, registry),
			_ => new CollisionService(new AggDesc(CollisionIdentifiers.ComponentName), registry)
		};
	}
}

public static class CollisionRuntime {
	public static void Register(IDacomRegistry registry) {
		ArgumentNullException.ThrowIfNull(registry);
		registry.RegisterComponent(new CollisionFactory(), DacomPriority.Normal);
	}
}

internal sealed class CollisionService : DacomComponent, ICollisionComponent {
	private readonly AggDesc _descriptor;
	private readonly IDacomRegistry _registry;
	private readonly Dictionary<int, ICollisionModel> _archetypes = [];
	private readonly Dictionary<int, CollisionInstanceRecord> _instances = [];
	private readonly CollisionStats _stats = new();
	private IEngine? _engine;

	public CollisionService(AggDesc descriptor, IDacomRegistry registry) {
		_descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
		_registry = registry ?? throw new ArgumentNullException(nameof(registry));

		RegisterInterface(CollisionIdentifiers.ComponentName, this);
		RegisterInterface(CollisionIdentifiers.InterfaceName, this);
		RegisterInterface(EngineIdentifiers.EngineComponentInterfaceName, this);
		RegisterInterface("IAggregateComponent", this);
	}

	public bool Initialize() {
		if (_descriptor.Properties.TryGetValue("Engine", out var engineObject) && engineObject is IEngine engine) {
			_engine = engine;
		}

		return true;
	}

	public bool CreateArchetype(int archetypeIndex, IFileSystem fileSystem) {
		ArgumentNullException.ThrowIfNull(fileSystem);
		if (_archetypes.ContainsKey(archetypeIndex)) {
			return false;
		}

		var model = CollisionLoader.TryLoad(fileSystem, _registry);
		if (model is null) {
			return false;
		}

		_archetypes[archetypeIndex] = model;
		return true;
	}

	public void DuplicateArchetype(int newArchetypeIndex, int oldArchetypeIndex) {
		if (_archetypes.ContainsKey(newArchetypeIndex) || !_archetypes.TryGetValue(oldArchetypeIndex, out var model)) {
			return;
		}

		_archetypes[newArchetypeIndex] = model;
	}

	public void DestroyArchetype(int archetypeIndex) {
		_archetypes.Remove(archetypeIndex);
	}

	public bool TryQueryArchetypeInterface(int archetypeIndex, string interfaceName, out object? implementation) {
		implementation = null;
		return string.Equals(interfaceName, CollisionIdentifiers.ModelInterfaceName, StringComparison.Ordinal) &&
		       _archetypes.TryGetValue(archetypeIndex, out var model) &&
		       (implementation = model) is not null;
	}

	public bool CreateInstance(int instanceIndex, int archetypeIndex) {
		if (_instances.ContainsKey(instanceIndex) || !_archetypes.TryGetValue(archetypeIndex, out var model)) {
			return false;
		}

		_instances[instanceIndex] = new CollisionInstanceRecord(model, archetypeIndex);
		_engine?.SetInstanceBoundingSphere(instanceIndex, EngineFlags.DontRecurse, model.BoundingRadius, model.BoundingCenter);
		return true;
	}

	public void DestroyInstance(int instanceIndex) {
		_instances.Remove(instanceIndex);
	}

	public void UpdateInstance(int instanceIndex, float dt) {
	}

	public VisState RenderInstance(object camera, int instanceIndex, float lodFraction, RenderFlags flags, Transform3? modifierTransform) =>
		VisState.Unknown;

	public bool TryQueryInstanceInterface(int instanceIndex, string interfaceName, out object? implementation) {
		implementation = null;
		return string.Equals(interfaceName, CollisionIdentifiers.ModelInterfaceName, StringComparison.Ordinal) &&
		       _instances.TryGetValue(instanceIndex, out var record) &&
		       (implementation = record.Model) is not null;
	}

	public void Update(float dt) {
	}

	public bool IntersectRayWithExtent(
		out Vector3 pointOfIntersection,
		out Vector3 normal,
		Vector3 rayOrigin,
		Vector3 rayDirection,
		BaseExtent extent,
		Transform3 objectCenterOfMassFrame) =>
		TryIntersectExtent(extent, objectCenterOfMassFrame, rayOrigin, rayDirection, out _, out pointOfIntersection, out normal);

	public bool IntersectRayWithExtentHierarchy(
		out Vector3 pointOfIntersection,
		out Vector3 normal,
		Vector3 rayOrigin,
		Vector3 rayDirection,
		BaseExtent rootExtent,
		Transform3 objectCenterOfMassFrame,
		bool findClosest = false) {
		pointOfIntersection = Vector3.Zero;
		normal = Vector3.Zero;
		var hit = false;
		var bestT = float.MaxValue;

		foreach (var extent in EnumerateDepthFirst(rootExtent, objectCenterOfMassFrame)) {
			if (!TryIntersectExtent(extent.Extent, extent.Transform, rayOrigin, rayDirection, out var t, out var point, out var hitNormal)) {
				continue;
			}

			if (!findClosest) {
				pointOfIntersection = point;
				normal = hitNormal;
				return true;
			}

			if (t < bestT) {
				bestT = t;
				pointOfIntersection = point;
				normal = hitNormal;
				hit = true;
			}
		}

		return hit;
	}

	public bool CollideExtents(BaseExtent root1, Transform3 transform1, BaseExtent root2, Transform3 transform2, float epsilon = 1e-3f) =>
		CollideExtentHierarchies(out _, out _, root1, transform1, root2, transform2, epsilon);

	public bool CollideExtentHierarchies(
		out BaseExtent? intersect1,
		out BaseExtent? intersect2,
		BaseExtent root1,
		Transform3 transform1,
		BaseExtent root2,
		Transform3 transform2,
		float epsilon = 1e-3f) {
		foreach (var left in EnumerateDepthFirst(root1, transform1)) {
			foreach (var right in EnumerateDepthFirst(root2, transform2)) {
				if (!TryCollideLeafExtents(left.Extent, left.Transform, right.Extent, right.Transform, epsilon, null)) {
					continue;
				}

				intersect1 = left.Extent;
				intersect2 = right.Extent;
				return true;
			}
		}

		intersect1 = null;
		intersect2 = null;
		return false;
	}

	public void ComputeContact(CollisionData data, BaseExtent extent1, Transform3 transform1, BaseExtent extent2, Transform3 transform2) {
		ArgumentNullException.ThrowIfNull(data);
		TryCollideLeafExtents(extent1, transform1, extent2, transform2, 1e-3f, data, computeOnlyContact: true);
	}

	public bool CollideExtents(CollisionData data, BaseExtent extent1, Transform3 transform1, BaseExtent extent2, Transform3 transform2) {
		ArgumentNullException.ThrowIfNull(data);
		return TryCollideLeafExtents(extent1, transform1, extent2, transform2, 1e-3f, data);
	}

	public bool CollideExtentHierarchies(CollisionData data, BaseExtent root1, Transform3 transform1, BaseExtent root2, Transform3 transform2) {
		ArgumentNullException.ThrowIfNull(data);
		foreach (var left in EnumerateDepthFirst(root1, transform1)) {
			foreach (var right in EnumerateDepthFirst(root2, transform2)) {
				if (!TryCollideLeafExtents(left.Extent, left.Transform, right.Extent, right.Transform, 1e-3f, data)) {
					continue;
				}

				return true;
			}
		}

		return false;
	}

	public CollisionStats GetCollisionStats() => _stats.Clone();

	private static IEnumerable<(BaseExtent Extent, Transform3 Transform)> EnumerateDepthFirst(BaseExtent root, Transform3 rootTransform) {
		var world = CollisionMath.Combine(rootTransform, root.Transform);
		yield return (root, world);
		foreach (var child in root.Children) {
			foreach (var item in EnumerateDepthFirst(child, world)) {
				yield return item;
			}
		}
	}

	private bool TryCollideLeafExtents(
		BaseExtent extent1,
		Transform3 transform1,
		BaseExtent extent2,
		Transform3 transform2,
		float epsilon,
		CollisionData? data,
		bool computeOnlyContact = false) {
		var sphere1 = CollisionMath.ComputeBoundingSphere(extent1, transform1);
		var sphere2 = CollisionMath.ComputeBoundingSphere(extent2, transform2);

		if (extent1 is SphereExtent leftSphere && extent2 is SphereExtent rightSphere) {
			_stats.SphereSphere++;
			return SolveSphereSphere(leftSphere, transform1, rightSphere, transform2, epsilon, data, computeOnlyContact);
		}

		if (extent1 is SphereExtent sphere && extent2 is BoxExtent box) {
			_stats.SphereBox++;
			return SolveSphereBox(sphere, transform1, box, transform2, epsilon, data, computeOnlyContact);
		}

		if (extent1 is BoxExtent box1 && extent2 is SphereExtent sphere2Extent) {
			_stats.SphereBox++;
			var hit = SolveSphereBox(sphere2Extent, transform2, box1, transform1, epsilon, data, computeOnlyContact);
			if (hit && data is not null) {
				data.Normal = -data.Normal;
				(data.E1, data.E2) = (data.E2, data.E1);
			}

			return hit;
		}

		if (extent1 is BoxExtent leftBox && extent2 is BoxExtent rightBox) {
			_stats.BoxBox++;
			return SolveBoxBox(leftBox, transform1, rightBox, transform2, epsilon, data, computeOnlyContact);
		}

		var delta = sphere1.Center - sphere2.Center;
		var distance = delta.Length();
		var hitSphere = computeOnlyContact || distance <= (sphere1.Radius + sphere2.Radius + epsilon);
		if (!hitSphere) {
			return false;
		}

		if (data is not null) {
			var normal = CollisionMath.SafeNormal(delta, Vector3.UnitZ);
			data.E1 = extent1;
			data.E2 = extent2;
			data.Normal = normal;
			data.Contact = sphere2.Center + (normal * sphere2.Radius);
		}

		IncrementGenericStats(extent1, extent2);
		return true;
	}

	private void IncrementGenericStats(BaseExtent left, BaseExtent right) {
		var pair = (left.Type, right.Type);
		switch (pair) {
			case (ExtentType.Sphere, ExtentType.Cylinder):
			case (ExtentType.Cylinder, ExtentType.Sphere):
				_stats.SphereCylinder++;
				break;
			case (ExtentType.Sphere, ExtentType.Tube):
			case (ExtentType.Tube, ExtentType.Sphere):
				_stats.SphereTube++;
				break;
			case (ExtentType.Sphere, ExtentType.ConvexMesh):
			case (ExtentType.ConvexMesh, ExtentType.Sphere):
				_stats.SphereConvexMesh++;
				break;
			case (ExtentType.Box, ExtentType.Cylinder):
			case (ExtentType.Cylinder, ExtentType.Box):
				_stats.BoxCylinder++;
				break;
			case (ExtentType.Box, ExtentType.Tube):
			case (ExtentType.Tube, ExtentType.Box):
				_stats.BoxTube++;
				break;
			case (ExtentType.Box, ExtentType.ConvexMesh):
			case (ExtentType.ConvexMesh, ExtentType.Box):
				_stats.BoxConvexMesh++;
				break;
			case (ExtentType.Tube, ExtentType.Tube):
				_stats.TubeTube++;
				break;
			case (ExtentType.Tube, ExtentType.Cylinder):
			case (ExtentType.Cylinder, ExtentType.Tube):
				_stats.TubeCylinder++;
				break;
			case (ExtentType.Tube, ExtentType.ConvexMesh):
			case (ExtentType.ConvexMesh, ExtentType.Tube):
				_stats.TubeConvexMesh++;
				break;
			case (ExtentType.Cylinder, ExtentType.Cylinder):
				_stats.CylinderCylinder++;
				break;
			case (ExtentType.Cylinder, ExtentType.ConvexMesh):
			case (ExtentType.ConvexMesh, ExtentType.Cylinder):
				_stats.CylinderConvexMesh++;
				break;
			case (ExtentType.ConvexMesh, ExtentType.ConvexMesh):
				_stats.ConvexMeshConvexMesh++;
				break;
		}
	}

	private static bool SolveSphereSphere(
		SphereExtent left,
		Transform3 leftTransform,
		SphereExtent right,
		Transform3 rightTransform,
		float epsilon,
		CollisionData? data,
		bool computeOnlyContact) {
		var leftCenter = leftTransform.Translation;
		var rightCenter = rightTransform.Translation;
		var delta = leftCenter - rightCenter;
		var distance = delta.Length();
		var hit = computeOnlyContact || distance <= (left.Sphere.Radius + right.Sphere.Radius + epsilon);
		if (!hit) {
			return false;
		}

		if (data is not null) {
			var normal = CollisionMath.SafeNormal(delta, Vector3.UnitZ);
			data.E1 = left;
			data.E2 = right;
			data.Normal = normal;
			data.Contact = rightCenter + (normal * right.Sphere.Radius);
		}

		return true;
	}

	private static bool SolveSphereBox(
		SphereExtent sphereExtent,
		Transform3 sphereTransform,
		BoxExtent boxExtent,
		Transform3 boxTransform,
		float epsilon,
		CollisionData? data,
		bool computeOnlyContact) {
		var localSphereCenter = CollisionMath.WorldToLocalPoint(boxTransform, sphereTransform.Translation);
		var closestLocal = CollisionMath.ClosestPointOnBox(localSphereCenter, boxExtent.Box);
		var closestWorld = CollisionMath.LocalToWorldPoint(boxTransform, closestLocal);
		var delta = sphereTransform.Translation - closestWorld;
		var distance = delta.Length();
		var hit = computeOnlyContact || distance <= (sphereExtent.Sphere.Radius + epsilon);
		if (!hit) {
			return false;
		}

		if (data is not null) {
			var normal = CollisionMath.SafeNormal(delta, CollisionMath.LocalToWorldDirection(boxTransform, Vector3.UnitZ));
			data.E1 = sphereExtent;
			data.E2 = boxExtent;
			data.Normal = normal;
			data.Contact = closestWorld;
		}

		return true;
	}

	private static bool SolveBoxBox(
		BoxExtent left,
		Transform3 leftTransform,
		BoxExtent right,
		Transform3 rightTransform,
		float epsilon,
		CollisionData? data,
		bool computeOnlyContact) {
		var (leftMin, leftMax) = ComputeWorldAabb(left.Box, leftTransform);
		var (rightMin, rightMax) = ComputeWorldAabb(right.Box, rightTransform);
		var overlapX = MathF.Min(leftMax.X, rightMax.X) - MathF.Max(leftMin.X, rightMin.X);
		var overlapY = MathF.Min(leftMax.Y, rightMax.Y) - MathF.Max(leftMin.Y, rightMin.Y);
		var overlapZ = MathF.Min(leftMax.Z, rightMax.Z) - MathF.Max(leftMin.Z, rightMin.Z);
		var hit = computeOnlyContact || (overlapX >= -epsilon && overlapY >= -epsilon && overlapZ >= -epsilon);
		if (!hit) {
			return false;
		}

		if (data is not null) {
			var centerDelta = leftTransform.Translation - rightTransform.Translation;
			var normal = MathF.Abs(overlapX) <= MathF.Abs(overlapY) && MathF.Abs(overlapX) <= MathF.Abs(overlapZ)
				? new Vector3(MathF.Sign(centerDelta.X == 0f ? 1f : centerDelta.X), 0f, 0f)
				: MathF.Abs(overlapY) <= MathF.Abs(overlapZ)
					? new Vector3(0f, MathF.Sign(centerDelta.Y == 0f ? 1f : centerDelta.Y), 0f)
					: new Vector3(0f, 0f, MathF.Sign(centerDelta.Z == 0f ? 1f : centerDelta.Z));
			data.E1 = left;
			data.E2 = right;
			data.Normal = normal;
			data.Contact = 0.5f * (
				new Vector3(
					Math.Clamp(rightTransform.Translation.X, leftMin.X, leftMax.X),
					Math.Clamp(rightTransform.Translation.Y, leftMin.Y, leftMax.Y),
					Math.Clamp(rightTransform.Translation.Z, leftMin.Z, leftMax.Z)) +
				new Vector3(
					Math.Clamp(leftTransform.Translation.X, rightMin.X, rightMax.X),
					Math.Clamp(leftTransform.Translation.Y, rightMin.Y, rightMax.Y),
					Math.Clamp(leftTransform.Translation.Z, rightMin.Z, rightMax.Z)));
		}

		return true;
	}

	private static (Vector3 Min, Vector3 Max) ComputeWorldAabb(CollisionBox box, Transform3 transform) {
		var corners = new[] {
			new Vector3(-box.HalfX, -box.HalfY, -box.HalfZ),
			new Vector3(-box.HalfX, -box.HalfY, box.HalfZ),
			new Vector3(-box.HalfX, box.HalfY, -box.HalfZ),
			new Vector3(-box.HalfX, box.HalfY, box.HalfZ),
			new Vector3(box.HalfX, -box.HalfY, -box.HalfZ),
			new Vector3(box.HalfX, -box.HalfY, box.HalfZ),
			new Vector3(box.HalfX, box.HalfY, -box.HalfZ),
			new Vector3(box.HalfX, box.HalfY, box.HalfZ)
		};

		var min = transform.Transform(corners[0]);
		var max = min;
		foreach (var corner in corners[1..]) {
			var world = transform.Transform(corner);
			min = Vector3.Min(min, world);
			max = Vector3.Max(max, world);
		}

		return (min, max);
	}

	private static bool TryIntersectExtent(
		BaseExtent extent,
		Transform3 worldTransform,
		Vector3 rayOrigin,
		Vector3 rayDirection,
		out float t,
		out Vector3 point,
		out Vector3 normal) {
		t = float.MaxValue;
		point = Vector3.Zero;
		normal = Vector3.Zero;

		switch (extent) {
			case SphereExtent sphere:
				return TryRaySphere(sphere, worldTransform, rayOrigin, rayDirection, out t, out point, out normal);
			case BoxExtent box:
				return TryRayBox(box, worldTransform, rayOrigin, rayDirection, out t, out point, out normal);
			case ConvexMeshExtent convexMesh:
				return TryRayMesh(convexMesh.Mesh, worldTransform, rayOrigin, rayDirection, out t, out point, out normal);
			case MeshExtent mesh:
				return TryRayMesh(mesh.Mesh, worldTransform, rayOrigin, rayDirection, out t, out point, out normal);
			case CylinderExtent cylinder:
				return TryRayBoundingSphere(cylinder, worldTransform, rayOrigin, rayDirection, out t, out point, out normal);
			case TubeExtent tube:
				return TryRayBoundingSphere(tube, worldTransform, rayOrigin, rayDirection, out t, out point, out normal);
			default:
				return false;
		}
	}

	private static bool TryRayBoundingSphere(BaseExtent extent, Transform3 transform, Vector3 rayOrigin, Vector3 rayDirection, out float t, out Vector3 point, out Vector3 normal) {
		var (center, radius) = CollisionMath.ComputeBoundingSphere(extent, transform);
		return TryRaySphere(center, radius, rayOrigin, rayDirection, out t, out point, out normal);
	}

	private static bool TryRaySphere(SphereExtent sphereExtent, Transform3 worldTransform, Vector3 rayOrigin, Vector3 rayDirection, out float t, out Vector3 point, out Vector3 normal) =>
		TryRaySphere(worldTransform.Translation, sphereExtent.Sphere.Radius, rayOrigin, rayDirection, out t, out point, out normal);

	private static bool TryRaySphere(Vector3 center, float radius, Vector3 rayOrigin, Vector3 rayDirection, out float t, out Vector3 point, out Vector3 normal) {
		var direction = Vector3.Normalize(rayDirection);
		var offset = rayOrigin - center;
		var a = Vector3.Dot(direction, direction);
		var b = 2f * Vector3.Dot(direction, offset);
		var c = Vector3.Dot(offset, offset) - (radius * radius);
		var discriminant = (b * b) - (4f * a * c);
		if (discriminant < 0f) {
			t = 0f;
			point = Vector3.Zero;
			normal = Vector3.Zero;
			return false;
		}

		var sqrt = MathF.Sqrt(discriminant);
		var t0 = (-b - sqrt) / (2f * a);
		var t1 = (-b + sqrt) / (2f * a);
		t = t0 >= 0f ? t0 : t1;
		if (t < 0f) {
			point = Vector3.Zero;
			normal = Vector3.Zero;
			return false;
		}

		point = rayOrigin + (direction * t);
		normal = CollisionMath.SafeNormal(point - center, Vector3.UnitZ);
		return true;
	}

	private static bool TryRayBox(BoxExtent boxExtent, Transform3 worldTransform, Vector3 rayOrigin, Vector3 rayDirection, out float t, out Vector3 point, out Vector3 normal) {
		var localOrigin = worldTransform.InverseTransform(rayOrigin);
		var localDirection = worldTransform.InverseRotate(rayDirection);
		var boundsMin = new Vector3(-boxExtent.Box.HalfX, -boxExtent.Box.HalfY, -boxExtent.Box.HalfZ);
		var boundsMax = new Vector3(boxExtent.Box.HalfX, boxExtent.Box.HalfY, boxExtent.Box.HalfZ);

		var tMin = float.NegativeInfinity;
		var tMax = float.PositiveInfinity;
		var localNormal = Vector3.Zero;

		for (var axis = 0; axis < 3; axis++) {
			var origin = axis switch { 0 => localOrigin.X, 1 => localOrigin.Y, _ => localOrigin.Z };
			var direction = axis switch { 0 => localDirection.X, 1 => localDirection.Y, _ => localDirection.Z };
			var min = axis switch { 0 => boundsMin.X, 1 => boundsMin.Y, _ => boundsMin.Z };
			var max = axis switch { 0 => boundsMax.X, 1 => boundsMax.Y, _ => boundsMax.Z };

			if (MathF.Abs(direction) < 1e-6f) {
				if (origin < min || origin > max) {
					t = 0f;
					point = Vector3.Zero;
					normal = Vector3.Zero;
					return false;
				}

				continue;
			}

			var invDirection = 1f / direction;
			var near = (min - origin) * invDirection;
			var far = (max - origin) * invDirection;
			var nearNormal = axis switch {
				0 => new Vector3(-MathF.Sign(direction), 0f, 0f),
				1 => new Vector3(0f, -MathF.Sign(direction), 0f),
				_ => new Vector3(0f, 0f, -MathF.Sign(direction))
			};

			if (near > far) {
				(near, far) = (far, near);
				nearNormal = -nearNormal;
			}

			if (near > tMin) {
				tMin = near;
				localNormal = nearNormal;
			}

			tMax = MathF.Min(tMax, far);
			if (tMin > tMax) {
				t = 0f;
				point = Vector3.Zero;
				normal = Vector3.Zero;
				return false;
			}
		}

		t = tMin >= 0f ? tMin : tMax;
		if (t < 0f) {
			point = Vector3.Zero;
			normal = Vector3.Zero;
			return false;
		}

		var localPoint = localOrigin + (localDirection * t);
		point = worldTransform.Transform(localPoint);
		normal = CollisionMath.LocalToWorldDirection(worldTransform, localNormal);
		return true;
	}

	private static bool TryRayMesh(CollisionMesh mesh, Transform3 worldTransform, Vector3 rayOrigin, Vector3 rayDirection, out float t, out Vector3 point, out Vector3 normal) {
		var localOrigin = worldTransform.InverseTransform(rayOrigin);
		var localDirection = worldTransform.InverseRotate(rayDirection);

		var hit = false;
		var bestT = float.MaxValue;
		var bestPoint = Vector3.Zero;
		var bestNormal = Vector3.Zero;

		foreach (var triangle in mesh.Triangles) {
			var v0 = mesh.Vertices[triangle.V0];
			var v1 = mesh.Vertices[triangle.V1];
			var v2 = mesh.Vertices[triangle.V2];
			if (!TryRayTriangle(localOrigin, localDirection, v0, v1, v2, out var localT, out var localPoint)) {
				continue;
			}

			if (localT >= bestT) {
				continue;
			}

			bestT = localT;
			bestPoint = localPoint;
			bestNormal = triangle.NormalIndex >= 0 && triangle.NormalIndex < mesh.Normals.Length
				? mesh.Normals[triangle.NormalIndex]
				: CollisionMath.SafeNormal(Vector3.Cross(v1 - v0, v2 - v0), Vector3.UnitZ);
			hit = true;
		}

		t = bestT;
		point = hit ? worldTransform.Transform(bestPoint) : Vector3.Zero;
		normal = hit ? CollisionMath.LocalToWorldDirection(worldTransform, bestNormal) : Vector3.Zero;
		return hit;
	}

	private static bool TryRayTriangle(Vector3 origin, Vector3 direction, Vector3 v0, Vector3 v1, Vector3 v2, out float t, out Vector3 point) {
		var edge1 = v1 - v0;
		var edge2 = v2 - v0;
		var pVec = Vector3.Cross(direction, edge2);
		var determinant = Vector3.Dot(edge1, pVec);
		if (MathF.Abs(determinant) < 1e-6f) {
			t = 0f;
			point = Vector3.Zero;
			return false;
		}

		var invDeterminant = 1f / determinant;
		var tVec = origin - v0;
		var u = Vector3.Dot(tVec, pVec) * invDeterminant;
		if (u < 0f || u > 1f) {
			t = 0f;
			point = Vector3.Zero;
			return false;
		}

		var qVec = Vector3.Cross(tVec, edge1);
		var v = Vector3.Dot(direction, qVec) * invDeterminant;
		if (v < 0f || (u + v) > 1f) {
			t = 0f;
			point = Vector3.Zero;
			return false;
		}

		t = Vector3.Dot(edge2, qVec) * invDeterminant;
		if (t < 0f) {
			point = Vector3.Zero;
			return false;
		}

		point = origin + (direction * t);
		return true;
	}

	private sealed record CollisionInstanceRecord(ICollisionModel Model, int ArchetypeIndex);
}

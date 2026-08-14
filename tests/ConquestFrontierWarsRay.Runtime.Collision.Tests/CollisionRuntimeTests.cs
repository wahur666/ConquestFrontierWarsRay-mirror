using System.Numerics;
using ConquestFrontierWarsRay.Runtime.Collision;
using Math3D;
using Xunit;

namespace ConquestFrontierWarsRay.Runtime.Collision.Tests;

public sealed class CollisionRuntimeTests {
	[Fact]
	public void CreateArchetype_LoadsRealRepoExtentTreeAndPublishesModel() {
		var collision = new CollisionService();

		Assert.True(collision.CreateArchetype(1, GetAsteroidModelPath()));
		Assert.True(collision.TryGetArchetypeModel(1, out var model));

		Assert.NotNull(model);
		Assert.IsType<SphereExtent>(model.RootExtent);
		Assert.True(model.BoundingRadius > 0f);
		Assert.NotEmpty(model.RootExtent.Children);
	}

	[Fact]
	public void CollisionService_RayAndHierarchyQueries_WorkAgainstRealAsteroidSample() {
		var collision = new CollisionService();
		Assert.True(collision.CreateArchetype(1, GetAsteroidModelPath()));
		Assert.True(collision.TryGetArchetypeModel(1, out var model));
		Assert.NotNull(model);

		var origin = model.BoundingCenter + new Vector3(0f, 0f, model.BoundingRadius * 3f);
		var direction = Vector3.Normalize(model.BoundingCenter - origin);
		Assert.True(collision.IntersectRayWithExtentHierarchy(
			out var point,
			out var normal,
			origin,
			direction,
			model.RootExtent,
			Transform3.Identity,
			findClosest: true));

		Assert.True(Vector3.Distance(point, model.BoundingCenter) <= model.BoundingRadius * 1.5f);
		Assert.True(normal.LengthSquared() > 0f);

		var data = new CollisionData();
		Assert.True(collision.CollideExtentHierarchies(
			data,
			model.RootExtent,
			Transform3.Identity,
			model.RootExtent,
			new Transform3(Matrix3.Identity, new Vector3(5f, 0f, 0f))));
		Assert.NotNull(data.E1);
		Assert.NotNull(data.E2);
	}

	[Fact]
	public void RegisterArchetype_AndCreateInstance_WorkWithDirectConstruction() {
		var collision = new CollisionService();
		var extent = new SphereExtent("ball", Transform3.Identity, new CollisionSphere(2f));
		var model = new TestCollisionModel(extent, 2f);

		collision.RegisterArchetype(7, model);

		Assert.True(collision.CreateInstance(11, 7));
		Assert.True(collision.TryGetArchetypeModel(7, out var archetypeModel));
		Assert.True(collision.TryGetInstanceModel(11, out var instanceModel));
		Assert.Same(model, archetypeModel);
		Assert.Same(model, instanceModel);
	}

	private static string GetAsteroidModelPath() => Path.Combine(GetRepoRoot(), "ConquestSharp", "3dbModels", "asteroid4.3db");

	private static string GetRepoRoot() {
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null) {
			if (File.Exists(Path.Combine(current.FullName, "CMakeLists.txt")) &&
			    Directory.Exists(Path.Combine(current.FullName, "ConquestSharp"))) {
				return current.FullName;
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException("Could not locate repository root from test output.");
	}

	private sealed record TestCollisionModel(
		BaseExtent RootExtent,
		float Radius) : ICollisionModel {
		public string SourceName => "test";
		public float Mass => 1f;
		public Vector3 CenterOfMass => Vector3.Zero;
		public Matrix3 InertiaTensor => Matrix3.Identity;
		public Vector3 BoundingCenter => Vector3.Zero;
		public float BoundingRadius => Radius;
	}
}

using System.Numerics;
using ConquestFrontierWarsRay.Runtime.Physics;
using Math3D;
using Xunit;

namespace ConquestFrontierWarsRay.Runtime.Physics.Tests;

public sealed class PhysicsRuntimeTests {
	[Fact]
	public void Constructor_UsesTrapezoidalSolverByDefault() {
		var physics = new PhysicsService();
		physics.RegisterInstance(1, new PhysicsInstanceDefinition {
			DynamicStateOverride = DynamicState.Dynamic
		});
		physics.AddForce(1, new Vector3(1f, 0f, 0f));

		physics.UpdateInstance(1, 0.5f);

		Assert.Equal(new Vector3(0.5f, 0f, 0f), physics.GetVelocity(1));
	}

	[Theory]
	[InlineData(PhysicsSolverKind.Euler, 1.5f)]
	[InlineData(PhysicsSolverKind.Rk4, 1.6484375f)]
	[InlineData(PhysicsSolverKind.Trapezoidal, 1.5f)]
	public void SolverFactory_SelectsRequestedImplementation(PhysicsSolverKind kind, float expectedValue) {
		var solver = PhysicsSolvers.Create(kind);
		var equation = new ExponentialGrowthEquation(1f);

		solver.Solve(equation, 0.5f);

		Assert.Equal(expectedValue, equation.Value, 6);
		Assert.Equal(0.5f, equation.GetTime(), 6);
	}

	[Fact]
	public void TrapSolver_PreservesActiveForwardOnlyBehavior() {
		var solver = new TrapezoidalSolver();
		var equation = new ExponentialGrowthEquation(1f);

		solver.Solve(equation, 0.5f);

		Assert.Equal(1.5f, equation.Value, 4);
		Assert.Equal(0.5f, equation.GetTime(), 4);
	}

	[Fact]
	public void UpdateInstance_AppliesForceUsingMinDtSubsteps() {
		var physics = CreatePhysics();
		physics.RegisterArchetype(7, new PhysicsArchetypeDefinition {
			Mass = 2f,
			DynamicState = DynamicState.Dynamic,
			Extent = new PhysicsExtent(5f, Vector3.Zero)
		});
		physics.RegisterInstance(11, new PhysicsInstanceDefinition {
			ArchetypeIndex = 7,
			Transform = new Transform3(Matrix3.Identity, Vector3.Zero)
		});
		physics.SetMinDt(0.25f);
		physics.AddForce(11, new Vector3(4f, 0f, 0f));

		physics.UpdateInstance(11, 1f);

		Assert.Equal(new Vector3(2f, 0f, 0f), physics.GetVelocity(11));
		Assert.Equal(new Vector3(4f, 0f, 0f), physics.GetMomentum(11));
		Assert.Equal(new Vector3(0.75f, 0f, 0f), physics.GetTransform(11).Translation);
		Assert.True(physics.TryGetExtent(11, out var extent));
		Assert.Equal(5f, extent.Radius);
	}

	[Fact]
	public void Update_AdvancesAllDynamicInstancesAndLeavesFixedOnesAlone() {
		var physics = CreatePhysics();
		physics.RegisterInstance(1, new PhysicsInstanceDefinition {
			DynamicStateOverride = DynamicState.Dynamic
		});
		physics.RegisterInstance(2, new PhysicsInstanceDefinition {
			DynamicStateOverride = DynamicState.Fixed
		});

		physics.SetVelocity(1, new Vector3(3f, 0f, 0f));
		physics.SetVelocity(2, new Vector3(3f, 0f, 0f));

		physics.Update(0.5f);

		Assert.Equal(new Vector3(1.5f, 0f, 0f), physics.GetTransform(1).Translation);
		Assert.Equal(Vector3.Zero, physics.GetTransform(2).Translation);
		Assert.Equal(new PhysicsInstanceStats(1, 0, 1), physics.GetStats());
	}

	private static PhysicsService CreatePhysics() => new();

	private sealed class ExponentialGrowthEquation : IOrdinaryDifferentialEquation {
		public ExponentialGrowthEquation(float initialValue) {
			Value = initialValue;
		}

		public float Value { get; private set; }

		private float Time { get; set; }

		public int GetStateLength() => 1;

		public void GetState(Span<float> destination, float time) {
			destination[0] = Value;
			Time = time;
		}

		public void GetDerivative(Span<float> destination, ReadOnlySpan<float> state, float time) {
			destination[0] = state[0];
			Time = time;
		}

		public void SetState(ReadOnlySpan<float> source) => Value = source[0];

		public float GetTime() => Time;

		public void SetTime(float time) => Time = time;
	}
}

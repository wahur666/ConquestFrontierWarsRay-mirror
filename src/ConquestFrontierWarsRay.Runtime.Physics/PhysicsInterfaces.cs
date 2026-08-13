using System.Numerics;
using DOSFile;
using ConquestSharp.Engine;
using ConquestSharp.SystemLayer;
using Math3D;

namespace ConquestFrontierWarsRay.Runtime.Physics;

public enum PhysicsState {
	UseForces,
	CollisionFriction,
	JointDynamics
}

public enum DynamicState {
	NonDynamic = 0,
	Dynamic = 1,
	Fixed = 2
}

public static class PhysicsIdentifiers {
	public const string ComponentName = "Physics";
	public const string InterfaceName = "IPhysics";
	public const string OdeInterfaceName = "IODESolver";
	public const string IntegrationInterfaceName = "IPhysicsIntegration";
	public const string EulerImplementation = "Euler";
	public const string Rk4Implementation = "RK4";
	public const string TrapezoidalImplementation = "Trapezoidal";
	public const int InvalidArchetypeIndex = -1;
}

public delegate bool PhysicsCollisionCallback(int firstInstanceIndex, int secondInstanceIndex, PhysicsCollisionData collision);

public readonly record struct PhysicsCollisionData(Vector3 Point, Vector3 Normal, float PenetrationDepth);

public readonly record struct PhysicsInstanceStats(int DynamicCount, int NonDynamicCount, int FixedCount);

public readonly record struct PhysicsExtent(float Radius, Vector3 Center);

public interface IJointDriver {
	void Drive(int parentInstanceIndex, int childInstanceIndex, float force, float torque);
}

public interface IPhysicsForceElement {
	void Apply(IPhysics physics, float dt);
}

public interface IOrdinaryDifferentialEquation {
	int GetStateLength();

	void GetState(Span<float> destination, float time);

	void GetDerivative(Span<float> destination, ReadOnlySpan<float> state, float time);

	void SetState(ReadOnlySpan<float> source);

	float GetTime();

	void SetTime(float time);
}

public interface IOrdinaryDifferentialEquationSolver {
	void Solve(IOrdinaryDifferentialEquation solvable, float timeStep);
}

public interface IPhysics {
	DynamicState GetDynamic(int instanceIndex);

	void SetDynamic(int instanceIndex, DynamicState dynamicState);

	float GetMass(int instanceIndex);

	bool TryGetArchetypeMass(int archetypeIndex, out float mass);

	Vector3 GetVelocity(int instanceIndex);

	Vector3 GetMomentum(int instanceIndex);

	Vector3 GetAngularVelocity(int instanceIndex);

	Vector3 GetAngularMomentum(int instanceIndex);

	Vector3 GetCenterOfMass(int instanceIndex);

	Vector3 GetLocalCenterOfMass(int instanceIndex);

	Matrix3 GetInertiaTensor(int instanceIndex);

	void SetVelocity(int instanceIndex, Vector3 velocity);

	void SetAngularVelocity(int instanceIndex, Vector3 angularVelocity);

	void SetMass(int instanceIndex, float mass);

	void SetInertiaTensor(int instanceIndex, Matrix3 tensor);

	void SetLocalCenterOfMass(int instanceIndex, Vector3 centerOfMass);

	void AddForce(int instanceIndex, Vector3 force);

	void AddForceAtPoint(int instanceIndex, Vector3 force, Vector3 point);

	void AddTorque(int instanceIndex, Vector3 torque);

	void AddImpulse(int instanceIndex, Vector3 impulse);

	void AddImpulseAtPoint(int instanceIndex, Vector3 impulse, Vector3 point);

	void SetCollisionCallback(PhysicsCollisionCallback? callback);

	int AddForceElement(IPhysicsForceElement forceElement);

	void RemoveForceElement(int handle);

	void ComputeCollisionResponse(PhysicsCollisionData data, int firstInstanceIndex, int secondInstanceIndex);

	void Enable(PhysicsState state);

	void Disable(PhysicsState state);

	bool IsEnabled(PhysicsState state);

	bool TryGetExtent(int instanceIndex, out PhysicsExtent extent);

	void SetJointDriver(int jointIndex, IJointDriver? driver);

	void SetMinDt(float minDt);

	float GetMinDt();

	void UpdateInstance(int instanceIndex, float dt);

	bool TryGetArchetypeExtent(int archetypeIndex, out PhysicsExtent extent);

	void ComputeHierarchyCollisionResponse(PhysicsCollisionData data, int firstInstanceIndex, int secondInstanceIndex);

	PhysicsInstanceStats GetStats();

	bool IsValid(int instanceIndex);
}

public interface IPhysicsIntegration {
	void RegisterArchetype(int archetypeIndex, PhysicsArchetypeDefinition definition);

	void RegisterInstance(int instanceIndex, PhysicsInstanceDefinition definition);

	void RemoveInstance(int instanceIndex);

	void SetTransform(int instanceIndex, Transform3 transform);

	Transform3 GetTransform(int instanceIndex);
}

public interface IPhysicsComponent : IPhysics, IPhysicsIntegration, IEngineComponent, IAggregateComponent {
}

public sealed record PhysicsArchetypeDefinition {
	public float Mass { get; init; } = 1f;

	public Matrix3 InertiaTensor { get; init; } = Matrix3.Identity;

	public Vector3 LocalCenterOfMass { get; init; } = Vector3.Zero;

	public DynamicState DynamicState { get; init; } = DynamicState.NonDynamic;

	public PhysicsExtent? Extent { get; init; }
}

public sealed record PhysicsInstanceDefinition {
	public int ArchetypeIndex { get; init; } = PhysicsIdentifiers.InvalidArchetypeIndex;

	public Transform3 Transform { get; init; } = Transform3.Identity;

	public Vector3 Velocity { get; init; } = Vector3.Zero;

	public Vector3 AngularVelocity { get; init; } = Vector3.Zero;

	public DynamicState? DynamicStateOverride { get; init; }
}

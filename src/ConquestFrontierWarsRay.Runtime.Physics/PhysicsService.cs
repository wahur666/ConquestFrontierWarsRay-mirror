using System.Numerics;
using DACOM;
using DOSFile;
using ConquestSharp.Engine;
using ConquestSharp.SystemLayer;
using Math3D;

namespace ConquestFrontierWarsRay.Runtime.Physics;

public sealed class PhysicsService : DacomComponent, IPhysicsComponent {
	private readonly IDacomRegistry _registry;
	private readonly string _solverImplementation;
	private readonly Dictionary<int, PhysicsArchetypeDefinition> _archetypes = [];
	private readonly Dictionary<int, PhysicsBodyState> _instances = [];
	private readonly Dictionary<int, IPhysicsForceElement> _forceElements = [];
	private readonly Dictionary<int, IJointDriver?> _jointDrivers = [];
	private IOrdinaryDifferentialEquationSolver? _solver;
	private PhysicsCollisionCallback? _collisionCallback;
	private float _minDt = 0.1f;
	private int _nextForceElementHandle = 1;
	private bool _useForces = true;
	private bool _collisionFriction;
	private bool _jointDynamics;

	internal PhysicsService(PhysicsDesc descriptor, IDacomRegistry registry) {
		_registry = registry ?? throw new ArgumentNullException(nameof(registry));
		_solverImplementation = string.IsNullOrWhiteSpace(descriptor.SolverImplementation)
			? PhysicsIdentifiers.TrapezoidalImplementation
			: descriptor.SolverImplementation;

		RegisterInterface(PhysicsIdentifiers.ComponentName, this);
		RegisterInterface(PhysicsIdentifiers.InterfaceName, this);
		RegisterInterface(PhysicsIdentifiers.IntegrationInterfaceName, this);
		RegisterInterface(EngineIdentifiers.EngineComponentInterfaceName, this);
		RegisterInterface("IAggregateComponent", this);
	}

	public bool Initialize() {
		if (_solver is not null) {
			return true;
		}

		_solver = (IOrdinaryDifferentialEquationSolver)_registry.CreateInstance(new OdeSolverDesc(_solverImplementation));
		return true;
	}

	public bool CreateArchetype(int archetypeIndex, IFileSystem fileSystem) {
		_ = fileSystem;
		if (archetypeIndex == PhysicsIdentifiers.InvalidArchetypeIndex) {
			return false;
		}

		RegisterArchetype(archetypeIndex, new PhysicsArchetypeDefinition());
		return true;
	}

	public void DuplicateArchetype(int newArchetypeIndex, int oldArchetypeIndex) {
		if (!_archetypes.TryGetValue(oldArchetypeIndex, out var definition)) {
			throw new InvalidOperationException($"Unknown physics archetype index {oldArchetypeIndex}.");
		}

		_archetypes[newArchetypeIndex] = definition with { };
	}

	public void DestroyArchetype(int archetypeIndex) {
		_archetypes.Remove(archetypeIndex);
	}

	public bool TryQueryArchetypeInterface(int archetypeIndex, string interfaceName, out object? implementation) {
		if (archetypeIndex != PhysicsIdentifiers.InvalidArchetypeIndex &&
		    _archetypes.ContainsKey(archetypeIndex) &&
		    string.Equals(interfaceName, PhysicsIdentifiers.InterfaceName, StringComparison.Ordinal)) {
			implementation = this;
			return true;
		}

		implementation = null;
		return false;
	}

	public bool CreateInstance(int instanceIndex, int archetypeIndex) {
		RegisterInstance(instanceIndex, new PhysicsInstanceDefinition {
			ArchetypeIndex = archetypeIndex
		});
		return true;
	}

	public void DestroyInstance(int instanceIndex) {
		_instances.Remove(instanceIndex);
	}

	public void UpdateInstance(int instanceIndex, float dt) {
		EnsureInitialized();
		if (!_instances.TryGetValue(instanceIndex, out var state) || dt <= 0f) {
			return;
		}

		if (state.DynamicState != DynamicState.Dynamic) {
			ResetAccumulators(state);
			return;
		}

		var stepCount = Math.Max(1, (int)MathF.Ceiling(dt / Math.Max(_minDt, 1e-5f)));
		var step = dt / stepCount;

		for (var index = 0; index < stepCount; index++) {
			ApplyForceElements(step);
			IntegrateLinearState(state, step);
			IntegrateAngularState(state, step);
		}

		ResetAccumulators(state);
	}

	public VisState RenderInstance(object camera, int instanceIndex, float lodFraction, RenderFlags flags, Transform3? modifierTransform) {
		_ = camera;
		_ = instanceIndex;
		_ = lodFraction;
		_ = flags;
		_ = modifierTransform;
		return VisState.Unknown;
	}

	public bool TryQueryInstanceInterface(int instanceIndex, string interfaceName, out object? implementation) {
		if (_instances.ContainsKey(instanceIndex) &&
		    string.Equals(interfaceName, PhysicsIdentifiers.InterfaceName, StringComparison.Ordinal)) {
			implementation = this;
			return true;
		}

		implementation = null;
		return false;
	}

	public void Update(float dt) {
		foreach (var instanceIndex in _instances.Keys.ToArray()) {
			UpdateInstance(instanceIndex, dt);
		}
	}

	public DynamicState GetDynamic(int instanceIndex) => GetBody(instanceIndex).DynamicState;

	public void SetDynamic(int instanceIndex, DynamicState dynamicState) => GetBody(instanceIndex).DynamicState = dynamicState;

	public float GetMass(int instanceIndex) => GetBody(instanceIndex).Mass;

	public bool TryGetArchetypeMass(int archetypeIndex, out float mass) {
		if (_archetypes.TryGetValue(archetypeIndex, out var definition)) {
			mass = definition.Mass;
			return true;
		}

		mass = 0f;
		return false;
	}

	public Vector3 GetVelocity(int instanceIndex) => GetBody(instanceIndex).Velocity;

	public Vector3 GetMomentum(int instanceIndex) => GetBody(instanceIndex).Momentum;

	public Vector3 GetAngularVelocity(int instanceIndex) => GetBody(instanceIndex).AngularVelocity;

	public Vector3 GetAngularMomentum(int instanceIndex) => GetBody(instanceIndex).AngularMomentum;

	public Vector3 GetCenterOfMass(int instanceIndex) {
		var body = GetBody(instanceIndex);
		return body.Transform.Transform(body.LocalCenterOfMass);
	}

	public Vector3 GetLocalCenterOfMass(int instanceIndex) => GetBody(instanceIndex).LocalCenterOfMass;

	public Matrix3 GetInertiaTensor(int instanceIndex) => GetBody(instanceIndex).InertiaTensor;

	public void SetVelocity(int instanceIndex, Vector3 velocity) {
		var body = GetBody(instanceIndex);
		body.Velocity = velocity;
		body.Momentum = velocity * body.Mass;
	}

	public void SetAngularVelocity(int instanceIndex, Vector3 angularVelocity) {
		var body = GetBody(instanceIndex);
		body.AngularVelocity = angularVelocity;
		body.AngularMomentum = angularVelocity * body.Mass;
	}

	public void SetMass(int instanceIndex, float mass) {
		var body = GetBody(instanceIndex);
		body.Mass = Math.Max(mass, 1e-5f);
		body.Momentum = body.Velocity * body.Mass;
		body.AngularMomentum = body.AngularVelocity * body.Mass;
	}

	public void SetInertiaTensor(int instanceIndex, Matrix3 tensor) => GetBody(instanceIndex).InertiaTensor = tensor;

	public void SetLocalCenterOfMass(int instanceIndex, Vector3 centerOfMass) => GetBody(instanceIndex).LocalCenterOfMass = centerOfMass;

	public void AddForce(int instanceIndex, Vector3 force) => GetBody(instanceIndex).AccumulatedForce += force;

	public void AddForceAtPoint(int instanceIndex, Vector3 force, Vector3 point) {
		var body = GetBody(instanceIndex);
		body.AccumulatedForce += force;
		body.AccumulatedTorque += Vector3.Cross(point - GetCenterOfMass(instanceIndex), force);
	}

	public void AddTorque(int instanceIndex, Vector3 torque) => GetBody(instanceIndex).AccumulatedTorque += torque;

	public void AddImpulse(int instanceIndex, Vector3 impulse) {
		var body = GetBody(instanceIndex);
		body.Velocity += impulse / body.Mass;
		body.Momentum = body.Velocity * body.Mass;
	}

	public void AddImpulseAtPoint(int instanceIndex, Vector3 impulse, Vector3 point) {
		AddImpulse(instanceIndex, impulse);
		AddTorque(instanceIndex, Vector3.Cross(point - GetCenterOfMass(instanceIndex), impulse));
	}

	public void SetCollisionCallback(PhysicsCollisionCallback? callback) => _collisionCallback = callback;

	public int AddForceElement(IPhysicsForceElement forceElement) {
		ArgumentNullException.ThrowIfNull(forceElement);
		var handle = _nextForceElementHandle++;
		_forceElements[handle] = forceElement;
		return handle;
	}

	public void RemoveForceElement(int handle) => _forceElements.Remove(handle);

	public void ComputeCollisionResponse(PhysicsCollisionData data, int firstInstanceIndex, int secondInstanceIndex) {
		if (_collisionCallback is not null && !_collisionCallback(firstInstanceIndex, secondInstanceIndex, data)) {
			return;
		}

		if (!_instances.TryGetValue(firstInstanceIndex, out var first) ||
		    !_instances.TryGetValue(secondInstanceIndex, out var second)) {
			return;
		}

		var normal = data.Normal.LengthSquared() > 0f ? Vector3.Normalize(data.Normal) : Vector3.UnitY;
		var relativeVelocity = first.Velocity - second.Velocity;
		var separatingSpeed = Vector3.Dot(relativeVelocity, normal);
		if (separatingSpeed >= 0f) {
			return;
		}

		var restitution = 0.5f;
		var inverseMass = (1f / first.Mass) + (1f / second.Mass);
		if (inverseMass <= 0f) {
			return;
		}

		var impulseMagnitude = -(1f + restitution) * separatingSpeed / inverseMass;
		var impulse = normal * impulseMagnitude;

		if (first.DynamicState == DynamicState.Dynamic) {
			AddImpulse(firstInstanceIndex, impulse);
		}

		if (second.DynamicState == DynamicState.Dynamic) {
			AddImpulse(secondInstanceIndex, -impulse);
		}
	}

	public void Enable(PhysicsState state) => SetState(state, true);

	public void Disable(PhysicsState state) => SetState(state, false);

	public bool IsEnabled(PhysicsState state) => state switch {
		PhysicsState.UseForces => _useForces,
		PhysicsState.CollisionFriction => _collisionFriction,
		PhysicsState.JointDynamics => _jointDynamics,
		_ => false
	};

	public bool TryGetExtent(int instanceIndex, out PhysicsExtent extent) {
		var body = GetBody(instanceIndex);
		if (body.Extent is { } localExtent) {
			extent = new PhysicsExtent(localExtent.Radius, body.Transform.Transform(localExtent.Center));
			return true;
		}

		extent = default;
		return false;
	}

	public void SetJointDriver(int jointIndex, IJointDriver? driver) => _jointDrivers[jointIndex] = driver;

	public void SetMinDt(float minDt) => _minDt = Math.Max(minDt, 1e-5f);

	public float GetMinDt() => _minDt;

	public bool TryGetArchetypeExtent(int archetypeIndex, out PhysicsExtent extent) {
		if (_archetypes.TryGetValue(archetypeIndex, out var definition) && definition.Extent is { } foundExtent) {
			extent = foundExtent;
			return true;
		}

		extent = default;
		return false;
	}

	public void ComputeHierarchyCollisionResponse(PhysicsCollisionData data, int firstInstanceIndex, int secondInstanceIndex) =>
		ComputeCollisionResponse(data, firstInstanceIndex, secondInstanceIndex);

	public PhysicsInstanceStats GetStats() {
		var dynamicCount = 0;
		var nonDynamicCount = 0;
		var fixedCount = 0;

		foreach (var body in _instances.Values) {
			switch (body.DynamicState) {
				case DynamicState.Dynamic:
					dynamicCount++;
					break;
				case DynamicState.Fixed:
					fixedCount++;
					break;
				default:
					nonDynamicCount++;
					break;
			}
		}

		return new PhysicsInstanceStats(dynamicCount, nonDynamicCount, fixedCount);
	}

	public bool IsValid(int instanceIndex) => _instances.ContainsKey(instanceIndex);

	public void RegisterArchetype(int archetypeIndex, PhysicsArchetypeDefinition definition) {
		ArgumentNullException.ThrowIfNull(definition);
		_archetypes[archetypeIndex] = definition;
	}

	public void RegisterInstance(int instanceIndex, PhysicsInstanceDefinition definition) {
		ArgumentNullException.ThrowIfNull(definition);

		var archetype = definition.ArchetypeIndex != PhysicsIdentifiers.InvalidArchetypeIndex &&
		                _archetypes.TryGetValue(definition.ArchetypeIndex, out var foundArchetype)
			? foundArchetype
			: new PhysicsArchetypeDefinition();

		var dynamicState = definition.DynamicStateOverride ?? archetype.DynamicState;
		var body = new PhysicsBodyState {
			ArchetypeIndex = definition.ArchetypeIndex,
			Transform = definition.Transform,
			Velocity = definition.Velocity,
			AngularVelocity = definition.AngularVelocity,
			Mass = Math.Max(archetype.Mass, 1e-5f),
			InertiaTensor = archetype.InertiaTensor,
			LocalCenterOfMass = archetype.LocalCenterOfMass,
			DynamicState = dynamicState,
			Extent = archetype.Extent
		};

		body.Momentum = body.Velocity * body.Mass;
		body.AngularMomentum = body.AngularVelocity * body.Mass;

		_instances[instanceIndex] = body;
	}

	public void RemoveInstance(int instanceIndex) => _instances.Remove(instanceIndex);

	public void SetTransform(int instanceIndex, Transform3 transform) => GetBody(instanceIndex).Transform = transform;

	public Transform3 GetTransform(int instanceIndex) => GetBody(instanceIndex).Transform;

	private void EnsureInitialized() {
		if (_solver is null) {
			Initialize();
		}
	}

	private PhysicsBodyState GetBody(int instanceIndex) {
		if (!_instances.TryGetValue(instanceIndex, out var body)) {
			throw new InvalidOperationException($"Unknown physics instance index {instanceIndex}.");
		}

		return body;
	}

	private void SetState(PhysicsState state, bool value) {
		switch (state) {
			case PhysicsState.UseForces:
				_useForces = value;
				break;
			case PhysicsState.CollisionFriction:
				_collisionFriction = value;
				break;
			case PhysicsState.JointDynamics:
				_jointDynamics = value;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown physics state.");
		}
	}

	private void ApplyForceElements(float step) {
		foreach (var element in _forceElements.Values) {
			element.Apply(this, step);
		}
	}

	private void IntegrateLinearState(PhysicsBodyState state, float step) {
		var acceleration = _useForces ? state.AccumulatedForce / state.Mass : Vector3.Zero;
		var equation = new LinearMotionEquation(state, acceleration);
		_solver!.Solve(equation, step);
		state.Transform = new Transform3(state.Transform.Orientation, equation.Position);
		state.Velocity = equation.Velocity;
		state.Momentum = state.Velocity * state.Mass;
	}

	private static void IntegrateAngularState(PhysicsBodyState state, float step) {
		var angularAcceleration = state.AccumulatedTorque / state.Mass;
		state.AngularVelocity += angularAcceleration * step;
		state.AngularMomentum = state.AngularVelocity * state.Mass;
	}

	private static void ResetAccumulators(PhysicsBodyState state) {
		state.AccumulatedForce = Vector3.Zero;
		state.AccumulatedTorque = Vector3.Zero;
	}

	private sealed class PhysicsBodyState {
		public int ArchetypeIndex { get; init; } = PhysicsIdentifiers.InvalidArchetypeIndex;

		public Transform3 Transform { get; set; } = Transform3.Identity;

		public Vector3 Velocity { get; set; }

		public Vector3 Momentum { get; set; }

		public Vector3 AngularVelocity { get; set; }

		public Vector3 AngularMomentum { get; set; }

		public Vector3 LocalCenterOfMass { get; set; }

		public Matrix3 InertiaTensor { get; set; } = Matrix3.Identity;

		public float Mass { get; set; } = 1f;

		public DynamicState DynamicState { get; set; } = DynamicState.NonDynamic;

		public Vector3 AccumulatedForce { get; set; }

		public Vector3 AccumulatedTorque { get; set; }

		public PhysicsExtent? Extent { get; set; }
	}

	private sealed class LinearMotionEquation : IOrdinaryDifferentialEquation {
		private readonly PhysicsBodyState _state;
		private readonly Vector3 _acceleration;
		private float _time;

		public LinearMotionEquation(PhysicsBodyState state, Vector3 acceleration) {
			_state = state;
			_acceleration = acceleration;
			Position = state.Transform.Translation;
			Velocity = state.Velocity;
		}

		public Vector3 Position { get; private set; }

		public Vector3 Velocity { get; private set; }

		public int GetStateLength() => 6;

		public void GetState(Span<float> destination, float time) {
			if (destination.Length < 6) {
				throw new ArgumentException("State buffer is shorter than the required linear-motion state vector.", nameof(destination));
			}

			destination[0] = Position.X;
			destination[1] = Position.Y;
			destination[2] = Position.Z;
			destination[3] = Velocity.X;
			destination[4] = Velocity.Y;
			destination[5] = Velocity.Z;
			_time = time;
		}

		public void GetDerivative(Span<float> destination, ReadOnlySpan<float> state, float time) {
			if (destination.Length < 6) {
				throw new ArgumentException("Derivative buffer is shorter than the required linear-motion state vector.", nameof(destination));
			}

			destination[0] = state[3];
			destination[1] = state[4];
			destination[2] = state[5];
			destination[3] = _acceleration.X;
			destination[4] = _acceleration.Y;
			destination[5] = _acceleration.Z;
			_time = time;
		}

		public void SetState(ReadOnlySpan<float> source) {
			if (source.Length < 6) {
				throw new ArgumentException("Source state is shorter than the required linear-motion state vector.", nameof(source));
			}

			Position = new Vector3(source[0], source[1], source[2]);
			Velocity = new Vector3(source[3], source[4], source[5]);

			_state.Transform = new Transform3(_state.Transform.Orientation, Position);
			_state.Velocity = Velocity;
		}

		public float GetTime() => _time;

		public void SetTime(float time) => _time = time;
	}
}

using System.Globalization;
using System.Numerics;

namespace RaySharp.Particle;

internal sealed class ParticlePreviewSystem : RenderableObject {
	private readonly Random random = new();
	private readonly List<ParticleInstance> particles = [];
	private float spawnAccumulator;

	public ParticlePreviewSystem(ParticleParameters parameters, Vector3 origin = default)
		: this(parameters, new Transform(origin)) {
	}

	public ParticlePreviewSystem(ParticleParameters parameters, Vector3 origin, Quaternion rotation, Vector3 scale)
		: this(parameters, new Transform(origin, rotation, scale)) {
	}

	public ParticlePreviewSystem(ParticleParameters parameters, Transform transform)
		: base(transform) {
		Parameters = parameters;
		UpdateSizeScale();
		MaxParticles = ComputeMaxParticles(parameters);
		Restart();
	}

	public ParticleParameters Parameters { get; }
	public Vector3 Origin => Transform.Position;
	public Matrix4x4 WorldMatrix => Transform.WorldMatrix;
	public Matrix4x4 NormalMatrix => Transform.NormalMatrix;
	public float SizeScale { get; private set; }
	public IReadOnlyList<ParticleInstance> Particles => particles;
	public int MaxParticles { get; }
	public int CreatedParticles { get; private set; }
	public float Elapsed { get; private set; }
	public float EmitterLifetime { get; private set; }
	public bool Finished { get; private set; }

	public void Restart() {
		particles.Clear();
		spawnAccumulator = Math.Max(0.0f, Parameters.InitialParticleCount);
		CreatedParticles = 0;
		Elapsed = 0.0f;
		EmitterLifetime = Parameters.Lifetime;
		Finished = false;
	}

	public override void Dispose() {
		particles.Clear();
	}

	public override void Update(RenderUpdateContext context) {
		Update(context.DeltaTime);
	}

	public void Update(float deltaTime) {
		ParticleParameters p = Parameters;
		Elapsed += deltaTime;

		if (deltaTime > 0.0f && p.Lifetime > 0.0f) {
			EmitterLifetime -= deltaTime;
		}

		for (int index = particles.Count - 1; index >= 0; index--) {
			ParticleInstance particle = particles[index];
			UpdateParticle(particle, p, deltaTime);
			if (particle.InitialLifetime > 0.0f && particle.Lifetime <= 0.0f) {
				particles.RemoveAt(index);
			}
		}

		bool canCreate = !Finished && (p.Lifetime <= 0.0f || EmitterLifetime > 0.0f);
		if (canCreate) {
			spawnAccumulator += Math.Max(0.0f, p.Frequency) * deltaTime;
			int count = (int)MathF.Floor(spawnAccumulator);
			spawnAccumulator -= count;

			if (p.MaxParticleCount > 0) {
				count = Math.Min(count, Math.Max(0, p.MaxParticleCount - CreatedParticles));
			}

			for (int i = 0; i < count; i++) {
				SpawnParticle();
			}
		}

		UpdateFinishedState();
	}

	public override void Render(RenderContext context) {
		// Particle rendering requires a camera and texture, so ParticleRenderer draws these systems.
	}

	public string LifecycleText() {
		return LifecycleText(Parameters, this);
	}

	public ParticleColorFrame ParticleColor(ParticleInstance particle) {
		const int frameMax = ParticleConstants.ColorKeyCount - 1;
		float life = particle.InitialLifetime > 0.0f
			? particle.InitialLifetime
			: Math.Max(1.0f, particle.Age + particle.Lifetime);
		float t = Math.Clamp(particle.Age / life, 0.0f, 1.0f);
		float frame = t * frameMax;
		int i0 = Math.Min(frameMax, (int)MathF.Floor(frame));
		int i1 = Math.Min(frameMax, i0 + 1);
		float mix = frame - i0;
		ParticleColorFrame c0 = Parameters.ColorFrames[i0];
		ParticleColorFrame c1 = Parameters.ColorFrames[i1];

		return new ParticleColorFrame(
			Lerp(c0.R, c1.R, mix),
			Lerp(c0.G, c1.G, mix),
			Lerp(c0.B, c1.B, mix),
			Lerp(c0.A, c1.A, mix));
	}

	public Vector3 TransformPosition(Vector3 position) {
		return Transform.TransformPoint(position);
	}

	public Vector3 TransformDirection(Vector3 direction) {
		return Transform.TransformDirection(direction);
	}

	public float TransformSize(float size) {
		return size * SizeScale;
	}

	public void SetTransform(Transform transform) {
		Transform.Set(transform.Position, transform.Rotation, transform.Scale);
		UpdateSizeScale();
	}

	public void Translate(Vector3 delta) {
		Transform.Translate(delta);
		UpdateSizeScale();
	}

	public void Rotate(Quaternion delta) {
		Transform.Rotate(delta);
		UpdateSizeScale();
	}

	public void ScaleBy(float factor) {
		Transform.ScaleBy(factor);
		UpdateSizeScale();
	}

	private void UpdateSizeScale() {
		Vector3 scale = Transform.Scale;
		SizeScale = Math.Max(0.01f, Math.Max(Math.Abs(scale.X), Math.Max(Math.Abs(scale.Y), Math.Abs(scale.Z))));
	}

	private void SpawnParticle() {
		ParticleParameters p = Parameters;
		if (particles.Count >= MaxParticles) {
			particles.RemoveAt(0);
		}

		Vector3 position = Vector3.Zero;
		if (p.ParticlePositionRandomizer != 0.0f) {
			position += new Vector3(
				RandomSigned() * p.ParticlePositionRandomizer,
				RandomSigned() * p.ParticlePositionRandomizer,
				RandomSigned() * p.ParticlePositionRandomizer);
		}

		float randomVelocity = RandomFloat() * Math.Max(0.0f, p.ParticleVelocityRandomizer);
		particles.Add(new ParticleInstance {
			Position = position,
			Direction = MakeEmitterDirection(p),
			Velocity = p.ParticleVelocity + p.ParticleVelocity * randomVelocity,
			Size = p.ParticleSize,
			Lifetime = p.ParticleLifetime,
			InitialLifetime = p.ParticleLifetime,
			Age = 0.0f
		});
		CreatedParticles++;
	}

	private static void UpdateParticle(ParticleInstance particle, ParticleParameters parameters, float deltaTime) {
		if (parameters.ParticleTwistVelocity != 0.0f) {
			float twist = parameters.ParticleTwistVelocity * deltaTime;
			particle.Direction.X -= particle.Direction.Y * twist;
			particle.Direction.Y += particle.Direction.X * twist;
			particle.Direction = NormalizeOrForward(particle.Direction);
		}

		if (parameters.ParticleSizeVelocity != 0.0f) {
			particle.Size += parameters.ParticleSizeVelocity * deltaTime;
		}

		particle.Direction += parameters.Gravity * deltaTime;
		particle.Position += particle.Direction * particle.Velocity * deltaTime;
		particle.Lifetime -= deltaTime;
		particle.Age += deltaTime;
	}

	private void UpdateFinishedState() {
		bool emitterDone = Parameters.Lifetime > 0.0f && EmitterLifetime <= 0.0f;
		bool spawnLimitDone = Parameters.MaxParticleCount > 0 && CreatedParticles >= Parameters.MaxParticleCount;
		if (emitterDone || spawnLimitDone) {
			Finished = particles.Count == 0;
		}
	}

	private Vector3 MakeEmitterDirection(ParticleParameters parameters) {
		Vector3 damp = parameters.EmitterNozzleDamp;
		bool degenerateDamp = damp.LengthSquared() == 0.0f;
		Vector3 direction = parameters.EmitterDirection;

		if (degenerateDamp) {
			return NormalizeOrForward(direction);
		}

		direction *= parameters.EmitterNozzleSize;
		direction.X += RandomSigned() * damp.X;
		direction.Y += RandomSigned() * damp.Y;
		direction.Z += RandomSigned() * damp.Z;
		return NormalizeOrForward(direction);
	}

	private float RandomSigned() {
		return RandomFloat() * 2.0f - 1.0f;
	}

	private float RandomFloat() {
		return (float)random.NextDouble();
	}

	public static int ComputeMaxParticles(ParticleParameters parameters) {
		float maxPossible;
		if (parameters.ParticleLifetime > 0.0f) {
			maxPossible = 1.25f * parameters.Frequency * parameters.ParticleLifetime + parameters.InitialParticleCount;
		} else if (parameters.Lifetime > 0.0f) {
			maxPossible = 1.25f * (1.0f + parameters.Frequency) + parameters.InitialParticleCount;
		} else {
			maxPossible = 128.0f + parameters.InitialParticleCount;
		}

		if (parameters.MaxParticleCount > 0) {
			maxPossible = Math.Min(maxPossible, parameters.MaxParticleCount);
		}

		return Math.Max(16, Math.Min(20000, (int)MathF.Ceiling(maxPossible)));
	}

	public static string LifecycleText(ParticleParameters parameters, ParticlePreviewSystem? system = null) {
		bool hasFiniteEmitter = parameters.Lifetime > 0.0f;
		bool hasFiniteParticleLife = parameters.ParticleLifetime > 0.0f;
		bool hasSpawnLimit = parameters.MaxParticleCount > 0;

		if (!hasFiniteEmitter && !hasSpawnLimit) {
			return hasFiniteParticleLife
				? $"Continuous emitter, particles {FormatNumber(parameters.ParticleLifetime)}s"
				: "Continuous emitter, infinite particles";
		}

		if (system is null) {
			return hasFiniteParticleLife ? "One-shot effect" : "One-shot emitter, infinite particles";
		}

		bool emitterDone = hasFiniteEmitter && system.EmitterLifetime <= 0.0f;
		bool spawnLimitDone = hasSpawnLimit && system.CreatedParticles >= parameters.MaxParticleCount;
		bool spawningDone = emitterDone || spawnLimitDone;

		if (system.Finished || spawningDone && system.particles.Count == 0) {
			return "Ended";
		}

		if (spawningDone) {
			return hasFiniteParticleLife
				? $"Draining {system.particles.Count} particles"
				: $"Emitter ended, {system.particles.Count} infinite particles";
		}

		if (hasFiniteEmitter) {
			return $"One-shot, {FormatNumber(Math.Max(0.0f, system.EmitterLifetime))}s emitter";
		}

		int remaining = Math.Max(0, parameters.MaxParticleCount - system.CreatedParticles);
		return hasFiniteParticleLife
			? $"Spawn-limited, {remaining} left"
			: $"Spawn-limited, {remaining} left, infinite particles";
	}

	private static Vector3 NormalizeOrForward(Vector3 value) {
		return value.LengthSquared() > 0.0f ? Vector3.Normalize(value) : new Vector3(0.0f, 0.0f, 1.0f);
	}

	private static float Lerp(float a, float b, float t) {
		return a + (b - a) * t;
	}

	private static string FormatNumber(float value) {
		return float.IsFinite(value)
			? value.ToString(MathF.Round(value) == value ? "0" : "0.####", CultureInfo.InvariantCulture)
			: "0";
	}
}

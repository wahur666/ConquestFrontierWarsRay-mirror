using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace RaySharp.Particle;

internal sealed class ParticlePreviewSystem : IDisposable {
	private readonly Random _random = new();
	private readonly List<ParticleInstance> _particles = [];
	private float _spawnAccumulator;

	public ParticlePreviewSystem(ParticleParameters parameters) {
		Parameters = parameters;
		SetWorldTransform(Matrix4x4.Identity);
		MaxParticles = ComputeMaxParticles(parameters);
		Restart();
	}

	public ParticleParameters Parameters { get; }
	public Vector3 Origin { get; private set; }
	public Matrix4x4 WorldMatrix { get; private set; }
	public Matrix4x4 NormalMatrix { get; private set; }
	public float SizeScale { get; private set; }
	public IReadOnlyList<ParticleInstance> Particles => _particles;
	public int MaxParticles { get; }
	public int CreatedParticles { get; private set; }
	public float Elapsed { get; private set; }
	public float EmitterLifetime { get; private set; }
	public bool Finished { get; private set; }

	public void Restart() {
		_particles.Clear();
		_spawnAccumulator = Math.Max(0f, Parameters.InitialParticleCount);
		CreatedParticles = 0;
		Elapsed = 0f;
		EmitterLifetime = Parameters.Lifetime;
		Finished = false;
	}

	public void Dispose() {
		_particles.Clear();
	}

	public void Update(float deltaTime) {
		var p = Parameters;
		Elapsed += deltaTime;

		if (deltaTime > 0f && p.Lifetime > 0f) {
			EmitterLifetime -= deltaTime;
		}

		for (var index = _particles.Count - 1; index >= 0; index--) {
			var particle = _particles[index];
			UpdateParticle(particle, p, deltaTime);
			if (particle.InitialLifetime > 0f && particle.Lifetime <= 0f) {
				_particles.RemoveAt(index);
			}
		}

		var canCreate = !Finished && (p.Lifetime <= 0f || EmitterLifetime > 0f);
		if (canCreate) {
			_spawnAccumulator += Math.Max(0f, p.Frequency) * deltaTime;
			var count = (int)MathF.Floor(_spawnAccumulator);
			_spawnAccumulator -= count;

			if (p.MaxParticleCount > 0) {
				count = Math.Min(count, Math.Max(0, p.MaxParticleCount - CreatedParticles));
			}

			for (var i = 0; i < count; i++) {
				SpawnParticle();
			}
		}

		UpdateFinishedState();
	}

	public ParticleColorFrame ParticleColor(ParticleInstance particle) {
		const int frameMax = ParticleConstants.ColorKeyCount - 1;
		var life = particle.InitialLifetime > 0f
			? particle.InitialLifetime
			: Math.Max(1f, particle.Age + particle.Lifetime);
		var t = Math.Clamp(particle.Age / life, 0f, 1f);
		var frame = t * frameMax;
		var i0 = Math.Min(frameMax, (int)MathF.Floor(frame));
		var i1 = Math.Min(frameMax, i0 + 1);
		var mix = frame - i0;
		var c0 = Parameters.ColorFrames[i0];
		var c1 = Parameters.ColorFrames[i1];

		return new ParticleColorFrame(
			Lerp(c0.R, c1.R, mix),
			Lerp(c0.G, c1.G, mix),
			Lerp(c0.B, c1.B, mix),
			Lerp(c0.A, c1.A, mix));
	}

	public Vector3 TransformPosition(Vector3 position) => Vector3.Transform(position, WorldMatrix);

	public float TransformSize(float size) => size * SizeScale;

	public void SetWorldTransform(Matrix4x4 worldMatrix) {
		WorldMatrix = worldMatrix;
		NormalMatrix = Matrix4x4.Invert(worldMatrix, out var inverseWorld)
			? Matrix4x4.Transpose(inverseWorld)
			: Matrix4x4.Identity;
		Origin = new Vector3(worldMatrix.M41, worldMatrix.M42, worldMatrix.M43);
		UpdateSizeScale(worldMatrix);
	}

	public static int ComputeMaxParticles(ParticleParameters parameters) {
		float maxPossible;
		if (parameters.ParticleLifetime > 0f) {
			maxPossible = 1.25f * parameters.Frequency * parameters.ParticleLifetime + parameters.InitialParticleCount;
		} else if (parameters.Lifetime > 0f) {
			maxPossible = 1.25f * (1f + parameters.Frequency) + parameters.InitialParticleCount;
		} else {
			maxPossible = 128f + parameters.InitialParticleCount;
		}

		if (parameters.MaxParticleCount > 0) {
			maxPossible = Math.Min(maxPossible, parameters.MaxParticleCount);
		}

		return Math.Max(16, Math.Min(20000, (int)MathF.Ceiling(maxPossible)));
	}

	public static string LifecycleText(ParticleParameters parameters, ParticlePreviewSystem? system = null) {
		var hasFiniteEmitter = parameters.Lifetime > 0f;
		var hasFiniteParticleLife = parameters.ParticleLifetime > 0f;
		var hasSpawnLimit = parameters.MaxParticleCount > 0;

		if (!hasFiniteEmitter && !hasSpawnLimit) {
			return hasFiniteParticleLife
				? $"Continuous emitter, particles {FormatNumber(parameters.ParticleLifetime)}s"
				: "Continuous emitter, infinite particles";
		}

		if (system is null) {
			return hasFiniteParticleLife ? "One-shot effect" : "One-shot emitter, infinite particles";
		}

		var emitterDone = hasFiniteEmitter && system.EmitterLifetime <= 0f;
		var spawnLimitDone = hasSpawnLimit && system.CreatedParticles >= parameters.MaxParticleCount;
		var spawningDone = emitterDone || spawnLimitDone;

		if (system.Finished || spawningDone && system._particles.Count == 0) {
			return "Ended";
		}

		if (spawningDone) {
			return hasFiniteParticleLife
				? $"Draining {system._particles.Count} particles"
				: $"Emitter ended, {system._particles.Count} infinite particles";
		}

		if (hasFiniteEmitter) {
			return $"One-shot, {FormatNumber(Math.Max(0f, system.EmitterLifetime))}s emitter";
		}

		var remaining = Math.Max(0, parameters.MaxParticleCount - system.CreatedParticles);
		return hasFiniteParticleLife
			? $"Spawn-limited, {remaining} left"
			: $"Spawn-limited, {remaining} left, infinite particles";
	}

	private void UpdateSizeScale(Matrix4x4 worldMatrix) {
		var scale = Transform3DFromMatrix(worldMatrix).Scale;
		SizeScale = Math.Max(0.01f, Math.Max(Math.Abs(scale.X), Math.Max(Math.Abs(scale.Y), Math.Abs(scale.Z))));
	}

	private void SpawnParticle() {
		var p = Parameters;
		if (_particles.Count >= MaxParticles) {
			_particles.RemoveAt(0);
		}

		var position = Vector3.Zero;
		if (p.ParticlePositionRandomizer != 0f) {
			position += new Vector3(
				RandomSigned() * p.ParticlePositionRandomizer,
				RandomSigned() * p.ParticlePositionRandomizer,
				RandomSigned() * p.ParticlePositionRandomizer);
		}

		var randomVelocity = RandomFloat() * Math.Max(0f, p.ParticleVelocityRandomizer);
		_particles.Add(new ParticleInstance {
			Position = position,
			Direction = MakeEmitterDirection(p),
			Velocity = p.ParticleVelocity + p.ParticleVelocity * randomVelocity,
			Size = p.ParticleSize,
			Lifetime = p.ParticleLifetime,
			InitialLifetime = p.ParticleLifetime,
			Age = 0f
		});
		CreatedParticles++;
	}

	private static void UpdateParticle(ParticleInstance particle, ParticleParameters parameters, float deltaTime) {
		if (parameters.ParticleTwistVelocity != 0f) {
			var twist = parameters.ParticleTwistVelocity * deltaTime;
			particle.Direction.X -= particle.Direction.Y * twist;
			particle.Direction.Y += particle.Direction.X * twist;
			particle.Direction = NormalizeOrForward(particle.Direction);
		}

		if (parameters.ParticleSizeVelocity != 0f) {
			particle.Size += parameters.ParticleSizeVelocity * deltaTime;
		}

		particle.Direction += parameters.Gravity * deltaTime;
		particle.Position += particle.Direction * particle.Velocity * deltaTime;
		particle.Lifetime -= deltaTime;
		particle.Age += deltaTime;
	}

	private void UpdateFinishedState() {
		var emitterDone = Parameters.Lifetime > 0f && EmitterLifetime <= 0f;
		var spawnLimitDone = Parameters.MaxParticleCount > 0 && CreatedParticles >= Parameters.MaxParticleCount;
		if (emitterDone || spawnLimitDone) {
			Finished = _particles.Count == 0;
		}
	}

	private Vector3 MakeEmitterDirection(ParticleParameters parameters) {
		var damp = parameters.EmitterNozzleDamp;
		var degenerateDamp = damp.LengthSquared() == 0f;
		var direction = parameters.EmitterDirection;

		if (degenerateDamp) {
			return NormalizeOrForward(direction);
		}

		direction *= parameters.EmitterNozzleSize;
		direction.X += RandomSigned() * damp.X;
		direction.Y += RandomSigned() * damp.Y;
		direction.Z += RandomSigned() * damp.Z;
		return NormalizeOrForward(direction);
	}

	private float RandomSigned() => RandomFloat() * 2f - 1f;

	private float RandomFloat() => (float)_random.NextDouble();

	private static Vector3 NormalizeOrForward(Vector3 value) {
		return value.LengthSquared() > 0f ? Vector3.Normalize(value) : new Vector3(0f, 0f, 1f);
	}

	private static float Lerp(float a, float b, float t) => a + ((b - a) * t);

	private static string FormatNumber(float value) {
		return float.IsFinite(value)
			? value.ToString(MathF.Round(value) == value ? "0" : "0.####", CultureInfo.InvariantCulture)
			: "0";
	}

	private static ConquestFrontierWarsRay.Framework.Transform3D Transform3DFromMatrix(Matrix4x4 matrix) {
		return ConquestFrontierWarsRay.Framework.Transform3D.FromMatrix(matrix);
	}
}

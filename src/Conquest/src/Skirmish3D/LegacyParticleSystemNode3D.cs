using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using RaySharp.Particle;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class LegacyParticleSystemNode3D : Node3D {
	private readonly string _particlePath;
	private readonly float _simulationScale;
	private ParticlePreviewSystem? _system;
	private ParticleTextureResource? _texture;
	private float _updateTraceAccumulator;
	private float _drawTraceAccumulator;
	private bool _firstUpdateLogged;
	private bool _firstDrawAttemptLogged;
	private bool _firstDrawLogged;
	private bool _firstSkipLogged;

	public LegacyParticleSystemNode3D(string particlePath, string? name = null, float simulationScale = 1f) : base(name ?? "LegacyParticleSystemNode3D") {
		_particlePath = particlePath;
		_simulationScale = simulationScale;
	}

	public bool IsLoaded => _system is not null && _texture is not null;
	public string DebugSummary { get; private set; } = "Standalone particle not loaded";

	protected override void OnInitialize() {
		AppLog.Info(Name, $"Loading standalone particle '{_particlePath}'.");
		var result = ParticleParameterLoader.Load(_particlePath);
		var parameters = ScaleParticleParameters(result.Parameters, _simulationScale);
		ParticleColorFrameInterpolator.RecalculateInBetweenFrames(parameters);
		_texture = ParticleTextureLoader.Load(result.Textures, parameters.TextureName);
		_system = new ParticlePreviewSystem(parameters);
		_system.SetWorldTransform(GlobalTransform);
		DebugSummary =
			$"{Path.GetFileName(_particlePath)} tex={parameters.TextureName} init={parameters.InitialParticleCount:0.###} freq={parameters.Frequency:0.###} plife={parameters.ParticleLifetime:0.###}";
		AppLog.Info(Name,
			$"Standalone particle loaded: texture='{parameters.TextureName}', fallbackTexture={_texture.IsFallback}, initial={parameters.InitialParticleCount:0.###}, frequency={parameters.Frequency:0.###}, particleLifetime={parameters.ParticleLifetime:0.###}, size={parameters.ParticleSize:0.###}, velocity={parameters.ParticleVelocity:0.###}.");
	}

	protected override void OnUpdate(float deltaTime) {
		if (!_firstUpdateLogged) {
			_firstUpdateLogged = true;
			AppLog.Info(Name, $"OnUpdate reached. deltaTime={deltaTime:0.####}, loaded={IsLoaded}, globalPosition={GlobalPosition}.");
		}

		if (_system is null) {
			_updateTraceAccumulator += deltaTime;
			if (_updateTraceAccumulator >= 1f) {
				_updateTraceAccumulator = 0f;
				AppLog.Warning(Name, "OnUpdate running, but particle system is still null.");
			}
			return;
		}

		_system.SetWorldTransform(GlobalTransform);
		_system.Update(deltaTime);
		_updateTraceAccumulator += deltaTime;
		if (_updateTraceAccumulator >= 1f) {
			_updateTraceAccumulator = 0f;
			AppLog.Info(Name,
				$"OnUpdate tick: particles={_system.Particles.Count}, created={_system.CreatedParticles}, elapsed={_system.Elapsed:0.###}, sizeScale={_system.SizeScale:0.###}, origin={_system.Origin}.");
		}
	}

	protected override void OnDraw() {
		if (!_firstDrawAttemptLogged) {
			_firstDrawAttemptLogged = true;
			AppLog.Info(Name, $"OnDraw reached. loaded={IsLoaded}, globalPosition={GlobalPosition}.");
		}

		if (_system is null || _texture is null) {
			_drawTraceAccumulator += 1f / 60f;
			if (_drawTraceAccumulator >= 1f) {
				_drawTraceAccumulator = 0f;
				AppLog.Warning(Name, $"OnDraw running, but draw resources are missing. systemNull={_system is null}, textureNull={_texture is null}.");
			}
			return;
		}

		var camera = FindCamera();
		if (camera is null) {
			_drawTraceAccumulator += 1f / 60f;
			if (_drawTraceAccumulator >= 1f) {
				_drawTraceAccumulator = 0f;
				AppLog.Warning(Name, "OnDraw running, but no Camera3DNode was found.");
			}
			return;
		}

		if (!_texture.IsValid || _system.Particles.Count == 0) {
			if (!_firstSkipLogged) {
				_firstSkipLogged = true;
				AppLog.Warning(Name,
					$"Standalone particle skipped draw: textureValid={_texture.IsValid}, fallbackTexture={_texture.IsFallback}, particleCount={_system.Particles.Count}.");
			}

			_drawTraceAccumulator += 1f / 60f;
			if (_drawTraceAccumulator >= 1f) {
				_drawTraceAccumulator = 0f;
				AppLog.Warning(Name,
					$"OnDraw skip tick: textureValid={_texture.IsValid}, fallbackTexture={_texture.IsFallback}, particleCount={_system.Particles.Count}, cameraPos={camera.GlobalPosition}.");
			}

			return;
		}

		if (!_firstDrawLogged) {
			_firstDrawLogged = true;
			AppLog.Info(Name,
				$"Standalone particle rendered with {_system.Particles.Count} particles, sizeScale={_system.SizeScale:0.###}, worldOrigin={_system.Origin}.");
		}

		DrawParticleSystem(camera.ToRaylibCamera(), _system, _texture);
		_drawTraceAccumulator += 1f / 60f;
		if (_drawTraceAccumulator >= 1f) {
			_drawTraceAccumulator = 0f;
			AppLog.Info(Name,
				$"OnDraw tick: particles={_system.Particles.Count}, created={_system.CreatedParticles}, cameraPos={camera.GlobalPosition}, worldOrigin={_system.Origin}.");
		}
	}

	protected override void OnDispose() {
		_system?.Dispose();
		_texture?.Dispose();
		_system = null;
		_texture = null;
		base.OnDispose();
	}

	private Camera3DNode? FindCamera() {
		for (Node? current = Parent; current is not null; current = current.Parent) {
			for (var index = 0; index < current.Children.Count; index++) {
				if (current.Children[index] is Camera3DNode camera3DNode) {
					return camera3DNode;
				}
			}
		}

		return null;
	}

	private static void DrawParticleSystem(Camera3D camera, ParticlePreviewSystem system, ParticleTextureResource texture) {
		var cameraForward = Vector3.Normalize(camera.Target - camera.Position);
		Rlgl.DisableBackfaceCulling();
		Rlgl.DisableDepthMask();
		Raylib.BeginBlendMode(SelectParticleBlendMode(system.Parameters));
		foreach (var particle in system.Particles
			         .OrderByDescending(particle => Vector3.Dot(system.TransformPosition(particle.Position) - camera.Position, cameraForward))) {
			var frame = system.ParticleColor(particle);
			var size = system.TransformSize(particle.Size);
			if (size <= 0.01f) {
				continue;
			}

			var tint = new Color(
				ToByte(frame.R),
				ToByte(frame.G),
				ToByte(frame.B),
				(byte)255);
			Raylib.DrawBillboard(
				camera,
				texture.Texture,
				system.TransformPosition(particle.Position),
				Math.Max(0.01f, size),
				tint);
		}

		Raylib.EndBlendMode();
		Rlgl.EnableDepthMask();
		Rlgl.EnableBackfaceCulling();
	}

	private static BlendMode SelectParticleBlendMode(ParticleParameters parameters) {
		return parameters.SrcBlend == ParticleConstants.BlendSrcAlpha
		       && parameters.DstBlend == ParticleConstants.BlendInvSrcAlpha
			? BlendMode.Alpha
			: BlendMode.Additive;
	}

	private static ParticleParameters ScaleParticleParameters(ParticleParameters source, float scale) {
		if (Math.Abs(scale - 1f) <= 0.0001f) {
			return source;
		}

		var scaled = source.Clone();
		scaled.ParticlePositionRandomizer *= scale;
		scaled.ParticleVelocity *= scale;
		scaled.ParticleSize *= scale;
		scaled.ParticleSizeVelocity *= scale;
		scaled.BoundingSphereRadius *= scale;
		return scaled;
	}

	private static byte ToByte(float value) {
		return (byte)(Math.Clamp(value, 0f, 1f) * 255f);
	}
}

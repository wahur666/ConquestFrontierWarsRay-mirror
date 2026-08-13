using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Raylib_cs;

namespace RaySharp.Particle;

internal sealed record ParticleSceneSnapshot(
	DateTimeOffset CapturedAt,
	double RaylibTime,
	float DeltaTime,
	string Reason,
	string DisplayName,
	string Status,
	ParticleCameraSnapshot Camera,
	ParticleTextureSnapshot Texture,
	ParticleSystemSnapshot System,
	ParticleParameterSnapshot Parameters,
	ParticleSnapshot[] Particles) {
	public string ToJson() {
		return JsonSerializer.Serialize(this, JsonOptions);
	}

	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public static ParticleSceneSnapshot Capture(
		float deltaTime,
		string reason,
		string displayName,
		string status,
		ParticleOrbitCamera orbitCamera,
		Camera3D camera,
		ParticlePreviewSystem? system,
		ParticleTextureResource? textureResource) {
		Vector3 cameraForward = NormalizeOrZero(camera.Target - camera.Position);
		Vector3 cameraRight = NormalizeOrZero(Vector3.Cross(cameraForward, camera.Up));
		Vector3 cameraUp = NormalizeOrZero(Vector3.Cross(cameraRight, cameraForward));
		ParticleParameters? parameters = system?.Parameters;
		ParticleSnapshot[] particles = system?.Particles.Select((particle, index) => {
			Vector3 worldPosition = system.TransformPosition(particle.Position);
			Vector3 worldDirection = system.TransformDirection(particle.Direction);
			Vector3 billboardFacing = NormalizeOrZero(camera.Position - worldPosition);
			ParticleColorFrame color = system.ParticleColor(particle);
			return new ParticleSnapshot(
				index,
				Vector(worldPosition),
				Vector(worldDirection),
				Vector(billboardFacing),
				Vector(cameraForward),
				Vector(cameraRight),
				Vector(cameraUp),
				Vector3.Dot(billboardFacing, -cameraForward),
				system.TransformSize(particle.Size),
				particle.Velocity,
				particle.Age,
				particle.Lifetime,
				particle.InitialLifetime,
				new ParticleColorSnapshot(color.R, color.G, color.B, color.A));
		}).ToArray() ?? [];

		return new ParticleSceneSnapshot(
			DateTimeOffset.Now,
			Raylib.GetTime(),
			deltaTime,
			reason,
			displayName,
			status,
			new ParticleCameraSnapshot(
				Vector(camera.Position),
				Vector(camera.Target),
				Vector(camera.Up),
				Vector(cameraForward),
				Vector(cameraRight),
				Vector(cameraUp),
				orbitCamera.Yaw,
				orbitCamera.Pitch,
				orbitCamera.Distance,
				orbitCamera.NearPlane,
				orbitCamera.FarPlane),
			new ParticleTextureSnapshot(
				textureResource?.Name ?? "none",
				textureResource?.Texture.Id ?? 0,
				textureResource?.Texture.Width ?? 0,
				textureResource?.Texture.Height ?? 0,
				textureResource?.IsFallback ?? false,
				textureResource?.IsValid ?? false),
			new ParticleSystemSnapshot(
				system?.Particles.Count ?? 0,
				system?.MaxParticles ?? 0,
				system?.CreatedParticles ?? 0,
				system?.Elapsed ?? 0.0f,
				system?.EmitterLifetime ?? 0.0f,
				system?.Finished ?? false,
				system?.LifecycleText() ?? "No particle system"),
			parameters is null
				? ParticleParameterSnapshot.Empty
				: new ParticleParameterSnapshot(
					parameters.TextureName,
					parameters.Frequency,
					parameters.InitialParticleCount,
					parameters.MaxParticleCount,
					parameters.Lifetime,
					parameters.ParticleLifetime,
					parameters.ParticleSize,
					parameters.ParticleVelocity,
					parameters.ParticleVelocityRandomizer,
					parameters.ParticlePositionRandomizer,
					Vector(parameters.Gravity),
					Vector(parameters.EmitterDirection),
					Vector(parameters.EmitterNozzleDamp),
					parameters.EmitterNozzleSize,
					parameters.SrcBlend,
					parameters.DstBlend,
					parameters.PspFlags),
			particles);
	}

	private static ParticleVectorSnapshot Vector(Vector3 vector) {
		return new ParticleVectorSnapshot(vector.X, vector.Y, vector.Z);
	}

	private static Vector3 NormalizeOrZero(Vector3 vector) {
		return vector.LengthSquared() > 0.0f ? Vector3.Normalize(vector) : Vector3.Zero;
	}
}

internal sealed record ParticleCameraSnapshot(
	ParticleVectorSnapshot Position,
	ParticleVectorSnapshot Target,
	ParticleVectorSnapshot Up,
	ParticleVectorSnapshot Forward,
	ParticleVectorSnapshot Right,
	ParticleVectorSnapshot EffectiveUp,
	float Yaw,
	float Pitch,
	float Distance,
	float NearPlane,
	float FarPlane);

internal sealed record ParticleTextureSnapshot(
	string Name,
	uint TextureId,
	int Width,
	int Height,
	bool IsFallback,
	bool IsValid);

internal sealed record ParticleSystemSnapshot(
	int ActiveParticles,
	int MaxParticles,
	int CreatedParticles,
	float Elapsed,
	float EmitterLifetime,
	bool Finished,
	string Lifecycle);

internal sealed record ParticleParameterSnapshot(
	string TextureName,
	float Frequency,
	float InitialParticleCount,
	int MaxParticleCount,
	float Lifetime,
	float ParticleLifetime,
	float ParticleSize,
	float ParticleVelocity,
	float ParticleVelocityRandomizer,
	float ParticlePositionRandomizer,
	ParticleVectorSnapshot Gravity,
	ParticleVectorSnapshot EmitterDirection,
	ParticleVectorSnapshot EmitterNozzleDamp,
	float EmitterNozzleSize,
	uint SrcBlend,
	uint DstBlend,
	uint PspFlags) {
	public static ParticleParameterSnapshot Empty { get; } = new(
		string.Empty,
		0.0f,
		0.0f,
		0,
		0.0f,
		0.0f,
		0.0f,
		0.0f,
		0.0f,
		0.0f,
		new ParticleVectorSnapshot(0.0f, 0.0f, 0.0f),
		new ParticleVectorSnapshot(0.0f, 0.0f, 0.0f),
		new ParticleVectorSnapshot(0.0f, 0.0f, 0.0f),
		0.0f,
		0,
		0,
		0);
}

internal sealed record ParticleSnapshot(
	int Index,
	ParticleVectorSnapshot Position,
	ParticleVectorSnapshot Direction,
	ParticleVectorSnapshot BillboardFacingToCamera,
	ParticleVectorSnapshot CameraForward,
	ParticleVectorSnapshot CameraRight,
	ParticleVectorSnapshot CameraUp,
	float FacingDotCamera,
	float Size,
	float Velocity,
	float Age,
	float Lifetime,
	float InitialLifetime,
	ParticleColorSnapshot Color);

internal sealed record ParticleVectorSnapshot(float X, float Y, float Z);

internal sealed record ParticleColorSnapshot(float R, float G, float B, float A);

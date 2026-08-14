using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using RaySharp.Mesh;
using RaySharp.Particle;
using Raylib_cs;

namespace RaySharp.MeshViewer;

internal static class MeshViewerDiagnostics {
	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public static string Snapshot(
		string reason,
		string status,
		MeshUvMode uvMode,
		bool useDecodedTextureFlags,
		bool showWireframe,
		Vector3 particleOriginOffset,
		MeshOrbitCamera orbitCamera,
		Camera3D camera,
		IReadOnlyList<MeshViewerModel> models,
		int lastDrawnParticles,
		int lastSkippedParticles) {
		MeshViewerSnapshot snapshot = Capture(
			reason,
			status,
			uvMode,
			useDecodedTextureFlags,
			showWireframe,
			particleOriginOffset,
			orbitCamera,
			camera,
			models,
			lastDrawnParticles,
			lastSkippedParticles);

		string json = JsonSerializer.Serialize(snapshot, JsonOptions);
		string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "mesh-viewer-snapshots.log");
		Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
		File.AppendAllText(logPath, json + Environment.NewLine + Environment.NewLine);
		Console.WriteLine("[MeshViewerSnapshot]");
		Console.WriteLine(json);
		return logPath;
	}

	private static MeshViewerSnapshot Capture(
		string reason,
		string status,
		MeshUvMode uvMode,
		bool useDecodedTextureFlags,
		bool showWireframe,
		Vector3 particleOriginOffset,
		MeshOrbitCamera orbitCamera,
		Camera3D camera,
		IReadOnlyList<MeshViewerModel> models,
		int lastDrawnParticles,
		int lastSkippedParticles) {
		Vector3 cameraForward = NormalizeOrZero(camera.Target - camera.Position);
		return new MeshViewerSnapshot(
			DateTimeOffset.Now,
			Raylib.GetTime(),
			reason,
			status,
			uvMode.ToString(),
			useDecodedTextureFlags,
			showWireframe,
			Vector(particleOriginOffset),
			lastDrawnParticles,
			lastSkippedParticles,
			new MeshViewerCameraSnapshot(
				Vector(camera.Position),
				Vector(camera.Target),
				Vector(camera.Up),
				Vector(cameraForward),
				orbitCamera.Yaw,
				orbitCamera.Pitch,
				orbitCamera.Distance),
			models.Select(CaptureModel).ToArray());
	}

	private static MeshViewerModelSnapshot CaptureModel(MeshViewerModel model, int index) {
		return new MeshViewerModelSnapshot(
			index,
			model.Geometry.Name,
			model.Geometry.VertexCount,
			model.Geometry.FaceCount,
			model.Geometry.MaterialCount,
			model.Geometry.TextureCount,
			model.UploadedTextureCount,
			model.Geometry.Triangles.Count,
			Bounds(model.Bounds),
			Matrix(model.WorldMatrix),
			model.ParticleSystems.Select(CaptureParticleSystem).ToArray());
	}

	private static MeshViewerParticleSystemSnapshot CaptureParticleSystem(MeshViewerParticleSystem system, int index) {
		ParticleParameters parameters = system.System.Parameters;
		ParticleExtents extents = ComputeParticleExtents(system.System);
		bool anyFrameAlpha = parameters.ColorFrames.Any(frame => frame.A > 0.001f);
		bool anyFrameRgb = parameters.ColorFrames.Any(frame => frame.R > 0.001f || frame.G > 0.001f || frame.B > 0.001f);
		return new MeshViewerParticleSystemSnapshot(
			index,
			system.Name,
			system.FileName,
			system.UnifiedPath ?? string.Empty,
			system.SimulationScale,
			system.TextureResource.Name,
			system.TextureResource.Texture.Id,
			system.TextureResource.Texture.Width,
			system.TextureResource.Texture.Height,
			system.TextureResource.IsFallback,
			system.TextureResource.IsValid,
			Vector(system.WorldOrigin),
			Vector(system.System.Transform.Scale),
			Matrix(system.LocalTransform),
			system.System.Particles.Count,
			system.System.MaxParticles,
			system.System.CreatedParticles,
			system.System.EmitterLifetime,
			system.System.Elapsed,
			system.System.Finished,
			system.System.LifecycleText(),
			parameters.TextureName,
			parameters.Frequency,
			parameters.InitialParticleCount,
			parameters.MaxParticleCount,
			parameters.Lifetime,
			parameters.ParticleLifetime,
			parameters.ParticleSize,
			parameters.ParticleSizeVelocity,
			parameters.ParticleVelocity,
			parameters.ParticleVelocityRandomizer,
			parameters.ParticlePositionRandomizer,
			parameters.EmitterNozzleSize,
			Vector(parameters.EmitterDirection),
			Vector(parameters.EmitterNozzleDamp),
			Vector(parameters.Gravity),
			parameters.SrcBlend,
			parameters.DstBlend,
			parameters.PspFlags,
			anyFrameRgb,
			anyFrameAlpha,
			extents);
	}

	private static ParticleExtents ComputeParticleExtents(ParticlePreviewSystem system) {
		if (system.Particles.Count == 0) {
			return ParticleExtents.Empty;
		}

		Vector3 min = new(float.PositiveInfinity);
		Vector3 max = new(float.NegativeInfinity);
		float minSize = float.PositiveInfinity;
		float maxSize = float.NegativeInfinity;
		float minAge = float.PositiveInfinity;
		float maxAge = float.NegativeInfinity;
		foreach (ParticleInstance particle in system.Particles) {
			Vector3 position = system.TransformPosition(particle.Position);
			min = Vector3.Min(min, position);
			max = Vector3.Max(max, position);
			float size = system.TransformSize(particle.Size);
			minSize = Math.Min(minSize, size);
			maxSize = Math.Max(maxSize, size);
			minAge = Math.Min(minAge, particle.Age);
			maxAge = Math.Max(maxAge, particle.Age);
		}

		return new ParticleExtents(Vector(min), Vector(max), minSize, maxSize, minAge, maxAge);
	}

	private static Vector3 NormalizeOrZero(Vector3 vector) {
		return vector.LengthSquared() > 0.0f ? Vector3.Normalize(vector) : Vector3.Zero;
	}

	private static MeshViewerVectorSnapshot Vector(Vector3 vector) {
		return new MeshViewerVectorSnapshot(vector.X, vector.Y, vector.Z);
	}

	private static MeshViewerBoundsSnapshot Bounds(BoundingBox bounds) {
		return new MeshViewerBoundsSnapshot(Vector(bounds.Min), Vector(bounds.Max));
	}

	private static float[] Matrix(Matrix4x4 matrix) {
		return [
			matrix.M11, matrix.M12, matrix.M13, matrix.M14,
			matrix.M21, matrix.M22, matrix.M23, matrix.M24,
			matrix.M31, matrix.M32, matrix.M33, matrix.M34,
			matrix.M41, matrix.M42, matrix.M43, matrix.M44
		];
	}
}

internal sealed record MeshViewerSnapshot(
	DateTimeOffset CapturedAt,
	double RaylibTime,
	string Reason,
	string Status,
	string UvMode,
	bool UseDecodedTextureFlags,
	bool ShowWireframe,
	MeshViewerVectorSnapshot ParticleOriginOffset,
	int LastDrawnParticles,
	int LastSkippedParticles,
	MeshViewerCameraSnapshot Camera,
	MeshViewerModelSnapshot[] Models);

internal sealed record MeshViewerCameraSnapshot(
	MeshViewerVectorSnapshot Position,
	MeshViewerVectorSnapshot Target,
	MeshViewerVectorSnapshot Up,
	MeshViewerVectorSnapshot Forward,
	float Yaw,
	float Pitch,
	float Distance);

internal sealed record MeshViewerModelSnapshot(
	int Index,
	string Name,
	int VertexCount,
	int FaceCount,
	int MaterialCount,
	int TextureCount,
	int UploadedTextureCount,
	int TriangleCount,
	MeshViewerBoundsSnapshot Bounds,
	float[] WorldMatrix,
	MeshViewerParticleSystemSnapshot[] ParticleSystems);

internal sealed record MeshViewerParticleSystemSnapshot(
	int Index,
	string Name,
	string FileName,
	string UnifiedPath,
	float SimulationScale,
	string TextureName,
	uint TextureId,
	int TextureWidth,
	int TextureHeight,
	bool TextureFallback,
	bool TextureValid,
	MeshViewerVectorSnapshot WorldOrigin,
	MeshViewerVectorSnapshot TransformScale,
	float[] LocalTransform,
	int ActiveParticles,
	int MaxParticles,
	int CreatedParticles,
	float EmitterLifetime,
	float Elapsed,
	bool Finished,
	string Lifecycle,
	string ParameterTextureName,
	float Frequency,
	float InitialParticleCount,
	int MaxParticleCount,
	float EmitterLifetimeParameter,
	float ParticleLifetime,
	float ParticleSize,
	float ParticleSizeVelocity,
	float ParticleVelocity,
	float ParticleVelocityRandomizer,
	float ParticlePositionRandomizer,
	float EmitterNozzleSize,
	MeshViewerVectorSnapshot EmitterDirection,
	MeshViewerVectorSnapshot EmitterNozzleDamp,
	MeshViewerVectorSnapshot Gravity,
	uint SrcBlend,
	uint DstBlend,
	uint PspFlags,
	bool AnyColorFrameRgb,
	bool AnyColorFrameAlpha,
	ParticleExtents Extents);

internal sealed record ParticleExtents(
	MeshViewerVectorSnapshot Min,
	MeshViewerVectorSnapshot Max,
	float MinSize,
	float MaxSize,
	float MinAge,
	float MaxAge) {
	public static ParticleExtents Empty { get; } = new(
		new MeshViewerVectorSnapshot(0.0f, 0.0f, 0.0f),
		new MeshViewerVectorSnapshot(0.0f, 0.0f, 0.0f),
		0.0f,
		0.0f,
		0.0f,
		0.0f);
}

internal sealed record MeshViewerBoundsSnapshot(MeshViewerVectorSnapshot Min, MeshViewerVectorSnapshot Max);

internal sealed record MeshViewerVectorSnapshot(float X, float Y, float Z);

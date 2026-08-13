using System.Numerics;
using Raylib_cs;

namespace RaySharp.Particle;

internal sealed class ParticleRenderer {
	public int LastDrawnParticles { get; private set; }
	public int LastSkippedParticles { get; private set; }

	public void DrawWorld(Camera3D camera, ParticlePreviewSystem? system, ParticleTextureResource? textureResource) {
		DrawWorld(camera, system is null ? [] : [system], textureResource);
	}

	public void DrawWorld(Camera3D camera, IReadOnlyList<ParticlePreviewSystem> systems, ParticleTextureResource? textureResource) {
		LastDrawnParticles = 0;
		LastSkippedParticles = 0;
		Raylib.DrawGrid(20, 50.0f);
		DrawAxes();

		if (systems.Count == 0 || textureResource is null) {
			return;
		}

		BlendMode blendMode = SelectBlendMode(systems[0].Parameters);
		Vector3 cameraForward = Vector3.Normalize(camera.Target - camera.Position);
		Rlgl.DisableBackfaceCulling();
		Rlgl.DisableDepthMask();
		Raylib.BeginBlendMode(blendMode);
		foreach ((ParticlePreviewSystem system, ParticleInstance particle) in systems
			.SelectMany(system => system.Particles.Select(particle => (system, particle)))
			.OrderByDescending(entry => Vector3.Dot(entry.system.TransformPosition(entry.particle.Position) - camera.Position, cameraForward))) {
			ParticleColorFrame frame = system.ParticleColor(particle);
			float size = system.TransformSize(particle.Size);
			if (size <= 0.01f) {
				LastSkippedParticles++;
				continue;
			}

			Color tint = new(
				ToByte(frame.R),
				ToByte(frame.G),
				ToByte(frame.B),
				255);
			Raylib.DrawBillboard(camera, textureResource.Texture, system.TransformPosition(particle.Position), Math.Max(0.01f, size), tint);
			LastDrawnParticles++;
		}

		Raylib.EndBlendMode();
		Rlgl.EnableDepthMask();
		Rlgl.EnableBackfaceCulling();
	}

	private static BlendMode SelectBlendMode(ParticleParameters parameters) {
		if (parameters.SrcBlend == ParticleConstants.BlendSrcAlpha
			&& parameters.DstBlend == ParticleConstants.BlendInvSrcAlpha) {
			return BlendMode.Alpha;
		}

		return BlendMode.Additive;
	}

	private static void DrawAxes() {
		Raylib.DrawLine3D(Vector3.Zero, new Vector3(150.0f, 0.0f, 0.0f), new Color(230, 83, 83, 255));
		Raylib.DrawLine3D(Vector3.Zero, new Vector3(0.0f, 150.0f, 0.0f), new Color(106, 220, 126, 255));
		Raylib.DrawLine3D(Vector3.Zero, new Vector3(0.0f, 0.0f, 150.0f), new Color(94, 153, 255, 255));
	}

	private static int ToByte(float value) {
		return (int)(Math.Clamp(value, 0.0f, 1.0f) * 255.0f);
	}
}

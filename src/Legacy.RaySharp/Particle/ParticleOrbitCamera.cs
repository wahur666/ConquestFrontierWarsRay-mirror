using System.Numerics;
using Raylib_cs;

namespace RaySharp.Particle;

internal sealed class ParticleOrbitCamera {
	private const float MinInitialFrameDistance = 927.2475f;
	private const float MaxInitialFrameDistance = 2730.4463f;

	private Vector2 previousMouse;
	private bool dragging;
	private bool panning;

	public Vector3 Target { get; private set; }
	public float Yaw { get; private set; } = MathF.PI / 4.0f;
	public float Pitch { get; private set; } = MathF.PI / 6.0f;
	public float Distance { get; private set; } = 1000.0f;
	public float NearPlane { get; private set; } = 1.0f;
	public float FarPlane { get; private set; } = 100000.0f;

	public void Frame(ParticleParameters parameters, bool clampInitialDistance = false) {
		float radius = Math.Max(50.0f, parameters.BoundingSphereRadius != 0.0f
			? parameters.BoundingSphereRadius
			: parameters.ParticleSize != 0.0f
				? parameters.ParticleSize * 8.0f
				: 500.0f);
		Target = Vector3.Zero;
		Distance = radius * 2.2f;
		if (clampInitialDistance) {
			Distance = 1500f; // Math.Clamp(Distance, MinInitialFrameDistance, MaxInitialFrameDistance);
		}

		Yaw = MathF.PI / 4.0f;
		Pitch = MathF.PI / 6.0f;
		NearPlane = Math.Max(0.01f, Distance / 1000.0f);
		FarPlane = Math.Max(1000.0f, Distance * 100.0f);
	}

	public void Update(Rectangle viewport) {
		Vector2 mouse = Raylib.GetMousePosition();
		bool insideViewport = Raylib.CheckCollisionPointRec(mouse, viewport);

		if (insideViewport && Raylib.IsMouseButtonPressed(MouseButton.Left)) {
			dragging = true;
			panning = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
			previousMouse = mouse;
		}

		if (insideViewport && Raylib.IsMouseButtonPressed(MouseButton.Middle)) {
			dragging = true;
			panning = true;
			previousMouse = mouse;
		}

		if (Raylib.IsMouseButtonReleased(MouseButton.Left) || Raylib.IsMouseButtonReleased(MouseButton.Middle)) {
			dragging = false;
		}

		if (dragging) {
			Vector2 delta = mouse - previousMouse;
			previousMouse = mouse;
			if (panning) {
				Pan(delta, viewport);
			} else {
				Yaw -= delta.X * 0.006f;
				Pitch = Math.Clamp(Pitch - delta.Y * 0.006f, -MathF.PI / 2.0f + 0.05f, MathF.PI / 2.0f - 0.05f);
			}
		}

		if (insideViewport) {
			float wheel = Raylib.GetMouseWheelMove();
			if (wheel != 0.0f) {
				Distance = Math.Max(0.1f, Distance * MathF.Exp(-wheel * 0.12f));
				NearPlane = Math.Max(0.01f, Distance / 1000.0f);
				FarPlane = Math.Max(1000.0f, Distance * 100.0f);
			}
		}
	}

	public Camera3D ToCamera() {
		float cosPitch = MathF.Cos(Pitch);
		return new Camera3D {
			Position = new Vector3(
				Target.X + Distance * MathF.Sin(Yaw) * cosPitch,
				Target.Y + Distance * MathF.Sin(Pitch),
				Target.Z + Distance * MathF.Cos(Yaw) * cosPitch),
			Target = Target,
			Up = Vector3.UnitY,
			FovY = 60.0f,
			Projection = CameraProjection.Perspective
		};
	}

	private void Pan(Vector2 delta, Rectangle viewport) {
		Camera3D camera = ToCamera();
		Vector3 offset = camera.Position - Target;
		Vector3 right = Vector3.Normalize(Vector3.Cross(camera.Up, offset));
		Vector3 up = Vector3.Normalize(Vector3.Cross(offset, right));
		float scale = Distance / Math.Max(viewport.Height, 1.0f);
		Target += right * delta.X * scale;
		Target += up * delta.Y * scale;
	}
}

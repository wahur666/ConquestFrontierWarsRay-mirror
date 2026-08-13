using System.Numerics;
using Raylib_cs;

namespace RaySharp.Mesh;

internal sealed class MeshOrbitCamera {
	private Vector2 previousMouse;
	private bool dragging;
	private bool panning;

	public Vector3 Target { get; private set; }
	public float Yaw { get; private set; } = MathF.PI / 4.0f;
	public float Pitch { get; private set; } = MathF.PI / 6.0f;
	public float Distance { get; private set; } = 10.0f;

	public void Frame(BoundingBox bounds) {
		Vector3 size = bounds.Max - bounds.Min;
		Target = (bounds.Min + bounds.Max) * 0.5f;
		float maxSize = Math.Max(Math.Max(size.X, size.Y), Math.Max(size.Z, 1.0f));
		Distance = maxSize * 1.7f;
		Yaw = MathF.PI / 4.0f;
		Pitch = MathF.PI / 6.0f;
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

		if (!insideViewport) {
			return;
		}

		float wheel = Raylib.GetMouseWheelMove();
		if (wheel != 0.0f) {
			Distance = Math.Max(0.1f, Distance * MathF.Exp(-wheel * 0.12f));
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

using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class ThreeDScene : ShowcaseScene {
	private const float SidebarWidth = 360f;
	private const float KeyboardMoveSpeed = 7.5f;
	private static readonly Vector3 SceneCenter = Vector3.Zero;
	private const int GridHalfExtent = 10;
	private readonly Camera3DNode _camera = new("ShowcaseCamera") {
		FovY = 48f,
		ZNear = 0.1f,
		ZFar = 250f
	};
	private readonly RenderableCubeNode _root = new("WorldRoot", Color.SkyBlue, new Vector3(1.8f, 1.8f, 1.8f));
	private readonly RenderableCubeNode _child = new("ChildNode", Color.Lime, new Vector3(1.1f, 1.1f, 1.1f)) {
		Position = new Vector3(2.6f, 0f, 0f)
	};
	private readonly RenderableCubeNode _grandChild = new("GrandChildNode", Color.Orange, new Vector3(0.8f, 0.8f, 0.8f)) {
		Position = new Vector3(-5.2f, 0f, 0f)
	};
	private Vector3 _orbitTarget = SceneCenter;
	private float _cameraYaw = MathF.PI / 4f;
	private float _cameraPitch = -0.42f;
	private float _cameraDistance;
	private float _elapsed;
	private Vector2? _previousMouse;
	private DragMode _dragMode;

	public ThreeDScene() : base("ThreeDScene", "3D Camera Node") {
		AddChild(_camera);
		AddChild(_root);
		_root.AddChild(_child);
		_child.AddChild(_grandChild);
		ResetCamera();
	}

	protected override void OnUpdate(float deltaTime) {
		_elapsed += deltaTime;
		_root.Rotation = Quaternion.CreateFromYawPitchRoll(_elapsed * 0.4f, _elapsed * 0.3f, 0f);
		_child.Rotation = Quaternion.CreateFromYawPitchRoll(0f, _elapsed * 1.1f, _elapsed * 0.6f);
		_child.Scale = Vector3.One * (1f + (MathF.Sin(_elapsed * 1.2f) * 0.18f));
		_grandChild.Rotation = Quaternion.CreateFromYawPitchRoll(_elapsed * 2f, 0f, _elapsed * 0.9f);
		_grandChild.Scale = Vector3.One * (0.8f + (MathF.Sin(_elapsed * 1.8f) * 0.12f));

		_camera.ViewportOverride = new CameraViewport(
			SidebarWidth,
			0f,
			Math.Max(1f, Raylib.GetScreenWidth() - SidebarWidth),
			Math.Max(1f, Raylib.GetScreenHeight()));

		HandleKeyboardMovement(deltaTime);
		HandleCameraInput();

		if (Raylib.IsKeyPressed(KeyboardKey.R)) {
			ResetCamera();
		}

		RefreshCameraPose();
	}

	protected override void OnDraw() {
		_camera.BeginMode();
		DrawOriginGrid();
		DrawNodeRecursive(_root);
		Raylib.EndMode3D();

		UiText.Draw("Camera3D node", 402f, 44f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("The scene uses Framework.Camera3DNode for raylib Camera3D output plus EngineCameras-style projection, near-plane reconstruction, and visibility queries.", 402f, 78f, 17f, new Color(176, 190, 212, 255));
		UiText.Draw("Drag LMB to orbit, Shift+LMB or MMB to pan, wheel to zoom, WASD moves on X/Z, Q/E moves on Y, R reframes.", 402f, 120f, 17f, new Color(206, 216, 232, 255));

		DrawInfoChip($"Camera pos: {_camera.GlobalPosition.X:0.00}, {_camera.GlobalPosition.Y:0.00}, {_camera.GlobalPosition.Z:0.00}", 402f, 576f);
		DrawInfoChip($"Target: {_camera.Target.X:0.00}, {_camera.Target.Y:0.00}, {_camera.Target.Z:0.00}", 402f, 604f);
		DrawInfoChip($"Angles yaw/pitch: {RadiansToDegrees(_cameraYaw):0.0} / {RadiansToDegrees(_cameraPitch):0.0} deg", 402f, 632f);
		DrawInfoChip($"FOV Y/X: {_camera.FovY:0.0} / {_camera.FovX:0.0} deg   aspect {_camera.Aspect:0.00}", 402f, 660f);
		DrawInfoChip($"Blue root: {_camera.ObjectVisibility(_root.GlobalPosition, 1.6f)}", 402f, 688f);
		DrawInfoChip($"Green cube: {_camera.ObjectVisibility(_child.GlobalPosition, 1.3f)}", 402f, 716f);
		DrawInfoChip($"Orange cube: {_camera.ObjectVisibility(_grandChild.GlobalPosition, 1.0f)}", 402f, 744f);
	}

	private void HandleCameraInput() {
		var mouse = Raylib.GetMousePosition();
		var viewport = _camera.Viewport;
		var viewportRect = new Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height);
		var insideViewport = Raylib.CheckCollisionPointRec(mouse, viewportRect);

		if (Raylib.IsMouseButtonPressed(MouseButton.Left) && insideViewport) {
			_dragMode = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift)
				? DragMode.Pan
				: DragMode.Orbit;
			_previousMouse = mouse;
		} else if (Raylib.IsMouseButtonPressed(MouseButton.Middle) && insideViewport) {
			_dragMode = DragMode.Pan;
			_previousMouse = mouse;
		}

		if (Raylib.IsMouseButtonReleased(MouseButton.Left) || Raylib.IsMouseButtonReleased(MouseButton.Middle)) {
			_dragMode = DragMode.None;
			_previousMouse = null;
		}

		if (_dragMode != DragMode.None && _previousMouse is { } previousMouse) {
			var delta = mouse - previousMouse;
			_previousMouse = mouse;

			if (_dragMode == DragMode.Orbit) {
				_cameraYaw -= delta.X * 0.006f;
				_cameraPitch = Math.Clamp(_cameraPitch - delta.Y * 0.006f, -1.35f, 1.35f);
			} else {
				PanCamera(delta, viewport.Height);
			}
		}

		if (!insideViewport) {
			return;
		}

		var wheel = Raylib.GetMouseWheelMove();
		if (wheel != 0f) {
			_cameraDistance = Math.Clamp(_cameraDistance * MathF.Exp(-wheel * 0.12f), 2.5f, 60f);
		}
	}

	private void HandleKeyboardMovement(float deltaTime) {
		var movement = Vector3.Zero;
		var planarForward = Vector3.Normalize(new Vector3(_camera.Forward.X, 0f, _camera.Forward.Z));
		if (float.IsNaN(planarForward.X) || float.IsNaN(planarForward.Y) || float.IsNaN(planarForward.Z)) {
			planarForward = -Vector3.UnitZ;
		}

		var planarRight = Vector3.Normalize(new Vector3(_camera.RightDirection.X, 0f, _camera.RightDirection.Z));
		if (float.IsNaN(planarRight.X) || float.IsNaN(planarRight.Y) || float.IsNaN(planarRight.Z)) {
			planarRight = Vector3.UnitX;
		}

		if (Raylib.IsKeyDown(KeyboardKey.W)) {
			movement += planarForward;
		}

		if (Raylib.IsKeyDown(KeyboardKey.S)) {
			movement -= planarForward;
		}

		if (Raylib.IsKeyDown(KeyboardKey.D)) {
			movement += planarRight;
		}

		if (Raylib.IsKeyDown(KeyboardKey.A)) {
			movement -= planarRight;
		}

		if (Raylib.IsKeyDown(KeyboardKey.E)) {
			movement += Vector3.UnitY;
		}

		if (Raylib.IsKeyDown(KeyboardKey.Q)) {
			movement -= Vector3.UnitY;
		}

		if (movement.LengthSquared() <= 0.000001f) {
			return;
		}

		movement = Vector3.Normalize(movement) * (KeyboardMoveSpeed * deltaTime);
		_orbitTarget += movement;
	}

	private void RefreshCameraPose() {
		var cosPitch = MathF.Cos(_cameraPitch);
		var offset = new Vector3(
			_cameraDistance * MathF.Sin(_cameraYaw) * cosPitch,
			_cameraDistance * MathF.Sin(_cameraPitch),
			_cameraDistance * MathF.Cos(_cameraYaw) * cosPitch);

		_camera.Position = _orbitTarget + offset;
		_camera.LookAt(_orbitTarget);
	}

	private void ResetCamera() {
		_orbitTarget = SceneCenter;
		_camera.Position = new Vector3(0f, 8f, -8f);
		_camera.LookAt(Vector3.Zero);
		var offset = _camera.GlobalPosition - _orbitTarget;
		_cameraDistance = offset.Length();
		var planarDistance = MathF.Sqrt((offset.X * offset.X) + (offset.Z * offset.Z));
		_cameraYaw = MathF.Atan2(offset.X, offset.Z);
		_cameraPitch = MathF.Atan2(offset.Y, planarDistance);
	}

	private void PanCamera(Vector2 delta, float viewportHeight) {
		var right = _camera.RightDirection;
		var up = _camera.UpDirection;
		var scale = _cameraDistance / Math.Max(viewportHeight, 1f);
		_orbitTarget -= right * delta.X * scale;
		_orbitTarget += up * delta.Y * scale;
	}

	private static void DrawInfoChip(string text, float x, float y) {
		var size = UiText.MeasureSize(text, 17f, UiTextStyle.Body);
		var bounds = new Rectangle(x - 8f, y - 4f, size.X + 16f, size.Y + 8f);
		Raylib.DrawRectangleRec(bounds, new Color(10, 14, 22, 224));
		Raylib.DrawRectangleLinesEx(bounds, 1f, new Color(88, 108, 144, 255));
		UiText.Draw(text, x, y, 17f, new Color(218, 226, 238, 255), UiTextStyle.Body);
	}

	private static void DrawNodeRecursive(Node node) {
		if (node is RenderableCubeNode cubeNode) {
			cubeNode.DrawWorldGizmo();
		}

		foreach (var child in node.Children) {
			DrawNodeRecursive(child);
		}
	}

	private static void DrawOriginGrid() {
		var minor = new Color(58, 70, 92, 255);
		for (var i = -GridHalfExtent; i <= GridHalfExtent; i++) {
			var lineColor = i == 0 ? new Color(92, 108, 138, 255) : minor;
			Raylib.DrawLine3D(new Vector3(i, 0f, -GridHalfExtent), new Vector3(i, 0f, GridHalfExtent), lineColor);
			Raylib.DrawLine3D(new Vector3(-GridHalfExtent, 0f, i), new Vector3(GridHalfExtent, 0f, i), lineColor);
		}

		Raylib.DrawLine3D(Vector3.Zero, new Vector3(GridHalfExtent, 0f, 0f), Color.Red);
		Raylib.DrawLine3D(Vector3.Zero, new Vector3(0f, GridHalfExtent, 0f), Color.Green);
		Raylib.DrawLine3D(Vector3.Zero, new Vector3(0f, 0f, GridHalfExtent), Color.Blue);
	}

	private static float RadiansToDegrees(float radians) => radians * (180f / MathF.PI);

	private enum DragMode {
		None,
		Orbit,
		Pan
	}

	private sealed class RenderableCubeNode : Node3D {
		private readonly Color _tint;
		private readonly Vector3 _boxSize;

		public RenderableCubeNode(string name, Color tint, Vector3 boxSize) : base(name) {
			_tint = tint;
			_boxSize = boxSize;
		}

		public void DrawWorldGizmo() {
			Matrix4x4.Decompose(GlobalTransform, out var scale, out var rotation, out var translation);
			var corners = BuildCorners(GlobalTransform, _boxSize);
			DrawSolidCube(corners, new Color((int)_tint.R, (int)_tint.G, (int)_tint.B, 110));
			DrawWireCube(corners, _tint);

			var basisX = Vector3.Transform(Vector3.UnitX, rotation) * (1.15f * scale.X);
			var basisY = Vector3.Transform(Vector3.UnitY, rotation) * (1.15f * scale.Y);
			var basisZ = Vector3.Transform(Vector3.UnitZ, rotation) * (1.15f * scale.Z);

			Raylib.DrawLine3D(translation, translation + basisX, Color.Red);
			Raylib.DrawLine3D(translation, translation + basisY, Color.Green);
			Raylib.DrawLine3D(translation, translation + basisZ, Color.Blue);
		}

		private static Vector3[] BuildCorners(Matrix4x4 world, Vector3 localSize) {
			var half = localSize * 0.5f;
			return [
				Vector3.Transform(new Vector3(-half.X, -half.Y, -half.Z), world),
				Vector3.Transform(new Vector3( half.X, -half.Y, -half.Z), world),
				Vector3.Transform(new Vector3( half.X,  half.Y, -half.Z), world),
				Vector3.Transform(new Vector3(-half.X,  half.Y, -half.Z), world),
				Vector3.Transform(new Vector3(-half.X, -half.Y,  half.Z), world),
				Vector3.Transform(new Vector3( half.X, -half.Y,  half.Z), world),
				Vector3.Transform(new Vector3( half.X,  half.Y,  half.Z), world),
				Vector3.Transform(new Vector3(-half.X,  half.Y,  half.Z), world)
			];
		}

		private static void DrawSolidCube(IReadOnlyList<Vector3> corners, Color fill) {
			DrawQuad(corners, 0, 1, 2, 3, fill);
			DrawQuad(corners, 4, 5, 6, 7, fill);
			DrawQuad(corners, 0, 1, 5, 4, fill);
			DrawQuad(corners, 1, 2, 6, 5, fill);
			DrawQuad(corners, 2, 3, 7, 6, fill);
			DrawQuad(corners, 3, 0, 4, 7, fill);
		}

		private static void DrawWireCube(IReadOnlyList<Vector3> corners, Color color) {
			DrawEdge(corners, 0, 1, color);
			DrawEdge(corners, 1, 2, color);
			DrawEdge(corners, 2, 3, color);
			DrawEdge(corners, 3, 0, color);
			DrawEdge(corners, 4, 5, color);
			DrawEdge(corners, 5, 6, color);
			DrawEdge(corners, 6, 7, color);
			DrawEdge(corners, 7, 4, color);
			DrawEdge(corners, 0, 4, color);
			DrawEdge(corners, 1, 5, color);
			DrawEdge(corners, 2, 6, color);
			DrawEdge(corners, 3, 7, color);
		}

		private static void DrawQuad(IReadOnlyList<Vector3> corners, int a, int b, int c, int d, Color fill) {
			Raylib.DrawTriangle3D(corners[a], corners[b], corners[c], fill);
			Raylib.DrawTriangle3D(corners[a], corners[c], corners[d], fill);
		}

		private static void DrawEdge(IReadOnlyList<Vector3> corners, int startIndex, int endIndex, Color color) {
			Raylib.DrawLine3D(corners[startIndex], corners[endIndex], color);
		}
	}
}

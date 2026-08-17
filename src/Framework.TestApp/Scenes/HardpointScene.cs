using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class HardpointScene : ShowcaseScene {
	private const float SidebarWidth = 360f;
	private const float MarkerRadius = 0.18f;
	private readonly Camera3DNode _camera = new("HardpointCamera") {
		FovY = 48f,
		ZNear = 0.1f,
		ZFar = 250f
	};
	private readonly HullNode _hull = new("Hull", new Vector3(5.4f, 1.4f, 2.8f), new Color(92, 144, 206, 255));
	private readonly HullNode _turret = new("Turret", new Vector3(1.9f, 0.9f, 1.9f), new Color(116, 212, 146, 255)) {
		Position = new Vector3(0f, 1.25f, 0f)
	};
	private readonly HullNode _barrel = new("Barrel", new Vector3(0.55f, 0.55f, 3.4f), new Color(237, 174, 92, 255)) {
		Position = new Vector3(0f, 0.12f, 1.95f)
	};
	private readonly Hardpoint3D _hullTurretMount = new("hp_turret_mount", "mount") {
		Position = new Vector3(0f, 1.25f, 0f),
		JointType = HardpointJointType.Fixed
	};
	private readonly Hardpoint3D _turretBaseAnchor = new("hp_base_anchor", "mount") {
		JointType = HardpointJointType.Fixed
	};
	private readonly Hardpoint3D _turretGunPivot = new("hp_gun_pivot", "weapon") {
		Position = new Vector3(0f, 0.12f, 1.0f),
		JointType = HardpointJointType.Revolute,
		Axis = Vector3.UnitX,
		Min0 = -0.45f,
		Max0 = 0.7f
	};
	private readonly Hardpoint3D _barrelBreech = new("hp_breech", "weapon") {
		JointType = HardpointJointType.Revolute,
		Axis = Vector3.UnitX,
		Min0 = -0.45f,
		Max0 = 0.7f
	};
	private readonly Hardpoint3D _hullDock = new("hp_dock_a", "dock") {
		Position = new Vector3(-1.8f, 0.15f, -1.45f),
		JointType = HardpointJointType.Prismatic,
		Axis = Vector3.UnitZ,
		Min0 = -1.5f,
		Max0 = 3.25f,
		SpringConstant = 8f,
		DampingConstant = 1.5f,
		RestLength = 0.65f
	};
	private readonly Hardpoint3D _droneLatch = new("hp_drone_latch", "dock") {
		JointType = HardpointJointType.Prismatic,
		Axis = Vector3.UnitZ,
		Min0 = -1.5f,
		Max0 = 3.25f,
		SpringConstant = 8f,
		DampingConstant = 1.5f,
		RestLength = 0.65f
	};
	private readonly List<Hardpoint3D> _hardpoints = [];
	private Vector3 _orbitTarget = new(0f, 1f, 0f);
	private float _cameraYaw = MathF.PI / 4f;
	private float _cameraPitch = -0.36f;
	private float _cameraDistance = 16f;
	private float _elapsed;
	private Vector2? _previousMouse;
	private DragMode _dragMode;

	public HardpointScene() : base("HardpointScene", "Hardpoint / Socket3D") {
		AddChild(_camera);
		AddChild(_hull);
		_hull.AddChild(_turret);
		_turret.AddChild(_barrel);

		_hull.AddChild(_hullTurretMount);
		_turret.AddChild(_turretBaseAnchor);
		_turret.AddChild(_turretGunPivot);
		_barrel.AddChild(_barrelBreech);
		_hull.AddChild(_hullDock);
		_barrel.AddChild(_droneLatch);

		_hardpoints.AddRange([
			_hullTurretMount,
			_turretBaseAnchor,
			_turretGunPivot,
			_barrelBreech,
			_hullDock,
			_droneLatch
		]);
		ResetCamera();
	}

	protected override void OnUpdate(float deltaTime) {
		_elapsed += deltaTime;
		_turret.Rotation = Quaternion.CreateFromYawPitchRoll(_elapsed * 0.65f, 0f, 0f);
		_barrel.Rotation = Quaternion.CreateFromYawPitchRoll(0f, MathF.Sin(_elapsed * 1.1f) * 0.22f, 0f);

		_camera.ViewportOverride = new CameraViewport(
			SidebarWidth,
			0f,
			Math.Max(1f, Raylib.GetScreenWidth() - SidebarWidth),
			Math.Max(1f, Raylib.GetScreenHeight()));

		HandleCameraInput();
		if (Raylib.IsKeyPressed(KeyboardKey.R)) {
			ResetCamera();
		}

		RefreshCameraPose();
	}

	protected override void OnDraw() {
		_camera.BeginMode();
		DrawOriginGrid();
		_hull.DrawNodeBody();
		_turret.DrawNodeBody();
		_barrel.DrawNodeBody();
		DrawHardpointMarkers();
		Raylib.EndMode3D();

		DrawHardpointLabels();
		DrawOverlayText();
	}

	private void DrawHardpointMarkers() {
		foreach (var hardpoint in _hardpoints) {
			var color = GroupColor(hardpoint.GroupName);
			Raylib.DrawSphere(hardpoint.GlobalPosition, MarkerRadius, new Color((int)color.R, (int)color.G, (int)color.B, 220));
			Raylib.DrawSphereWires(hardpoint.GlobalPosition, MarkerRadius, 10, 10, new Color(18, 20, 26, 255));

			var basis = hardpoint.GlobalTransform3D;
			Raylib.DrawLine3D(hardpoint.GlobalPosition, basis.TransformPoint(Vector3.UnitX * 0.7f), Color.Red);
			Raylib.DrawLine3D(hardpoint.GlobalPosition, basis.TransformPoint(Vector3.UnitY * 0.7f), Color.Green);
			Raylib.DrawLine3D(hardpoint.GlobalPosition, basis.TransformPoint(Vector3.UnitZ * 0.7f), Color.Blue);
		}
	}

	private void DrawHardpointLabels() {
		foreach (var hardpoint in _hardpoints
			         .OrderByDescending(point => Vector3.DistanceSquared(point.GlobalPosition, _camera.GlobalPosition))) {
			if (!_camera.TryPointToScreen(hardpoint.GlobalPosition, out var screenX, out var screenY, out _)) {
				continue;
			}

			var screen = new Vector2(screenX, screenY);
			var text = $"{hardpoint.Name} [{hardpoint.GroupName}]";
			var size = UiText.MeasureSize(text, 14f, UiTextStyle.Body);
			var rect = new Rectangle(screen.X - (size.X * 0.5f) - 6f, screen.Y - 26f, size.X + 12f, size.Y + 6f);
			Raylib.DrawRectangleRec(rect, new Color(12, 15, 18, 220));
			Raylib.DrawRectangleLinesEx(rect, 1f, GroupColor(hardpoint.GroupName));
			UiText.Draw(text, rect.X + 6f, rect.Y + 3f, 14f, new Color(244, 247, 252, 255));
		}
	}

	private void DrawOverlayText() {
		UiText.Draw("Hardpoint3D + Socket3D", 402f, 44f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("This scene isolates native-style hardpoint semantics instead of treating them as markers. Each hardpoint is a real node with transform, group, joint type, axis, and optional limits.", 402f, 78f, 17f, new Color(176, 190, 212, 255));
		UiText.Draw("Drag LMB to orbit, Shift+LMB or MMB to pan, wheel to zoom, R reframes.", 402f, 132f, 17f, new Color(206, 216, 232, 255));

		var fixedConnection = _hullTurretMount.BuildConnectionTo(_turretBaseAnchor);
		var weaponConnection = _turretGunPivot.BuildConnectionTo(_barrelBreech);
		var dockConnection = _hullDock.BuildConnectionTo(_droneLatch);

		DrawInfoChip($"Sockets under hull: {_hullTurretMount.Parent?.Children.OfType<Socket3D>().Count() ?? 0}", 402f, 574f);
		DrawInfoChip($"Fixed connect: rel pos {FormatVector(fixedConnection.RelativePosition)} rel rot {FormatQuaternion(fixedConnection.RelativeOrientation)}", 402f, 604f);
		DrawInfoChip($"Revolute connect: axis {FormatVector(weaponConnection.Axis)} limits {weaponConnection.Min0:0.00}..{weaponConnection.Max0:0.00}", 402f, 634f);
		DrawInfoChip($"Prismatic connect: axis {FormatVector(dockConnection.Axis)} spring {_hullDock.SpringConstant:0.0} damp {_hullDock.DampingConstant:0.0} rest {_hullDock.RestLength:0.00}", 402f, 664f);
		DrawInfoChip($"World example: {_turretGunPivot.Name} -> {FormatVector(_turretGunPivot.GlobalPosition)}", 402f, 694f);
		DrawInfoChip("Groups: mount = blue, weapon = green, dock = orange", 402f, 724f);
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
			_cameraDistance = Math.Clamp(_cameraDistance * MathF.Exp(-wheel * 0.12f), 3.5f, 60f);
		}
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
		_orbitTarget = new Vector3(0f, 1.15f, 0f);
		_cameraDistance = 16f;
		_cameraYaw = MathF.PI / 4f;
		_cameraPitch = -0.36f;
		RefreshCameraPose();
	}

	private void PanCamera(Vector2 delta, float viewportHeight) {
		var right = _camera.RightDirection;
		var up = _camera.UpDirection;
		var scale = _cameraDistance / Math.Max(viewportHeight, 1f);
		_orbitTarget -= right * delta.X * scale;
		_orbitTarget += up * delta.Y * scale;
	}

	private static void DrawInfoChip(string text, float x, float y) {
		var size = UiText.MeasureSize(text, 16f, UiTextStyle.Body);
		var bounds = new Rectangle(x - 8f, y - 4f, size.X + 16f, size.Y + 8f);
		Raylib.DrawRectangleRec(bounds, new Color(10, 14, 22, 224));
		Raylib.DrawRectangleLinesEx(bounds, 1f, new Color(88, 108, 144, 255));
		UiText.Draw(text, x, y, 16f, new Color(218, 226, 238, 255), UiTextStyle.Body);
	}

	private static string FormatVector(Vector3 value) => $"({value.X:0.00},{value.Y:0.00},{value.Z:0.00})";

	private static string FormatQuaternion(Quaternion value) => $"({value.X:0.00},{value.Y:0.00},{value.Z:0.00},{value.W:0.00})";

	private static Color GroupColor(string groupName) {
		return groupName.ToLowerInvariant() switch {
			"weapon" => new Color(110, 214, 138, 255),
			"dock" => new Color(234, 174, 90, 255),
			_ => new Color(104, 164, 255, 255)
		};
	}

	private static void DrawOriginGrid() {
		const int halfExtent = 10;
		var minor = new Color(58, 70, 92, 255);
		for (var i = -halfExtent; i <= halfExtent; i++) {
			var color = i == 0 ? new Color(92, 108, 138, 255) : minor;
			Raylib.DrawLine3D(new Vector3(i, 0f, -halfExtent), new Vector3(i, 0f, halfExtent), color);
			Raylib.DrawLine3D(new Vector3(-halfExtent, 0f, i), new Vector3(halfExtent, 0f, i), color);
		}

		Raylib.DrawLine3D(Vector3.Zero, new Vector3(halfExtent, 0f, 0f), Color.Red);
		Raylib.DrawLine3D(Vector3.Zero, new Vector3(0f, halfExtent, 0f), Color.Green);
		Raylib.DrawLine3D(Vector3.Zero, new Vector3(0f, 0f, halfExtent), Color.Blue);
	}

	private enum DragMode {
		None,
		Orbit,
		Pan
	}

	private sealed class HullNode : Node3D {
		private readonly Vector3 _size;
		private readonly Color _color;

		public HullNode(string name, Vector3 size, Color color) : base(name) {
			_size = size;
			_color = color;
		}

		public void DrawNodeBody() {
			var corners = BuildCorners(GlobalTransform, _size);
			DrawSolidBox(corners, new Color((int)_color.R, (int)_color.G, (int)_color.B, 110));
			DrawWireBox(corners, _color);
		}

		private static Vector3[] BuildCorners(Matrix4x4 world, Vector3 localSize) {
			var half = localSize * 0.5f;
			return [
				Vector3.Transform(new Vector3(-half.X, -half.Y, -half.Z), world),
				Vector3.Transform(new Vector3(half.X, -half.Y, -half.Z), world),
				Vector3.Transform(new Vector3(half.X, half.Y, -half.Z), world),
				Vector3.Transform(new Vector3(-half.X, half.Y, -half.Z), world),
				Vector3.Transform(new Vector3(-half.X, -half.Y, half.Z), world),
				Vector3.Transform(new Vector3(half.X, -half.Y, half.Z), world),
				Vector3.Transform(new Vector3(half.X, half.Y, half.Z), world),
				Vector3.Transform(new Vector3(-half.X, half.Y, half.Z), world)
			];
		}

		private static void DrawSolidBox(IReadOnlyList<Vector3> corners, Color fill) {
			DrawQuad(corners, 0, 1, 2, 3, fill);
			DrawQuad(corners, 4, 5, 6, 7, fill);
			DrawQuad(corners, 0, 1, 5, 4, fill);
			DrawQuad(corners, 1, 2, 6, 5, fill);
			DrawQuad(corners, 2, 3, 7, 6, fill);
			DrawQuad(corners, 3, 0, 4, 7, fill);
		}

		private static void DrawWireBox(IReadOnlyList<Vector3> corners, Color color) {
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

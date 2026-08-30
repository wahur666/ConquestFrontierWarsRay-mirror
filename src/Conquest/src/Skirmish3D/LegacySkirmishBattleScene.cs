using System;
using System.IO;
using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using RaySharp.Particle;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class LegacySkirmishBattleScene : Node {
	private const float KeyboardMoveSpeed = 8f;
	private const int GridHalfExtent = 12;
	private readonly Func<Node> _returnSceneFactory;
	private readonly Camera3DNode _camera = new("SkirmishCamera") {
		FovY = 48f,
		ZNear = 0.1f,
		ZFar = 500f
	};
	private readonly LegacyMeshNode3D _meshNode;
	private readonly LegacyParticleSystemNode3D? _standaloneParticleNode;
	private Vector3 _orbitTarget = Vector3.Zero;
	private float _cameraYaw = MathF.PI / 4f;
	private float _cameraPitch = -0.42f;
	private float _cameraDistance = 12f;
	private Vector2? _previousMouse;
	private DragMode _dragMode;
	private bool _cameraFramed;
	private string _sampleName;

	public LegacySkirmishBattleScene(Func<Node> returnSceneFactory) : base("LegacySkirmishBattleScene") {
		_returnSceneFactory = returnSceneFactory;
		AddChild(_camera);
		var meshPath = LegacyMeshPathResolver.ResolveDefaultMeshPath();
		_sampleName = Path.GetFileName(meshPath);
		AppLog.Info(Name, $"Starting legacy skirmish preview with mesh '{meshPath}'.");
		if (!string.IsNullOrWhiteSpace(AppLog.FilePath)) {
			AppLog.Info(Name, $"Tracing to '{AppLog.FilePath}'.");
		}
		_meshNode = AddChild(new LegacyMeshNode3D(meshPath, "SkirmishPreviewMesh"));
		var standaloneParticlePath = ResolveStandaloneParticlePath();
		if (!string.IsNullOrWhiteSpace(standaloneParticlePath)) {
			_standaloneParticleNode = AddChild(new LegacyParticleSystemNode3D(standaloneParticlePath, "StandaloneParticlePreview"));
			_standaloneParticleNode.Position = new Vector3(4f, 1.2f, 0f);
			_standaloneParticleNode.Scale = new Vector3(0.01f, 0.01f, 0.01f);
			AppLog.Info(Name, $"Added standalone particle preview '{standaloneParticlePath}' at {_standaloneParticleNode.Position}.");
		} else {
			AppLog.Warning(Name, "Could not resolve a standalone particle preview sample.");
		}
	}

	protected override void OnUpdate(float deltaTime) {
		if (!_cameraFramed && _meshNode.IsLoaded) {
			FrameCamera();
		}

		HandleKeyboardMovement(deltaTime);
		HandleCameraInput();
		RefreshCameraPose();

		if (Raylib.IsKeyPressed(KeyboardKey.R) && _meshNode.IsLoaded) {
			FrameCamera();
		}

		if (Input.UiEsc || Raylib.IsKeyPressed(KeyboardKey.Escape)) {
			Tree.ChangeRoot(_returnSceneFactory());
		}
	}

	public override void DrawRecursive() {
		Raylib.ClearBackground(new Color(8, 12, 19, 255));
		_camera.BeginMode();
		DrawOriginGrid();
		foreach (var child in Children) {
			child.DrawRecursive();
		}
		_camera.EndMode();
		DrawOverlay();
	}

	private void FrameCamera() {
		var bounds = _meshNode.WorldBounds;
		_orbitTarget = (bounds.Min + bounds.Max) * 0.5f;
		var extent = bounds.Max - bounds.Min;
		var radius = Math.Max(1f, extent.Length() * 0.5f);
		_camera.FrameSphere(_orbitTarget, radius, _cameraYaw, _cameraPitch, 1.35f);
		var offset = _camera.GlobalPosition - _orbitTarget;
		_cameraDistance = Math.Max(2.5f, offset.Length());
		var planarDistance = MathF.Sqrt((offset.X * offset.X) + (offset.Z * offset.Z));
		_cameraYaw = MathF.Atan2(offset.X, offset.Z);
		_cameraPitch = MathF.Atan2(offset.Y, planarDistance);
		_cameraFramed = true;
	}

	private void HandleCameraInput() {
		var mouse = Raylib.GetMousePosition();

		if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
			_dragMode = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift)
				? DragMode.Pan
				: DragMode.Orbit;
			_previousMouse = mouse;
		} else if (Raylib.IsMouseButtonPressed(MouseButton.Middle)) {
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
				PanCamera(delta, Raylib.GetScreenHeight());
			}
		}

		var wheel = Raylib.GetMouseWheelMove();
		if (wheel != 0f) {
			_cameraDistance = Math.Clamp(_cameraDistance * MathF.Exp(-wheel * 0.12f), 2.5f, 100f);
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

	private void PanCamera(Vector2 delta, float viewportHeight) {
		var right = _camera.RightDirection;
		var up = _camera.UpDirection;
		var scale = _cameraDistance / Math.Max(viewportHeight, 1f);
		_orbitTarget -= right * delta.X * scale;
		_orbitTarget += up * delta.Y * scale;
	}

	private void DrawOverlay() {
		Raylib.DrawRectangle(16, 16, 680, 108, new Color(6, 10, 16, 210));
		Raylib.DrawRectangleLines(16, 16, 680, 108, new Color(78, 98, 128, 255));
		Raylib.DrawText("Skirmish Scene Preview", 28, 28, 24, Color.RayWhite);
		Raylib.DrawText(_sampleName, 28, 56, 18, new Color(173, 188, 210, 255));
		Raylib.DrawText(_meshNode.ParticleDebugSummary, 28, 80, 16, new Color(220, 210, 160, 255));
		if (_standaloneParticleNode is not null) {
			Raylib.DrawText(_standaloneParticleNode.DebugSummary, 28, 98, 14, new Color(132, 214, 188, 255));
		} else if (!string.IsNullOrWhiteSpace(AppLog.FilePath)) {
			Raylib.DrawText("Standalone particle sample not available", 28, 98, 14, new Color(190, 130, 130, 255));
		}
		if (!string.IsNullOrWhiteSpace(AppLog.FilePath)) {
			Raylib.DrawText(Path.GetFileName(AppLog.FilePath), 28, 114, 14, new Color(132, 150, 174, 255));
		}
		Raylib.DrawText("LMB orbit  MMB/Shift+LMB pan  Wheel zoom  WASD/QE move  R frame  Esc back", 16, Raylib.GetScreenHeight() - 28, 16, new Color(196, 208, 224, 255));
	}

	private static string? ResolveStandaloneParticlePath() {
		return ParticleSampleResolver.Resolve("redspray.pte.unified.xml")
			?? ParticleSampleResolver.Resolve("particle.pte.unified.xml")
			?? ParticleSampleResolver.Resolve("bluespray.pte.unified.xml");
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

	private enum DragMode {
		None,
		Orbit,
		Pan
	}
}

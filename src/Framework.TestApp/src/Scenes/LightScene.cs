using System.Numerics;
using System.Xml.Linq;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class LightScene : ShowcaseScene {
	private const float SidebarWidth = 360f;
	private readonly Camera3DNode _camera = new("LightCamera") {
		FovY = 48f,
		ZNear = 0.1f,
		ZFar = 250f
	};
	private readonly Node3D _lightRig = new("LightRig");
	private readonly Light3D _ambientLight;
	private readonly Light3D _directionalLight = new("DirectionalLight") {
		Type = Light3DType.Directional,
		Color = new Color(255, 244, 212, 255),
		Energy = 0.95f
	};
	private readonly Light3D _bluePointLight;
	private readonly Light3D _spotLight = new("SpotLight") {
		Type = Light3DType.Spot,
		Color = new Color(255, 192, 108, 255),
		Energy = 1.2f,
		Range = 11f,
		Attenuation = 0.16f,
		InnerSpotAngleDegrees = 22f,
		OuterSpotAngleDegrees = 36f,
		Position = new Vector3(0f, 6f, -2f)
	};
	private readonly LightShader3D _shader = new(maxLights: 8);
	private Vector3 _orbitTarget = new(0f, 1.2f, 0f);
	private float _cameraYaw = MathF.PI / 4f;
	private float _cameraPitch = -0.38f;
	private float _cameraDistance = 18f;
	private float _elapsed;
	private Vector2? _previousMouse;
	private DragMode _dragMode;

	public LightScene() : base("LightScene", "Light3D") {
		var xmlAmbient = LoadXmlLightDefinition("LIGHT!!Ambient.xml");
		_ambientLight = new Light3D("XmlAmbientLight") {
			Ambient = xmlAmbient.Ambient,
			Type = xmlAmbient.Type,
			Color = xmlAmbient.Color,
			Range = xmlAmbient.Range,
			InnerSpotAngleDegrees = xmlAmbient.InnerSpotAngleDegrees,
			OuterSpotAngleDegrees = xmlAmbient.OuterSpotAngleDegrees,
			Energy = 1f
		};

		var xmlLight = LoadXmlLightDefinition("LIGHT!!Blue.xml");
		_bluePointLight = new Light3D("XmlBluePointLight") {
			Type = xmlLight.Type,
			Color = xmlLight.Color,
			Range = xmlLight.Range,
			InnerSpotAngleDegrees = xmlLight.InnerSpotAngleDegrees,
			OuterSpotAngleDegrees = xmlLight.OuterSpotAngleDegrees,
			Energy = 1f,
			Attenuation = 0.3f,
			Position = new Vector3(-4f, 2.2f, -1.5f)
		};

		AddChild(_camera);
		AddChild(_lightRig);
		_lightRig.AddChild(_ambientLight);
		_lightRig.AddChild(_directionalLight);
		_lightRig.AddChild(_bluePointLight);
		_lightRig.AddChild(_spotLight);

		_directionalLight.Rotation = Quaternion.CreateFromYawPitchRoll(-0.7f, -0.95f, 0f);
		ResetCamera();
	}

	protected override void OnUpdate(float deltaTime) {
		_elapsed += deltaTime;
		_camera.ViewportOverride = new CameraViewport(
			SidebarWidth,
			0f,
			Math.Max(1f, Raylib.GetScreenWidth() - SidebarWidth),
			Math.Max(1f, Raylib.GetScreenHeight()));

		_bluePointLight.Position = new Vector3(
			-4f + (MathF.Cos(_elapsed * 0.7f) * 1.3f),
			2.2f + (MathF.Sin(_elapsed * 1.4f) * 0.35f),
			-1.5f + (MathF.Sin(_elapsed * 0.7f) * 1.1f));

		var spotTarget = new Vector3(
			MathF.Cos(_elapsed * 0.8f) * 3.2f,
			0.8f,
			MathF.Sin(_elapsed * 0.8f) * 2.6f);
		_spotLight.Rotation = LookRotation(_spotLight.GlobalPosition, spotTarget, Vector3.UnitY);

		if (Raylib.IsKeyPressed(KeyboardKey.One)) {
			_ambientLight.Enabled = !_ambientLight.Enabled;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Two)) {
			_directionalLight.Enabled = !_directionalLight.Enabled;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Three)) {
			_bluePointLight.Enabled = !_bluePointLight.Enabled;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Four)) {
			_spotLight.Enabled = !_spotLight.Enabled;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.R)) {
			ResetCamera();
		}

		HandleCameraInput();
		RefreshCameraPose();
	}

	protected override void OnDraw() {
		var lights = new[] {
			_ambientLight.ToLightState(),
			_directionalLight.ToLightState(),
			_bluePointLight.ToLightState(),
			_spotLight.ToLightState()
		};

		_camera.BeginMode();
		_shader.Begin(_camera.GlobalPosition, lights);
		DrawLitSceneGeometry();
		_shader.End();
		DrawLightMarkers(lights);
		Raylib.EndMode3D();

		DrawOverlay(lights);
	}

	protected override void OnDispose() {
		_shader.Dispose();
	}

	private void DrawLitSceneGeometry() {
		DrawOriginGrid();
		Raylib.DrawPlane(Vector3.Zero, new Vector2(14f, 14f), Color.White);
		Raylib.DrawCube(new Vector3(0f, 1.4f, 0f), 2.4f, 2.8f, 2.4f, new Color(222, 226, 235, 255));
		Raylib.DrawCube(new Vector3(-3.3f, 0.8f, 2.6f), 1.4f, 1.6f, 1.4f, new Color(214, 194, 150, 255));
		Raylib.DrawCube(new Vector3(3.4f, 1.15f, -2.1f), 1.9f, 2.3f, 1.9f, new Color(170, 196, 222, 255));
		Raylib.DrawSphere(new Vector3(2.1f, 1.2f, 2.2f), 1.2f, new Color(224, 224, 216, 255));
		Raylib.DrawCylinder(new Vector3(-1.2f, 1f, -3.4f), 0.9f, 0.9f, 2f, 24, new Color(184, 208, 182, 255));
	}

	private static void DrawLightMarkers(IReadOnlyList<Light3DState> lights) {
		foreach (var light in lights) {
			if (light.Type == Light3DType.Directional) {
				var start = new Vector3(4.5f, 5.5f, 4.5f);
				var end = start + (light.Direction * 2.4f);
				var markerColor = light.Enabled ? light.Color : new Color((int)light.Color.R, (int)light.Color.G, (int)light.Color.B, 90);
				Raylib.DrawLine3D(start, end, markerColor);
				Raylib.DrawSphere(start, 0.16f, markerColor);
				continue;
			}

			var color = light.Enabled ? light.Color : new Color((int)light.Color.R, (int)light.Color.G, (int)light.Color.B, 70);
			Raylib.DrawSphere(light.Position, 0.24f, color);
			Raylib.DrawSphereWires(light.Position, 0.24f, 10, 10, new Color(20, 24, 30, 255));
			Raylib.DrawLine3D(light.Position, light.Target, color);

			if (light.Type == Light3DType.Spot) {
				var coneEnd = light.Position + (light.Direction * 2.2f);
				Raylib.DrawSphere(coneEnd, 0.08f, color);
			}
		}
	}

	private void DrawOverlay(IReadOnlyList<Light3DState> lights) {
		UiText.Draw("Light3D showcase", 402f, 44f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("This scene uses a local shader fed by framework Light3D nodes, including a BT_LIGHT xml-driven ambient light that uploads through raylib shader uniforms before the dynamic light pass.", 402f, 78f, 17f, new Color(176, 190, 212, 255));
		UiText.Draw("Keys: [1] ambient on/off, [2] directional on/off, [3] blue xml point on/off, [4] spot on/off, mouse orbit/pan/zoom, [R] reframe.", 402f, 120f, 17f, new Color(206, 216, 232, 255));

		DrawInfoChip($"Ambient XML: LIGHT!!Ambient.xml -> {(lights[0].Enabled ? "on" : "off")} color {FormatColor(lights[0].Color)}", 402f, 586f);
		DrawInfoChip($"Directional: {(lights[1].Enabled ? "on" : "off")} dir {FormatVector(lights[1].Direction)}", 402f, 616f);
		DrawInfoChip($"Blue point XML: LIGHT!!Blue.xml -> {(lights[2].Enabled ? "on" : "off")} pos {FormatVector(lights[2].Position)} range {lights[2].Range:0.0}", 402f, 646f);
		DrawInfoChip($"Spot: {(lights[3].Enabled ? "on" : "off")} outer {lights[3].OuterSpotAngleDegrees:0.0} deg target {FormatVector(lights[3].Target)}", 402f, 676f);
		DrawInfoChip("Ambient is accumulated into the shader baseline, while directional/point/spot lights stay in the dynamic light array.", 402f, 706f);
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
			_cameraDistance = Math.Clamp(_cameraDistance * MathF.Exp(-wheel * 0.12f), 4f, 60f);
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
		_orbitTarget = new Vector3(0f, 1.2f, 0f);
		_cameraDistance = 18f;
		_cameraYaw = MathF.PI / 4f;
		_cameraPitch = -0.38f;
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

	private static string FormatColor(Color color) => $"({color.R},{color.G},{color.B})";

	private static void DrawOriginGrid() {
		const int halfExtent = 10;
		var minor = new Color(58, 70, 92, 255);
		for (var i = -halfExtent; i <= halfExtent; i++) {
			Color lineColor = i == 0 ? new Color(92, 108, 138, 255) : minor;
			Raylib.DrawLine3D(new Vector3(i, 0f, -halfExtent), new Vector3(i, 0f, halfExtent), lineColor);
			Raylib.DrawLine3D(new Vector3(-halfExtent, 0f, i), new Vector3(halfExtent, 0f, i), lineColor);
		}

		Raylib.DrawLine3D(Vector3.Zero, new Vector3(halfExtent, 0f, 0f), Color.Red);
		Raylib.DrawLine3D(Vector3.Zero, new Vector3(0f, halfExtent, 0f), Color.Green);
		Raylib.DrawLine3D(Vector3.Zero, new Vector3(0f, 0f, halfExtent), Color.Blue);
	}

	private static Quaternion LookRotation(Vector3 position, Vector3 target, Vector3 up) {
		var direction = target - position;
		if (direction.LengthSquared() <= 0.000001f) {
			return Quaternion.Identity;
		}

		var yaw = MathF.Atan2(-direction.X, -direction.Z);
		var planar = MathF.Sqrt((direction.X * direction.X) + (direction.Z * direction.Z));
		var pitch = MathF.Atan2(direction.Y, planar);
		return Quaternion.CreateFromYawPitchRoll(yaw, pitch, 0f);
	}

	private static XmlLightDefinition LoadXmlLightDefinition(string fileName) {
		var path = ResolveBtLightXmlPath(fileName);
		var document = XDocument.Load(path);
		var root = document.Root ?? throw new InvalidDataException("Missing BT_LIGHT root.");
		var colorNode = root.Element("color") ?? throw new InvalidDataException("Missing 'color' element.");
		var directionNode = root.Element("direction") ?? throw new InvalidDataException("Missing 'direction' element.");

		var color = new Color(
			(byte)ReadInt(colorNode, "red"),
			(byte)ReadInt(colorNode, "green"),
			(byte)ReadInt(colorNode, "blue"),
			(byte)255);
		var range = ReadSingle(root, "range");
		var direction = ReadVector3(directionNode);
		var cutoff = ReadSingle(root, "cutoff");
		var infinite = ReadBool(root, "infinite");
		var ambient = ReadBool(root, "ambient");

		var type = infinite
			? Light3DType.Directional
			: cutoff > 0f
				? Light3DType.Spot
				: Light3DType.Point;

		return new XmlLightDefinition(
			type,
			color,
			range,
			direction,
			cutoff * (180f / MathF.PI),
			Math.Max(cutoff * (180f / MathF.PI), 0f),
			ambient);
	}

	private static string ResolveBtLightXmlPath(string fileName) {
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null) {
			var outputAssetsPath = Path.Combine(
				current.FullName,
				"Assets",
				"DB",
				"xml",
				"GameTypes.db",
				"BT_LIGHT",
				fileName);
			if (File.Exists(outputAssetsPath)) {
				return outputAssetsPath;
			}

			var repoAssetsPath = Path.Combine(
				current.FullName,
				"assets",
				"DB",
				"xml",
				"GameTypes.db",
				"BT_LIGHT",
				fileName);
			if (File.Exists(repoAssetsPath)) {
				return repoAssetsPath;
			}

			current = current.Parent;
		}

		throw new FileNotFoundException(
			$"Could not locate BT light xml '{fileName}' from '{AppContext.BaseDirectory}'.",
			fileName);
	}

	private static int ReadInt(XElement parent, string name) {
		var child = parent.Element(name) ?? throw new InvalidDataException($"Missing '{name}' element.");
		return int.Parse(child.Value.Trim());
	}

	private static float ReadSingle(XElement parent, string name) {
		var child = parent.Element(name) ?? throw new InvalidDataException($"Missing '{name}' element.");
		return float.Parse(child.Value.Trim(), System.Globalization.CultureInfo.InvariantCulture);
	}

	private static bool ReadBool(XElement parent, string name) {
		var child = parent.Element(name) ?? throw new InvalidDataException($"Missing '{name}' element.");
		return bool.Parse(child.Value.Trim());
	}

	private static Vector3 ReadVector3(XElement? element) {
		if (element is null) {
			return Vector3.Zero;
		}

		return new Vector3(
			float.Parse((element.Element("x")?.Value ?? "0").Trim(), System.Globalization.CultureInfo.InvariantCulture),
			float.Parse((element.Element("y")?.Value ?? "0").Trim(), System.Globalization.CultureInfo.InvariantCulture),
			float.Parse((element.Element("z")?.Value ?? "0").Trim(), System.Globalization.CultureInfo.InvariantCulture));
	}

	private enum DragMode {
		None,
		Orbit,
		Pan
	}

	private readonly record struct XmlLightDefinition(
		Light3DType Type,
		Color Color,
		float Range,
		Vector3 Direction,
		float InnerSpotAngleDegrees,
		float OuterSpotAngleDegrees,
		bool Ambient);
}

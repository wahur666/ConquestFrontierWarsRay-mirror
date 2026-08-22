using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using ConquestFrontierWarsRay.Framework.TestApp.Scenes;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp;

internal sealed class ShowcaseShellNode : Node {
	private readonly MenuNavigation _navigation = new();
	private readonly MenuList _sceneMenu = new();
	private readonly PauseDialog _pauseDialog = new("Framework Showcase");
	private readonly Node _sceneHost = new("SceneHost");
	private readonly PanelNode _sidebarPanel = new("Sidebar") {
		Position = new Vector2(20f, 20f),
		Size = new Vector2(320f, 680f),
		Fill = new Color(20, 26, 40, 248),
		Outline = new Color(104, 126, 162, 255),
		OutlineThickness = 2f
	};
	private readonly SidebarOverlayNode _sidebarOverlay;
	private readonly List<SceneDefinition> _scenes = [];
	private ShowcaseScene? _currentScene;
	private string[] _summaryLines = [];

	public ShowcaseShellNode() : base("FrameworkShowcaseRoot") {
		_sidebarOverlay = new SidebarOverlayNode(this);
		AddChild(_sceneHost);
		AddChild(_sidebarPanel);
		AddChild(_sidebarOverlay);
	}

	protected override void OnInitialize() {
		_scenes.AddRange([
			new SceneDefinition("Hierarchy / 2D", "Node, CanvasItem, and Node2D composition with transform chaining and draw ordering.", () => new HierarchyScene()),
			new SceneDefinition("Controls", "Control, PanelNode, TextNode, and ButtonNode wired into live interactions.", () => new ControlsScene()),
			new SceneDefinition("UiText Animation", "UiText as a real Control with measured bounds, sine-wave scaling, circular motion, and pivot rotation.", () => new UiTextAnimationScene()),
			new SceneDefinition("Hot Rect Events", "Core.UI spike: UiEventSource dispatches raw pointer input into HotRectNode targets and bubbles events upward through parent nodes.", () => new HotRectEventScene()),
			new SceneDefinition("Audio Player", "AudioPlayer with mp3 and wav resources, transport controls, and slider-driven seek, volume, and pan.", () => new AudioPlayerScene()),
			new SceneDefinition("Legacy ListBox", "Core.UI legacy listbox seam with authored GT geometry, imperative item payloads, caret/selection behavior, and shape-backed or primitive drawing.", () => new LegacyListBoxScene()),
			new SceneDefinition("Talking Head", "Game-level TalkingHeadPlayer composite built from Sprite nodes, AudioPlayer transport, txt timing data, additive fuzz, and legacy border layout rules.", () => new TalkingHeadScene()),
			new SceneDefinition("Video Player", "VideoPlayer extracted from the legacy runtime into the framework as a standalone Control node with Media Foundation decoding.", () => new VideoPlayerScene()),
			new SceneDefinition("Sprites", "CompressedTexture2D, AtlasDefinitionResource, AtlasTexture, and Sprite in one resource-driven scene.", () => new SpriteScene()),
			new SceneDefinition("3D Camera Node", "Camera3DNode driving a raylib camera with EngineCameras-style projection helpers, visibility checks, and interactive orbit/pan/zoom controls.", () => new ThreeDScene()),
			new SceneDefinition("Light3D", "Framework Light3D nodes feeding a local raylib shader with directional, point, and spot lights. Includes xml-driven BT_LIGHT source data.", () => new LightScene()),
			new SceneDefinition("Hardpoints", "Socket3D and Hardpoint3D isolated as real attachment nodes with native-style joint metadata and connection math.", () => new HardpointScene())
		]);

		for (var i = 0; i < _scenes.Count; i++) {
			var sceneIndex = i;
			_sceneMenu.AddItem(_scenes[i].Title, () => SwitchScene(sceneIndex));
		}

		_pauseDialog.AddResumeItem();
		_pauseDialog.AddItem("Print tree", () => DebugTreeView.Print(this));
		_pauseDialog.AddItem("Swap root", SwapRoot);
		_pauseDialog.AddItem("Quit", RequestQuit);
		SwitchScene(0);
	}

	protected override void OnUpdate(float deltaTime) {
		_navigation.Update(Input);

		if (_navigation.ExitComboPressed || Input.IsExitRequested()) {
			RequestQuit();
			return;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Tab)) {
			SwapRoot();
			return;
		}

		if (_pauseDialog.IsOpen) {
			_pauseDialog.HandleInput(_navigation);
			return;
		}

		if (Input.UiEsc) {
			_pauseDialog.Open();
			return;
		}

		if (_currentScene?.HandleNavigation(_navigation) == true) {
			return;
		}

		_sceneMenu.HandleInput(_navigation);
	}

	protected override void OnDraw() {
		Raylib.ClearBackground(new Color(8, 12, 20, 255));
		_pauseDialog.Draw();
	}

	private void SwitchScene(int sceneIndex) {
		if (sceneIndex < 0 || sceneIndex >= _scenes.Count) {
			return;
		}

		if (_currentScene is not null) {
			_sceneHost.RemoveChild(_currentScene);
			_currentScene.Dispose();
		}

		_currentScene = _scenes[sceneIndex].Factory();
		_sceneHost.AddChild(_currentScene);
		_summaryLines = WrapText(_scenes[sceneIndex].Summary, 260f, 16f).ToArray();
	}

	private void SwapRoot() {
		Tree.ChangeRoot(new RootSwapDemoNode());
	}

	private void DrawSidebarText() {
		UiText.Draw("Scenes", 42f, 128f, 22f, new Color(204, 214, 230, 255));
		_sceneMenu.Draw(180f, 168f, 36f, 21f);

		UiText.Draw("Current", 42f, 336f, 22f, new Color(222, 230, 242, 255));
		if (_currentScene is not null) {
			UiText.Draw(_currentScene.Title, 42f, 372f, 22f, new Color(255, 213, 92, 255));
		}

		for (var i = 0; i < _summaryLines.Length; i++) {
			UiText.Draw(_summaryLines[i], 42f, 410f + (i * 22f), 17f, new Color(206, 216, 232, 255));
		}

		var footerY = 572f;
		UiText.Draw("Keys", 42f, footerY, 22f, new Color(222, 230, 242, 255));
		UiText.Draw("Arrow keys   navigate menu", 42f, footerY + 34f, 17f, new Color(214, 223, 236, 255));
		UiText.Draw("Enter        open scene", 42f, footerY + 58f, 17f, new Color(214, 223, 236, 255));
		UiText.Draw("Escape       pause overlay", 42f, footerY + 82f, 17f, new Color(214, 223, 236, 255));
		UiText.Draw("Tab          swap SceneTree root", 42f, footerY + 106f, 17f, new Color(214, 223, 236, 255));
		UiText.Draw("Backspace    quit combo alias", 42f, footerY + 130f, 17f, new Color(214, 223, 236, 255));
	}

	private static IEnumerable<string> WrapText(string text, float maxWidth, float fontSize) {
		if (string.IsNullOrWhiteSpace(text)) {
			yield break;
		}

		var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var current = string.Empty;
		foreach (var word in words) {
			var candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
			if (UiText.MeasureWidth(candidate, fontSize) <= maxWidth) {
				current = candidate;
				continue;
			}

			if (!string.IsNullOrEmpty(current)) {
				yield return current;
			}

			current = word;
		}

		if (!string.IsNullOrEmpty(current)) {
			yield return current;
		}
	}

	private sealed class SidebarOverlayNode : Node2D {
		private readonly ShowcaseShellNode _owner;

		public SidebarOverlayNode(ShowcaseShellNode owner) : base("SidebarOverlay") {
			_owner = owner;
		}

		protected override void Draw() {
			UiText.Draw("Framework Test App", 42f, 38f, 34f, new Color(244, 247, 252, 255), UiTextStyle.Title);
			UiText.Draw("MenuList + SceneTree runtime showcase", 42f, 80f, 18f, new Color(196, 209, 228, 255));
			_owner.DrawSidebarText();
		}
	}

	private sealed record SceneDefinition(string Title, string Summary, Func<ShowcaseScene> Factory);
}

using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class HotRectEventScene : ShowcaseScene {
	private const int MaxLogEntries = 12;
	private readonly PanelNode _panel = new("EventPanel") {
		Position = new Vector2(380f, 28f),
		Size = new Vector2(860f, 664f),
		Fill = new Color(17, 24, 38, 255),
		Outline = new Color(83, 105, 140, 255),
		OutlineThickness = 2f
	};
	private readonly CompressedTexture2D _texture;
	private readonly AtlasDefinitionResource _atlasDefinition;
	private readonly SpriteFrames _frames;
	private readonly Sprite _sprite;
	private readonly UiEventSource _eventSource = new("SceneEventSource");
	private readonly HotRectNode _outerRect = new("OuterHotRect") {
		Label = "Outer hot rect",
		Position = new Vector2(520f, 168f),
		Size = new Vector2(460f, 278f),
		Fill = new Color(28, 54, 96, 76)
	};
	private readonly HotRectNode _bubbleChild = new("BubbleChildHotRect") {
		Label = "Bubble child",
		Position = new Vector2(34f, 42f),
		Size = new Vector2(190f, 116f),
		Fill = new Color(44, 88, 142, 112),
		StopsPointerPropagation = false
	};
	private readonly HotRectNode _blockingChild = new("BlockingChildHotRect") {
		Label = "Blocking child",
		Position = new Vector2(236f, 118f),
		Size = new Vector2(182f, 112f),
		Fill = new Color(122, 72, 36, 118),
		PressedFill = new Color(174, 106, 62, 190),
		StopsPointerPropagation = true
	};
	private readonly Queue<string> _eventLines = new();
	private string _eventLog = "Move over the boxes, then click them.";
	private string _routeLog = "Expected routes: Bubble child -> Outer hot rect, Blocking child only.";

	public HotRectEventScene() : base("HotRectEventScene", "Hot Rect Events") {
		var assetRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "interface");
		_texture = new CompressedTexture2D(Path.Combine(assetRoot, "animMulti_atlas.png"));
		_atlasDefinition = new AtlasDefinitionResource(Path.Combine(assetRoot, "animMulti_atlas.json"));
		_frames = SpriteFrames.FromAtlas(_texture, _atlasDefinition);
		_sprite = new Sprite(_frames.GetFrameTexture(0), "DebugSprite") {
			Position = new Vector2(152f, 92f),
			Scale = new Vector2(1.45f, 1.45f)
		};

		_eventSource.ScopeRoot = this;
		_outerRect.PointerEvent += HandleHotRectEvent;
		_bubbleChild.PointerEvent += HandleHotRectEvent;
		_blockingChild.PointerEvent += HandleHotRectEvent;

		AddChild(_panel);
		AddChild(_eventSource);
		AddChild(_outerRect);
		_outerRect.AddChild(_sprite);
		_outerRect.AddChild(_bubbleChild);
		_outerRect.AddChild(_blockingChild);
	}

	protected override void OnDraw() {
		UiText.Draw("Primitive event source spike", 416f, 62f, 28f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("UiEventSource samples Raylib input, resolves the topmost HotRectNode, then bubbles the event through parent nodes that implement the same pointer handler contract.", 416f, 100f, 17f, new Color(196, 208, 224, 255));
		UiText.Draw("Events currently emitted by the spike: Enter, Leave, Move, Down, Up, Click, Wheel.", 416f, 126f, 17f, new Color(196, 208, 224, 255));
		UiText.Draw("Click Bubble child: the child event bubbles into Outer hot rect. Click Blocking child: it marks the event handled and stops the parent route.", 416f, 152f, 17f, new Color(196, 208, 224, 255));
		UiText.Draw($"Last event: {_eventLog}", 416f, 494f, 18f, new Color(255, 214, 96, 255));
		UiText.Draw($"Route: {_routeLog}", 416f, 522f, 17f, new Color(214, 224, 238, 255));
		UiText.Draw("Recent routed events", 416f, 556f, 20f, Color.RayWhite);
		var lineIndex = 0;
		foreach (var line in _eventLines) {
			UiText.Draw(line, 416f, 586f + (lineIndex * 20f), 15f, new Color(196, 208, 224, 255));
			lineIndex++;
		}
		Raylib.DrawRectangleLinesEx(new Rectangle(516f, 164f, 468f, 286f), 2f, new Color(108, 126, 162, 255));
	}

	protected override void OnDispose() {
		_frames.Dispose();
		_atlasDefinition.Dispose();
		_texture.Dispose();
	}

	private void HandleHotRectEvent(HotRectNode sender, UiPointerEvent pointerEvent) {
		_eventLog = $"{pointerEvent.Kind} received by {sender.Name}";
		_routeLog = $"{pointerEvent.OriginalTarget.Name} -> {pointerEvent.CurrentTarget.Name}    handled={pointerEvent.Handled}";
		var line = $"{pointerEvent.Kind,-5} target={pointerEvent.OriginalTarget.Name} current={pointerEvent.CurrentTarget.Name} handler={sender.Name} handled={pointerEvent.Handled}";
		_eventLines.Enqueue(line);
		while (_eventLines.Count > MaxLogEntries) {
			_eventLines.Dequeue();
		}

		Console.WriteLine($"[HotRectEventScene] {line}");
	}
}

using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class HotRectEventScene : ShowcaseScene {
	private readonly PanelNode _panel = new("EventPanel") {
		Position = new Vector2(380f, 28f),
		Size = new Vector2(860f, 664f),
		Fill = new Color(17, 24, 38, 255),
		Outline = new Color(83, 105, 140, 255),
		OutlineThickness = 2f
	};
	private readonly UiEventSource _eventSource = new("SceneEventSource");
	private readonly CompressedTexture2D _texture;
	private readonly AtlasDefinitionResource _atlasDefinition;
	private readonly SpriteFrames _frames;
	private readonly AnimatedSprite2D _animatedSprite;
	private readonly HotRectNode _animatedSurface;
	private readonly ButtonNode _restartButton = new("RestartButton") {
		Position = new Vector2(416f, 640f),
		Size = new Vector2(164f, 40f),
		Text = "Restart",
		FontSize = 18f
	};
	private readonly OverlayNode _overlay;
	private string _pointerStatus = "Move over the animation, then click it.";
	private string _playbackStatus = "Click toggles pause/resume.";
	private string _loopStatus = "Loop: on (press L to toggle)";
	private int _completionCount;

	public HotRectEventScene() : base("HotRectEventScene", "Hot Rect Events") {
		var assetRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "interface");
		_texture = new CompressedTexture2D(Path.Combine(assetRoot, "animMulti_atlas.png"));
		_atlasDefinition = new AtlasDefinitionResource(Path.Combine(assetRoot, "animMulti_atlas.json"));
		_frames = SpriteFrames.FromAtlas(_texture, _atlasDefinition);

		const float spriteScale = 1.85f;
		var frame0 = _frames.GetFrameRegion(0);
		var surfaceSize = new Vector2(frame0.Width * spriteScale, frame0.Height * spriteScale);

		_animatedSurface = new HotRectNode("AnimatedSurface") {
			Label = "AnimatedSprite2D hit surface",
			Position = new Vector2(810f, 326f),
			Size = surfaceSize,
			Pivot = new Vector2(0.5f, 0.5f),
			Fill = new Color(22, 50, 90, 28),
			HoverFill = new Color(32, 86, 150, 56),
			PressedFill = new Color(54, 118, 192, 96)
		};
		_animatedSprite = new AnimatedSprite2D(_frames, "AnimatedSpriteProbe") {
			Scale = new Vector2(spriteScale, spriteScale),
			SpeedFps = 15f,
			Loop = true
		};
		_overlay = new OverlayNode(this);

		_eventSource.ScopeRoot = this;
		_animatedSurface.PointerEvent += HandleAnimatedSurfaceEvent;
		_animatedSprite.PlaybackCompleted += HandlePlaybackCompleted;
		_restartButton.Clicked += _ => RestartIfLoopIsOff();

		AddChild(_panel);
		AddChild(_eventSource);
		AddChild(_animatedSurface);
		_animatedSurface.AddChild(_animatedSprite);
		AddChild(_restartButton);
		AddChild(_overlay);
	}

	protected override void OnUpdate(float deltaTime) {
		if (Raylib.IsKeyPressed(KeyboardKey.L)) {
			_animatedSprite.Loop = !_animatedSprite.Loop;
			_loopStatus = _animatedSprite.Loop
				? "Loop: on (press L to toggle)"
				: "Loop: off (press L to toggle)";
			_playbackStatus = _animatedSprite.Loop
				? "Looping restored."
				: "Looping disabled. Use Restart after completion.";
		}
	}

	protected override void OnDispose() {
		_animatedSprite.PlaybackCompleted -= HandlePlaybackCompleted;
		_frames.Dispose();
		_atlasDefinition.Dispose();
		_texture.Dispose();
	}

	private void HandleAnimatedSurfaceEvent(HotRectNode sender, UiPointerEvent pointerEvent) {
		var buttonText = pointerEvent.Button?.ToString() ?? "None";
		_pointerStatus = $"{pointerEvent.Kind} on {sender.Name} ({buttonText}) route={pointerEvent.OriginalTarget.Name}->{pointerEvent.CurrentTarget.Name}";

		if (pointerEvent.Kind != UiPointerEventKind.Click || pointerEvent.Button != MouseButton.Left) {
			return;
		}

		if (_animatedSprite.Playing) {
			_animatedSprite.Pause();
			_playbackStatus = "Paused from click on HotRectNode.";
		}
		else {
			_animatedSprite.Play();
			_playbackStatus = "Resumed from click on HotRectNode.";
		}
	}

	private void HandlePlaybackCompleted(AnimatedSprite2D sprite) {
		_completionCount++;
		_playbackStatus = $"Completed and stopped on last frame. Restart available. End count: {_completionCount}";
	}

	private void RestartIfLoopIsOff() {
		if (_animatedSprite.Loop) {
			_playbackStatus = "Restart ignored while loop is on.";
			return;
		}

		_animatedSprite.Restart();
		_playbackStatus = "Restarted from button while loop is off.";
	}

	private sealed class OverlayNode : Node2D {
		private readonly HotRectEventScene _owner;

		public OverlayNode(HotRectEventScene owner) : base("HotRectEventOverlay") {
			_owner = owner;
		}

		protected override void Draw() {
			UiText.Draw("AnimatedSprite2D + HotRectNode", 416f, 62f, 28f, Color.RayWhite, UiTextStyle.Title);
			UiText.Draw("This scene composes one AnimatedSprite2D with one HotRectNode and lets UiEventSource route raw mouse input into the rectangular hit surface.", 416f, 100f, 17f, new Color(196, 208, 224, 255));
			UiText.Draw("The goal is narrow: prove that animation playback can stay in AnimatedSprite2D while pointer ownership stays in a separate hot-rect style control.", 416f, 126f, 17f, new Color(196, 208, 224, 255));
			UiText.Draw("Hover highlights the surface. Click toggles playback. Press L to toggle looping. When looping is off, the animation raises a completion event and the Restart button starts it again.", 416f, 152f, 17f, new Color(196, 208, 224, 255));
			UiText.Draw($"Pointer: {_owner._pointerStatus}", 416f, 522f, 18f, new Color(255, 214, 96, 255));
			UiText.Draw($"Playback: {_owner._playbackStatus}", 416f, 550f, 17f, new Color(214, 224, 238, 255));
			UiText.Draw(_owner._loopStatus, 416f, 578f, 17f, new Color(214, 224, 238, 255));
			UiText.Draw($"Frame: {_owner._animatedSprite.Frame + 1:00} / {_owner._animatedSprite.Frames.Count:00}    End events: {_owner._completionCount}", 416f, 606f, 17f, new Color(214, 224, 238, 255));
			Raylib.DrawRectangleLinesEx(new Rectangle(_owner._animatedSurface.GlobalBounds.X - 4f, _owner._animatedSurface.GlobalBounds.Y - 4f, _owner._animatedSurface.GlobalBounds.Width + 8f, _owner._animatedSurface.GlobalBounds.Height + 8f), 2f, new Color(108, 126, 162, 255));
		}
	}
}

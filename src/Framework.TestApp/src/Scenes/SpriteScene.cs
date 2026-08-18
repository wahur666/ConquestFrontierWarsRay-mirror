using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class SpriteScene : ShowcaseScene {
	private const float DefaultAnimationFps = 15f;
	private const float MinAnimationFps = 1f;
	private const float MaxAnimationFps = 60f;
	private readonly PanelNode _panel = new("SpritePanel") {
		Position = new Vector2(364f, 20f),
		Size = new Vector2(896f, 680f),
		Fill = new Color(14, 18, 30, 255),
		Outline = new Color(68, 84, 110, 255),
		OutlineThickness = 2f
	};
	private readonly TextNode _statusLabel = new("SpriteStatus") {
		Position = new Vector2(402f, 88f),
		FontSize = 18f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _frameLabel = new("FrameStatus") {
		Position = new Vector2(402f, 118f),
		FontSize = 18f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly CompressedTexture2D _posterTexture;
	private readonly AtlasDefinitionResource _atlasDefinition;
	private readonly AtlasTexture _frameTexture;
	private readonly Sprite _posterSprite;
	private readonly Sprite _frameSprite;
	private int _frameIndex;
	private float _frameElapsed;
	private float _animationFps = DefaultAnimationFps;

	public SpriteScene() : base("SpriteScene", "Sprites") {
		var assetRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "interface");
		_posterTexture = new CompressedTexture2D(Path.Combine(assetRoot, "animMedia_atlas.png"));
		_atlasDefinition = new AtlasDefinitionResource(Path.Combine(assetRoot, "animMedia_atlas.json"));
		var initialFrame = _atlasDefinition.GetFrame(0);
		_frameTexture = new AtlasTexture(_posterTexture, ToRectangle(initialFrame));
		_posterSprite = new Sprite(_posterTexture, "PosterSprite") {
			Position = new Vector2(782f, 300f),
			Scale = new Vector2(0.34f, 0.34f)
		};
		_frameSprite = new Sprite(_frameTexture, "FrameSprite") {
			Position = new Vector2(1080f, 230f),
			Scale = new Vector2(1.1f, 1.1f)
		};

		AddChild(_panel);
		AddChild(_posterSprite);
		AddChild(_frameSprite);
		AddChild(_statusLabel);
		AddChild(_frameLabel);
	}

	protected override void OnUpdate(float deltaTime) {
		if (Input.UiLeft) {
			_animationFps = MathF.Max(MinAnimationFps, _animationFps - 1f);
		}

		if (Input.UiRight) {
			_animationFps = MathF.Min(MaxAnimationFps, _animationFps + 1f);
		}

		_frameElapsed += deltaTime;
		var frameDuration = 1f / _animationFps;
		while (_frameElapsed >= frameDuration) {
			_frameElapsed -= frameDuration;
			_frameIndex = (_frameIndex + 1) % _atlasDefinition.Frames.Count;
			_frameTexture.Region = ToRectangle(_atlasDefinition.GetFrame(_frameIndex));
		}

		_statusLabel.Text = $"Texture loaded: {_posterTexture.IsLoaded}    Atlas loaded: {_atlasDefinition.IsLoaded}    Playback: {_animationFps:0} FPS";
		_frameLabel.Text = $"Atlas frame: {_frameIndex + 1} / {_atlasDefinition.Frames.Count}    Left/Right adjusts rate";
	}

	protected override void OnDraw() {
		UiText.Draw("Sprite resources", 402f, 50f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("The full atlas PNG comes from assets/interface. The preview on the right is sampled from the same texture through AtlasTexture regions loaded from the exported atlas JSON.", 402f, 86f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Frames advance at 15 FPS by default. Use Left/Right to change the rate while the scene is active.", 402f, 170f, 17f, new Color(188, 200, 218, 255));
		Raylib.DrawRectangleLinesEx(new Rectangle(955f, 170f, 250f, 250f), 2f, Color.Gold);
		UiText.Draw("Active atlas frame", 986f, 432f, 18f, Color.Gold);
	}

	protected override void OnDispose() {
		_frameTexture.Dispose();
		_atlasDefinition.Dispose();
		_posterTexture.Dispose();
	}

	private static Rectangle ToRectangle(AtlasDefinitionResource.AtlasFrame frame) {
		return new Rectangle(frame.X, frame.Y, frame.Width, frame.Height);
	}
}

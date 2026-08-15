using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class SpriteScene : ShowcaseScene {
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

	public SpriteScene() : base("SpriteScene", "Sprites") {
		var assetRoot = Path.Combine(AppContext.BaseDirectory, "Assets");
		_posterTexture = new CompressedTexture2D(Path.Combine(assetRoot, "soundtrack Conquest - Frontier Wars.jpg"));
		_atlasDefinition = new AtlasDefinitionResource(Path.Combine(assetRoot, "demo-atlas.json"));
		var initialFrame = _atlasDefinition.GetFrame(0);
		_frameTexture = new AtlasTexture(_posterTexture, ToRectangle(initialFrame));
		_posterSprite = new Sprite(_posterTexture, "PosterSprite") {
			Position = new Vector2(612f, 390f),
			Scale = new Vector2(0.42f, 0.42f)
		};
		_frameSprite = new Sprite(_frameTexture, "FrameSprite") {
			Position = new Vector2(1080f, 238f),
			Scale = new Vector2(0.95f, 0.95f)
		};

		AddChild(_panel);
		AddChild(_posterSprite);
		AddChild(_frameSprite);
		AddChild(_statusLabel);
		AddChild(_frameLabel);
	}

	protected override void OnUpdate(float deltaTime) {
		_frameElapsed += deltaTime;
		if (_frameElapsed >= 1.15f) {
			_frameElapsed = 0f;
			_frameIndex = (_frameIndex + 1) % _atlasDefinition.Frames.Count;
			_frameTexture.Region = ToRectangle(_atlasDefinition.GetFrame(_frameIndex));
		}

		_statusLabel.Text = $"Texture loaded: {_posterTexture.IsLoaded}    Atlas loaded: {_atlasDefinition.IsLoaded}";
		_frameLabel.Text = $"Atlas frame: {_frameIndex + 1} / {_atlasDefinition.Frames.Count}";
	}

	protected override void OnDraw() {
		UiText.Draw("Sprite resources", 402f, 50f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("The full poster uses CompressedTexture2D directly. The preview on the right is the same image sampled through AtlasTexture regions loaded from AtlasDefinitionResource JSON.", 402f, 86f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Frames cycle automatically to keep Sprite.SetTexture/Region behavior visible without manual setup.", 402f, 170f, 17f, new Color(188, 200, 218, 255));
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

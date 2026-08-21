using System.Numerics;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class SpriteScene : ShowcaseScene {
	private const float DefaultAnimationFps = 15f;
	private const float MinAnimationFps = 1f;
	private const float MaxAnimationFps = 60f;
	private const string DemoVfxShapeId = "VFXShape!!AnimateMedia";
	private const string Demo2VfxShapeId = "VFXShape!!Dread";
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
	private readonly string _animateId;
	private readonly string _vfxShapeId;
	private readonly string _atlasJsonFileName;
	private readonly string _atlasPngFileName;
	private readonly AtlasFramesResource _posterAtlas;
	private readonly CompressedTexture2D _test2Texture;
	private readonly Sprite _posterSprite;
	private readonly AnimatedSprite2D _animatedSprite;
	private readonly Sprite _test2Sprite;
	private float _animationFps = DefaultAnimationFps;

	public SpriteScene() : base("SpriteScene", "Sprites") {
		var vfxRepository = VfxAnimationDataRepository.LocateFromRepo();
		var vfxData = vfxRepository.Load();
		if (!vfxData.TryGetAtlasByVfxShapeId(DemoVfxShapeId, out var entry)) {
			throw new InvalidOperationException($"Could not find atlas entry for '{DemoVfxShapeId}' in vfx-animation-data.json.");
		}

		if (!vfxData.TryGetImageByVfxShapeId(Demo2VfxShapeId, out var entry2)) {
			throw new InvalidOperationException($"Could not find atlas entry for '{Demo2VfxShapeId}' in vfx-animation-data.json.");
		}
		_animateId = entry.Key;
		_vfxShapeId = entry.Value.VfxShapeId;
		_atlasPngFileName = entry.Value.Filename;
		_atlasJsonFileName = entry.Value.MetaJson;

		_posterAtlas = vfxRepository.CreateAtlasFramesResource(entry.Value);
		_posterSprite = new Sprite(_posterAtlas.Texture, "PosterSprite") {
			Position = new Vector2(782f, 300f),
			Scale = new Vector2(0.34f, 0.34f)
		};
		_animatedSprite = new AnimatedSprite2D(_posterAtlas.Frames, "FrameSprite") {
			Position = new Vector2(1080f, 130f),
			Scale = new Vector2(1.1f, 1.1f),
			SpeedFps = DefaultAnimationFps
		};

		_test2Texture = new CompressedTexture2D(vfxRepository.GetInterfaceAssetPath(entry2.Value));
		_test2Sprite = new Sprite(_test2Texture, "Dread") {
			Position = new Vector2(1080f, 500f),
			Scale = new Vector2(2f, 2f)
		};

		AddChild(_panel);
		AddChild(_posterSprite);
		AddChild(_test2Sprite);
		AddChild(_animatedSprite);
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
		_animatedSprite.SpeedFps = _animationFps;

		_statusLabel.Text = $"Animate: {_animateId}    Shape: {_vfxShapeId}    Texture loaded: {_posterAtlas.Texture.IsLoaded}    Atlas loaded: {_posterAtlas.AtlasDefinition.IsLoaded}    Playback: {_animationFps:0} FPS";
		_frameLabel.Text = $"Atlas frame: {_animatedSprite.Frame + 1:00} / {_animatedSprite.Frames.Count}    Source: {EntryFileNames()}    Left/Right adjusts rate";
	}

	protected override void OnDraw() {
		UiText.Draw("Sprite resources", 402f, 50f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("This scene now resolves sprite assets from assets/DB/vfx-animation-data.json by original VFXShape id. The preview on the right still samples the same atlas through AtlasTexture regions loaded from exported atlas JSON.", 402f, 86f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Frames advance at 15 FPS by default. Use Left/Right to change the rate while the scene is active.", 402f, 170f, 17f, new Color(188, 200, 218, 255));
		Raylib.DrawRectangleLinesEx(new Rectangle(955f, 170f, 250f, 250f), 2f, Color.Gold);
		UiText.Draw("Active atlas frame", 986f, 432f, 18f, Color.Gold);
	}

	private string EntryFileNames() {
		return $"{_atlasPngFileName} + {_atlasJsonFileName}";
	}

	protected override void OnDispose() {
		_posterAtlas.Dispose();
	}
}

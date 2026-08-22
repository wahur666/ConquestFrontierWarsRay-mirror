using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class SpriteScene : ShowcaseScene {
	private const int FpsStep = 5;
	private const int MinAnimationFps = 5;
	private const int MaxAnimationFps = 30;
	private const string DemoVfxShapeId = "VFXShape!!AnimateMedia";
	private const string Demo2VfxShapeId = "VFXShape!!Dread";
	private readonly UiEventSource _eventSource = new("SpriteSceneEventSource");
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
	private readonly TextNode _speedLabel = new("SpeedLabel") {
		Position = new Vector2(402f, 170f),
		FontSize = 18f,
		Tint = new Color(210, 220, 236, 255)
	};
	private readonly TextNode _stateGalleryLabel = new("SliderStateGalleryLabel") {
		Position = new Vector2(402f, 270f),
		FontSize = 18f,
		Tint = new Color(210, 220, 236, 255)
	};
	private readonly LegacySliderNode _fpsSlider = new("LegacyFpsSlider");
	private readonly LegacySliderNode _disabledPreviewSlider = new("DisabledPreviewSlider") { Enabled = false };
	private readonly LegacySliderNode _normalPreviewSlider = new("NormalPreviewSlider");
	private readonly LegacySliderNode _highlightPreviewSlider = new("HighlightPreviewSlider");
	private readonly LegacySliderNode _alertPreviewSlider = new("AlertPreviewSlider");
	private readonly string _animateId;
	private readonly string _vfxShapeId;
	private readonly string _atlasJsonFileName;
	private readonly string _atlasPngFileName;
	private readonly AtlasFramesResource _posterAtlas;
	private readonly CompressedTexture2D _test2Texture;
	private readonly Sprite _posterSprite;
	private readonly AnimatedSprite2D _animatedSprite;
	private readonly Sprite _test2Sprite;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private int _animationFps = 15;

	public SpriteScene() : base("SpriteScene", "Sprites") {
		_vfxRepository = VfxAnimationDataRepository.LocateFromRepo();
		var vfxData = _vfxRepository.Load();
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

		_posterAtlas = _vfxRepository.CreateAtlasFramesResource(entry.Value);
		_posterSprite = new Sprite(_posterAtlas.Texture, "PosterSprite") {
			Position = new Vector2(782f, 300f),
			Scale = new Vector2(0.34f, 0.34f)
		};
		_animatedSprite = new AnimatedSprite2D(_posterAtlas.Frames, "FrameSprite") {
			Position = new Vector2(1080f, 130f),
			Scale = new Vector2(1.1f, 1.1f),
			SpeedFps = _animationFps
		};

		_test2Texture = new CompressedTexture2D(_vfxRepository.GetInterfaceAssetPath(entry2.Value));
		_test2Sprite = new Sprite(_test2Texture, "Dread") {
			Position = new Vector2(1080f, 500f),
			Scale = new Vector2(2f, 2f)
		};

		_eventSource.ScopeRoot = this;
		_fpsSlider.ApplyLegacyDefinition(
			new GT_SLIDER {
				DisabledColor = new GT_COLOR { Red = 100, Green = 100, Blue = 100 },
				NormalColor = new GT_COLOR { Red = 255, Green = 0, Blue = 0 },
				HighlightColor = new GT_COLOR { Red = 255, Green = 255, Blue = 0 },
				AlertColor = new GT_COLOR { Red = 0, Green = 0, Blue = 255 },
				Vertical = false,
				Indent = 6,
				ShapeFile = "slider.shp"
			},
			new SLIDER_DATA {
				ScreenRect = new RECT {
					Left = 0,
					Top = 0,
					Right = 120,
					Bottom = 20
				},
				XOrigin = 402,
				YOrigin = 200
			},
			_vfxRepository);
		_fpsSlider.SetRange(0, (MaxAnimationFps - MinAnimationFps) / FpsStep);
		_fpsSlider.SetSliderPosition((_animationFps - MinAnimationFps) / FpsStep, emitEvent: false);
		_fpsSlider.ValueChanged += HandleSliderValueChanged;

		var sliderContainer = new Node2D("Slider container") {
			Position = new Vector2(402f, 400f)
		};
		ConfigurePreviewSlider(_disabledPreviewSlider, 0, 0, 0, LegacySliderNode.LegacySliderVisualState.Disabled);
		ConfigurePreviewSlider(_normalPreviewSlider, 0, 40, 1, LegacySliderNode.LegacySliderVisualState.Normal);
		ConfigurePreviewSlider(_highlightPreviewSlider, 0, 80, 2, LegacySliderNode.LegacySliderVisualState.Highlight);
		ConfigurePreviewSlider(_alertPreviewSlider, 0, 120, 3, LegacySliderNode.LegacySliderVisualState.Alert);

		AddChild(_eventSource);
		AddChild(_panel);
		AddChild(_posterSprite);
		AddChild(_test2Sprite);
		AddChild(_animatedSprite);
		AddChild(_statusLabel);
		AddChild(_frameLabel);
		AddChild(_speedLabel);
		AddChild(_stateGalleryLabel);
		AddChild(_fpsSlider);
		sliderContainer.AddChild(_disabledPreviewSlider);
		sliderContainer.AddChild(_normalPreviewSlider);
		sliderContainer.AddChild(_highlightPreviewSlider);
		sliderContainer.AddChild(_alertPreviewSlider);
		AddChild(sliderContainer);
	}

	protected override void OnUpdate(float deltaTime) {
		_animatedSprite.SpeedFps = _animationFps;

		_statusLabel.Text = $"Animate: {_animateId}    Shape: {_vfxShapeId}    Texture loaded: {_posterAtlas.Texture.IsLoaded}    Atlas loaded: {_posterAtlas.AtlasDefinition.IsLoaded}    Playback: {_animationFps:0} FPS";
		_frameLabel.Text = $"Atlas frame: {_animatedSprite.Frame + 1:00} / {_animatedSprite.Frames.Count}    Source: {EntryFileNames()}";
		_speedLabel.Text = $"Animation speed: {_animationFps} FPS    Slider step: {_fpsSlider.SliderPosition + 1} / {((MaxAnimationFps - MinAnimationFps) / FpsStep) + 1}";
		_stateGalleryLabel.Text = "Legacy slider state gallery: disabled, normal, highlight, alert";
	}

	protected override void OnDraw() {
		UiText.Draw("Sprite resources", 402f, 50f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("This scene resolves sprite assets from assets/DB/vfx-animation-data.json by original VFXShape id. The FPS selector below uses LegacySliderNode and resolves slider.shp through the same repository instead of hardcoded atlas filenames.", 402f, 86f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Drag the slider to snap animation speed in 5 FPS increments from 5 to 30.", 402f, 240f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Disabled", 534f, 320f, 16f, new Color(188, 200, 218, 255));
		UiText.Draw("Normal", 534f, 360f, 16f, new Color(188, 200, 218, 255));
		UiText.Draw("Highlight", 534f, 400f, 16f, new Color(188, 200, 218, 255));
		UiText.Draw("Alert", 534f, 440f, 16f, new Color(188, 200, 218, 255));
		Raylib.DrawRectangleLinesEx(new Rectangle(955f, 170f, 250f, 250f), 2f, Color.Gold);
		UiText.Draw("Active atlas frame", 986f, 432f, 18f, Color.Gold);
	}

	private string EntryFileNames() {
		return $"{_atlasPngFileName} + {_atlasJsonFileName}";
	}

	protected override void OnDispose() {
		_posterAtlas.Dispose();
	}

	private void HandleSliderValueChanged(LegacySliderNode slider) {
		_animationFps = MinAnimationFps + (slider.SliderPosition * FpsStep);
	}

	private void ConfigurePreviewSlider(LegacySliderNode slider, int xOrigin, int yOrigin, int position, LegacySliderNode.LegacySliderVisualState visualState) {
		slider.ApplyLegacyDefinition(
			new GT_SLIDER {
				DisabledColor = new GT_COLOR { Red = 100, Green = 100, Blue = 100 },
				NormalColor = new GT_COLOR { Red = 255, Green = 0, Blue = 0 },
				HighlightColor = new GT_COLOR { Red = 255, Green = 255, Blue = 0 },
				AlertColor = new GT_COLOR { Red = 0, Green = 0, Blue = 255 },
				Vertical = false,
				Indent = 6,
				ShapeFile = "slider.shp"
			},
			new SLIDER_DATA {
				ScreenRect = new RECT {
					Left = 0,
					Top = 0,
					Right = 120,
					Bottom = 20
				},
				XOrigin = xOrigin,
				YOrigin = yOrigin
			},
			_vfxRepository);
		slider.SetRange(0, (MaxAnimationFps - MinAnimationFps) / FpsStep);
		slider.SetSliderPosition(position, emitEvent: false);
		slider.VisualStateOverride = visualState;
	}
}

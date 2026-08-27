using System.Numerics;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored progress control with optional shape fill and split text colors.
/// </summary>
public sealed class LegacyProgressStaticNode : Control {
	private const float DefaultFontSize = 16f;
	private const float AlignmentLeftInset = 2f;
	private const float AlignmentRightInset = 4f;
	private const float ShadowOffset = 2f;
	private const float MaxRollupDurationMilliseconds = 8000f;
	private const float HashSpacing = 6f;

	private AtlasFramesResource? _art;
	private bool _backdraw;
	private GT_DRAWTYPE _backgroundDraw;
	private Color _filledColor = Color.Blank;
	private Color _emptyColor = Color.Blank;
	private float _fontSize = DefaultFontSize;
	private string _fontName = string.Empty;
	private Color _normalTextColor = Color.RayWhite;
	private Color _overTextColor = Color.Black;
	private uint _progressCurrent;
	private uint _progressMax = 100;
	private string _text = string.Empty;
	private LegacyStaticNode.StaticAlignment _alignment = LegacyStaticNode.StaticAlignment.Left;
	private uint _rollupTarget;
	private float _rollupTimerMilliseconds;
	private bool _useNegativeRollup;

	public LegacyProgressStaticNode(string? name = null) : base(name) {
	}

	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;

	public void ApplyLegacyDefinition(
		GT_PROGRESS_STATIC archetype,
		Vector2 position,
		Vector2? size = null,
		LegacyStaticNode.StaticAlignment alignment = LegacyStaticNode.StaticAlignment.Left,
		VfxAnimationDataRepository? repository = null) {
		ArgumentNullException.ThrowIfNull(archetype);

		DisposeArt();

		_fontName = archetype.FontName;
		_fontSize = ResolveFontSize(archetype.FontName);
		_normalTextColor = ToColor(archetype.NormalText);
		_overTextColor = ToColor(archetype.OverText);
		_filledColor = ToColor(archetype.Background);
		_emptyColor = ToColor(archetype.Background2);
		_backgroundDraw = archetype.BackgroundDraw;
		_backdraw = archetype.Backdraw;
		_alignment = alignment;
		Position = position;

		if (!string.IsNullOrWhiteSpace(archetype.ShapeFile) && repository is not null) {
			_art = LoadArt(repository, archetype.ShapeFile);
		}

		var resolvedSize = size ?? (_art is not null
			? new Vector2(_art.Frames.GetFrameRegion(0).Width, _art.Frames.GetFrameRegion(0).Height)
			: Vector2.Zero);
		Size = new Vector2(Math.Max(0f, resolvedSize.X), Math.Max(0f, resolvedSize.Y));
	}

	public void SetText(string? text) {
		_text = text ?? string.Empty;
	}

	public void EnableRollupBehavior(int value) {
		if (value == 0) {
			_rollupTarget = 0;
			_rollupTimerMilliseconds = 0f;
			_useNegativeRollup = false;
			SetText("0");
			return;
		}

		_rollupTimerMilliseconds = 0f;
		_useNegativeRollup = value < 0;
		_rollupTarget = (uint)Math.Abs(value);
	}

	public void SetProgress(uint current, uint max) {
		_progressCurrent = current;
		_progressMax = max;
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		if (_rollupTarget == 0) {
			return;
		}

		_rollupTimerMilliseconds += deltaTime * 1000f;
		var speed = _rollupTarget > MaxRollupDurationMilliseconds
			? _rollupTarget / MaxRollupDurationMilliseconds
			: 1f;
		var displayValue = MathF.Min(_rollupTarget, _rollupTimerMilliseconds * speed);
		var roundedValue = (uint)displayValue;
		SetText(_useNegativeRollup ? $"-{roundedValue}" : $"{roundedValue}");
		if (roundedValue >= _rollupTarget) {
			_rollupTarget = 0;
		}
	}

	protected override void Draw() {
		if (!Visible || Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		var progress = ResolveProgressFraction();
		DrawProgress(bounds, progress);

		if (!string.IsNullOrEmpty(_text) && !string.IsNullOrWhiteSpace(_fontName)) {
			DrawText(bounds, progress);
		}
	}

	protected override void OnDispose() {
		DisposeArt();
		base.OnDispose();
	}

	private float ResolveProgressFraction() {
		if (_progressMax == 0) {
			return 1f;
		}

		return Math.Clamp((float)_progressCurrent / _progressMax, 0f, 1f);
	}

	private void DrawProgress(Rectangle bounds, float progress) {
		var fillWidth = MathF.Floor(bounds.Width * progress);
		if (_art is not null) {
			if (fillWidth <= 0f) {
				return;
			}

			var clipBounds = new Rectangle(bounds.X, bounds.Y, fillWidth, bounds.Height);
			DrawClipped(clipBounds, () => {
				var slice = _art.Texture.GetSlice();
				var source = _art.Frames.GetFrameRegion(0);
				Raylib.DrawTexturePro(slice.Texture, source, bounds, Vector2.Zero, 0f, Color.White);
			});
			return;
		}

		var filledBounds = new Rectangle(bounds.X, bounds.Y, fillWidth, bounds.Height);
		var emptyBounds = new Rectangle(bounds.X + fillWidth, bounds.Y, Math.Max(0f, bounds.Width - fillWidth), bounds.Height);

		switch (_backgroundDraw) {
			case GT_DRAWTYPE.FILL:
				if (filledBounds.Width > 0f) {
					Raylib.DrawRectangleRec(filledBounds, _filledColor);
				}

				if (emptyBounds.Width > 0f) {
					Raylib.DrawRectangleRec(emptyBounds, _emptyColor);
				}
				break;
			case GT_DRAWTYPE.HASH:
				if (filledBounds.Width > 0f) {
					DrawHash(filledBounds, _filledColor);
				}

				if (emptyBounds.Width > 0f) {
					DrawHash(emptyBounds, _emptyColor);
				}
				break;
		}
	}

	private void DrawText(Rectangle bounds, float progress) {
		var textWidth = UiText.MeasureWidth(_text, _fontSize, TextStyle);
		var textHeight = _fontSize;
		var textX = bounds.X + ResolveAlignedX(bounds.Width, textWidth);
		var textY = bounds.Y + ResolveAlignedY(bounds.Height, textHeight);
		var fillWidth = MathF.Floor(bounds.Width * progress);
		var filledBounds = new Rectangle(bounds.X, bounds.Y, fillWidth, bounds.Height);
		var emptyBounds = new Rectangle(bounds.X + fillWidth, bounds.Y, Math.Max(0f, bounds.Width - fillWidth), bounds.Height);

		if (_backdraw) {
			UiText.Draw(_text, textX + ShadowOffset, textY + ShadowOffset, _fontSize, Color.Black, TextStyle);
		}

		if (filledBounds.Width > 0f) {
			DrawClipped(filledBounds, () => UiText.Draw(_text, textX, textY, _fontSize, _overTextColor, TextStyle));
		}

		if (emptyBounds.Width > 0f) {
			DrawClipped(emptyBounds, () => UiText.Draw(_text, textX, textY, _fontSize, _normalTextColor, TextStyle));
		}
	}

	private void DrawHash(Rectangle bounds, Color color) {
		Raylib.DrawRectangleLinesEx(bounds, 1f, color);
		DrawClipped(bounds, () => {
			for (var x = bounds.X - bounds.Height; x < bounds.X + bounds.Width; x += HashSpacing) {
				var start = new Vector2(x, bounds.Y + bounds.Height);
				var end = new Vector2(x + bounds.Height, bounds.Y);
				Raylib.DrawLineEx(start, end, 1f, color);
			}
		});
	}

	private static void DrawClipped(Rectangle bounds, Action drawAction) {
		if (bounds.Width <= 0f || bounds.Height <= 0f) {
			return;
		}

		Raylib.BeginScissorMode(
			(int)MathF.Floor(bounds.X),
			(int)MathF.Floor(bounds.Y),
			(int)MathF.Ceiling(bounds.Width),
			(int)MathF.Ceiling(bounds.Height));
		try {
			drawAction();
		} finally {
			Raylib.EndScissorMode();
		}
	}

	private float ResolveAlignedX(float availableWidth, float textWidth) {
		return _alignment switch {
			LegacyStaticNode.StaticAlignment.Center => Math.Max(0f, (availableWidth - textWidth) * 0.5f),
			LegacyStaticNode.StaticAlignment.Right => Math.Max(0f, availableWidth - textWidth - AlignmentRightInset),
			_ => AlignmentLeftInset
		};
	}

	private float ResolveAlignedY(float availableHeight, float textHeight) {
		return _alignment switch {
			LegacyStaticNode.StaticAlignment.TopLeft => AlignmentLeftInset,
			_ => Math.Max(0f, (availableHeight - textHeight) * 0.5f)
		};
	}

	private void DisposeArt() {
		_art?.Dispose();
		_art = null;
	}

	private static AtlasFramesResource? LoadArt(VfxAnimationDataRepository repository, string shapeId) {
		if (!repository.TryGetAtlasByShapeId(shapeId, out var atlasEntry)) {
			return null;
		}

		return new AtlasFramesResource(
			repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: false),
			repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: true),
			TextureFilter.Point);
	}

	private static float ResolveFontSize(string fontName) {
		return fontName switch {
			"Font!!Button" => 16f,
			"Font!!Button3D" => 16f,
			"Font!!MessageFutureReserved" => 16f,
			_ => DefaultFontSize
		};
	}

	private static Color ToColor(GT_COLOR color) {
		return new Color((byte)color.Red, (byte)color.Green, (byte)color.Blue, (byte)255);
	}
}

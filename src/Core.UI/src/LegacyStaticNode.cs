using System.Numerics;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored static control that supports shape-only, text-only, and mixed static surfaces.
/// </summary>
public sealed class LegacyStaticNode : Control, IUiPointerEventHandler {
	private const float DefaultFontSize = 16f;
	private const float AlignmentLeftInset = 2f;
	private const float AlignmentRightInset = 4f;
	private const float LeftSixInset = 6f;
	private const float TenBySixXInset = 10f;
	private const float TenBySixYInset = 6f;
	private const float ShadowOffset = 2f;
	private const float MaxRollupDurationMilliseconds = 8000f;
	private const float HashSpacing = 6f;

	private AtlasFramesResource? _art;
	private Framework.Texture2D? _imageArt;
	private bool _backdraw;
	private GT_DRAWTYPE _backgroundDraw;
	private Color _backgroundFill = Color.Blank;
	private LegacyButtonNode? _buddyControl;
	private bool _enabled = true;
	private float _fontSize = DefaultFontSize;
	private string _fontName = string.Empty;
	private bool _hovered;
	private bool _pointerPressed;
	private uint _rollupTarget;
	private float _rollupTimerMilliseconds;
	private string _text = string.Empty;
	private bool _useNegativeRollup;
	private bool _visible = true;

	public LegacyStaticNode(string? name = null) : base(name) {
	}

	public uint ControlId { get; set; }
	public uint StaticTextId { get; private set; }
	public uint StaticTooltipId { get; private set; }
	public uint StaticHintboxId { get; private set; }
	public StaticAlignment Alignment { get; private set; } = StaticAlignment.Left;
	public Color NormalTextColor { get; private set; } = new(255, 255, 255, 255);
	public string Text => _text;
	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;
	public bool IsPointerInputEnabled => Visible && _visible && _enabled && Size.X > 0f && Size.Y > 0f;

	public event Action<LegacyStaticNode>? Activated;

	public void ApplyLegacyDefinition(GT_STATIC archetype, STATIC_DATA? data = null, VfxAnimationDataRepository? repository = null) {
		ArgumentNullException.ThrowIfNull(archetype);

		DisposeArt();

		_fontName = archetype.FontName;
		_fontSize = ResolveFontSize(archetype.FontName);
		NormalTextColor = ToColor(archetype.NormalText);
		_backgroundFill = ToColor(archetype.Background);
		_backgroundDraw = archetype.BackgroundDraw;
		_backdraw = archetype.Backdraw;

		if (!string.IsNullOrWhiteSpace(archetype.ShapeFile) && repository is not null) {
			LoadArt(repository, archetype.ShapeFile);
		}

		if (_art is not null) {
			var frame = _art.Frames.GetFrameRegion(0);
			Size = new Vector2(frame.Width, frame.Height);
		} else if (_imageArt is not null) {
			Size = _imageArt.Size;
		}

		if (data is null) {
			return;
		}

		StaticTextId = data.StaticText;
		StaticTooltipId = data.StaticTooltip;
		StaticHintboxId = data.StaticHintbox;
		Alignment = (StaticAlignment)data.Alignment;
		Position = new Vector2(data.XOrigin, data.YOrigin);

		var resolvedWidth = data.Width > 0 ? data.Width : Size.X;
		var resolvedHeight = data.Height > 0 ? data.Height : Size.Y;
		Size = new Vector2(Math.Max(0f, resolvedWidth), Math.Max(0f, resolvedHeight));
	}

	public void SetText(string? text) {
		_text = text ?? string.Empty;
	}

	public int GetText(Span<char> buffer) {
		if (_text.Length == 0) {
			return 0;
		}

		var written = Math.Min(buffer.Length, _text.Length);
		_text.AsSpan(0, written).CopyTo(buffer);
		return written;
	}

	public void SetVisible(bool visible) {
		_visible = visible;
		if (!visible) {
			_hovered = false;
			_pointerPressed = false;
		}
	}

	public void EnableStatic(bool enabled) {
		_enabled = enabled;
		if (!enabled) {
			_hovered = false;
			_pointerPressed = false;
		}
	}

	public void SetTextColor(Color color) {
		NormalTextColor = color;
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

	public void SetBuddyControl(LegacyButtonNode? buddyControl) {
		_buddyControl = buddyControl;
	}

	public float GetStringWidth() {
		return string.IsNullOrEmpty(_text) ? 0f : UiText.MeasureWidth(_text, _fontSize, TextStyle);
	}

	public bool HitTest(Vector2 screenPoint) {
		return ContainsPoint(screenPoint);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		if (!_visible || !Visible) {
			return;
		}

		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Enter:
			case UiPointerEventKind.Move:
				_hovered = HitTest(pointerEvent.Position);
				break;
			case UiPointerEventKind.Leave:
				_hovered = false;
				_pointerPressed = false;
				break;
			case UiPointerEventKind.Down:
				if (pointerEvent.Button == MouseButton.Left && _enabled && HitTest(pointerEvent.Position)) {
					_hovered = true;
					_pointerPressed = true;
					ForwardToBuddy(pointerEvent);
					pointerEvent.RequestPointerCapture();
					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Up:
				if (pointerEvent.Button == MouseButton.Left && _pointerPressed) {
					var releaseInside = HitTest(pointerEvent.Position);
					_pointerPressed = false;
					_hovered = releaseInside;
					ForwardToBuddy(pointerEvent);
					if (releaseInside) {
						Activated?.Invoke(this);
					}

					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Click:
				_hovered = HitTest(pointerEvent.Position);
				break;
		}
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
		if (!_visible || !Visible || Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		DrawBackground(bounds);

		if (!string.IsNullOrEmpty(_text) && !string.IsNullOrWhiteSpace(_fontName)) {
			DrawText(bounds);
		}
	}

	protected override void OnDispose() {
		DisposeArt();
		base.OnDispose();
	}

	private void DrawBackground(Rectangle bounds) {
		if (_art is not null) {
			var slice = _art.Texture.GetSlice();
			var source = _art.Frames.GetFrameRegion(0);
			Raylib.DrawTexturePro(slice.Texture, source, bounds, Vector2.Zero, 0f, Color.White);
			return;
		}

		if (_imageArt is not null) {
			var slice = _imageArt.GetSlice();
			Raylib.DrawTexturePro(slice.Texture, slice.Source, bounds, Vector2.Zero, 0f, Color.White);
			return;
		}

		switch (_backgroundDraw) {
			case GT_DRAWTYPE.FILL:
				Raylib.DrawRectangleRec(bounds, _backgroundFill);
				break;
			case GT_DRAWTYPE.HASH:
				DrawHash(bounds, _backgroundFill);
				break;
		}
	}

	private void DrawHash(Rectangle bounds, Color color) {
		Raylib.DrawRectangleLinesEx(bounds, 1f, color);
		Raylib.BeginScissorMode(
			(int)MathF.Floor(bounds.X),
			(int)MathF.Floor(bounds.Y),
			(int)MathF.Ceiling(bounds.Width),
			(int)MathF.Ceiling(bounds.Height));

		try {
			for (var x = bounds.X - bounds.Height; x < bounds.X + bounds.Width; x += HashSpacing) {
				var start = new Vector2(x, bounds.Y + bounds.Height);
				var end = new Vector2(x + bounds.Height, bounds.Y);
				Raylib.DrawLineEx(start, end, 1f, color);
			}
		} finally {
			Raylib.EndScissorMode();
		}
	}

	private void DrawText(Rectangle bounds) {
		var lines = MeasureWrappedLines(bounds.Width);
		if (lines.Count == 0) {
			return;
		}

		var lineHeight = _fontSize;
		var contentHeight = lines.Count * lineHeight;
		var startY = ResolveAlignedY(bounds.Height, contentHeight);

		for (var index = 0; index < lines.Count; index++) {
			var line = lines[index];
			var textWidth = UiText.MeasureWidth(line, _fontSize, TextStyle);
			var textX = bounds.X + ResolveAlignedX(bounds.Width, textWidth);
			var textY = bounds.Y + startY + (index * lineHeight);
			if (_backdraw) {
				UiText.Draw(line, textX + ShadowOffset, textY + ShadowOffset, _fontSize, Color.Black, TextStyle);
			}

			UiText.Draw(line, textX, textY, _fontSize, NormalTextColor, TextStyle);
		}
	}

	private float ResolveAlignedX(float availableWidth, float textWidth) {
		return Alignment switch {
			StaticAlignment.Center => Math.Max(0f, (availableWidth - textWidth) * 0.5f),
			StaticAlignment.Right => Math.Max(0f, availableWidth - textWidth - AlignmentRightInset),
			StaticAlignment.TopLeft => AlignmentLeftInset,
			StaticAlignment.TenBySix => TenBySixXInset,
			StaticAlignment.LeftSix => LeftSixInset,
			_ => AlignmentLeftInset
		};
	}

	private float ResolveAlignedY(float availableHeight, float contentHeight) {
		return Alignment switch {
			StaticAlignment.Center => Math.Max(0f, (availableHeight - contentHeight) * 0.5f),
			StaticAlignment.TopLeft => AlignmentLeftInset,
			StaticAlignment.TenBySix => TenBySixYInset,
			_ => Math.Max(0f, (availableHeight - _fontSize) * 0.5f)
		};
	}

	private List<string> MeasureWrappedLines(float availableWidth) {
		if (string.IsNullOrEmpty(_text)) {
			return [];
		}

		var maxWidth = Math.Max(1f, availableWidth - (AlignmentLeftInset * 2f));
		var rawLines = _text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
		var result = new List<string>();

		foreach (var rawLine in rawLines) {
			if (string.IsNullOrEmpty(rawLine)) {
				result.Add(string.Empty);
				continue;
			}

			var remaining = rawLine.TrimEnd();
			while (!string.IsNullOrEmpty(remaining)) {
				if (UiText.MeasureWidth(remaining, _fontSize, TextStyle) <= maxWidth) {
					result.Add(remaining);
					break;
				}

				var breakIndex = FindWrapBreakIndex(remaining, maxWidth);
				if (breakIndex <= 0) {
					result.Add(remaining);
					break;
				}

				result.Add(remaining[..breakIndex].TrimEnd());
				remaining = remaining[breakIndex..].TrimStart();
			}
		}

		return result;
	}

	private int FindWrapBreakIndex(string text, float maxWidth) {
		var lastSpace = -1;
		for (var index = 0; index < text.Length; index++) {
			if (char.IsWhiteSpace(text[index])) {
				lastSpace = index;
			}

			var candidate = text[..(index + 1)];
			if (UiText.MeasureWidth(candidate, _fontSize, TextStyle) > maxWidth) {
				if (lastSpace > 0) {
					return lastSpace;
				}

				return Math.Max(1, index);
			}
		}

		return text.Length;
	}

	private void ForwardToBuddy(UiPointerEvent sourceEvent) {
		if (_buddyControl is null || !_hovered) {
			return;
		}

		var buddyEvent = new UiPointerEvent(
			sourceEvent.Kind,
			sourceEvent.Position,
			sourceEvent.Button,
			sourceEvent.WheelDelta,
			_buddyControl);
		_buddyControl.OnPointerEvent(buddyEvent);
	}

	private void DisposeArt() {
		_art?.Dispose();
		_art = null;
		_imageArt?.Dispose();
		_imageArt = null;
	}

	private void LoadArt(VfxAnimationDataRepository repository, string shapeId) {
		if (repository.TryGetAtlasByShapeId(shapeId, out var atlasEntry)) {
			_art = new AtlasFramesResource(
				repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: false),
				repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: true),
				TextureFilter.Point);
			return;
		}

		if (repository.TryGetImageByShapeId(shapeId, out var imageEntry)) {
			_imageArt = new CompressedTexture2D(repository.GetInterfaceAssetPath(imageEntry.Value)) {
				Filter = TextureFilter.Point
			};
		}
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
		return new Color(color.Red, color.Green, color.Blue, (byte)255);
	}

	public enum StaticAlignment : uint {
		Left = 0,
		Center = 1,
		Right = 2,
		TopLeft = 3,
		TenBySix = 4,
		LeftSix = 5
	}
}

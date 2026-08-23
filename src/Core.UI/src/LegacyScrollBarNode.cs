using System.Numerics;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored scrollbar with vertical-first behavior matching the Trim-era control closely enough
/// to compose into listboxes and other authored surfaces.
/// </summary>
public sealed class LegacyScrollBarNode : Control, IUiPointerEventHandler {
	private const float HighlightFactor = 1.5f;
	private const float ShadowFactor = 0.5f;
	private const float MinimumThumbArea = 10f;
	private const float MinimumThumbSize = 6f;
	private const float RepeatIntervalSeconds = 0.2f;
	private const float ScrollBreakoffDistance = 40f;

	private AtlasFramesResource? _trackArt;
	private AtlasFramesResource? _upButtonArt;
	private AtlasFramesResource? _downButtonArt;
	private bool _dragging;
	private bool _enabled = true;
	private bool _horizontal;
	private bool _hoveredDownButton;
	private bool _hoveredThumb;
	private bool _hoveredTrack;
	private bool _hoveredUpButton;
	private bool _paging;
	private bool _pagingActive;
	private RepeatAction _repeatAction = RepeatAction.None;
	private float _repeatAccumulator;
	private float _slideOffset;
	private int _oldPosition;
	private int _scrollPosition;
	private int _scrollRange;
	private int _viewRange;
	private bool _visible;

	public LegacyScrollBarNode(string? name = null) : base(name) {
	}

	public event Action<LegacyScrollBarNode, int>? ScrollPositionChanged;
	public event Action<LegacyScrollBarNode>? LineUpRequested;
	public event Action<LegacyScrollBarNode>? LineDownRequested;
	public event Action<LegacyScrollBarNode>? PageUpRequested;
	public event Action<LegacyScrollBarNode>? PageDownRequested;

	public Color ThumbColor { get; private set; } = new(63, 96, 128, 255);
	public Color BackgroundColor { get; private set; } = new(80, 111, 128, 255);
	public Color DisabledColor { get; private set; } = new(64, 64, 64, 255);
	public bool IsHorizontal => _horizontal;
	public bool IsActive => _scrollRange > _viewRange;
	public int ButtonWidth { get; private set; } = 24;
	public int ButtonHeight { get; private set; } = 18;
	public bool IsPointerInputEnabled => Visible && _visible && IsActive && Size.X > 0f && Size.Y > 0f;

	public void ApplyLegacyDefinition(GT_SCROLLBAR archetype, GT_BUTTON upButtonArchetype, GT_BUTTON downButtonArchetype,
		VfxAnimationDataRepository? repository = null) {
		ArgumentNullException.ThrowIfNull(archetype);
		ArgumentNullException.ThrowIfNull(upButtonArchetype);
		ArgumentNullException.ThrowIfNull(downButtonArchetype);

		DisposeArt();

		ThumbColor = ToColor(archetype.ThumbColor);
		BackgroundColor = ToColor(archetype.BackgroundColor);
		DisabledColor = ToColor(archetype.DisabledColor);
		_horizontal = archetype.Horizontal;

		if (repository is not null) {
			if (!string.IsNullOrWhiteSpace(archetype.ShapeFile)) {
				_trackArt = LoadArt(repository, archetype.ShapeFile);
			}

			if (!string.IsNullOrWhiteSpace(upButtonArchetype.ShapeFile)) {
				_upButtonArt = LoadArt(repository, upButtonArchetype.ShapeFile);
			}

			if (!string.IsNullOrWhiteSpace(downButtonArchetype.ShapeFile)) {
				_downButtonArt = LoadArt(repository, downButtonArchetype.ShapeFile);
			}
		}

		ButtonWidth = ResolveButtonWidth();
		ButtonHeight = ResolveButtonHeight();
	}

	public void EnableScrollBar(bool enabled) {
		_enabled = enabled;
		if (!enabled) {
			_dragging = false;
			_paging = false;
			_repeatAction = RepeatAction.None;
			_repeatAccumulator = 0f;
		}
	}

	public void SetVisible(bool visible) {
		_visible = visible;
		if (!visible) {
			_dragging = false;
			_paging = false;
			_repeatAction = RepeatAction.None;
			_repeatAccumulator = 0f;
		}
	}

	public int GetScrollPosition() {
		return _scrollPosition;
	}

	public int SetScrollPosition(int newIndex) {
		var clamped = ClampScrollPosition(newIndex);
		var previous = _scrollPosition;
		_scrollPosition = clamped;
		return previous;
	}

	public void SetScrollRange(int range) {
		_scrollRange = Math.Max(0, range);
		_scrollPosition = ClampScrollPosition(_scrollPosition);
	}

	public void SetViewRange(int viewSize) {
		_viewRange = Math.Max(0, viewSize);
		_scrollPosition = ClampScrollPosition(_scrollPosition);
	}

	public bool HitTest(Vector2 screenPoint) {
		return _visible && IsActive && ContainsPoint(screenPoint);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Enter:
			case UiPointerEventKind.Move:
				UpdateHover(pointerEvent.Position);
				if (_dragging && pointerEvent.Button is null) {
					ApplyPointerMotion(pointerEvent.Position);
					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Leave:
				if (!_dragging && !_paging) {
					ClearHover();
				}
				break;
			case UiPointerEventKind.Down:
				if (pointerEvent.Button == MouseButton.Left) {
					HandleLeftPointerDown(pointerEvent);
				}
				break;
			case UiPointerEventKind.Up:
				if (pointerEvent.Button == MouseButton.Left && (_dragging || _repeatAction != RepeatAction.None)) {
					_dragging = false;
					_paging = false;
					_pagingActive = false;
					_repeatAction = RepeatAction.None;
					_repeatAccumulator = 0f;
					UpdateHover(pointerEvent.Position);
					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Click:
				UpdateHover(pointerEvent.Position);
				break;
			case UiPointerEventKind.Wheel:
				if (!_enabled || !_visible || !IsActive) {
					break;
				}

				if (pointerEvent.WheelDelta >= 0f) {
					LineUpRequested?.Invoke(this);
				} else {
					LineDownRequested?.Invoke(this);
				}

				pointerEvent.MarkHandled();
				break;
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		if (!_visible || !_enabled || !IsActive || _repeatAction == RepeatAction.None) {
			return;
		}

		if (_paging && !_pagingActive) {
			return;
		}

		_repeatAccumulator += deltaTime;
		while (_repeatAccumulator >= RepeatIntervalSeconds) {
			_repeatAccumulator -= RepeatIntervalSeconds;
			InvokeRepeatAction(_repeatAction);
		}
	}

	protected override void Draw() {
		if (!_visible || !Visible || !IsActive || Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var trackBounds = GetTrackBounds();
		if (_enabled) {
			if (_trackArt is not null && _trackArt.Frames.Count > 0) {
				DrawFrame(_trackArt, 0, trackBounds);
			} else {
				Raylib.DrawRectangleRec(trackBounds, BackgroundColor);
			}
		} else {
			Raylib.DrawRectangleRec(trackBounds, DisabledColor);
		}

		var thumbBounds = GetThumbBounds();
		if (_enabled && GetThumbActive() && thumbBounds.Width > 0f && thumbBounds.Height > 0f) {
			if (_trackArt is not null && _trackArt.Frames.Count > 1) {
				DrawFrame(_trackArt, 1, thumbBounds);
			} else {
				DrawPrimitiveThumb(thumbBounds);
			}
		}

		DrawButton(_upButtonArt, GetTopLeftButtonBounds(), _hoveredUpButton, _repeatAction == RepeatAction.LineUp);
		DrawButton(_downButtonArt, GetBottomRightButtonBounds(), _hoveredDownButton, _repeatAction == RepeatAction.LineDown);
	}

	protected override void OnDispose() {
		DisposeArt();
		base.OnDispose();
	}

	private void ApplyPointerMotion(Vector2 position) {
		var trackBounds = GetTrackBounds();
		var thumbBounds = GetThumbBounds();
		if (_horizontal) {
			if ((position.Y < trackBounds.Y - ScrollBreakoffDistance || position.Y > trackBounds.Y + trackBounds.Height + ScrollBreakoffDistance)
				&& _scrollPosition != _oldPosition) {
				EmitScrollPositionChanged(_oldPosition);
				return;
			}

			var thumbWidth = thumbBounds.Width;
			var slideDistance = Math.Max(1f, trackBounds.Width - thumbWidth);
			var newLeft = Math.Clamp(position.X - _slideOffset, trackBounds.X, trackBounds.X + slideDistance);
			var ratio = slideDistance <= 0f ? 0f : (newLeft - trackBounds.X) / slideDistance;
			var newPosition = (int)MathF.Round(ratio * Math.Max(0, _scrollRange - _viewRange), MidpointRounding.AwayFromZero);
			EmitScrollPositionChanged(newPosition);
			return;
		}

		if (position.X < trackBounds.X - ScrollBreakoffDistance || position.X > trackBounds.X + trackBounds.Width + ScrollBreakoffDistance) {
			EmitScrollPositionChanged(_oldPosition);
			return;
		}

		var thumbHeight = thumbBounds.Height;
		var slideDist = Math.Max(1f, trackBounds.Height - thumbHeight);
		var newTop = Math.Clamp(position.Y - _slideOffset, trackBounds.Y, trackBounds.Y + slideDist);
		var verticalRatio = slideDist <= 0f ? 0f : (newTop - trackBounds.Y) / slideDist;
		var newScrollPosition = (int)MathF.Round(verticalRatio * Math.Max(0, _scrollRange - _viewRange), MidpointRounding.AwayFromZero);
		EmitScrollPositionChanged(newScrollPosition);
	}

	private void ClearHover() {
		_hoveredUpButton = false;
		_hoveredDownButton = false;
		_hoveredTrack = false;
		_hoveredThumb = false;
	}

	private int ClampScrollPosition(int value) {
		var max = Math.Max(0, _scrollRange - _viewRange);
		return Math.Clamp(value, 0, max);
	}

	private void DisposeArt() {
		_trackArt?.Dispose();
		_upButtonArt?.Dispose();
		_downButtonArt?.Dispose();
		_trackArt = null;
		_upButtonArt = null;
		_downButtonArt = null;
	}

	private static void DrawFrame(AtlasFramesResource resource, int frameIndex, Rectangle destination) {
		var slice = resource.Texture.GetSlice();
		var safeIndex = Math.Clamp(frameIndex, 0, Math.Max(0, resource.Frames.Count - 1));
		var source = resource.Frames.GetFrameRegion(safeIndex);
		Raylib.DrawTexturePro(slice.Texture, source, destination, Vector2.Zero, 0f, Color.White);
	}

	private void DrawButton(AtlasFramesResource? art, Rectangle bounds, bool hovered, bool pressed) {
		if (bounds.Width <= 0f || bounds.Height <= 0f) {
			return;
		}

		if (art is not null) {
			DrawFrame(art, ResolveButtonFrameIndex(art.Frames.Count, hovered, pressed), bounds);
			return;
		}

		var fill = !_enabled ? DisabledColor : pressed ? Darken(BackgroundColor, 0.2f) : hovered ? Lighten(BackgroundColor, 0.12f) : BackgroundColor;
		Raylib.DrawRectangleRec(bounds, fill);
		Raylib.DrawRectangleLinesEx(bounds, 1f, _enabled ? Lighten(fill, 0.2f) : DisabledColor);
	}

	private void DrawPrimitiveThumb(Rectangle thumbBounds) {
		var bright = ScaleColor(ThumbColor, HighlightFactor);
		var dark = ScaleColor(ThumbColor, ShadowFactor);
		Raylib.DrawLineEx(new Vector2(thumbBounds.X, thumbBounds.Y), new Vector2(thumbBounds.X + thumbBounds.Width - 1f, thumbBounds.Y), 1f, bright);
		Raylib.DrawLineEx(new Vector2(thumbBounds.X, thumbBounds.Y + 1f), new Vector2(thumbBounds.X + thumbBounds.Width - 2f, thumbBounds.Y + 1f), 1f, bright);
		Raylib.DrawLineEx(new Vector2(thumbBounds.X, thumbBounds.Y + 2f), new Vector2(thumbBounds.X, thumbBounds.Y + thumbBounds.Height - 1f), 1f, bright);
		Raylib.DrawLineEx(new Vector2(thumbBounds.X + 1f, thumbBounds.Y + 2f), new Vector2(thumbBounds.X + 1f, thumbBounds.Y + thumbBounds.Height - 2f), 1f, bright);
		Raylib.DrawLineEx(new Vector2(thumbBounds.X, thumbBounds.Y + thumbBounds.Height), new Vector2(thumbBounds.X + thumbBounds.Width, thumbBounds.Y + thumbBounds.Height), 1f, dark);
		Raylib.DrawLineEx(new Vector2(thumbBounds.X + 1f, thumbBounds.Y + thumbBounds.Height - 1f), new Vector2(thumbBounds.X + thumbBounds.Width, thumbBounds.Y + thumbBounds.Height - 1f), 1f, dark);
		Raylib.DrawLineEx(new Vector2(thumbBounds.X + thumbBounds.Width, thumbBounds.Y), new Vector2(thumbBounds.X + thumbBounds.Width, thumbBounds.Y + thumbBounds.Height - 2f), 1f, dark);
		Raylib.DrawLineEx(new Vector2(thumbBounds.X + thumbBounds.Width - 1f, thumbBounds.Y + 1f), new Vector2(thumbBounds.X + thumbBounds.Width - 1f, thumbBounds.Y + thumbBounds.Height - 2f), 1f, dark);
		Raylib.DrawRectangleRec(new Rectangle(thumbBounds.X + 2f, thumbBounds.Y + 2f, Math.Max(0f, thumbBounds.Width - 4f), Math.Max(0f, thumbBounds.Height - 4f)), ThumbColor);
	}

	private void EmitScrollPositionChanged(int requestedPosition) {
		var clamped = ClampScrollPosition(requestedPosition);
		if (_scrollPosition == clamped) {
			return;
		}

		_scrollPosition = clamped;
		ScrollPositionChanged?.Invoke(this, clamped);
	}

	private Rectangle GetBottomRightButtonBounds() {
		var bounds = GlobalBounds;
		return _horizontal
			? new Rectangle(bounds.X + bounds.Width - ButtonWidth, bounds.Y, ButtonWidth, bounds.Height)
			: new Rectangle(bounds.X, bounds.Y + bounds.Height - ButtonHeight, bounds.Width, ButtonHeight);
	}

	private bool GetThumbActive() {
		var trackBounds = GetTrackBounds();
		return _enabled && (_horizontal ? trackBounds.Width : trackBounds.Height) > MinimumThumbArea;
	}

	private Rectangle GetThumbBounds() {
		if (!GetThumbActive()) {
			return new Rectangle();
		}

		var trackBounds = GetTrackBounds();
		if (_horizontal) {
			var thumbWidth = _scrollRange > 0 ? trackBounds.Width * (_viewRange / (float)_scrollRange) : 0f;
			thumbWidth = Math.Max(MinimumThumbSize, thumbWidth);
			var scrollDelta = _scrollRange - _viewRange > 0
				? (trackBounds.Width - thumbWidth) / (_scrollRange - _viewRange)
				: 0f;
			return new Rectangle(trackBounds.X + (_scrollPosition * scrollDelta), trackBounds.Y, thumbWidth, trackBounds.Height);
		}

		var thumbHeight = _scrollRange > 0 ? trackBounds.Height * (_viewRange / (float)_scrollRange) : 0f;
		thumbHeight = Math.Max(MinimumThumbSize, thumbHeight);
		var verticalDelta = _scrollRange - _viewRange > 0
			? (trackBounds.Height - thumbHeight) / (_scrollRange - _viewRange)
			: 0f;
		var width = _trackArt is not null && _trackArt.Frames.Count > 1
			? _trackArt.Frames.GetFrameRegion(1).Width
			: trackBounds.Width;
		var x = trackBounds.X + ((trackBounds.Width - width) * 0.5f);
		return new Rectangle(x, trackBounds.Y + (_scrollPosition * verticalDelta), width, thumbHeight);
	}

	private Rectangle GetTopLeftButtonBounds() {
		var bounds = GlobalBounds;
		return _horizontal
			? new Rectangle(bounds.X, bounds.Y, ButtonWidth, bounds.Height)
			: new Rectangle(bounds.X, bounds.Y, bounds.Width, ButtonHeight);
	}

	private Rectangle GetTrackBounds() {
		var bounds = GlobalBounds;
		return _horizontal
			? new Rectangle(bounds.X + ButtonWidth, bounds.Y, Math.Max(0f, bounds.Width - (ButtonWidth * 2f)), bounds.Height)
			: new Rectangle(bounds.X, bounds.Y + ButtonHeight, bounds.Width, Math.Max(0f, bounds.Height - (ButtonHeight * 2f)));
	}

	private void HandleLeftPointerDown(UiPointerEvent pointerEvent) {
		if (!_enabled || !_visible || !IsActive) {
			return;
		}

		var position = pointerEvent.Position;
		if (Raylib.CheckCollisionPointRec(position, GetTopLeftButtonBounds())) {
			UpdateHover(position);
			_repeatAction = RepeatAction.LineUp;
			_repeatAccumulator = 0f;
			InvokeRepeatAction(_repeatAction);
			pointerEvent.RequestPointerCapture();
			pointerEvent.MarkHandled();
			return;
		}

		if (Raylib.CheckCollisionPointRec(position, GetBottomRightButtonBounds())) {
			UpdateHover(position);
			_repeatAction = RepeatAction.LineDown;
			_repeatAccumulator = 0f;
			InvokeRepeatAction(_repeatAction);
			pointerEvent.RequestPointerCapture();
			pointerEvent.MarkHandled();
			return;
		}

		if (!GetThumbActive()) {
			return;
		}

		var thumbBounds = GetThumbBounds();
		if (Raylib.CheckCollisionPointRec(position, thumbBounds)) {
			_oldPosition = _scrollPosition;
			_dragging = true;
			_slideOffset = _horizontal ? position.X - thumbBounds.X : position.Y - thumbBounds.Y;
			pointerEvent.RequestPointerCapture();
			pointerEvent.MarkHandled();
			return;
		}

		var trackBounds = GetTrackBounds();
		if (!Raylib.CheckCollisionPointRec(position, trackBounds)) {
			return;
		}

		_paging = true;
		_pagingActive = true;
		_repeatAccumulator = 0f;
		_repeatAction = _horizontal
			? position.X < thumbBounds.X ? RepeatAction.PageUp : RepeatAction.PageDown
			: position.Y < thumbBounds.Y ? RepeatAction.PageUp : RepeatAction.PageDown;
		InvokeRepeatAction(_repeatAction);
		pointerEvent.RequestPointerCapture();
		pointerEvent.MarkHandled();
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

	private void InvokeRepeatAction(RepeatAction action) {
		switch (action) {
			case RepeatAction.LineUp:
				LineUpRequested?.Invoke(this);
				break;
			case RepeatAction.LineDown:
				LineDownRequested?.Invoke(this);
				break;
			case RepeatAction.PageUp:
				PageUpRequested?.Invoke(this);
				break;
			case RepeatAction.PageDown:
				PageDownRequested?.Invoke(this);
				break;
		}
	}

	private int ResolveButtonFrameIndex(int frameCount, bool hovered, bool pressed) {
		if (!_enabled) {
			return 0;
		}

		if (pressed) {
			return Math.Min(frameCount - 1, 3);
		}

		if (hovered) {
			return Math.Min(frameCount - 1, 2);
		}

		return Math.Min(frameCount - 1, 1);
	}

	private int ResolveButtonHeight() {
		var upHeight = _upButtonArt?.Frames.GetFrameRegion(0).Height ?? ButtonHeight;
		var downHeight = _downButtonArt?.Frames.GetFrameRegion(0).Height ?? ButtonHeight;
		return (int)MathF.Max(upHeight, downHeight);
	}

	private int ResolveButtonWidth() {
		var upWidth = _upButtonArt?.Frames.GetFrameRegion(0).Width ?? ButtonWidth;
		var downWidth = _downButtonArt?.Frames.GetFrameRegion(0).Width ?? ButtonWidth;
		var trackWidth = _trackArt is not null && _trackArt.Frames.Count > 1 ? _trackArt.Frames.GetFrameRegion(1).Width : 0f;
		return (int)MathF.Max(trackWidth, MathF.Max(upWidth, downWidth));
	}

	private static Color ScaleColor(Color color, float factor) {
		return new Color(
			(int)Math.Clamp(MathF.Round(color.R * factor), 0, 255),
			(int)Math.Clamp(MathF.Round(color.G * factor), 0, 255),
			(int)Math.Clamp(MathF.Round(color.B * factor), 0, 255),
			color.A);
	}

	private static Color ToColor(GT_COLOR color) {
		return new Color((byte)color.Red, (byte)color.Green, (byte)color.Blue, (byte)255);
	}

	private void UpdateHover(Vector2 position) {
		var trackBounds = GetTrackBounds();
		var thumbBounds = GetThumbBounds();
		_hoveredUpButton = Raylib.CheckCollisionPointRec(position, GetTopLeftButtonBounds());
		_hoveredDownButton = Raylib.CheckCollisionPointRec(position, GetBottomRightButtonBounds());
		_hoveredTrack = Raylib.CheckCollisionPointRec(position, trackBounds);
		_hoveredThumb = thumbBounds.Width > 0f && thumbBounds.Height > 0f && Raylib.CheckCollisionPointRec(position, thumbBounds);
		_pagingActive = !_dragging && _paging && _hoveredTrack;
	}

	private static Color Darken(Color color, float amount) {
		return Lighten(color, -amount);
	}

	private static Color Lighten(Color color, float amount) {
		amount = Math.Clamp(amount, -1f, 1f);
		return new Color(
			(int)Math.Clamp(MathF.Round(color.R + (255f * amount)), 0, 255),
			(int)Math.Clamp(MathF.Round(color.G + (255f * amount)), 0, 255),
			(int)Math.Clamp(MathF.Round(color.B + (255f * amount)), 0, 255),
			color.A);
	}

	private enum RepeatAction {
		None,
		LineUp,
		LineDown,
		PageUp,
		PageDown
	}
}

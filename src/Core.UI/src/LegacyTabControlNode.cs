using System.Numerics;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored tab strip container with selected-page switching.
/// </summary>
public sealed class LegacyTabControlNode : Control, ILegacyKeyboardFocusable {
	private readonly List<LegacyTabButtonNode> _tabs = [];
	private AtlasFramesResource? _art;
	private bool _enabled = true;
	private bool _hasKeyboardFocus;
	private bool _keyboardFocusEnabled;
	private bool _visible = true;
	private TABCONTROL_DATA? _layoutData;

	public LegacyTabControlNode(string? name = null) : base(name) {
	}

	public event Action<LegacyTabControlNode, int>? SelectedTabChanged;

	public int SelectedTabIndex { get; private set; }

	public IReadOnlyList<LegacyTabButtonNode> Tabs => _tabs;

	public bool UpperTabs { get; private set; }

	public void ApplyLegacyDefinition(
		GT_TABCONTROL tabArchetype,
		GT_HOTBUTTON hotButtonArchetype,
		GT_VFXSHAPE shapeArchetype,
		TABCONTROL_DATA data,
		IReadOnlyList<string>? labels = null,
		VfxAnimationDataRepository? repository = null) {
		ArgumentNullException.ThrowIfNull(tabArchetype);
		ArgumentNullException.ThrowIfNull(hotButtonArchetype);
		ArgumentNullException.ThrowIfNull(shapeArchetype);
		ArgumentNullException.ThrowIfNull(data);

		DisposeArt();
		ClearTabs();
		_layoutData = data;
		UpperTabs = data.UpperTabs != 0;

		if (!string.IsNullOrWhiteSpace(shapeArchetype.Filename) && repository is not null) {
			_art = LoadArt(repository, shapeArchetype.Filename);
		}

		Position = new Vector2(data.XPos, data.YPos);
		Size = ResolveControlSize(data);
		var textLabels = labels ?? [];
		var baseImage = data.BaseImage == 0 ? 1 : data.BaseImage;
		var headerX = 0f;
		var headerY = UpperTabs ? -ResolveHeaderHeight(baseImage) : Size.Y;

		for (var i = 0; i < data.NumTabs; i++) {
			var tab = AddChild(new LegacyTabButtonNode($"{Name}Tab{i + 1}"));
			tab.TabIndex = i;
			tab.Text = i < textLabels.Count ? textLabels[i] : $"Tab {i + 1}";
			tab.SetTextColors(
				ToColor(tabArchetype.NormalColor),
				ToColor(tabArchetype.HiliteColor),
				ToColor(tabArchetype.SelectedColor));

			var headerData = new HOTBUTTON_DATA {
				BaseImage = (uint)(baseImage + 1 + (i * 4)),
				XOrigin = (int)headerX,
				YOrigin = (int)headerY
			};
			tab.ApplyLegacyDefinition(hotButtonArchetype, shapeArchetype, headerData, (int)headerData.BaseImage, repository);
			tab.PageOffset = new Vector2(-headerX, 0f);
			tab.Activated += HandleTabActivated;
			if (_keyboardFocusEnabled) {
				tab.EnableKeyboardFocusing();
			}

			_tabs.Add(tab);
			headerX += tab.Size.X;
		}

		SetCurrentTab(Math.Clamp(SelectedTabIndex, 0, Math.Max(0, _tabs.Count - 1)), emitEvent: false);
	}

	public T AddTabPageChild<T>(int tabIndex, T child, bool registerForFocus = true) where T : Node {
		return GetTab(tabIndex).AddPageChild(child, registerForFocus);
	}

	public void EnableKeyboardFocusing() {
		_keyboardFocusEnabled = true;
		for (var i = 0; i < _tabs.Count; i++) {
			_tabs[i].EnableKeyboardFocusing();
		}
	}

	public void EnableControl(bool enabled) {
		_enabled = enabled;
		for (var i = 0; i < _tabs.Count; i++) {
			_tabs[i].EnableButton(enabled);
		}

		if (!enabled) {
			_hasKeyboardFocus = false;
		}
	}

	public void SetVisible(bool visible) {
		_visible = visible;
		for (var i = 0; i < _tabs.Count; i++) {
			_tabs[i].SetVisible(visible);
		}
	}

	public bool SetKeyboardFocus(bool enabled) {
		_hasKeyboardFocus = enabled && _enabled && _visible && Visible;
		if (_tabs.Count == 0) {
			return false;
		}

		if (!_hasKeyboardFocus) {
			for (var i = 0; i < _tabs.Count; i++) {
				_tabs[i].SetSelected(i == SelectedTabIndex);
				_tabs[i].SetKeyboardFocusActive(false);
			}

			return false;
		}

		SetCurrentTab(SelectedTabIndex, emitEvent: false);
		return true;
	}

	public LegacyTabButtonNode GetTab(int tabIndex) {
		if (tabIndex < 0 || tabIndex >= _tabs.Count) {
			throw new ArgumentOutOfRangeException(nameof(tabIndex));
		}

		return _tabs[tabIndex];
	}

	public int SetCurrentTab(int tabIndex) {
		return SetCurrentTab(tabIndex, emitEvent: true);
	}

	public int SetCurrentTabSilently(int tabIndex) {
		return SetCurrentTab(tabIndex, emitEvent: false);
	}

	public void SetDefaultControlForTab(int tabIndex, ILegacyKeyboardFocusable? focusable) {
		GetTab(tabIndex).SetDefaultFocusControl(focusable);
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		if (!_visible || !_enabled || !Visible || !_hasKeyboardFocus || !_keyboardFocusEnabled || _tabs.Count == 0) {
			return;
		}

		if (!Raylib.IsKeyPressed(KeyboardKey.Tab)) {
			return;
		}

		var reverse = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
		SelectRelative(reverse ? -1 : 1);
	}

	protected override void Draw() {
		if (!_visible || !Visible || Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		if (_art is not null) {
			DrawArt(bounds);
			return;
		}

		Raylib.DrawRectangleRec(bounds, new Color(10, 14, 20, 255));
		Raylib.DrawRectangleLinesEx(bounds, 1f, new Color(66, 88, 118, 255));
	}

	protected override void OnDispose() {
		DisposeArt();
		base.OnDispose();
	}

	private void ClearTabs() {
		for (var i = _tabs.Count - 1; i >= 0; i--) {
			if (RemoveChild(_tabs[i])) {
				_tabs[i].Dispose();
			}
		}

		_tabs.Clear();
	}

	private void DisposeArt() {
		_art?.Dispose();
		_art = null;
	}

	private void DrawArt(Rectangle bounds) {
		if (_layoutData is null) {
			return;
		}

		var frameIndex = _layoutData.BaseImage == 0 ? 1 : _layoutData.BaseImage;
		if (frameIndex < 0 || frameIndex >= _art!.Frames.Count) {
			return;
		}

		var source = _art.Frames.GetFrameRegion(frameIndex);
		if (source.Width < 16 || source.Height < 16) {
			return;
		}

		var slice = _art.Texture.GetSlice();
		Raylib.DrawTexturePro(slice.Texture, source, bounds, Vector2.Zero, 0f, Color.White);
	}

	private void HandleTabActivated(LegacyTabButtonNode tab) {
		SetCurrentTab(tab.TabIndex);
	}

	private float ResolveHeaderHeight(int baseImage) {
		if (_art is null) {
			return 18f;
		}

		var headerFrameIndex = Math.Clamp(baseImage + 1, 0, _art.Frames.Count - 1);
		return _art.Frames.GetFrameRegion(headerFrameIndex).Height;
	}

	private Vector2 ResolveControlSize(TABCONTROL_DATA data) {
		if (_art is not null) {
			var frameIndex = data.BaseImage == 0 ? 1 : data.BaseImage;
			if (frameIndex >= 0 && frameIndex < _art.Frames.Count) {
				var frame = _art.Frames.GetFrameRegion(frameIndex);
				return new Vector2(frame.Width, frame.Height);
			}
		}

		var width = Math.Max(1, data.NumTabs * 64);
		return new Vector2(width, 32f);
	}

	private void SelectRelative(int delta) {
		if (_tabs.Count == 0) {
			return;
		}

		var next = (SelectedTabIndex + delta + _tabs.Count) % _tabs.Count;
		SetCurrentTab(next);
	}

	private int SetCurrentTab(int tabIndex, bool emitEvent) {
		if (tabIndex < 0 || tabIndex >= _tabs.Count) {
			return -1;
		}

		var oldTab = SelectedTabIndex;
		SelectedTabIndex = tabIndex;
		for (var i = 0; i < _tabs.Count; i++) {
			_tabs[i].SetSelected(i == tabIndex);
			_tabs[i].SetKeyboardFocusActive(_hasKeyboardFocus && i == tabIndex);
		}

		if (emitEvent && oldTab != tabIndex) {
			SelectedTabChanged?.Invoke(this, tabIndex);
		}

		return oldTab;
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

	private static Color ToColor(GT_COLOR color) {
		return new Color(color.Red, color.Green, color.Blue, (byte)255);
	}
}

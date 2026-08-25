using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.UserProfiles;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class LegacyOptionsModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private readonly Action _closed;
	private readonly GT_NEWPLAYER _newPlayer;
	private readonly GT_OPTIONS _options;
	private readonly LegacyRcStringResolver _strings;
	private readonly UserProfilesRepository _userProfilesRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly List<LegacyButtonNode> _interactiveButtons = [];
	private LegacyDropdownNode? _deviceDropdown;
	private LegacyListBoxNode? _playerList;
	private LegacyStaticNode? _playerNameLabel;
	private LegacyDropdownNode? _resolutionDropdown;
	private LegacySliderNode? _gammaSlider;
	private LegacySliderNode? _playerMouseSlider;
	private LegacyTabControlNode? _tabControl;
	private LegacyNewUserModal? _newUserModal;

	public LegacyOptionsModal(
		GT_OPTIONS options,
		GT_NEWPLAYER newPlayer,
		UserProfilesRepository userProfilesRepository,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action closed) : base("LegacyOptionsModal") {
		_options = options;
		_newPlayer = newPlayer;
		_userProfilesRepository = userProfilesRepository;
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
		_closed = closed;
	}

	protected override void OnInitialize() {
		base.OnInitialize();
		ContentRoot.Position = ResolveScreenPosition(_options.ScreenRect);

		AddStaticNode("Background", _options.Background);
		AddStaticNode("Title", _options.Title);

		_tabControl = AddTabControl();
		BuildPlayerTab();
		BuildGraphicsTab();
		BuildSoundTab();

		var ok = AddButtonNode("ButtonOk", _options.ButtonOk);
		var cancel = AddButtonNode("ButtonCancel", _options.ButtonCancel);
		ok.Activated += _ => CloseModal();
		cancel.Activated += _ => CloseModal();
		ok.SetKeyboardFocus(true);
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);
		if (_newUserModal is not null) {
			return;
		}

		if (Input.IsActionJustPressed(InputManager.UiEscapeAction)) {
			CloseModal();
		}
	}

	protected override void OnEnterTree() {
		base.OnEnterTree();
		WriteHitZoneSnapshot("enter-tree");
	}

	private void WriteHitZoneSnapshot(string reason) {
		try {
			var path = LegacyUiHitZoneSnapshot.WriteToArtifacts(this, Name, reason);
			AppLog.Info(Name, $"Wrote legacy options hit-zone snapshot to '{path}'.");
		} catch (Exception ex) {
			AppLog.Warning(Name, $"Failed to write legacy options hit-zone snapshot: {ex.Message}");
		}
	}

	private void BuildPlayerTab() {
		_playerNameLabel = AddTabStatic(0, "StaticName", _options.StaticName);

		_playerList = AddTabListBox(0, "PlayerList", _options.ListNames);
		_playerList.SetPointerHitInsets(right: 13f);
		_playerList.CommitOnDoubleClick = true;
		_playerList.SelectionCommitted += list => {
			_userProfilesRepository.SetCurrentUser(list.SelectedLabel);
			UpdatePlayerNameLabel(list.SelectedLabel);
		};
		RefreshPlayerList();

		var buttonNew = AddTabButton(0, "ButtonNew", _options.ButtonNew);
		var buttonChange = AddTabButton(0, "ButtonChange", _options.ButtonChange);
		var buttonDelete = AddTabButton(0, "ButtonDelete", _options.ButtonDelete);
		buttonNew.Activated += _ => OpenNewUserModal();
		buttonChange.Activated += _ => OpenChangeUserModal();
		buttonDelete.Activated += _ => DeleteSelectedUser();
		AddCheckboxWithLabel(0, "PushDInput", _options.StaticDInput, _options.PushDInput, initialState: false);
		_playerMouseSlider = AddLabeledSlider(0, "SliderMouse", _options.StaticMouse, _options.SliderMouse, 0, 10, 7);
		AddLabeledSlider(0, "SliderSpeed", _options.StaticSpeed, _options.SliderSpeed, 0, 10, 5);
		AddLabeledSlider(0, "SliderScroll", _options.StaticScroll, _options.SliderScroll, 0, 10, 6);
		AddCheckboxWithLabel(0, "PushStatus", _options.StaticStatus, _options.PushStatus, initialState: true);
		AddCheckboxWithLabel(0, "PushRollover", _options.StaticRollover, _options.PushRollover, initialState: true);
		AddCheckboxWithLabel(0, "PushSectorMap", _options.StaticSectorMap, _options.PushSectorMap, initialState: true);
		AddCheckboxWithLabel(0, "PushRightClick", _options.StaticRightClick, _options.PushRightClick, initialState: true);
		AddCheckboxWithLabel(0, "PushSubtitles", _options.StaticSubtitles, _options.PushSubtitles, initialState: true);

		_tabControl!.SetDefaultControlForTab(0, _playerList);
	}

	private void BuildGraphicsTab() {
		AddCheckboxWithLabel(1, "Push3DHardware", _options.Static3DHardware, _options.Push3DHardware, initialState: true);
		AddTabStatic(1, "StaticDevice", _options.StaticDevice);

		_resolutionDropdown = AddTabDropdown(1, "ResolutionDropdown", _options.DropResolution);
		foreach (var resolution in new[] { "800x600", "1024x768", "1280x720", "1600x900", "1920x1080" }) {
			_resolutionDropdown.AddString(resolution);
		}
		_resolutionDropdown.SetCurrentSelection(1);

		_deviceDropdown = AddTabDropdown(1, "DeviceDropdown", _options.DropDevice);
		foreach (var device in new[] { "Primary Display Driver", "OpenGL HAL", "Direct3D HAL" }) {
			_deviceDropdown.AddString(device);
		}
		_deviceDropdown.SetCurrentSelection(0);

		AddTabStatic(1, "StaticResolution", _options.StaticResolution);
		AddTabStatic(1, "StaticGamma", _options.StaticGamma);
		_gammaSlider = AddTabSlider(1, "GammaSlider", _options.SliderGamma, 0, 10, 6);
		AddLabeledSlider(1, "DrawBackSlider", _options.StaticDrawBack, _options.SlideDrawBack, 0, 10, 7);
		AddLabeledSlider(1, "Ships3DSlider", _options.StaticShips3D, _options.SliderShips3D, 0, 10, 8);
		AddCheckboxWithLabel(1, "PushTrails", _options.StaticTrails, _options.PushTrails, initialState: true);
		AddCheckboxWithLabel(1, "PushEmissive", _options.StaticEmissive, _options.PushEmissive, initialState: true);
		AddCheckboxWithLabel(1, "PushDetail", _options.StaticDetail, _options.PushDetail, initialState: true);

		_tabControl!.SetDefaultControlForTab(1, _resolutionDropdown);
	}

	private void BuildSoundTab() {
		AddCheckboxWithLabel(2, "PushSound", _options.StaticSound, _options.PushSound, initialState: true);
		var soundSlider = AddTabSlider(2, "SliderSound", _options.SliderSound, 0, 10, 8);
		AddCheckboxWithLabel(2, "PushMusic", _options.StaticMusic, _options.PushMusic, initialState: true);
		AddTabSlider(2, "SliderMusic", _options.SliderMusic, 0, 10, 6);
		AddCheckboxWithLabel(2, "PushComm", _options.StaticComm, _options.PushComm, initialState: true);
		AddTabSlider(2, "SliderComm", _options.SliderComm, 0, 10, 7);
		AddTabStatic(2, "StaticChat", _options.StaticChat);
		AddTabSlider(2, "SliderChat", _options.SliderChat, 0, 10, 5);

		_tabControl!.SetDefaultControlForTab(2, soundSlider);
	}

	private void RefreshPlayerList(string? preferredSelectedUser = null) {
		if (_playerList is null) {
			return;
		}

		var profiles = _userProfilesRepository.Load();
		_playerList.ResetContent();
		foreach (var user in profiles.Users) {
			_playerList.AddString(user.Name);
		}

		var selectedUser = string.IsNullOrWhiteSpace(preferredSelectedUser)
			? profiles.CurrentUser
			: preferredSelectedUser;
		var selectedIndex = profiles.Users
			.Select((user, index) => (user, index))
			.FirstOrDefault(tuple => string.Equals(tuple.user.Name, selectedUser, StringComparison.OrdinalIgnoreCase))
			.index;
		if (profiles.Users.Count > 0) {
			_playerList.SetCurrentSelection(Math.Clamp(selectedIndex, 0, profiles.Users.Count - 1));
			UpdatePlayerNameLabel(profiles.Users[Math.Clamp(selectedIndex, 0, profiles.Users.Count - 1)].Name);
			return;
		}

		UpdatePlayerNameLabel(string.Empty);
	}

	private void DeleteSelectedUser() {
		if (_playerList is null) {
			return;
		}

		var selectedUser = _playerList.SelectedLabel;
		if (_userProfilesRepository.TryDeleteUser(selectedUser, out _, out _)) {
			RefreshPlayerList();
			return;
		}
	}

	private void OpenNewUserModal() {
		OpenUserModal(string.Empty);
	}

	private void OpenChangeUserModal() {
		if (_playerList is null || string.IsNullOrWhiteSpace(_playerList.SelectedLabel)) {
			return;
		}

		OpenUserModal(_playerList.SelectedLabel);
	}

	private void OpenUserModal(string existingName) {
		if (_newUserModal is not null) {
			return;
		}

		SetInteractiveState(false);
		_newUserModal = AddChild(new LegacyNewUserModal(
			_newPlayer,
			_userProfilesRepository,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			OnUserSaved,
			existingName,
			CloseNewUserModal));
	}

	private void OnUserSaved(UserProfilesData profiles, string selectedUser) {
		CloseNewUserModal();
		RefreshPlayerList(selectedUser);
		UpdatePlayerNameLabel(selectedUser);
	}

	private void CloseNewUserModal() {
		if (_newUserModal is not null && RemoveChild(_newUserModal)) {
			_newUserModal.Dispose();
		}

		_newUserModal = null;
		SetInteractiveState(true);
	}

	private void SetInteractiveState(bool enabled) {
		_tabControl?.EnableControl(enabled);
		foreach (var button in _interactiveButtons) {
			button.EnableButton(enabled);
		}
	}

	private void UpdatePlayerNameLabel(string currentUser) {
		if (_playerNameLabel is null) {
			return;
		}

		var baseText = ResolveString(_options.StaticName.StaticText, "Player Name");
		_playerNameLabel.SetText(string.IsNullOrWhiteSpace(currentUser)
			? baseText
			: $"{baseText} {currentUser}");
	}

	private void CloseModal() {
		if (_newUserModal is not null) {
			CloseNewUserModal();
		}

		_closed();
	}

	private LegacyTabControlNode AddTabControl() {
		var tab = ContentRoot.AddChild(new LegacyTabControlNode("OptionsTabControl"));
		var tabArchetype = ReadTypedEntry<GT_TABCONTROL>("GT_TABCONTROL", _options.Tab.TabControlType);
		var hotButtonArchetype = ReadTypedEntry<GT_HOTBUTTON>("GT_HOTBUTTON", _options.Tab.HotButtonType);
		var tabShape = ReadTypedEntry<GT_VFXSHAPE>("GT_VFXSHAPE", "VFXShape!!TabOptions");
		tab.ApplyLegacyDefinition(
			tabArchetype,
			hotButtonArchetype,
			tabShape,
			_options.Tab,
			ResolveTabLabels(_options.Tab.TextIds),
			_vfxRepository);
		tab.SelectedTabChanged += (_, tabIndex) => WriteHitZoneSnapshot($"tab-{tabIndex}");
		var centeredModalPosition = ResolveScreenPosition(_options.ScreenRect);
		var authoredToCenteredOffsetX = centeredModalPosition.X - _options.ScreenRect.Left;

		if (tab.Tabs.Count > 0) {
			tab.Tabs[0].PageOffset += new Vector2(0, 5f);
		}
		
		if (tab.Tabs.Count > 1) {
			tab.Tabs[1].PageOffset += new Vector2(authoredToCenteredOffsetX, 0f) + new Vector2(50f, 5f);
		}

		if (tab.Tabs.Count > 2) {
			tab.Tabs[2].PageOffset += new Vector2(authoredToCenteredOffsetX, 0f) + new Vector2(200f, 5f);
		}
		tab.EnableKeyboardFocusing();
		tab.SetKeyboardFocus(true);
		return tab;
	}

	private IReadOnlyList<string> ResolveTabLabels(IEnumerable<uint> textIds) {
		return textIds
			.Take(Math.Max(0, _options.Tab.NumTabs))
			.Select(id => ResolveString(id, $"Tab {id}"))
			.ToArray();
	}

	private LegacyButtonNode AddTabButton(int tabIndex, string label, BUTTON_DATA data) {
		var node = CreateButtonNode(label, data);
		_tabControl!.AddTabPageChild(tabIndex, node);
		_interactiveButtons.Add(node);
		return node;
	}

	private LegacyStaticNode AddTabStatic(int tabIndex, string label, STATIC_DATA data) {
		var node = CreateStaticNode(label, data);
		_tabControl!.AddTabPageChild(tabIndex, node, registerForFocus: false);
		return node;
	}

	private LegacyListBoxNode AddTabListBox(int tabIndex, string label, LISTBOX_DATA data) {
		var archetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", data.ListboxType);
		var node = new LegacyListBoxNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository, _xmlDbRepository);
		_tabControl!.AddTabPageChild(tabIndex, node);
		return node;
	}

	private LegacyDropdownNode AddTabDropdown(int tabIndex, string label, DROPDOWN_DATA data) {
		var buttonArchetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", data.ButtonData.ButtonType);
		var listboxArchetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", data.ListboxData.ListboxType);
		var node = new LegacyDropdownNode(label) {
			Placeholder = "Select"
		};
		node.ApplyLegacyDefinition(buttonArchetype, listboxArchetype, data, _vfxRepository, _xmlDbRepository);
		_tabControl!.AddTabPageChild(tabIndex, node);
		return node;
	}

	private LegacySliderNode AddTabSlider(int tabIndex, string label, SLIDER_DATA data, int min, int max, int value) {
		var archetype = ReadTypedEntry<GT_SLIDER>("GT_SLIDER", data.SliderType);
		var node = new LegacySliderNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		node.SetRange(min, max);
		node.SetSliderPosition(value, emitEvent: false);
		_tabControl!.AddTabPageChild(tabIndex, node);
		return node;
	}

	private LegacySliderNode AddLabeledSlider(
		int tabIndex,
		string label,
		STATIC_DATA staticData,
		SLIDER_DATA sliderData,
		int min,
		int max,
		int value) {
		AddTabStatic(tabIndex, $"{label}Label", staticData);
		return AddTabSlider(tabIndex, label, sliderData, min, max, value);
	}

	private LegacyButtonNode AddCheckboxWithLabel(
		int tabIndex,
		string label,
		STATIC_DATA staticData,
		BUTTON_DATA buttonData,
		bool initialState) {
		AddTabStatic(tabIndex, $"{label}Label", staticData);
		var button = AddTabButton(tabIndex, label, buttonData);
		button.PushState = initialState;
		button.Activated += current => current.PushState = !current.PushState;
		return button;
	}

	private LegacyButtonNode AddButtonNode(string label, BUTTON_DATA data) {
		var node = CreateButtonNode(label, data);
		ContentRoot.AddChild(node);
		return node;
	}

	private LegacyStaticNode AddStaticNode(string label, STATIC_DATA data) {
		var node = CreateStaticNode(label, data);
		ContentRoot.AddChild(node);
		return node;
	}

	private LegacyButtonNode CreateButtonNode(string label, BUTTON_DATA data) {
		var archetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", data.ButtonType);
		var node = new LegacyButtonNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.ButtonText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.Text = text;
		}

		return node;
	}

	private LegacyStaticNode CreateStaticNode(string label, STATIC_DATA data) {
		var archetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", data.StaticType);
		var node = new LegacyStaticNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.StaticText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.SetText(text);
		}

		return node;
	}

	private string ResolveString(uint id, string fallback) {
		return _strings.TryResolve(id, out var text) && !string.IsNullOrWhiteSpace(text)
			? text
			: fallback;
	}

	private T ReadTypedEntry<T>(string typeName, string fileName) where T : class {
		var details = _xmlDbRepository.ReadEntryDetails("GenData.db", typeName, fileName);
		return details.TypedValue as T
		       ?? throw new InvalidOperationException(
			       $"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
	}

	private static Vector2 ResolveScreenPosition(RECT screenRect) {
		var width = Math.Max(0f, screenRect.Right - screenRect.Left);
		var height = Math.Max(0f, screenRect.Bottom - screenRect.Top);
		return new Vector2(
			(LegacyScreenWidth - width) * 0.5f,
			(LegacyScreenHeight - height) * 0.5f);
	}
}

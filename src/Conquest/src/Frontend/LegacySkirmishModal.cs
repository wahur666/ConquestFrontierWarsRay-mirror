using System;
using System.Numerics;
using System.Threading.Tasks;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal enum SkirmishMode {
	QuickBattle,
	Multiplayer
}

internal enum MultiplayerNetworkKind {
	LocalAreaNetwork,
	Internet
}

internal sealed class LegacySkirmishModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private readonly Action _closed;
	private readonly GT_MENU1_MSHELL _mshellMenu;
	private readonly GT_MENU1_MAP _mapMenu;
	private readonly GT_MENU1_FINAL _finalMenu;
	private readonly LegacyRcStringResolver _strings;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly SkirmishMode _mode;
	private readonly MultiplayerNetworkKind _networkKind;
	private readonly bool _isHost;
	private LegacyCheckboxNode? _acceptCheckbox;
	private LegacyEditNode? _chatEdit;
	private LegacyListBoxNode? _chatList;
	private LegacyButtonNode? _mapTypeButton;
	private LegacyDropdownNode? _gameTypeDropdown;
	private LegacyDropdownNode? _moneyDropdown;
	private LegacyDropdownNode? _sizeDropdown;
	private LegacyDropdownNode? _terrainDropdown;
	private LegacyDropdownNode? _visibilityDropdown;
	private LegacyDropdownNode? _unitsDropdown;
	private LegacyDropdownNode? _systemsDropdown;
	private LegacySliderNode? _speedSlider;
	private LegacySliderNode? _commandPointsSlider;
	private LegacyCheckboxNode? _spectatorCheckbox;
	private LegacyCheckboxNode? _diplomacyCheckbox;
	private LegacyCheckboxNode? _lockSettingsCheckbox;
	private LegacyCheckboxNode? _moonsCheckbox;
	private LegacyStaticNode? _commandPointsDisplay;
	private LegacyStaticNode? _titleNode;
	private LegacyStaticNode? _ipAddressNode;
	private LegacyStaticNode? _enterChatNode;
	private LegacyStaticNode? _staticSpectatorNode;
	private LegacyStaticNode? _staticDiplomacyNode;
	private LegacyStaticNode? _staticLockSettingsNode;
	private LegacyStaticNode? _staticStateNode;
	private LegacyStaticNode? _staticNameNode;
	private LegacyStaticNode? _staticColorNode;
	private LegacyStaticNode? _staticRaceNode;
	private LegacyStaticNode? _staticTeamNode;
	private LegacyStaticNode? _staticPingNode;
	private LegacyStaticNode? _staticAcceptNode;
	private LegacyButtonNode? _startButton;
	private Task<NetworkAddressInfo>? _networkInfoTask;
	private TextNode? _statusLabel;

	public LegacySkirmishModal(
		GT_MENU1_MSHELL mshellMenu,
		GT_MENU1_MAP mapMenu,
		GT_MENU1_FINAL finalMenu,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		SkirmishMode mode,
		MultiplayerNetworkKind networkKind,
		bool isHost,
		Action closed) : base("LegacySkirmishModal", closed) {
		_mshellMenu = mshellMenu;
		_mapMenu = mapMenu;
		_finalMenu = finalMenu;
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
		_mode = mode;
		_networkKind = networkKind;
		_isHost = isHost;
		_closed = closed;
	}

	protected override void OnInitialize() {
		base.OnInitialize();
		ContentRoot.Position = ResolveScreenPosition(_mshellMenu.ScreenRect);

		AddStaticNode("Background", _mshellMenu.Background);
		_titleNode = AddStaticNode("Title", _mshellMenu.Title);
		_ipAddressNode = AddStaticNode("IpAddress", _mshellMenu.Ipaddress);
		_enterChatNode = AddStaticNode("EnterChat", _mshellMenu.EnterChat);
		_chatEdit = AddEditNode("EditChat", _mshellMenu.EditChat);
		_chatEdit.SetMaxChars(128);
		_chatEdit.EnableLockedTextBehavior();
		_chatEdit.SetText("Chat preview");
		_chatEdit.Activated += _ => AppendChatPreview();
		_chatList = AddListBoxNode("ListChat", _mshellMenu.ListChat);
		SeedChatPreview();
		ApplyModeHeader();
		BuildMapControls();

		_staticStateNode = AddStaticNode("StaticState", _finalMenu.StaticState);
		_staticNameNode = AddStaticNode("StaticName", _finalMenu.StaticName);
		_staticColorNode = AddStaticNode("StaticColor", _finalMenu.StaticColor);
		_staticRaceNode = AddStaticNode("StaticRace", _finalMenu.StaticRace);
		_staticTeamNode = AddStaticNode("StaticTeam", _finalMenu.StaticTeam);
		_staticPingNode = AddStaticNode("StaticPing", _finalMenu.StaticPing);
		AddStaticNode("Description", _finalMenu.Description);
		_staticAcceptNode = AddStaticNode("StaticAccept", _finalMenu.StaticAccept);
		AddStaticNode("StaticCountdown", _finalMenu.StaticCountdown).SetVisible(false);

		_acceptCheckbox = AddCheckboxNode("Accept", _finalMenu.Accept);
		_acceptCheckbox.Activated += current => {
			current.IsChecked = !current.IsChecked;
			ShowStatus(current.IsChecked ? "Accepted. Multiplayer flow is not ported yet." : string.Empty);
		};

		_startButton = AddButtonNode("Start", _finalMenu.Start);
		var cancel = AddButtonNode("Cancel", _finalMenu.Cancel);
		_startButton.Activated += _ => ShowStatus(_mode == SkirmishMode.Multiplayer
			? "Multiplayer start flow is not ported yet."
			: "Quick Battle start flow is not ported yet.");
		cancel.Activated += _ => CloseModal();
		_startButton.SetKeyboardFocus(true);

		_statusLabel = AddChild(new TextNode("StatusLabel") {
			Position = new Vector2(182f, 532f),
			FontSize = 14f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(200, 180, 140, 255),
			Text = string.Empty
		});

		ApplyModeVisibility();
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);
		if (_networkInfoTask is { IsCompletedSuccessfully: true }) {
			ApplyNetworkAddress(_networkInfoTask.Result);
			_networkInfoTask = null;
		}
	}

	private void ApplyModeHeader() {
		if (_titleNode is not null) {
			_titleNode.SetText(_mode == SkirmishMode.Multiplayer ? "Multiplayer" : "Quick Battle");
		}

		if (_mode == SkirmishMode.Multiplayer) {
			if (_ipAddressNode is not null) {
				_ipAddressNode.SetText(_networkKind == MultiplayerNetworkKind.Internet
					? "IP Address  Loading public IP..."
					: "IP Address  Loading local IPs...");
			}

			_networkInfoTask = NetworkService.GetNetworkAddressesAsync();
			return;
		}

		_ipAddressNode?.SetText(string.Empty);
	}

	private void ApplyNetworkAddress(NetworkAddressInfo info) {
		if (_ipAddressNode is null || _mode != SkirmishMode.Multiplayer) {
			return;
		}

		var value = _networkKind == MultiplayerNetworkKind.Internet
			? info.PublicIp
			: string.Join(", ", info.LocalIpv4Addresses);
		_ipAddressNode.SetText(string.IsNullOrWhiteSpace(value) ? "IP Address  Unavailable" : $"IP Address  {value}");
	}

	private void ApplyModeVisibility() {
		var isMultiplayer = _mode == SkirmishMode.Multiplayer;

		_enterChatNode?.SetVisible(isMultiplayer);
		_chatEdit?.SetVisible(isMultiplayer);
		_chatEdit?.EnableEdit(isMultiplayer);
		_chatList?.SetVisible(isMultiplayer);
		_chatList?.EnableListbox(isMultiplayer);
		_ipAddressNode?.SetVisible(isMultiplayer);

		_staticSpectatorNode?.SetVisible(isMultiplayer);
		_staticDiplomacyNode?.SetVisible(isMultiplayer);
		_staticLockSettingsNode?.SetVisible(isMultiplayer);

		_spectatorCheckbox?.SetVisible(isMultiplayer);
		_spectatorCheckbox?.EnableButton(isMultiplayer);
		_diplomacyCheckbox?.SetVisible(isMultiplayer);
		_diplomacyCheckbox?.EnableButton(isMultiplayer);
		_lockSettingsCheckbox?.SetVisible(isMultiplayer);
		_lockSettingsCheckbox?.EnableButton(isMultiplayer);

		// _staticStateNode?.SetVisible(isMultiplayer);
		// _staticNameNode?.SetVisible(isMultiplayer);
		// _staticColorNode?.SetVisible(isMultiplayer);
		// _staticRaceNode?.SetVisible(isMultiplayer);
		// _staticTeamNode?.SetVisible(isMultiplayer);
		_staticPingNode?.SetVisible(isMultiplayer);
		_staticAcceptNode?.SetVisible(isMultiplayer);
		_acceptCheckbox?.SetVisible(isMultiplayer && !_isHost);
		_acceptCheckbox?.EnableButton(isMultiplayer && !_isHost);
		_startButton?.SetVisible(!isMultiplayer || _isHost);
		_startButton?.EnableButton(!isMultiplayer || _isHost);
	}

	private void BuildMapControls() {
		AddStaticNode("StaticGameType", _mapMenu.StaticGameType);
		AddStaticNode("StaticSpeed", _mapMenu.StaticSpeed);
		AddStaticNode("StaticMoney", _mapMenu.StaticMoney);
		AddStaticNode("StaticUnits", _mapMenu.StaticUnits);
		AddStaticNode("StaticMapType", _mapMenu.StaticMapType);
		AddStaticNode("StaticSize", _mapMenu.StaticSize);
		AddStaticNode("StaticTerrain", _mapMenu.StaticTerrain);
		AddStaticNode("StaticVisibility", _mapMenu.StaticVisibility);
		_staticSpectatorNode = AddStaticNode("StaticSpectator", _mapMenu.StaticSpectator);
		_staticDiplomacyNode = AddStaticNode("StaticDiplomacy", _mapMenu.StaticDiplomacy);
		_staticLockSettingsNode = AddStaticNode("StaticLockSettings", _mapMenu.StaticLockSettings);
		AddStaticNode("StaticSystems", _mapMenu.StaticSystems);
		var commandPointsLabel = AddStaticNode("StaticCmdPoints", _mapMenu.StaticCmdPoints);
		commandPointsLabel.SetText("Command Points");
		_commandPointsDisplay = AddStaticNode("StaticCmdPointsDisplay", _mapMenu.StaticCmdPointsDisplay);
		_commandPointsDisplay.SetText("150");

		_mapTypeButton = AddButtonNode("MapType", _mapMenu.MapType);
		_mapTypeButton.Text = "Random";
		_mapTypeButton.Activated += _ => ShowStatus("Map selection flow is not ported yet.");

		_gameTypeDropdown = AddDropdownNode("GameTypeDropdown", _mapMenu.DropGameType);
		SeedDropdown(_gameTypeDropdown, "Kill Units", "Kill HQ/Platforms", "Kill Platforms/Fabs");
		_moneyDropdown = AddDropdownNode("MoneyDropdown", _mapMenu.DropMoney);
		SeedDropdown(_moneyDropdown, "Low", "Medium", "High");
		_sizeDropdown = AddDropdownNode("SizeDropdown", _mapMenu.DropSize);
		SeedDropdown(_sizeDropdown, "Small", "Medium", "Large");
		_terrainDropdown = AddDropdownNode("TerrainDropdown", _mapMenu.DropTerrain);
		SeedDropdown(_terrainDropdown, "Light", "Medium", "Heavy");
		_visibilityDropdown = AddDropdownNode("VisibilityDropdown", _mapMenu.DropVisibility);
		SeedDropdown(_visibilityDropdown, "Normal", "Explored", "None");
		_unitsDropdown = AddDropdownNode("UnitsDropdown", _mapMenu.DropUnits);
		SeedDropdown(_unitsDropdown, "Minimal", "Medium", "Heavy");
		_systemsDropdown = AddDropdownNode("SystemsDropdown", _mapMenu.DropSystems);
		SeedDropdown(_systemsDropdown, "3", "4", "5", "6", "7", "8");

		_speedSlider = AddSliderNode("SpeedSlider", _mapMenu.SliderSpeed, -5, 5, 0);
		_speedSlider.ValueChanged += _ => ShowStatus($"Game speed {_speedSlider.SliderPosition:+0;-0;0} preview only.");
		_commandPointsSlider = AddSliderNode("CommandPointsSlider", _mapMenu.SliderCmdPoints, -2, 1, 0);
		_commandPointsSlider.ValueChanged += _ => UpdateCommandPointsDisplay();

		_spectatorCheckbox = AddCheckboxNode("Spectator", _mapMenu.PushSpectator);
		_diplomacyCheckbox = AddCheckboxNode("Diplomacy", _mapMenu.PushDiplomacy);
		_lockSettingsCheckbox = AddCheckboxNode("LockSettings", _mapMenu.PushLockSettings);
		var moonsCheckboxData = _mapMenu.PushHard with {
			XOrigin = _mapMenu.PushHard.XOrigin - 6,
		};
		_moonsCheckbox = AddCheckboxNode("Moons", moonsCheckboxData );

		var moonsStaticData = _mapMenu.StaticHard with {
			XOrigin = _mapMenu.StaticHard.XOrigin - 4
		};
		var moonsLabel = AddStaticNode("StaticMoons", moonsStaticData);
		moonsLabel.SetText("Moons");

		_spectatorCheckbox.Activated += checkbox => checkbox.IsChecked = !checkbox.IsChecked;
		_diplomacyCheckbox.Activated += checkbox => checkbox.IsChecked = !checkbox.IsChecked;
		_lockSettingsCheckbox.Activated += checkbox => checkbox.IsChecked = !checkbox.IsChecked;
		_moonsCheckbox.Activated += checkbox => checkbox.IsChecked = !checkbox.IsChecked;
	}

	private void ShowStatus(string message) {
		if (_statusLabel is not null) {
			_statusLabel.Text = message;
		}
	}

	private void SeedChatPreview() {
		if (_chatList is null) {
			return;
		}

		_chatList.ResetContent();
		_chatList.AddString("[Host] Welcome to skirmish staging.");
		_chatList.AddString("[Local] Chat preview is not networked yet.");
		_chatList.EnsureVisible(0);
	}

	private void AppendChatPreview() {
		if (_chatEdit is null || _chatList is null) {
			return;
		}

		var text = _chatEdit.Text.Trim();
		if (text.Length == 0) {
			return;
		}

		var prefix = _mode == SkirmishMode.Multiplayer ? "[Local]" : "[Quick Battle]";
		var index = _chatList.AddString($"{prefix} {text}");
		_chatList.EnsureVisible(index > 0 ? index - 1 : 0);
		_chatEdit.SetText(string.Empty);
	}

	private void SeedDropdown(LegacyDropdownNode? dropdown, params string[] labels) {
		if (dropdown is null) {
			return;
		}

		dropdown.ResetContent();
		foreach (var label in labels) {
			dropdown.AddString(label);
		}

		if (labels.Length > 0) {
			dropdown.SetCurrentSelection(0);
		}

		dropdown.SelectionCommitted += _ => ShowStatus($"{dropdown.Name} changed to {dropdown.SelectedLabel}.");
	}

	private void UpdateCommandPointsDisplay() {
		if (_commandPointsSlider is null || _commandPointsDisplay is null) {
			return;
		}

		var value = _commandPointsSlider.SliderPosition switch {
			-2 => 100,
			-1 => 150,
			0 => 200,
			1 => 300,
			_ => 150
		};
		_commandPointsDisplay.SetText(value.ToString());
		ShowStatus($"Command points set to {value} preview only.");
	}

	private void CloseModal() {
		_closed();
	}

	private LegacyButtonNode AddButtonNode(string label, BUTTON_DATA data) {
		var archetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", data.ButtonType);
		var node = new LegacyButtonNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.ButtonText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.Text = text;
		}

		ContentRoot.AddChild(node);
		return node;
	}

	private LegacyCheckboxNode AddCheckboxNode(string label, BUTTON_DATA data) {
		var archetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", data.ButtonType);
		var node = new LegacyCheckboxNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		ContentRoot.AddChild(node);
		return node;
	}

	private LegacyDropdownNode AddDropdownNode(string label, DROPDOWN_DATA data) {
		var buttonArchetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", data.ButtonData.ButtonType);
		var listboxArchetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", data.ListboxData.ListboxType);
		var node = new LegacyDropdownNode(label) {
			Placeholder = "Select"
		};
		node.ApplyLegacyDefinition(buttonArchetype, listboxArchetype, data, _vfxRepository, _xmlDbRepository);
		ContentRoot.AddChild(node);
		return node;
	}

	private LegacyEditNode AddEditNode(string label, EDIT_DATA data) {
		var archetype = ReadTypedEntry<GT_EDIT>("GT_EDIT", data.EditType);
		var node = new LegacyEditNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		ContentRoot.AddChild(node);
		return node;
	}

	private LegacySliderNode AddSliderNode(string label, SLIDER_DATA data, int min, int max, int value) {
		var archetype = ReadTypedEntry<GT_SLIDER>("GT_SLIDER", data.SliderType);
		var node = new LegacySliderNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		node.SetRange(min, max);
		node.SetSliderPosition(value, emitEvent: false);
		ContentRoot.AddChild(node);
		return node;
	}

	private LegacyListBoxNode AddListBoxNode(string label, LISTBOX_DATA data) {
		var archetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", data.ListboxType);
		var node = new LegacyListBoxNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository, _xmlDbRepository);
		ContentRoot.AddChild(node);
		return node;
	}

	private LegacyStaticNode AddStaticNode(string label, STATIC_DATA data) {
		var archetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", data.StaticType);
		var node = new LegacyStaticNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.StaticText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.SetText(text);
		}

		ContentRoot.AddChild(node);
		return node;
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

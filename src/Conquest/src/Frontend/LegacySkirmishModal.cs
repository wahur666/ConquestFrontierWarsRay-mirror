using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
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

internal enum SkirmishSlotState {
	Open,
	Closed,
	Active,
	Ready
}

internal enum SkirmishSlotType {
	Human,
	Computer
}

internal enum SkirmishComputerChallenge {
	Easy,
	Average,
	Hard,
	Impossible,
	Nightmare
}

internal enum SkirmishRace {
	Terran,
	Mantis,
	Solarian,
	Vyrium
}

internal enum SkirmishColor {
	Yellow,
	Red,
	Blue,
	Pink,
	Green,
	Orange,
	Purple,
	Aqua
}

internal enum SkirmishTeam {
	None,
	Team1,
	Team2,
	Team3,
	Team4
}

internal sealed class LegacySkirmishModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private const int MaxPlayers = 8;
	private static readonly Vector2 SlotOriginOffset = new(87f, 93f);
	private readonly Action _closed;
	private readonly GT_MENU1_MSHELL _mshellMenu;
	private readonly GT_MENU1_MAP _mapMenu;
	private readonly GT_MENU1_SLOTS _slotsMenu;
	private readonly GT_MENU1_FINAL _finalMenu;
	private readonly LegacyRcStringResolver _strings;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly SkirmishMode _mode;
	private readonly MultiplayerNetworkKind _networkKind;
	private readonly bool _isHost;
	private readonly Action _started;
	private readonly Lock _networkSync = new();
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
	private readonly LegacyDropdownNode?[] _slotStateDropdowns = new LegacyDropdownNode?[MaxPlayers];
	private readonly LegacyDropdownNode?[] _slotRaceDropdowns = new LegacyDropdownNode?[MaxPlayers];
	private readonly LegacyDropdownNode?[] _slotColorDropdowns = new LegacyDropdownNode?[MaxPlayers];
	private readonly LegacyDropdownNode?[] _slotTeamDropdowns = new LegacyDropdownNode?[MaxPlayers];
	private readonly LegacyStaticNode?[] _slotNameNodes = new LegacyStaticNode?[MaxPlayers];
	private readonly LegacyStaticNode?[] _slotPingNodes = new LegacyStaticNode?[MaxPlayers];
	private readonly SkirmishSlotPreview[] _slotPreview = new SkirmishSlotPreview[MaxPlayers];
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
	private LegacyStaticNode? _staticPingNode;
	private LegacyStaticNode? _staticAcceptNode;
	private LegacyButtonNode? _startButton;
	private LegacyNetLoadingModal? _netLoadingModal;
	private Task<NetworkAddressInfo>? _networkInfoTask;
	private TextNode? _statusLabel;
	private LanLobbyState? _pendingLobbyState;
	private readonly List<LanLobbyChannelMessage> _pendingChannelMessages = [];

	public LegacySkirmishModal(
		GT_MENU1_MSHELL mshellMenu,
		GT_MENU1_MAP mapMenu,
		GT_MENU1_SLOTS slotsMenu,
		GT_MENU1_FINAL finalMenu,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		SkirmishMode mode,
		MultiplayerNetworkKind networkKind,
		bool isHost,
		Action started,
		Action closed) : base("LegacySkirmishModal", closed) {
		_mshellMenu = mshellMenu;
		_mapMenu = mapMenu;
		_slotsMenu = slotsMenu;
		_finalMenu = finalMenu;
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
		_mode = mode;
		_networkKind = networkKind;
		_isHost = isHost;
		_started = started;
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
		_chatEdit.SetText(string.Empty);
		_chatEdit.Activated += _ => AppendChatPreview();
		_chatList = AddListBoxNode("ListChat", _mshellMenu.ListChat);
		SeedChatPreview();
		ApplyModeHeader();
		BuildMapControls();
		BuildSlotControls();

		AddStaticNode("StaticState", _finalMenu.StaticState);
		AddStaticNode("StaticName", _finalMenu.StaticName);
		AddStaticNode("StaticColor", _finalMenu.StaticColor);
		AddStaticNode("StaticRace", _finalMenu.StaticRace);
		AddStaticNode("StaticTeam", _finalMenu.StaticTeam);
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
		_startButton.Activated += _ => _started();
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
		AttachNetworkBindings();
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);
		ApplyPendingNetworkUpdates();
		if (_networkInfoTask is { IsCompletedSuccessfully: true }) {
			ApplyNetworkAddress(_networkInfoTask.Result);
			_networkInfoTask = null;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.F1)) {
			ToggleNetLoadingPreview();
		}
	}

	protected override void OnDispose() {
		DetachNetworkBindings();
		base.OnDispose();
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

	private void AttachNetworkBindings() {
		if (_mode != SkirmishMode.Multiplayer || _networkKind != MultiplayerNetworkKind.LocalAreaNetwork) {
			return;
		}

		NetworkService.LanLobbyStateChanged += HandleLanLobbyStateChanged;
		NetworkService.LanLobbyChannelMessageReceived += HandleLanLobbyChannelMessageReceived;
		var currentState = NetworkService.GetCurrentLanLobbyState();
		if (currentState is not null) {
			ApplyLobbyState(currentState);
		}
	}

	private void DetachNetworkBindings() {
		if (_mode != SkirmishMode.Multiplayer || _networkKind != MultiplayerNetworkKind.LocalAreaNetwork) {
			return;
		}

		NetworkService.LanLobbyStateChanged -= HandleLanLobbyStateChanged;
		NetworkService.LanLobbyChannelMessageReceived -= HandleLanLobbyChannelMessageReceived;
	}

	private void HandleLanLobbyStateChanged(LanLobbyState state) {
		lock (_networkSync) {
			_pendingLobbyState = state;
		}
	}

	private void HandleLanLobbyChannelMessageReceived(LanLobbyChannelMessage message) {
		lock (_networkSync) {
			_pendingChannelMessages.Add(message);
		}
	}

	private void ApplyPendingNetworkUpdates() {
		LanLobbyState? lobbyState = null;
		List<LanLobbyChannelMessage>? channelMessages = null;
		lock (_networkSync) {
			if (_pendingLobbyState is not null) {
				lobbyState = _pendingLobbyState;
				_pendingLobbyState = null;
			}

			if (_pendingChannelMessages.Count > 0) {
				channelMessages = [.. _pendingChannelMessages];
				_pendingChannelMessages.Clear();
			}
		}

		if (lobbyState is not null) {
			ApplyLobbyState(lobbyState);
		}

		if (channelMessages is not null) {
			foreach (var message in channelMessages) {
				AppendNetworkChatMessage(message);
			}
		}
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
		UpdateCommandPointsDisplay();

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

	private void BuildSlotControls() {
		for (var index = 0; index < MaxPlayers; index++) {
			_slotStateDropdowns[index] = AddSlotDropdownNode($"SlotState{index}", _slotsMenu.DropSlots[index]);
			_slotRaceDropdowns[index] = AddSlotDropdownNode($"SlotRace{index}", _slotsMenu.DropRaces[index]);
			_slotColorDropdowns[index] = AddSlotDropdownNode($"SlotColor{index}", _slotsMenu.DropPlayers[index]);
			_slotTeamDropdowns[index] = AddSlotDropdownNode($"SlotTeam{index}", _slotsMenu.DropTeams[index]);
			_slotNameNodes[index] = AddSlotStaticNode($"SlotName{index}", _slotsMenu.StaticNames[index]);
			_slotPingNodes[index] = AddSlotStaticNode($"SlotPing{index}", _slotsMenu.StaticPings[index]);

			var capturedIndex = index;
			_slotStateDropdowns[index]!.SelectionCommitted += _ => OnSlotStateChanged(capturedIndex);
			_slotRaceDropdowns[index]!.SelectionCommitted += _ => OnSlotRaceChanged(capturedIndex);
			_slotColorDropdowns[index]!.SelectionCommitted += _ => OnSlotColorChanged(capturedIndex);
			_slotTeamDropdowns[index]!.SelectionCommitted += _ => OnSlotTeamChanged(capturedIndex);
		}

		InitializeSlotPreviewState();
		SeedSlotDropdownOptions();
		RefreshSlotPreview();
	}

	private void InitializeSlotPreviewState() {
		for (var index = 0; index < _slotPreview.Length; index++) {
			_slotPreview[index] = new SkirmishSlotPreview {
				State = _mode == SkirmishMode.Multiplayer ? SkirmishSlotState.Open : SkirmishSlotState.Closed,
				Type = _mode == SkirmishMode.Multiplayer ? SkirmishSlotType.Human : SkirmishSlotType.Human,
				CompChallenge = SkirmishComputerChallenge.Easy,
				Race = SkirmishRace.Terran,
				Color = (SkirmishColor)Math.Min((int)SkirmishColor.Aqua, (int)SkirmishColor.Yellow + index),
				Team = SkirmishTeam.None,
				Name = string.Empty,
				Ping = string.Empty,
				IsLocal = false
			};
		}

		_slotPreview[0] = new SkirmishSlotPreview {
			State = _mode == SkirmishMode.Multiplayer ? SkirmishSlotState.Active : SkirmishSlotState.Ready,
			Type = SkirmishSlotType.Human,
			CompChallenge = SkirmishComputerChallenge.Easy,
			Race = SkirmishRace.Terran,
			Color = SkirmishColor.Yellow,
			Team = SkirmishTeam.None,
			Name = _mode == SkirmishMode.Multiplayer ? (_isHost ? "Host Commander" : "Local Commander") : "Quick Battle Commander",
			Ping = _mode == SkirmishMode.Multiplayer ? "0" : string.Empty,
			IsLocal = true
		};

		_slotPreview[1] = new SkirmishSlotPreview {
			State = _mode == SkirmishMode.Multiplayer ? SkirmishSlotState.Open : SkirmishSlotState.Ready,
			Type = _mode == SkirmishMode.Multiplayer ? SkirmishSlotType.Human : SkirmishSlotType.Computer,
			CompChallenge = SkirmishComputerChallenge.Average,
			Race = SkirmishRace.Mantis,
			Color = SkirmishColor.Red,
			Team = SkirmishTeam.Team2,
			Name = _mode == SkirmishMode.Multiplayer ? string.Empty : ResolveComputerName(1, SkirmishRace.Mantis),
			Ping = _mode == SkirmishMode.Multiplayer ? string.Empty : "AI",
			IsLocal = false
		};

		_slotPreview[2] = new SkirmishSlotPreview {
			State = _mode == SkirmishMode.Multiplayer ? SkirmishSlotState.Open : SkirmishSlotState.Ready,
			Type = _mode == SkirmishMode.Multiplayer ? SkirmishSlotType.Human : SkirmishSlotType.Computer,
			CompChallenge = SkirmishComputerChallenge.Hard,
			Race = SkirmishRace.Solarian,
			Color = SkirmishColor.Blue,
			Team = SkirmishTeam.Team3,
			Name = _mode == SkirmishMode.Multiplayer ? string.Empty : ResolveComputerName(2, SkirmishRace.Solarian),
			Ping = _mode == SkirmishMode.Multiplayer ? string.Empty : "AI",
			IsLocal = false
		};
	}

	private void SeedSlotDropdownOptions() {
		for (var index = 0; index < MaxPlayers; index++) {
			SeedSlotStateDropdown(_slotStateDropdowns[index]);
			SeedSlotRaceDropdown(_slotRaceDropdowns[index]);
			SeedSlotColorDropdown(_slotColorDropdowns[index]);
			SeedSlotTeamDropdown(_slotTeamDropdowns[index]);
		}
	}

	private void SeedSlotStateDropdown(LegacyDropdownNode? dropdown) {
		if (dropdown is null) {
			return;
		}

		dropdown.ResetContent();
		dropdown.AddString("Open");
		dropdown.AddString("Closed");
		dropdown.AddString("AI Easy");
		dropdown.AddString("AI Average");
		dropdown.AddString("AI Hard");
		dropdown.AddString("AI Impossible");
		dropdown.AddString("AI Nightmare");
		dropdown.AddString("Human");
		dropdown.SetCurrentSelection(0);
	}

	private void SeedSlotRaceDropdown(LegacyDropdownNode? dropdown) {
		if (dropdown is null) {
			return;
		}

		dropdown.ResetContent();
		AddDropdownItem(dropdown, "Terran", (uint)SkirmishRace.Terran);
		AddDropdownItem(dropdown, "Mantis", (uint)SkirmishRace.Mantis);
		AddDropdownItem(dropdown, "Solarian", (uint)SkirmishRace.Solarian);
		AddDropdownItem(dropdown, "Vyrium", (uint)SkirmishRace.Vyrium);
		dropdown.SetCurrentSelection(0);
	}

	private void SeedSlotColorDropdown(LegacyDropdownNode? dropdown) {
		if (dropdown is null) {
			return;
		}

		dropdown.ResetContent();
		AddDropdownItem(dropdown, "Yellow", (uint)SkirmishColor.Yellow, ResolveLegacyColor(SkirmishColor.Yellow));
		AddDropdownItem(dropdown, "Red", (uint)SkirmishColor.Red, ResolveLegacyColor(SkirmishColor.Red));
		AddDropdownItem(dropdown, "Blue", (uint)SkirmishColor.Blue, ResolveLegacyColor(SkirmishColor.Blue));
		AddDropdownItem(dropdown, "Pink", (uint)SkirmishColor.Pink, ResolveLegacyColor(SkirmishColor.Pink));
		AddDropdownItem(dropdown, "Green", (uint)SkirmishColor.Green, ResolveLegacyColor(SkirmishColor.Green));
		AddDropdownItem(dropdown, "Orange", (uint)SkirmishColor.Orange, ResolveLegacyColor(SkirmishColor.Orange));
		AddDropdownItem(dropdown, "Purple", (uint)SkirmishColor.Purple, ResolveLegacyColor(SkirmishColor.Purple));
		AddDropdownItem(dropdown, "Aqua", (uint)SkirmishColor.Aqua, ResolveLegacyColor(SkirmishColor.Aqua));
		dropdown.SetCurrentSelection(0);
	}

	private void SeedSlotTeamDropdown(LegacyDropdownNode? dropdown) {
		if (dropdown is null) {
			return;
		}

		dropdown.ResetContent();
		AddDropdownItem(dropdown, "None", (uint)SkirmishTeam.None);
		AddDropdownItem(dropdown, "Team 1", (uint)SkirmishTeam.Team1);
		AddDropdownItem(dropdown, "Team 2", (uint)SkirmishTeam.Team2);
		AddDropdownItem(dropdown, "Team 3", (uint)SkirmishTeam.Team3);
		AddDropdownItem(dropdown, "Team 4", (uint)SkirmishTeam.Team4);
		dropdown.SetCurrentSelection(0);
	}

	private static void AddDropdownItem(LegacyDropdownNode dropdown, string label, uint value, Color? color = null) {
		var index = dropdown.AddString(label);
		dropdown.SetDataValue(index, value);
		if (color.HasValue) {
			dropdown.SetColorValue(index, color.Value);
		}
	}

	private void RefreshSlotPreview() {
		for (var index = 0; index < _slotPreview.Length; index++) {
			var slot = _slotPreview[index];
			var stateDropdown = _slotStateDropdowns[index];
			var raceDropdown = _slotRaceDropdowns[index];
			var colorDropdown = _slotColorDropdowns[index];
			var teamDropdown = _slotTeamDropdowns[index];
			var nameNode = _slotNameNodes[index];
			var pingNode = _slotPingNodes[index];
			if (stateDropdown is null || raceDropdown is null || colorDropdown is null || teamDropdown is null ||
			    nameNode is null || pingNode is null) {
				continue;
			}

			stateDropdown.SetCurrentSelection(ResolveStateSelection(slot));
			SelectDropdownValue(raceDropdown, (uint)slot.Race);
			SelectDropdownValue(colorDropdown, (uint)slot.Color);
			SelectDropdownValue(teamDropdown, (uint)slot.Team);
			colorDropdown.SetSelectionColor(ResolveLegacyColor(slot.Color));

			var slotVisible = slot.State is not SkirmishSlotState.Open and not SkirmishSlotState.Closed;
			var canEditState = _isHost || _mode == SkirmishMode.QuickBattle;
			var canEditIdentity = slotVisible && (slot.IsLocal || slot.Type == SkirmishSlotType.Computer || _mode == SkirmishMode.QuickBattle);

			stateDropdown.EnableDropdown(canEditState && !slot.IsLocal);
			raceDropdown.EnableDropdown(canEditIdentity);
			raceDropdown.SetVisible(slotVisible);
			colorDropdown.EnableDropdown(canEditIdentity);
			colorDropdown.SetVisible(slotVisible);
			teamDropdown.EnableDropdown(canEditIdentity);
			teamDropdown.SetVisible(slotVisible);

			nameNode.SetText(slot.Name);
			nameNode.SetTextColor(ResolveLegacyColor(slot.Color));
			nameNode.SetVisible(slotVisible);

			pingNode.SetText(_mode == SkirmishMode.Multiplayer ? slot.Ping : string.Empty);
			pingNode.SetTextColor(ResolveLegacyColor(slot.Color));
			pingNode.SetVisible(_mode == SkirmishMode.Multiplayer && slotVisible);
		}
	}

	private void OnSlotStateChanged(int index) {
		var dropdown = _slotStateDropdowns[index];
		if (dropdown is null) {
			return;
		}

		var selection = dropdown.GetCurrentSelection();
		var slot = _slotPreview[index];
		switch (selection) {
			case 0:
				slot.State = SkirmishSlotState.Open;
				slot.Type = SkirmishSlotType.Human;
				slot.Name = string.Empty;
				slot.Ping = string.Empty;
				break;
			case 1:
				slot.State = SkirmishSlotState.Closed;
				slot.Type = SkirmishSlotType.Human;
				slot.Name = string.Empty;
				slot.Ping = string.Empty;
				break;
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
				slot.State = SkirmishSlotState.Ready;
				slot.Type = SkirmishSlotType.Computer;
				slot.CompChallenge = selection switch {
					2 => SkirmishComputerChallenge.Easy,
					3 => SkirmishComputerChallenge.Average,
					4 => SkirmishComputerChallenge.Hard,
					5 => SkirmishComputerChallenge.Impossible,
					_ => SkirmishComputerChallenge.Nightmare
				};
				slot.Name = ResolveComputerName(index, slot.Race);
				slot.Ping = "AI";
				slot.Team = index == 0 ? SkirmishTeam.None : (SkirmishTeam)Math.Min((int)SkirmishTeam.Team4, index);
				break;
			case 7:
				slot.State = slot.IsLocal ? SkirmishSlotState.Active : SkirmishSlotState.Ready;
				slot.Type = SkirmishSlotType.Human;
				slot.Name = slot.IsLocal ? slot.Name : $"Player {index + 1}";
				slot.Ping = _mode == SkirmishMode.Multiplayer ? $"{22 + (index * 7)}" : string.Empty;
				break;
		}

		_slotPreview[index] = slot;
		RefreshSlotPreview();
		ShowStatus($"Slot {index + 1} set to {dropdown.SelectedLabel}.");
	}

	private void OnSlotRaceChanged(int index) {
		var dropdown = _slotRaceDropdowns[index];
		if (dropdown is null || dropdown.GetCurrentSelection() < 0) {
			return;
		}

		var slot = _slotPreview[index];
		slot.Race = (SkirmishRace)dropdown.GetDataValue(dropdown.GetCurrentSelection());
		if (slot.Type == SkirmishSlotType.Computer && slot.State is SkirmishSlotState.Active or SkirmishSlotState.Ready) {
			slot.Name = ResolveComputerName(index, slot.Race);
		}

		_slotPreview[index] = slot;
		RefreshSlotPreview();
		ShowStatus($"Slot {index + 1} race changed to {dropdown.SelectedLabel}.");
	}

	private void OnSlotColorChanged(int index) {
		var dropdown = _slotColorDropdowns[index];
		if (dropdown is null || dropdown.GetCurrentSelection() < 0) {
			return;
		}

		var slot = _slotPreview[index];
		slot.Color = (SkirmishColor)dropdown.GetDataValue(dropdown.GetCurrentSelection());
		_slotPreview[index] = slot;
		RefreshSlotPreview();
		ShowStatus($"Slot {index + 1} color changed to {dropdown.SelectedLabel}.");
	}

	private void OnSlotTeamChanged(int index) {
		var dropdown = _slotTeamDropdowns[index];
		if (dropdown is null || dropdown.GetCurrentSelection() < 0) {
			return;
		}

		var slot = _slotPreview[index];
		slot.Team = (SkirmishTeam)dropdown.GetDataValue(dropdown.GetCurrentSelection());
		_slotPreview[index] = slot;
		RefreshSlotPreview();
		ShowStatus($"Slot {index + 1} team changed to {dropdown.SelectedLabel}.");
	}

	private static void SelectDropdownValue(LegacyDropdownNode dropdown, uint value) {
		for (var index = 0; index < dropdown.GetNumberOfItems(); index++) {
			if (dropdown.GetDataValue(index) == value) {
				dropdown.SetCurrentSelection(index);
				return;
			}
		}
	}

	private static int ResolveStateSelection(SkirmishSlotPreview slot) {
		return slot.State switch {
			SkirmishSlotState.Open => 0,
			SkirmishSlotState.Closed => 1,
			_ when slot.Type == SkirmishSlotType.Computer => slot.CompChallenge switch {
				SkirmishComputerChallenge.Easy => 2,
				SkirmishComputerChallenge.Average => 3,
				SkirmishComputerChallenge.Hard => 4,
				SkirmishComputerChallenge.Impossible => 5,
				_ => 6
			},
			_ => 7
		};
	}

	private string ResolveComputerName(int index, SkirmishRace race) {
		return race switch {
			SkirmishRace.Mantis => _slotsMenu.MantisComputerNames.ElementAtOrDefault(index) ?? $"Mantis AI {index + 1}",
			SkirmishRace.Solarian => _slotsMenu.SolarianComputerNames.ElementAtOrDefault(index) ?? $"Solarian AI {index + 1}",
			_ => _slotsMenu.TerranComputerNames.ElementAtOrDefault(index) ?? $"Terran AI {index + 1}"
		};
	}

	private static Color ResolveLegacyColor(SkirmishColor color) {
		return color switch {
			SkirmishColor.Yellow => new Color(255, 221, 79, 255),
			SkirmishColor.Red => new Color(224, 84, 84, 255),
			SkirmishColor.Blue => new Color(98, 149, 255, 255),
			SkirmishColor.Pink => new Color(255, 136, 214, 255),
			SkirmishColor.Green => new Color(88, 201, 116, 255),
			SkirmishColor.Orange => new Color(244, 153, 64, 255),
			SkirmishColor.Purple => new Color(171, 109, 239, 255),
			SkirmishColor.Aqua => new Color(97, 224, 228, 255),
			_ => new Color(180, 160, 120, 255)
		};
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
		if (_mode == SkirmishMode.Multiplayer && _networkKind == MultiplayerNetworkKind.LocalAreaNetwork) {
			var lobbyState = NetworkService.GetCurrentLanLobbyState();
			if (lobbyState is not null) {
				var index = _chatList.AddString($"[Lobby] {lobbyState.SessionName} [{lobbyState.LobbyCode}]");
				_chatList.EnsureVisible(index);
			}
			return;
		}

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

		if (_mode == SkirmishMode.Multiplayer && _networkKind == MultiplayerNetworkKind.LocalAreaNetwork) {
			_ = SendNetworkChatAsync(text);
			return;
		}

		var prefix = _mode == SkirmishMode.Multiplayer ? "[Local]" : "[Quick Battle]";
		var index = _chatList.AddString($"{prefix} {text}");
		_chatList.EnsureVisible(index > 0 ? index - 1 : 0);
		_chatEdit.SetText(string.Empty);
	}

	private async Task SendNetworkChatAsync(string text) {
		try {
			await NetworkService.SendLobbyChatMessageAsync(text);
			_chatEdit?.SetText(string.Empty);
		} catch (Exception ex) {
			ShowStatus($"Chat send failed: {ex.Message}");
		}
	}

	private void ApplyLobbyState(LanLobbyState state) {
		if (_mode != SkirmishMode.Multiplayer) {
			return;
		}

		for (var index = 0; index < _slotPreview.Length; index++) {
			var current = _slotPreview[index];
			current.Type = SkirmishSlotType.Human;
			current.CompChallenge = SkirmishComputerChallenge.Easy;
			current.Name = string.Empty;
			current.Ping = string.Empty;
			current.IsLocal = false;
			current.State = SkirmishSlotState.Open;
			_slotPreview[index] = current;
		}

		foreach (var player in state.Slots.Where(static player => player.IsConnected)) {
			if (player.SlotIndex < 0 || player.SlotIndex >= MaxPlayers) {
				continue;
			}

			var slot = _slotPreview[player.SlotIndex];
			slot.Type = SkirmishSlotType.Human;
			slot.State = player.IsLocal ? SkirmishSlotState.Active : SkirmishSlotState.Ready;
			slot.Name = player.Name;
			slot.Ping = player.IsLocal ? "0" : "--";
			slot.IsLocal = player.IsLocal;
			_slotPreview[player.SlotIndex] = slot;
		}

		RefreshSlotPreview();
		ShowStatus($"Lobby {state.LobbyCode}: {state.Slots.Count(static slot => slot.IsConnected)}/{state.MaxPlayers} players");
	}

	private void AppendNetworkChatMessage(LanLobbyChannelMessage message) {
		if (_chatList is null || !string.Equals(message.Channel, "chat", StringComparison.OrdinalIgnoreCase)) {
			return;
		}

		var prefix = $"[{message.FromPlayerName}]";
		var index = _chatList.AddString(string.IsNullOrWhiteSpace(message.Text) ? prefix : $"{prefix} {message.Text}");
		_chatList.EnsureVisible(index > 0 ? index - 1 : 0);
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

	private int GetCommandPointValue(LegacySliderNode commandPointsSlider) {
		return commandPointsSlider.SliderPosition switch {
			-2 => 100,
			-1 => 150,
			0 => 200,
			1 => 300,
			_ => 150
		};
	}
	
	private void UpdateCommandPointsDisplay() {
		if (_commandPointsSlider is null || _commandPointsDisplay is null) {
			return;
		}

		var value = GetCommandPointValue(_commandPointsSlider);
		_commandPointsDisplay.SetText(value.ToString());
		ShowStatus($"Command points set to {value} preview only.");
	}

	private void CloseModal() {
		if (_netLoadingModal is not null && RemoveChild(_netLoadingModal)) {
			_netLoadingModal.Dispose();
		}

		_netLoadingModal = null;
		_closed();
	}

	private void ToggleNetLoadingPreview() {
		if (_netLoadingModal is not null) {
			CloseNetLoadingPreview();
			return;
		}

		_netLoadingModal = AddChild(new LegacyNetLoadingModal(
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			CloseNetLoadingPreview));
	}

	private void CloseNetLoadingPreview() {
		if (_netLoadingModal is not null && RemoveChild(_netLoadingModal)) {
			_netLoadingModal.Dispose();
		}

		_netLoadingModal = null;
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

	private LegacyDropdownNode AddSlotDropdownNode(string label, DROPDOWN_DATA data) {
		var node = AddDropdownNode(label, data);
		node.Position += SlotOriginOffset;
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

	private LegacyStaticNode AddSlotStaticNode(string label, STATIC_DATA data) {
		var node = AddStaticNode(label, data);
		node.Position += SlotOriginOffset;
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

	private struct SkirmishSlotPreview {
		public SkirmishSlotState State;
		public SkirmishSlotType Type;
		public SkirmishComputerChallenge CompChallenge;
		public SkirmishRace Race;
		public SkirmishColor Color;
		public SkirmishTeam Team;
		public string Name;
		public string Ping;
		public bool IsLocal;
	}
}

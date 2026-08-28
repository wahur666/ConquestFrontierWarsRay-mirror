using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class LegacyLocalNetworkSessionModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private readonly Action _closed;
	private readonly Action<MultiplayerNetworkKind, bool, string> _proceed;
	private readonly GT_MENU1_NET_SESSIONS2 _menuData;
	private readonly MultiplayerNetworkKind _networkKind;
	private readonly LegacyRcStringResolver _strings;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly string _playerName;
	private readonly bool _preferCreate;
	private readonly List<SessionPreview> _sessions = [];
	private LegacyListBoxNode? _sessionList;
	private LegacyButtonNode? _nextButton;
	private TextNode? _descriptionLabel;
	private Task<PendingActivationResult>? _pendingActivationTask;
	private float _refreshCountdown = 0.6f;
	private bool _activationInProgress;
	private bool _listUpdated;

	public LegacyLocalNetworkSessionModal(
		GT_MENU1_NET_SESSIONS2 menuData,
		MultiplayerNetworkKind networkKind,
		string playerName,
		bool preferCreate,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action<MultiplayerNetworkKind, bool, string> proceed,
		Action closed) : base("LegacyLocalNetworkSessionModal", closed) {
		_menuData = menuData;
		_networkKind = networkKind;
		_playerName = string.IsNullOrWhiteSpace(playerName) ? "Commander" : playerName.Trim();
		_preferCreate = preferCreate;
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
		_proceed = proceed;
		_closed = closed;
	}

	protected override void OnInitialize() {
		base.OnInitialize();
		ContentRoot.Position = ResolveScreenPosition(_menuData.ScreenRect);

		AddStaticNode("Background", _menuData.Background);
		AddStaticNode("DescriptionFrame", _menuData.Description);
		AddStaticNode("VersionLabel", _menuData.Version);
		AddStaticNode("Title", _menuData.Title);
		AddStaticNode("StaticGame", _menuData.StaticGame);
		AddStaticNode("StaticPlayers", _menuData.StaticPlayers);
		AddStaticNode("StaticSpeed", _menuData.StaticSpeed);
		AddStaticNode("StaticMap", _menuData.StaticMap);
		AddStaticNode("StaticResources", _menuData.StaticResources);

		var versionNode = AddStaticNode("VersionValue", _menuData.Version);
		versionNode.SetText($"Version {ResolveVersionText()}");

		_sessionList = AddListBoxNode("SessionList", _menuData.List);
		_sessionList.CommitOnDoubleClick = true;
		_sessionList.CaretMoved += _ => UpdateSelectionState();
		_sessionList.SelectionCommitted += _ => ActivateCurrentSelection();

		_nextButton = AddButtonNode("Next", _menuData.Next);
		var backButton = AddButtonNode("Back", _menuData.Back);
		_nextButton.EnableButton(false);
		_nextButton.Activated += _ => ActivateCurrentSelection();
		backButton.Activated += _ => _closed();

		_descriptionLabel = AddChild(new TextNode("DescriptionLabel") {
			Position = new Vector2(90f, 535f),
			FontSize = 15f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(195, 180, 145, 255),
			Text = ResolveSearchingText()
		});

		if (_networkKind == MultiplayerNetworkKind.LocalAreaNetwork && !_preferCreate) {
			NetworkService.StartLanSessionDiscovery();
		}

		PopulateSessionList();
		_sessionList.SetKeyboardFocus(true);
		if (_preferCreate) {
			StartPreferredCreateActivation();
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);
		if (_pendingActivationTask is { IsCompleted: true }) {
			CompletePendingActivation();
		}

		if (_sessionList is null) {
			return;
		}

		_refreshCountdown -= deltaTime;
		if (_refreshCountdown > 0f) {
			return;
		}

		_refreshCountdown = _networkKind == MultiplayerNetworkKind.LocalAreaNetwork ? 0.5f : 2f;
		PopulateSessionList();
	}

	protected override void OnDispose() {
		if (_networkKind == MultiplayerNetworkKind.LocalAreaNetwork && !NetworkService.IsHostingLanSession) {
			NetworkService.StopLanSessionDiscovery();
		}

		base.OnDispose();
	}

	private void PopulateSessionList() {
		if (_sessionList is null) {
			return;
		}

		var previousKey = GetSelectedSessionKey();
		_sessions.Clear();
		if (_networkKind == MultiplayerNetworkKind.LocalAreaNetwork) {
			_sessions.Add(new SessionPreview {
				Key = "__create__",
				SessionName = "Create New LAN Game",
				IsCreateEntry = true,
				PlayerCount = 0,
				GameSpeed = "Create",
				MapType = string.Empty,
				Resources = string.Empty
			});
		}

		_sessions.AddRange(BuildDiscoveredSessions());
		_sessionList.ResetContent();
		for (var index = 0; index < _sessions.Count; index++) {
			var preview = _sessions[index];
			var rowIndex = _sessionList.AddString(FormatSessionRow(preview));
			_sessionList.SetDataValue(rowIndex, (uint)index);
			if (preview.IsCreateEntry) {
				_sessionList.SetColorValue(rowIndex, new Color(220, 210, 120, 255));
			}
		}

		var selectedIndex = FindSelectionIndex(previousKey);
		if (selectedIndex < 0 && _preferCreate && _sessions.Count > 0 && _sessions[0].IsCreateEntry) {
			selectedIndex = 0;
		}

		if (selectedIndex >= 0) {
			_sessionList.SetCurrentSelection(selectedIndex);
		}

		_listUpdated = true;
		UpdateSelectionState();
	}

	private List<SessionPreview> BuildDiscoveredSessions() {
		if (_networkKind != MultiplayerNetworkKind.LocalAreaNetwork) {
			return [];
		}

		return NetworkService.GetLanSessionAnnouncements()
			.Select(announcement => new SessionPreview {
				Key = announcement.Key,
				SessionName = announcement.SessionName,
				PlayerCount = 0,
				GameSpeed = announcement.IsLocalHost ? "Host" : "Join",
				MapType = announcement.HostIp,
				Resources = announcement.Port.ToString(),
				Announcement = announcement
			})
			.ToList();
	}

	private void UpdateSelectionState() {
		if (_sessionList is null || _nextButton is null || _descriptionLabel is null) {
			return;
		}

		var selection = _sessionList.GetCurrentSelection();
		_nextButton.EnableButton(selection >= 0 && !_activationInProgress);
		if (selection < 0) {
			_descriptionLabel.Text = _listUpdated
				? _sessions.Count == 0
					? ResolveNoGamesText()
					: ResolveSelectGameText()
				: ResolveSearchingText();
			return;
		}

		var preview = GetSelectedPreview();
		if (preview is null) {
			_descriptionLabel.Text = ResolveSelectGameText();
			return;
		}

		_descriptionLabel.Text = preview.Value.IsCreateEntry
			? ResolveCreateGameText()
			: $"Join {preview.Value.SessionName} at {preview.Value.MapType}:{preview.Value.Resources}.";
	}

	private void ActivateCurrentSelection() {
		var preview = GetSelectedPreview();
		if (preview is null || _activationInProgress) {
			return;
		}

		_activationInProgress = true;
		UpdateSelectionState();
		_pendingActivationTask = BeginActivationAsync(preview.Value);
	}

	private void StartPreferredCreateActivation() {
		if (_activationInProgress) {
			return;
		}

		_activationInProgress = true;
		UpdateSelectionState();
		_pendingActivationTask = BeginPreferredCreateActivationAsync();
	}

	private async Task<PendingActivationResult> BeginPreferredCreateActivationAsync() {
		if (_networkKind != MultiplayerNetworkKind.LocalAreaNetwork) {
			return new PendingActivationResult(_playerName, true);
		}

		return await BeginCreateActivationAsync();
	}

	private async Task<PendingActivationResult> BeginActivationAsync(SessionPreview preview) {
		if (preview.IsCreateEntry) {
			return await BeginCreateActivationAsync();
		}

		if (preview.Announcement is null) {
			throw new InvalidOperationException("Selected session is missing its LAN announcement.");
		}

		await NetworkService.JoinLanSessionAsync(preview.Announcement);
		return new PendingActivationResult(preview.SessionName, false);
	}

	private async Task<PendingActivationResult> BeginCreateActivationAsync() {
		if (_networkKind != MultiplayerNetworkKind.LocalAreaNetwork) {
			return new PendingActivationResult(DetermineCreatedSessionName(), true);
		}

		NetworkService.StopLanSessionDiscovery();
		var sessionName = DetermineCreatedSessionName();
		await NetworkService.HostLanSessionAsync(sessionName);
		return new PendingActivationResult(sessionName, true);
	}

	private void CompletePendingActivation() {
		var task = _pendingActivationTask;
		if (task is null || !task.IsCompleted) {
			return;
		}

		_pendingActivationTask = null;
		try {
			var result = task.GetAwaiter().GetResult();
			_activationInProgress = false;
			UpdateSelectionState();
			_proceed(_networkKind, result.IsHost, result.SessionName);
		} catch (Exception ex) {
			_activationInProgress = false;
			AppLog.Error("LegacyLocalNetworkSessionModal", "Failed to activate LAN session.", ex);
			if (_descriptionLabel is not null) {
				_descriptionLabel.Text = $"Network error: {ex.Message}";
			}
			UpdateSelectionState();
		}
	}

	private SessionPreview? GetSelectedPreview() {
		if (_sessionList is null) {
			return null;
		}

		var selection = _sessionList.GetCurrentSelection();
		if (selection < 0 || selection >= _sessions.Count) {
			return null;
		}

		return _sessions[selection];
	}

	private string DetermineCreatedSessionName() {
		var baseName = _playerName;
		var matches = _sessions.Count(session =>
			!session.IsCreateEntry &&
			session.SessionName.StartsWith(baseName, StringComparison.OrdinalIgnoreCase));
		return matches > 0 ? $"{baseName}({matches + 1})" : baseName;
	}

	private int FindSelectionIndex(string? key) {
		if (string.IsNullOrWhiteSpace(key)) {
			return -1;
		}

		for (var index = 0; index < _sessions.Count; index++) {
			if (string.Equals(_sessions[index].Key, key, StringComparison.OrdinalIgnoreCase)) {
				return index;
			}
		}

		return -1;
	}

	private string? GetSelectedSessionKey() {
		return GetSelectedPreview()?.Key;
	}

	private string FormatSessionRow(SessionPreview preview) {
		return string.Format(
			"{0,-20}{1,9}{2,12}{3,20}{4,10}",
			TrimColumn(preview.SessionName, 18),
			preview.IsCreateEntry ? string.Empty : preview.PlayerCount.ToString(),
			TrimColumn(preview.GameSpeed, 10),
			TrimColumn(preview.MapType, 18),
			TrimColumn(preview.Resources, 8));
	}

	private static string TrimColumn(string value, int maxLength) {
		if (string.IsNullOrWhiteSpace(value)) {
			return string.Empty;
		}

		var trimmed = value.Trim();
		return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
	}

	private string ResolveSearchingText() {
		return _networkKind == MultiplayerNetworkKind.LocalAreaNetwork
			? "Searching for local multiplayer games..."
			: "Searching for internet multiplayer games...";
	}

	private string ResolveNoGamesText() {
		return _networkKind == MultiplayerNetworkKind.LocalAreaNetwork
			? "No local multiplayer games found."
			: "No multiplayer games found for the selected connection.";
	}

	private static string ResolveSelectGameText() {
		return "Select a game or create a new session.";
	}

	private static string ResolveCreateGameText() {
		return "Create a new multiplayer session with the current player name.";
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

	private static string ResolveVersionText() {
		var version = Assembly.GetEntryAssembly()?.GetName().Version;
		if (version is null) {
			return "Unknown";
		}

		return version.Build >= 0
			? $"{version.Major}.{version.Minor}.{version.Build}"
			: $"{version.Major}.{version.Minor}";
	}

	private static Vector2 ResolveScreenPosition(RECT screenRect) {
		var width = Math.Max(0f, screenRect.Right - screenRect.Left);
		var height = Math.Max(0f, screenRect.Bottom - screenRect.Top);
		return new Vector2(
			(LegacyScreenWidth - width) * 0.5f,
			(LegacyScreenHeight - height) * 0.5f);
	}

	private struct SessionPreview {
		public string Key;
		public string SessionName;
		public int PlayerCount;
		public string GameSpeed;
		public string MapType;
		public string Resources;
		public bool IsCreateEntry;
		public LanSessionAnnouncement? Announcement;
	}

	private readonly record struct PendingActivationResult(string SessionName, bool IsHost);
}

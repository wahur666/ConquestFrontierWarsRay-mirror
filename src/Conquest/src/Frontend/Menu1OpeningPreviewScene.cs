using System;
using System.Collections.Generic;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.UserProfiles;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class Menu1OpeningPreviewScene : Node2D {
	private readonly UiEventSource _eventSource = new("Menu1OpeningEventSource");
	private readonly LegacyMenuRoot _legacyMenuRoot = new("LegacyMenuRoot");
	private readonly bool _showAboutOnInitialize;

	public Menu1OpeningPreviewScene(bool showAboutOnInitialize = false) : base("Menu1OpeningPreviewScene") {
		_showAboutOnInitialize = showAboutOnInitialize;
		_eventSource.ScopeRoot = this;
		AddChild(_eventSource);
		AddChild(_legacyMenuRoot);
	}

	protected override void OnInitialize() {
		try {
			_legacyMenuRoot.SetContentRoot(new Menu1OpeningPreviewSurface(_showAboutOnInitialize));
		} catch (Exception ex) {
			AppLog.Error("Menu1OpeningPreviewScene", "Failed to initialize menu opening preview.", ex);
			_legacyMenuRoot.SetContentRoot(new Menu1OpeningErrorSurface(ex));
		}
	}

	protected override void Draw() {
		Raylib.ClearBackground(Color.Black);
	}
}

internal sealed class Menu1OpeningPreviewSurface : Node2D {
	private const uint ConfirmTitleTextId = 1345;
	private const uint ConfirmQuitMessageTextId = 1406;
	private const string MainMenuMusicTrack = "Conquest Frontier Wars soundtrack - Main Menu Screen Music.mp3";
	private const string SinglePlayerMusicTrack = "Conquest Frontier Wars soundtrack - Single Player Menu Music.mp3";
	private const string MultiplayerMenuMusicTrack = "Conquest Frontier Wars soundtrack - Multiplayer MenuCredits Music.mp3";
	private static readonly Color AnimationMarker = new(214, 120, 228, 255);
	private const float MusicFadeInDurationSeconds = 2f;
	private readonly List<AtlasFramesResource> _atlasResources = [];
	private readonly List<LegacyButtonNode> _openingButtons = [];
	private readonly List<AnimatedSprite2D?> _animatedSprite2Ds = [];
	private readonly GT_MENU1_HELPMENU _helpMenuData;
	private readonly GT_MENU1_OPENING _opening;
	private readonly GT_MENU1_SINGLEPLAYER_MENU _singlePlayerMenu;
	private readonly GT_MENU1_SELECT_CAMPAIGN _selectCampaignMenu;
	private readonly GT_MENU1_NET_CONNECTIONS _networkConnectionMenu;
	private readonly GT_MENU1_IP_ADDRESS _onlineNetworkSessionMenu;
	private readonly GT_MENU1_NET_SESSIONS2 _localNetworkSessionMenu;
	private readonly GT_MENU1_MSHELL _mshellMenu;
	private readonly GT_MENU1_MAP _mapMenu;
	private readonly GT_MENU1_SLOTS _slotsMenu;
	private readonly GT_MENU1_FINAL _finalMenu;
	private readonly GT_OPTIONS _options;
	private readonly bool _showAboutOnInitialize;
	private readonly GT_MESSAGEBOX _quitMessageBox;

	private readonly GT_NEWPLAYER _newPlayer;

	// private readonly UtfDbRepository _utfDbRepository;
	private readonly UserProfilesRepository _userProfilesRepository;
	private readonly bool _requiresInitialUser;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly LegacyRcStringResolver _strings;
	private Menu1HelpModalOverlay? _aboutModal;
	private LegacyMessageBoxModal? _exitModal;
	private LegacyNewUserModal? _newUserModal;
	private LegacyOptionsModal? _optionsModal;
	private LegacySinglePlayerModal? _singlePlayerModal;
	private LegacyNetworkConnectionModal? _networkConnectionModal;
	private LegacyOnlineNetworkSessionModal? _onlineNetworkSessionModal;
	private LegacyLocalNetworkSessionModal? _localNetworkSessionModal;
	private LegacySkirmishModal? _skirmishModal;
	private SinglePlayerLoadingPreviewModal? _inProgressAnimModal;
	private SkirmishReturnTarget _skirmishReturnTarget;
	private MultiplayerNetworkKind _lastMultiplayerNetworkKind = MultiplayerNetworkKind.LocalAreaNetwork;
	private bool _lastMultiplayerIsHost = true;
	private string _lastMultiplayerSessionName = string.Empty;
	private string _currentMusicTrack = MainMenuMusicTrack;
	private AudioPlayer? _musicPlayer;
	private float _musicVolume = 0.5f;

	public Menu1OpeningPreviewSurface(bool showAboutOnInitialize) : base("Menu1OpeningPreviewSurface") {
		_showAboutOnInitialize = showAboutOnInitialize;
		_xmlDbRepository = new XmlDbRepository(RepoPaths.LocateUtfDbPaths());
		_vfxRepository = VfxAnimationDataRepository.LocateFromRepo();
		_userProfilesRepository = UserProfilesRepository.LocateFromRepo();
		_requiresInitialUser = !_userProfilesRepository.HasUsers();
		_strings = LegacyRcStringResolver.LoadFromRepo();
		var menu1 = ReadTypedEntry<GT_MENU1>("GT_MENU1", "Menu1");
		_opening = menu1.Opening;
		_singlePlayerMenu = menu1.SinglePlayerMenu;
		_selectCampaignMenu = menu1.SelectCampaign;
		_networkConnectionMenu = menu1.NetConnections;
		_onlineNetworkSessionMenu = menu1.IpAddress;
		_localNetworkSessionMenu = menu1.NetSessions2;
		_mshellMenu = menu1.MShell;
		_mapMenu = menu1.Map;
		_slotsMenu = menu1.Slots;
		_finalMenu = menu1.Final;
		_helpMenuData = menu1.HelpMenu;
		_quitMessageBox = ReadTypedEntry<GT_MESSAGEBOX>("GT_MESSAGEBOX", "CQMessageBox");
		_newPlayer = ReadTypedEntry<GT_NEWPLAYER>("GT_NEWPLAYER", "MenuNewPlayer");
		_options = ReadTypedEntry<GT_OPTIONS>("GT_OPTIONS", "MenuOptions");
	}

	protected override void OnInitialize() {
		AddStaticNode("Background", _opening.Background);
		var btnSingle = AddButtonNode("Single", GetOpeningButton(0));
		var btnMulti = AddButtonNode("Multi", GetOpeningButton(1));
		var btnIntro = AddButtonNode("Intro", GetOpeningButton(2));
		var btnOptions = AddButtonNode("Options", GetOpeningButton(3));
		var btnHelp = AddButtonNode("Help", GetOpeningButton(4));
		var btnQuit = AddButtonNode("Quit", GetOpeningButton(5));
		AddStaticNode("StaticSingle", GetOpeningStaticLabel(0));
		AddStaticNode("StaticMulti", GetOpeningStaticLabel(1));
		AddStaticNode("StaticIntro", GetOpeningStaticLabel(2));
		AddStaticNode("StaticOptions", GetOpeningStaticLabel(3));
		AddStaticNode("StaticHelp", GetOpeningStaticLabel(4));
		var animMedia = AddAnimationNode("AnimMedia", GetOpeningAnimation(0));
		var animSingle = AddAnimationNode("AnimSingle", GetOpeningAnimation(1));
		var animMulti = AddAnimationNode("AnimMulti", GetOpeningAnimation(2));
		var animOptions = AddAnimationNode("AnimOptions", GetOpeningAnimation(3));
		var animQuestion = AddAnimationNode("AnimQuestion", GetOpeningAnimation(4));
		_animatedSprite2Ds.AddRange(animSingle, animMulti, animOptions, animQuestion);
		ConnectButtonHover(btnSingle, animSingle);
		ConnectButtonHover(btnMulti, animMulti);
		ConnectButtonHover(btnIntro, animMedia);
		ConnectButtonHover(btnOptions, animOptions);
		ConnectButtonHover(btnHelp, animQuestion);
		btnSingle.Activated += _ => OpenSinglePlayerModal();
		btnMulti.Activated += _ => OpenNetworkConnectionModal();
		btnIntro.Activated += _ => OpenMovie(@"Assets\Movies\cq_intro.mp4");
		btnOptions.Activated += _ => OpenOptionsModal();
		btnHelp.Activated += _ => OpenAboutModal();
		btnQuit.Activated += _ => RequestQuit();
		AddStaticNode("StaticLegal", _opening.StaticLegal);
		_musicPlayer = AddMusicPlayer();
		RefreshMusicSettings();
		if (_showAboutOnInitialize) {
			OpenAboutModal();
		} else if (_requiresInitialUser) {
			OpenNewUserModal();
		}
	}

	protected override void OnExitTree() {
		_musicPlayer?.Stop();
		base.OnExitTree();
	}

	private void OpenMovie(string moviePath) {
		_musicPlayer?.Stop();
		Tree.ChangeRoot(new MovieScene(moviePath, () => new Menu1OpeningPreviewScene()));
	}

	protected override void OnUpdate(float deltaTime) {
		FadeInMusic(deltaTime);
		if (Raylib.IsKeyPressed(KeyboardKey.F2)) {
			if (_inProgressAnimModal is not null) {
				CloseInProgressAnimModal();
				return;
			}

			if (!HasBlockingModal()) {
				OpenInProgressAnimModal();
				return;
			}
		}

		if (!HasBlockingModal() && Input.UiEsc) {
			OpenExitModal();
		}
	}

	private void FadeInMusic(float deltaTime) {
		if (_musicPlayer is not null && _musicPlayer.HasAudio && _musicPlayer.Volume < _musicVolume) {
			_musicPlayer.Volume = MathF.Min(_musicVolume, _musicPlayer.Volume + (deltaTime / MusicFadeInDurationSeconds));
		}
	}

	private AudioPlayer AddMusicPlayer() {
		return AddChild(new AudioPlayer("AudioPlayer") {
			Looping = true,
			Volume = 0f
		});
	}

	private void SetMusicTrack(string trackName) {
		_currentMusicTrack = trackName;
		if (_musicPlayer is null) {
			return;
		}

		if (_musicVolume <= 0f) {
			ReleaseMusic();
			return;
		}

		LoadAndPlayMusic(trackName, startAtTargetVolume: false);
	}

	private void RefreshMusicSettings() {
		ApplyMusicSettings(_userProfilesRepository.Load().Sound, immediate: false);
	}

	private void PreviewMusicSettings(SoundOptionsData soundSettings) {
		ApplyMusicSettings(soundSettings, immediate: true);
	}

	private void ApplyMusicSettings(SoundOptionsData soundSettings, bool immediate) {
		_musicVolume = soundSettings.MusicEnabled
			? Math.Clamp(soundSettings.MusicVolume / 10f, 0f, 1f)
			: 0f;
		if (_musicPlayer is null) {
			return;
		}

		if (_musicVolume <= 0f) {
			ReleaseMusic();
			return;
		}

		if (!_musicPlayer.HasAudio) {
			LoadAndPlayMusic(_currentMusicTrack, startAtTargetVolume: immediate);
			return;
		}

		if (immediate || _musicPlayer.Volume > _musicVolume) {
			_musicPlayer.Volume = _musicVolume;
		}
	}

	private void LoadAndPlayMusic(string trackName, bool startAtTargetVolume) {
		if (_musicPlayer is null) {
			return;
		}

		var music = Shared.ResourceManager.Audio.OpenMusic(trackName, AudioPlaybackBackend.NAudio);
		_musicPlayer.SetOwnedAudio(music);
		_musicPlayer.Volume = startAtTargetVolume ? _musicVolume : 0f;
		_musicPlayer.Play();
	}

	private void ReleaseMusic() {
		if (_musicPlayer is null || !_musicPlayer.HasAudio) {
			return;
		}

		_musicPlayer.Stop();
		_musicPlayer.DisposeAudio();
	}

	private static void ConnectButtonHover(LegacyButtonNode button, AnimatedSprite2D? animation) {
		if (animation is null) {
			return;
		}

		button.Entered += _ => {
			animation.Visible = true;
			animation.Play();
		};
		button.Exited += _ => {
			animation.Visible = false;
			animation.Pause();
		};
	}

	private LegacyButtonNode AddButtonNode(string label, BUTTON_DATA data) {
		var archetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", data.ButtonType);
		var node = new LegacyButtonNode(label);

		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.ButtonText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.Text = text;
		}

		AddChild(node);
		_openingButtons.Add(node);
		return node;
	}

	private void AddStaticNode(string label, STATIC_DATA data) {
		var archetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", data.StaticType);
		var node = new LegacyStaticNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.StaticText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.SetText(text);
		}

		AddChild(node);
	}

	private AnimatedSprite2D? AddAnimationNode(string label, ANIMATE_DATA data) {
		var archetype = ReadTypedEntry<GT_ANIMATE>("GT_ANIMATE", data.AnimateType);
		var vfxData = _vfxRepository.Load();
		if (!vfxData.TryGetAtlasByVfxShapeId(archetype.VfxType, out var atlasEntry)) {
			AddMarker(label, new Vector2(data.XOrigin, data.YOrigin), new Vector2(34f, 34f), AnimationMarker);
			return null;
		}

		var atlas = new AtlasFramesResource(
			_vfxRepository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: false),
			_vfxRepository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: true));
		_atlasResources.Add(atlas);

		var node = new AnimatedSprite2D(atlas.Frames, label) {
			Position = new Vector2(data.XOrigin, data.YOrigin),
			Pivot = Vector2.Zero,
			Loop = true,
			SpeedFps = 15,
			Visible = false
		};
		AddChild(node);
		return node;
	}

	private void AddMarker(string label, Vector2 position, Vector2 size, Color color) {
		AddChild(new PanelNode($"{label}Marker") {
			Position = position,
			Size = size,
			Fill = new Color(color.R, color.G, color.B, (byte)38),
			Outline = color,
			OutlineThickness = 1f
		});
	}

	private void OpenAboutModal() {
		if (HasBlockingModal()) {
			return;
		}

		_animatedSprite2Ds.ForEach(x => x?.Visible = false);
		SetOpeningButtonsEnabled(false);
		_aboutModal = AddChild(new Menu1HelpModalOverlay(
			ToHelpMenuRecord(),
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			() => new MenuCreditsScene(() => new Menu1OpeningPreviewScene(showAboutOnInitialize: true)),
			CloseAboutModal));
	}

	private void CloseAboutModal() {
		if (_aboutModal is null) {
			return;
		}

		if (RemoveChild(_aboutModal)) {
			_aboutModal.Dispose();
		}

		_aboutModal = null;
		SetOpeningButtonsEnabled(true);
	}

	private void OpenExitModal() {
		if (HasBlockingModal()) {
			return;
		}

		_animatedSprite2Ds.ForEach(x => x?.Visible = false);
		SetOpeningButtonsEnabled(false);
		_exitModal = AddChild(new LegacyMessageBoxModal(
			_quitMessageBox,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			ResolveString(ConfirmTitleTextId, "Confirm Choice"),
			ResolveString(ConfirmQuitMessageTextId, "Do you really want to quit?"),
			LegacyMessageBoxButtons.OkCancel,
			OnExitModalCompleted));
	}

	private void OpenInProgressAnimModal() {
		if (HasBlockingModal()) {
			return;
		}

		_animatedSprite2Ds.ForEach(x => x?.Visible = false);
		SetOpeningButtonsEnabled(false);
		_inProgressAnimModal = AddChild(new SinglePlayerLoadingPreviewModal(
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			CloseInProgressAnimModal));
	}

	private void CloseInProgressAnimModal() {
		if (_inProgressAnimModal is not null && RemoveChild(_inProgressAnimModal)) {
			_inProgressAnimModal.Dispose();
		}

		_inProgressAnimModal = null;
		SetOpeningButtonsEnabled(true);
	}

	private void OnExitModalCompleted(bool confirmed) {
		if (_exitModal is not null && RemoveChild(_exitModal)) {
			_exitModal.Dispose();
		}

		_exitModal = null;
		SetOpeningButtonsEnabled(true);
		if (confirmed) {
			RequestQuit();
		}
	}

	private void OpenNewUserModal() {
		if (HasBlockingModal()) {
			return;
		}

		_animatedSprite2Ds.ForEach(x => x?.Visible = false);
		SetOpeningButtonsEnabled(false);
		_newUserModal = AddChild(new LegacyNewUserModal(
			_newPlayer,
			_userProfilesRepository,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			OnNewUserSaved,
			string.Empty,
			_requiresInitialUser ? RequestQuit : CloseNewUserModal));
	}

	private void OnNewUserSaved(UserProfilesData _, string __) {
		CloseNewUserModal();
	}

	private void CloseNewUserModal() {
		if (_newUserModal is null) {
			return;
		}

		if (RemoveChild(_newUserModal)) {
			_newUserModal.Dispose();
		}

		_newUserModal = null;
		SetOpeningButtonsEnabled(true);
	}

	private void OpenOptionsModal() {
		if (HasBlockingModal()) {
			return;
		}

		_animatedSprite2Ds.ForEach(x => x?.Visible = false);
		SetOpeningButtonsEnabled(false);
		_optionsModal = AddChild(new LegacyOptionsModal(
			_options,
			_newPlayer,
			_userProfilesRepository,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			PreviewMusicSettings,
			CloseOptionsModal));
	}

	private void CloseOptionsModal() {
		if (_optionsModal is not null && RemoveChild(_optionsModal)) {
			_optionsModal.Dispose();
		}

		_optionsModal = null;
		RefreshMusicSettings();
		SetOpeningButtonsEnabled(true);
	}

	private void OpenSinglePlayerModal() {
		if (HasBlockingModal()) {
			return;
		}

		_animatedSprite2Ds.ForEach(x => x?.Visible = false);
		SetOpeningButtonsEnabled(false);
		_singlePlayerModal = AddChild(new LegacySinglePlayerModal(
			_singlePlayerMenu,
			_selectCampaignMenu,
			_userProfilesRepository,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			OpenQuickBattleModal,
			CloseSinglePlayerModal));
	}

	private void CloseSinglePlayerModal() {
		if (_singlePlayerModal is not null && RemoveChild(_singlePlayerModal)) {
			_singlePlayerModal.Dispose();
		}

		_singlePlayerModal = null;
		SetOpeningButtonsEnabled(true);
	}

	private void OpenNetworkConnectionModal() {
		if (HasBlockingModal()) {
			return;
		}

		_animatedSprite2Ds.ForEach(x => x?.Visible = false);
		SetOpeningButtonsEnabled(false);
		_networkConnectionModal = AddChild(new LegacyNetworkConnectionModal(
			_networkConnectionMenu,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			OpenNetworkConnectionSelection,
			CloseNetworkConnectionModal));
	}

	private void OpenNetworkConnectionSelection(NetworkConnectionKind kind) {
		CloseNetworkConnectionModalInternal(restoreOpeningButtons: false, restoreMusic: false);
		switch (kind) {
			case NetworkConnectionKind.Online:
				OpenOnlineNetworkSessionModal(MultiplayerNetworkKind.Internet);
				break;
			case NetworkConnectionKind.Local:
				OpenLocalNetworkSessionModal(MultiplayerNetworkKind.LocalAreaNetwork, preferCreate: false);
				break;
		}
	}

	private void CloseNetworkConnectionModal() {
		CloseNetworkConnectionModalInternal(restoreOpeningButtons: true, restoreMusic: true);
	}

	private void CloseNetworkConnectionModalInternal(bool restoreOpeningButtons, bool restoreMusic) {
		if (_networkConnectionModal is not null && RemoveChild(_networkConnectionModal)) {
			_networkConnectionModal.Dispose();
		}

		_networkConnectionModal = null;
		if (restoreOpeningButtons) {
			SetOpeningButtonsEnabled(true);
		}
	}

	private void OpenOnlineNetworkSessionModal(MultiplayerNetworkKind networkKind) {
		if (HasBlockingModal()) {
			return;
		}

		_lastMultiplayerNetworkKind = networkKind;
		_animatedSprite2Ds.ForEach(x => x?.Visible = false);
		SetOpeningButtonsEnabled(false);
		_onlineNetworkSessionModal = AddChild(new LegacyOnlineNetworkSessionModal(
			_onlineNetworkSessionMenu,
			networkKind,
			_userProfilesRepository,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			StartOnlineMultiplayerSkirmish,
			CloseOnlineNetworkSessionModal));
	}

	private void OpenLocalNetworkSessionModal(MultiplayerNetworkKind networkKind, bool preferCreate) {
		if (HasBlockingModal()) {
			return;
		}

		_lastMultiplayerNetworkKind = networkKind;
		_lastMultiplayerIsHost = preferCreate;
		_animatedSprite2Ds.ForEach(x => x?.Visible = false);
		SetOpeningButtonsEnabled(false);
		SetMusicTrack(MultiplayerMenuMusicTrack);
		_localNetworkSessionModal = AddChild(new LegacyLocalNetworkSessionModal(
			_localNetworkSessionMenu,
			networkKind,
			_userProfilesRepository.Load().CurrentUser,
			preferCreate,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			StartMultiplayerSkirmish,
			CloseLocalNetworkSessionModal));
	}

	private void StartOnlineMultiplayerSkirmish(MultiplayerNetworkKind networkKind, bool isHost) {
		_lastMultiplayerNetworkKind = networkKind;
		_lastMultiplayerIsHost = isHost;
		CloseOnlineNetworkSessionModalInternal(restoreConnections: false, restoreOpeningButtons: false, restoreMusic: false);
		StartMultiplayerSkirmish(networkKind, isHost, _userProfilesRepository.Load().CurrentUser);
	}

	private void CloseOnlineNetworkSessionModal() {
		CloseOnlineNetworkSessionModalInternal(restoreConnections: true, restoreOpeningButtons: false, restoreMusic: false);
	}

	private void CloseOnlineNetworkSessionModalInternal(bool restoreConnections, bool restoreOpeningButtons, bool restoreMusic) {
		if (_onlineNetworkSessionModal is not null && RemoveChild(_onlineNetworkSessionModal)) {
			_onlineNetworkSessionModal.Dispose();
		}

		_onlineNetworkSessionModal = null;
		if (restoreConnections) {
			OpenNetworkConnectionModal();
		}

		if (restoreOpeningButtons) {
			SetOpeningButtonsEnabled(true);
		}
	}

	private void StartMultiplayerSkirmish(MultiplayerNetworkKind networkKind, bool isHost, string sessionName) {
		_lastMultiplayerNetworkKind = networkKind;
		_lastMultiplayerIsHost = isHost;
		_lastMultiplayerSessionName = sessionName;
		CloseLocalNetworkSessionModalInternal(restoreConnections: false, restoreOpeningButtons: false, restoreMusic: false);
		OpenSkirmishModal(
			SkirmishMode.Multiplayer,
			networkKind,
			isHost,
			networkKind == MultiplayerNetworkKind.Internet
				? SkirmishReturnTarget.OnlineNetworkSession
				: SkirmishReturnTarget.LocalNetworkSession);
	}

	private void CloseLocalNetworkSessionModal() {
		CloseLocalNetworkSessionModalInternal(restoreConnections: true, restoreOpeningButtons: false, restoreMusic: false);
	}

	private void CloseLocalNetworkSessionModalInternal(bool restoreConnections, bool restoreOpeningButtons, bool restoreMusic) {
		if (_localNetworkSessionModal is not null && RemoveChild(_localNetworkSessionModal)) {
			_localNetworkSessionModal.Dispose();
		}

		_localNetworkSessionModal = null;
		if (restoreConnections) {
			OpenNetworkConnectionModal();
		}

		if (restoreOpeningButtons) {
			SetOpeningButtonsEnabled(true);
		}
	}

	private void OpenQuickBattleModal() {
		OpenSkirmishModal(SkirmishMode.QuickBattle, returnTarget: SkirmishReturnTarget.SinglePlayer);
	}

	private void OpenSkirmishModal(
		SkirmishMode mode,
		MultiplayerNetworkKind networkKind = MultiplayerNetworkKind.LocalAreaNetwork,
		bool isHost = true,
		SkirmishReturnTarget returnTarget = SkirmishReturnTarget.Opening) {
		if (_skirmishModal is not null) {
			return;
		}

		_skirmishReturnTarget = returnTarget;
		if (_singlePlayerModal is not null) {
			if (RemoveChild(_singlePlayerModal)) {
				_singlePlayerModal.Dispose();
			}
			_singlePlayerModal = null;
		} else if (!HasBlockingModal()) {
			_animatedSprite2Ds.ForEach(x => x?.Visible = false);
			SetOpeningButtonsEnabled(false);
		}

		SetMusicTrack(mode == SkirmishMode.Multiplayer ? MultiplayerMenuMusicTrack : SinglePlayerMusicTrack);
		_skirmishModal = AddChild(new LegacySkirmishModal(
			_mshellMenu,
			_mapMenu,
			_slotsMenu,
			_finalMenu,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			mode,
			networkKind,
			isHost,
			StartSkirmishScene,
			CloseSkirmishModal));
	}

	private void StartSkirmishScene() {
		Tree.ChangeRoot(new LegacySkirmishBattleScene(() => new Menu1OpeningPreviewScene()));
	}

	private void CloseSkirmishModal() {
		if (_skirmishModal is not null && RemoveChild(_skirmishModal)) {
			_skirmishModal.Dispose();
		}

		_skirmishModal = null;
		switch (_skirmishReturnTarget) {
			case SkirmishReturnTarget.SinglePlayer:
				OpenSinglePlayerModal();
				break;
			case SkirmishReturnTarget.OnlineNetworkSession:
				OpenOnlineNetworkSessionModal(_lastMultiplayerNetworkKind);
				break;
			case SkirmishReturnTarget.LocalNetworkSession:
				OpenLocalNetworkSessionModal(_lastMultiplayerNetworkKind, preferCreate: false);
				break;
			default:
				SetOpeningButtonsEnabled(true);
				SetMusicTrack(MainMenuMusicTrack);
				break;
		}
	}

	private bool HasBlockingModal() {
		return _aboutModal is not null ||
		       _inProgressAnimModal is not null ||
		       _exitModal is not null ||
		       _newUserModal is not null ||
		       _optionsModal is not null ||
		       _singlePlayerModal is not null ||
		       _networkConnectionModal is not null ||
		       _onlineNetworkSessionModal is not null ||
		       _localNetworkSessionModal is not null ||
		       _skirmishModal is not null;
	}

	private void SetOpeningButtonsEnabled(bool enabled) {
		foreach (var button in _openingButtons) {
			button.EnableButton(enabled);
			button.SetKeyboardFocus(false);
		}
	}

	private BUTTON_DATA GetOpeningButton(int index) => _opening.Buttons[index];

	private STATIC_DATA GetOpeningStaticLabel(int index) => _opening.StaticLabels[index];

	private ANIMATE_DATA GetOpeningAnimation(int index) => _opening.Animations[index];

	private Menu1HelpMenuData ToHelpMenuRecord() {
		return new Menu1HelpMenuData(
			_helpMenuData.ScreenRect,
			_helpMenuData.Background,
			_helpMenuData.Title,
			_helpMenuData.StaticConquest,
			_helpMenuData.StaticVersion,
			_helpMenuData.StaticNumber,
			_helpMenuData.ButtonOk,
			_helpMenuData.StaticProductId,
			_helpMenuData.StaticProductNumber,
			_helpMenuData.StaticLegal,
			_helpMenuData.ButtonCredits);
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

	protected override void OnDispose() {
		for (var index = _atlasResources.Count - 1; index >= 0; index--) {
			_atlasResources[index].Dispose();
		}

		_atlasResources.Clear();
		base.OnDispose();
	}

	private enum SkirmishReturnTarget {
		Opening,
		SinglePlayer,
		OnlineNetworkSession,
		LocalNetworkSession
	}
}

internal sealed class Menu1OpeningErrorSurface : Node2D {
	public Menu1OpeningErrorSurface(Exception exception) : base("Menu1OpeningErrorSurface") {
		ArgumentNullException.ThrowIfNull(exception);

		AddChild(new PanelNode("ErrorBackground") {
			Position = Vector2.Zero,
			Size = new Vector2(800f, 600f),
			Fill = new Color(24, 14, 14, 255),
			Outline = new Color(128, 62, 62, 255),
			OutlineThickness = 2f
		});
		AddChild(new TextNode("ErrorTitle") {
			Position = new Vector2(24f, 24f),
			FontSize = 24f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(255, 220, 220, 255),
			Text = "Menu1 opening preview failed"
		});
		AddChild(new TextNode("ErrorMessage") {
			Position = new Vector2(24f, 64f),
			FontSize = 16f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(255, 178, 178, 255),
			Text = exception.Message
		});
	}
}

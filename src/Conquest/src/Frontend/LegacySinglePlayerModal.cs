using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.UserProfiles;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class LegacySinglePlayerModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private readonly Action _closed;
	private readonly Action _openQuickBattle;
	private readonly GT_MENU1_SINGLEPLAYER_MENU _singlePlayerMenu;
	private readonly GT_MENU1_SELECT_CAMPAIGN _selectCampaignMenu;
	private readonly GT_MENU1_SELECT_MISSION _selectMissionMenu;
	private readonly LegacyRcStringResolver _strings;
	private readonly UserProfilesRepository _userProfilesRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly XmlDbRepository _xmlDbRepository;
	private LegacyButtonNode? _buttonCampaign;
	private LegacyButtonNode? _buttonSkirmish;
	private LegacyButtonNode? _buttonLoad;
	private LegacyButtonNode? _buttonQuickbattleLoad;
	private LegacyButtonNode? _buttonBack;
	private LegacyCampaignModal? _campaignModal;
	private TextNode? _statusLabel;

	public LegacySinglePlayerModal(
		GT_MENU1_SINGLEPLAYER_MENU singlePlayerMenu,
		GT_MENU1_SELECT_CAMPAIGN selectCampaignMenu,
		GT_MENU1_SELECT_MISSION selectMissionMenu,
		UserProfilesRepository userProfilesRepository,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action openQuickBattle,
		Action closed) : base("LegacySinglePlayerModal", closed) {
		_singlePlayerMenu = singlePlayerMenu;
		_selectCampaignMenu = selectCampaignMenu;
		_selectMissionMenu = selectMissionMenu;
		_userProfilesRepository = userProfilesRepository;
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
		_openQuickBattle = openQuickBattle;
		_closed = closed;
	}

	protected override void OnInitialize() {
		base.OnInitialize();
		ContentRoot.Position = ResolveScreenPosition(_singlePlayerMenu.ScreenRect);

		AddStaticNode("Background", _singlePlayerMenu.Background);
		AddStaticNode("Title", _singlePlayerMenu.StaticSingle);
		AddPlayerNameNode();

		_buttonCampaign = AddButtonNode("Campaign", GetButton(0));
		_buttonSkirmish = AddButtonNode("Skirmish", GetButton(1));
		_buttonLoad = AddButtonNode("LoadSaved", GetButton(2));
		_buttonQuickbattleLoad = AddButtonNode("LoadQuickbattle", GetButton(3));
		_buttonBack = AddButtonNode("Back", GetButton(4));

		_buttonCampaign.Activated += _ => OpenCampaignModal();
		_buttonSkirmish.Activated += _ => _openQuickBattle();
		_buttonLoad.Activated += _ => ShowStatus("Saved-game loading is not ported yet.");
		_buttonQuickbattleLoad.Activated += _ => ShowStatus("Quickbattle loading is not ported yet.");
		_buttonBack.Activated += _ => CloseModal();
		_buttonCampaign.SetKeyboardFocus(true);

		ApplySaveAvailability();

		_statusLabel = AddChild(new TextNode("StatusLabel") {
			Position = new Vector2(106f, 274f),
			FontSize = 14f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(200, 180, 140, 255),
			Text = string.Empty
		});
	}

	private void AddPlayerNameNode() {
		var node = CreateStaticNode("PlayerName", _singlePlayerMenu.StaticName);
		var currentUser = _userProfilesRepository.Load().CurrentUser;
		node.SetText(currentUser);
		ContentRoot.AddChild(node);
	}

	private void ApplySaveAvailability() {
		var currentUser = _userProfilesRepository.Load().CurrentUser;
		var playerSaveDirectory = ResolvePlayerSaveDirectory(currentUser);
		var singlePlayerSaveCount = CountSavedGames(playerSaveDirectory, 'f');
		var skirmishSaveCount = CountSavedGames(playerSaveDirectory, 'm');

		_buttonLoad?.EnableButton(singlePlayerSaveCount > 0);
		_buttonQuickbattleLoad?.EnableButton(skirmishSaveCount > 0);
	}

	private static string ResolvePlayerSaveDirectory(string currentUser) {
		return Path.Combine(RepoPaths.LocateRepoRoot(), "SavedGame", currentUser);
	}

	private static int CountSavedGames(string directoryPath, char expectedPrefix) {
		if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath)) {
			return 0;
		}

		return Directory.EnumerateFiles(directoryPath, "*.mission", SearchOption.TopDirectoryOnly)
			.Count(file => {
				var name = Path.GetFileName(file);
				return !string.IsNullOrWhiteSpace(name) &&
				       char.ToLowerInvariant(name[0]) == char.ToLowerInvariant(expectedPrefix);
			});
	}

	private void ShowStatus(string message) {
		if (_statusLabel is not null) {
			_statusLabel.Text = message;
		}
	}

	private void OpenCampaignModal() {
		if (_campaignModal is not null) {
			return;
		}

		SetInteractiveState(false);
		ShowStatus(string.Empty);
		_campaignModal = AddChild(new LegacyCampaignModal(
			_selectCampaignMenu,
			_selectMissionMenu,
			_userProfilesRepository,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			CloseCampaignModal));
	}

	private void CloseCampaignModal() {
		if (_campaignModal is not null && RemoveChild(_campaignModal)) {
			_campaignModal.Dispose();
		}

		_campaignModal = null;
		SetInteractiveState(true);
		_buttonCampaign?.SetKeyboardFocus(true);
	}

	private void SetInteractiveState(bool enabled) {
		_buttonCampaign?.EnableButton(enabled);
		_buttonSkirmish?.EnableButton(enabled);
		_buttonBack?.EnableButton(enabled);

		if (!enabled) {
			_buttonLoad?.EnableButton(false);
			_buttonQuickbattleLoad?.EnableButton(false);
			return;
		}

		ApplySaveAvailability();
	}

	private void CloseModal() {
		if (_campaignModal is not null) {
			CloseCampaignModal();
		}

		_closed();
	}

	private BUTTON_DATA GetButton(int index) => _singlePlayerMenu.Buttons[index];

	private LegacyButtonNode AddButtonNode(string label, BUTTON_DATA data) {
		var node = CreateButtonNode(label, data);
		ContentRoot.AddChild(node);
		return node;
	}

	private void AddStaticNode(string label, STATIC_DATA data) {
		var node = CreateStaticNode(label, data);
		ContentRoot.AddChild(node);
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

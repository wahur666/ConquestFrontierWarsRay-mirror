using System;
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

internal sealed class LegacyCampaignModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private readonly Action _closed;
	private readonly GT_MENU1_SELECT_CAMPAIGN _campaignMenu;
	private readonly GT_MENU1_SELECT_MISSION _missionMenu;
	private readonly LegacyRcStringResolver _strings;
	private readonly UserProfilesRepository _userProfilesRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly XmlDbRepository _xmlDbRepository;
	private LegacyButtonNode? _terranButton;
	private LegacyButtonNode? _mantisButton;
	private LegacyButtonNode? _solarianButton;
	private LegacyButtonNode? _backButton;
	private LegacyBriefingModal? _briefingModal;
	private LegacyMissionModal? _missionModal;
	private LegacyButtonNode? _briefingReturnFocusButton;
	private TextNode? _statusLabel;

	public LegacyCampaignModal(
		GT_MENU1_SELECT_CAMPAIGN campaignMenu,
		GT_MENU1_SELECT_MISSION missionMenu,
		UserProfilesRepository userProfilesRepository,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action closed) : base("LegacyCampaignModal", closed) {
		_campaignMenu = campaignMenu;
		_missionMenu = missionMenu;
		_userProfilesRepository = userProfilesRepository;
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
		_closed = closed;
	}

	protected override void OnInitialize() {
		base.OnInitialize();
		ContentRoot.Position = ResolveScreenPosition(_campaignMenu.ScreenRect);

		AddStaticNode("Background", _campaignMenu.Background);
		AddStaticNode("Title", _campaignMenu.Title);
		AddPlayerNameNode();

		_terranButton = AddButtonNode("TerranCampaign", GetButton(0));
		_mantisButton = AddButtonNode("MantisTraining", GetButton(1));
		_solarianButton = AddButtonNode("SolarianTraining", GetButton(2));
		_backButton = AddButtonNode("Back", GetButton(3));

		_terranButton.Activated += _ => OpenMissionModal();
		_mantisButton.Activated += _ => OpenTrainingBriefing("Mantis_Train.dmission", _mantisButton);
		_solarianButton.Activated += _ => OpenTrainingBriefing("Sol_Train.dmission", _solarianButton);
		_backButton.Activated += _ => CloseModal();
		_terranButton.SetKeyboardFocus(true);

		_statusLabel = AddChild(new TextNode("StatusLabel") {
			Position = new Vector2(144f, 274f),
			FontSize = 14f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(200, 180, 140, 255),
			Text = string.Empty
		});
	}

	private void AddPlayerNameNode() {
		var node = CreateStaticNode("PlayerName", _campaignMenu.StaticName);
		node.SetText(_userProfilesRepository.Load().CurrentUser);
		ContentRoot.AddChild(node);
	}

	private void ShowStatus(string message) {
		if (_statusLabel is not null) {
			_statusLabel.Text = message;
		}
	}

	private void OpenMissionModal() {
		if (_missionModal is not null || _briefingModal is not null) {
			return;
		}

		SetInteractiveState(false);
		ShowStatus(string.Empty);
		_missionModal = AddChild(new LegacyMissionModal(
			_missionMenu,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			CloseMissionModal));
	}

	private void CloseMissionModal() {
		if (_missionModal is not null && RemoveChild(_missionModal)) {
			_missionModal.Dispose();
		}

		_missionModal = null;
		SetInteractiveState(true);
		_terranButton?.SetKeyboardFocus(true);
	}

	private void OpenTrainingBriefing(string missionFileName, LegacyButtonNode? returnFocusButton) {
		if (_briefingModal is not null) {
			return;
		}

		_briefingReturnFocusButton = returnFocusButton;
		SetInteractiveState(false);
		ShowStatus(string.Empty);
		_briefingModal = AddChild(new LegacyBriefingModal(
			missionFileName,
			_xmlDbRepository,
			_vfxRepository,
			_strings,
			CloseTrainingBriefing));
	}

	private void CloseTrainingBriefing() {
		if (_briefingModal is not null && RemoveChild(_briefingModal)) {
			_briefingModal.Dispose();
		}

		_briefingModal = null;
		SetInteractiveState(true);
		(_briefingReturnFocusButton ?? _terranButton)?.SetKeyboardFocus(true);
		_briefingReturnFocusButton = null;
	}

	private void SetInteractiveState(bool enabled) {
		_terranButton?.EnableButton(enabled);
		_mantisButton?.EnableButton(enabled);
		_solarianButton?.EnableButton(enabled);
		_backButton?.EnableButton(enabled);
	}

	private void CloseModal() {
		if (_missionModal is not null) {
			CloseMissionModal();
		}

		if (_briefingModal is not null) {
			CloseTrainingBriefing();
		}

		_closed();
	}

	private BUTTON_DATA GetButton(int index) => _campaignMenu.Buttons[index];

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

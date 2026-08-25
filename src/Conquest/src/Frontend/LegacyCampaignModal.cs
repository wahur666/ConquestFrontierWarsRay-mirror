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
	private readonly LegacyRcStringResolver _strings;
	private readonly UserProfilesRepository _userProfilesRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly XmlDbRepository _xmlDbRepository;
	private TextNode? _statusLabel;

	public LegacyCampaignModal(
		GT_MENU1_SELECT_CAMPAIGN campaignMenu,
		UserProfilesRepository userProfilesRepository,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action closed) : base("LegacyCampaignModal", closed) {
		_campaignMenu = campaignMenu;
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

		var terran = AddButtonNode("TerranCampaign", GetButton(0));
		var mantis = AddButtonNode("MantisTraining", GetButton(1));
		var solarian = AddButtonNode("SolarianTraining", GetButton(2));
		var back = AddButtonNode("Back", GetButton(3));

		terran.Activated += _ => ShowStatus("Terran campaign mission flow is not ported yet.");
		mantis.Activated += _ => ShowStatus("Mantis training briefing flow is not ported yet.");
		solarian.Activated += _ => ShowStatus("Solarian training briefing flow is not ported yet.");
		back.Activated += _ => CloseModal();
		terran.SetKeyboardFocus(true);

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

	private void CloseModal() {
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

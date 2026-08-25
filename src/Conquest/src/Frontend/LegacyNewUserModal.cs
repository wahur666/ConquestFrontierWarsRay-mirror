using System;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.UserProfiles;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class LegacyNewUserModal : LegacyModalNode {
	private readonly Action _cancelled;
	private readonly Action<UserProfilesData> _created;
	private readonly GT_NEWPLAYER _newPlayer;
	private readonly UserProfilesRepository _userProfilesRepository;
	private readonly LegacyRcStringResolver _strings;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private LegacyEditNode? _nameEdit;
	private TextNode? _statusLabel;

	public LegacyNewUserModal(
		GT_NEWPLAYER newPlayer,
		UserProfilesRepository userProfilesRepository,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action<UserProfilesData> created,
		Action cancelled) : base("LegacyNewUserModal", cancelled) {
		_newPlayer = newPlayer;
		_userProfilesRepository = userProfilesRepository;
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
		_created = created;
		_cancelled = cancelled;
	}

	protected override void OnInitialize() {
		base.OnInitialize();
		ContentRoot.Position = new Vector2(_newPlayer.ScreenRect.Left, _newPlayer.ScreenRect.Top);

		AddStaticNode("Background", _newPlayer.Background);
		AddStaticNode("Title", _newPlayer.Title);
		AddStaticNode("StaticHeading", _newPlayer.StaticHeading);

		_nameEdit = AddEditNode("NameEdit", _newPlayer.Edit);
		_nameEdit.SetMaxChars(24);
		_nameEdit.Activated += _ => TrySubmit();
		_nameEdit.RequestKeyboardFocus();

		var ok = AddButtonNode("Ok", _newPlayer.Ok);
		var cancel = AddButtonNode("Cancel", _newPlayer.Cancel);
		ok.Activated += _ => TrySubmit();
		cancel.Activated += _ => _cancelled();

		_statusLabel = AddChild(new TextNode("StatusLabel") {
			Position = new Vector2(100f, 164f),
			FontSize = 15f,
			TextStyle = UiTextStyle.Body,
			Tint = new Raylib_cs.Color(230, 120, 120, 255),
			Text = string.Empty
		});
	}

	private void TrySubmit() {
		if (_nameEdit is null || _statusLabel is null) {
			return;
		}

		if (_userProfilesRepository.TryCreateUser(_nameEdit.Text, out var savedData, out var errorMessage)) {
			_statusLabel.Text = string.Empty;
			_created(savedData);
			return;
		}

		_statusLabel.Text = errorMessage;
		_nameEdit.RequestKeyboardFocus();
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

	private LegacyEditNode AddEditNode(string label, EDIT_DATA data) {
		var archetype = ReadTypedEntry<GT_EDIT>("GT_EDIT", data.EditType);
		var node = new LegacyEditNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		ContentRoot.AddChild(node);
		return node;
	}

	private T ReadTypedEntry<T>(string typeName, string fileName) where T : class {
		var details = _xmlDbRepository.ReadEntryDetails("GenData.db", typeName, fileName);
		return details.TypedValue as T
		       ?? throw new InvalidOperationException(
			       $"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
	}
}

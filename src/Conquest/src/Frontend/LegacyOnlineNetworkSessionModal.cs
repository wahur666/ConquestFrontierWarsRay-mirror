using System;
using System.Numerics;
using System.Threading.Tasks;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.UserProfiles;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class LegacyOnlineNetworkSessionModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private readonly Action _closed;
	private readonly Action<MultiplayerNetworkKind, bool> _proceed;
	private readonly GT_MENU1_IP_ADDRESS _menuData;
	private readonly MultiplayerNetworkKind _networkKind;
	private readonly UserProfilesRepository _userProfilesRepository;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly LegacyRcStringResolver _strings;
	private LegacyCheckboxNode? _joinCheckbox;
	private LegacyCheckboxNode? _createCheckbox;
	private LegacyButtonNode? _nextButton;
	private LegacyComboboxNode? _ipCombobox;
	private LegacyStaticNode? _enterIpLabel;
	private TextNode? _descriptionLabel;
	private Task<NetworkAddressInfo>? _networkInfoTask;
	private string _defaultIpAddress = "0.0.0.0";
	private bool _joinSelected;

	public LegacyOnlineNetworkSessionModal(
		GT_MENU1_IP_ADDRESS menuData,
		MultiplayerNetworkKind networkKind,
		UserProfilesRepository userProfilesRepository,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action<MultiplayerNetworkKind, bool> proceed,
		Action closed) : base("LegacyOnlineNetworkSessionModal", closed) {
		_menuData = menuData;
		_networkKind = networkKind;
		_userProfilesRepository = userProfilesRepository;
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
		_enterIpLabel = AddStaticNode("EnterIp", _menuData.EnterIp);
		AddStaticNode("EnterName", _menuData.EnterName);
		var playerNameNode = AddStaticNode("PlayerName", _menuData.StaticName);
		playerNameNode.SetText(_userProfilesRepository.Load().CurrentUser);

		_joinCheckbox = AddCheckboxNode("Join", _menuData.CheckJoin);
		_createCheckbox = AddCheckboxNode("Create", _menuData.CheckCreate);
		AddStaticNode("StaticJoin", _menuData.StaticJoin);
		AddStaticNode("StaticCreate", _menuData.StaticCreate);

		_ipCombobox = AddComboboxNode("IpCombobox", _menuData.ComboboxIp);
		_ipCombobox.SetMaxChars(40);
		_defaultIpAddress = ResolveString(_menuData.ComboboxIp.EditData.EditText, "0.0.0.0");
		_ipCombobox.SetText(_defaultIpAddress);
		_ipCombobox.AddString(_defaultIpAddress);

		_nextButton = AddButtonNode("Next", _menuData.Next);
		var backButton = AddButtonNode("Back", _menuData.Back);

		_descriptionLabel = AddChild(new TextNode("DescriptionLabel") {
			Position = new Vector2(86f, 454f),
			FontSize = 15f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(195, 180, 145, 255),
			Text = string.Empty
		});

		_joinCheckbox.Activated += _ => SelectJoin();
		_createCheckbox.Activated += _ => SelectCreate();
		_nextButton.Activated += _ => Proceed();
		backButton.Activated += _ => _closed();
		_ipCombobox.SelectionCommitted += _ => UpdateDescription();
		_ipCombobox.TextChanged += _ => UpdateDescription();

		SelectCreate();
		_nextButton.SetKeyboardFocus(true);

		if (_networkKind == MultiplayerNetworkKind.Internet) {
			_networkInfoTask = NetworkService.GetNetworkAddressesAsync();
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);
		if (_networkInfoTask is { IsCompletedSuccessfully: true }) {
			SeedNetworkAddresses(_networkInfoTask.Result);
			_networkInfoTask = null;
		}
	}

	private void SelectJoin() {
		_joinSelected = true;
		if (_joinCheckbox is not null) {
			_joinCheckbox.IsChecked = true;
		}

		if (_createCheckbox is not null) {
			_createCheckbox.IsChecked = false;
		}

		ApplyVisibility();
		UpdateDescription();
	}

	private void SelectCreate() {
		_joinSelected = false;
		if (_joinCheckbox is not null) {
			_joinCheckbox.IsChecked = false;
		}

		if (_createCheckbox is not null) {
			_createCheckbox.IsChecked = true;
		}

		ApplyVisibility();
		UpdateDescription();
	}

	private void ApplyVisibility() {
		var showIpEntry = _joinSelected && _networkKind == MultiplayerNetworkKind.Internet;
		_enterIpLabel?.SetVisible(showIpEntry);
		_ipCombobox?.SetVisible(showIpEntry);
		_ipCombobox?.EnableCombobox(showIpEntry);
		_nextButton?.EnableButton(_createCheckbox?.IsChecked == true || _joinCheckbox?.IsChecked == true);
	}

	private void UpdateDescription() {
		if (_descriptionLabel is null) {
			return;
		}

		_descriptionLabel.Text = _joinSelected
			? _networkKind == MultiplayerNetworkKind.Internet
				? $"Join a multiplayer game at {_ipCombobox?.Text ?? _defaultIpAddress}."
				: "Join a multiplayer game on the local area network."
			: _networkKind == MultiplayerNetworkKind.Internet
				? "Create an internet multiplayer session."
				: "Create a local area network multiplayer session.";
	}

	private void SeedNetworkAddresses(NetworkAddressInfo info) {
		if (_ipCombobox is null) {
			return;
		}

		if (!string.IsNullOrWhiteSpace(info.PublicIp)) {
			if (_ipCombobox.FindStringExact(info.PublicIp) < 0) {
				_ipCombobox.AddString(info.PublicIp);
			}
		}

		foreach (var address in info.LocalIpv4Addresses) {
			if (_ipCombobox.FindStringExact(address) < 0) {
				_ipCombobox.AddString(address);
			}
		}
	}

	private void Proceed() {
		var isHost = !_joinSelected;
		if (_networkKind == MultiplayerNetworkKind.Internet && _joinSelected && _ipCombobox is not null) {
			var value = _ipCombobox.Text.Trim();
			if (string.IsNullOrWhiteSpace(value)) {
				_descriptionLabel!.Text = "Enter a host IP address.";
				_ipCombobox.RequestKeyboardFocus();
				return;
			}
		}

		_proceed(_networkKind, isHost);
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

	private LegacyComboboxNode AddComboboxNode(string label, COMBOBOX_DATA data) {
		var comboboxArchetype = ReadTypedEntry<GT_COMBOBOX>("GT_COMBOBOX", data.ComboboxType);
		var editArchetype = ReadTypedEntry<GT_EDIT>("GT_EDIT", data.EditData.EditType);
		var buttonArchetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", data.ButtonData.ButtonType);
		var listboxArchetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", data.ListboxData.ListboxType);
		var node = new LegacyComboboxNode(label);
		node.ApplyLegacyDefinition(
			comboboxArchetype,
			editArchetype,
			buttonArchetype,
			listboxArchetype,
			data,
			_vfxRepository,
			_xmlDbRepository);
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

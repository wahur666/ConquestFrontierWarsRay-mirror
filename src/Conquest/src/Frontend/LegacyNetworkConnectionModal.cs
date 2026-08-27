using System;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal enum NetworkConnectionKind {
	Online,
	Local
}

internal sealed class LegacyNetworkConnectionModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private readonly GT_MENU1_NET_CONNECTIONS _menuData;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly LegacyRcStringResolver _strings;
	private readonly Action<NetworkConnectionKind> _selectionCommitted;
	private readonly Action _closed;
	private LegacyListBoxNode? _list;
	private LegacyButtonNode? _nextButton;
	private TextNode? _descriptionLabel;

	public LegacyNetworkConnectionModal(
		GT_MENU1_NET_CONNECTIONS menuData,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action<NetworkConnectionKind> selectionCommitted,
		Action closed) : base("LegacyNetworkConnectionModal", closed) {
		_menuData = menuData;
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
		_selectionCommitted = selectionCommitted;
		_closed = closed;
	}

	protected override void OnInitialize() {
		base.OnInitialize();
		ContentRoot.Position = ResolveScreenPosition(_menuData.ScreenRect);

		AddStaticNode("Background", _menuData.Background);
		AddStaticNode("DescriptionFrame", _menuData.Description);
		AddStaticNode("Title", _menuData.StaticTitle);

		_list = AddListBoxNode("Connections", _menuData.List);
		_list.CommitOnDoubleClick = true;
		_list.CaretMoved += _ => UpdateSelectionState();
		_list.SelectionCommitted += _ => CommitSelection();

		var onlineIndex = _list.AddString("Online Network Play");
		_list.SetDataValue(onlineIndex, (uint)NetworkConnectionKind.Online);
		var localIndex = _list.AddString("Local Network Play");
		_list.SetDataValue(localIndex, (uint)NetworkConnectionKind.Local);
		_list.SetCurrentSelection(localIndex);

		_nextButton = AddButtonNode("Next", _menuData.Next);
		_nextButton.Activated += _ => CommitSelection();
		var backButton = AddButtonNode("Back", _menuData.Back);
		backButton.Activated += _ => _closed();

		_descriptionLabel = AddChild(new TextNode("DescriptionLabel") {
			Position = new Vector2(84f, 454f),
			FontSize = 15f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(195, 180, 145, 255),
			Text = string.Empty
		});

		UpdateSelectionState();
		_list.SetKeyboardFocus(true);
	}

	private void UpdateSelectionState() {
		if (_list is null || _nextButton is null || _descriptionLabel is null) {
			return;
		}

		var selectedIndex = _list.GetCurrentSelection();
		_nextButton.EnableButton(selectedIndex >= 0);
		_descriptionLabel.Text = selectedIndex < 0
			? "Select a network connection type."
			: ResolveDescription((NetworkConnectionKind)_list.GetDataValue(selectedIndex));
	}

	private void CommitSelection() {
		if (_list is null || _list.GetCurrentSelection() < 0) {
			return;
		}

		var kind = (NetworkConnectionKind)_list.GetDataValue(_list.GetCurrentSelection());
		_selectionCommitted(kind);
	}

	private static string ResolveDescription(NetworkConnectionKind kind) {
		return kind switch {
			NetworkConnectionKind.Online => "Online play opens the join/create screen with host IP entry.",
			NetworkConnectionKind.Local => "Local network play opens the detected session list for LAN games.",
			_ => "Select a network connection type."
		};
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

	private static Vector2 ResolveScreenPosition(RECT screenRect) {
		var width = Math.Max(0f, screenRect.Right - screenRect.Left);
		var height = Math.Max(0f, screenRect.Bottom - screenRect.Top);
		return new Vector2(
			(LegacyScreenWidth - width) * 0.5f,
			(LegacyScreenHeight - height) * 0.5f);
	}
}

using System;
using System.Numerics;
using System.Reflection;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class Menu1HelpModalOverlay : LegacyModalNode {
	private readonly Func<Node> _creditsSceneFactory;
	private readonly Menu1HelpMenuData _helpMenu;
	private readonly LegacyRcStringResolver _strings;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;

	public Menu1HelpModalOverlay(
		Menu1HelpMenuData helpMenu,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Func<Node> creditsSceneFactory,
		Action closeRequested) : base("Menu1HelpModalOverlay", closeRequested) {
		_creditsSceneFactory = creditsSceneFactory;
		_helpMenu = helpMenu;
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
	}

	protected override void OnInitialize() {
		base.OnInitialize();

		ContentRoot.Position = new Vector2(_helpMenu.ScreenRect.Left, _helpMenu.ScreenRect.Top);

		AddStaticNode("Background", _helpMenu.Background);
		AddStaticNode("Title", _helpMenu.Title);
		AddStaticNode("StaticConquest", _helpMenu.StaticConquest);
		AddStaticNode("StaticVersion", _helpMenu.StaticVersion);

		var staticNumber = AddStaticNode("StaticNumber", _helpMenu.StaticNumber);
		staticNumber.SetText(ResolveVersionText());

		var buttonOk = AddButtonNode("ButtonOk", _helpMenu.ButtonOk);
		var buttonCredits = AddButtonNode("ButtonCredits", _helpMenu.ButtonCredits);
		buttonOk.Activated += _ => RequestClose();
		buttonOk.SetKeyboardFocus(true);
		buttonCredits.Activated += _ => Tree.ChangeRoot(_creditsSceneFactory());

		var staticProductId = AddStaticNode("StaticProductId", _helpMenu.StaticProductId);
		var staticProductNumber = AddStaticNode("StaticProductNumber", _helpMenu.StaticProductNumber);
		staticProductId.SetVisible(false);
		staticProductNumber.SetVisible(false);

		AddStaticNode("StaticLegal", _helpMenu.StaticLegal);
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
}

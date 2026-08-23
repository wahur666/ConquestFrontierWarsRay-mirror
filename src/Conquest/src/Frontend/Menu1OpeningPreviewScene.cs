using System;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay;

internal sealed class Menu1OpeningPreviewScene : Node2D {
	private readonly LegacyMenuRoot _legacyMenuRoot = new("LegacyMenuRoot");

	public Menu1OpeningPreviewScene() : base("Menu1OpeningPreviewScene") {
		AddChild(_legacyMenuRoot);

		try {
			var opening = new Menu1OpeningDataReader().ReadOpening();
			_legacyMenuRoot.SetContentRoot(new Menu1OpeningPreviewSurface(opening));
		} catch (Exception ex) {
			Console.Error.WriteLine(ex);
			_legacyMenuRoot.SetContentRoot(new Menu1OpeningErrorSurface(ex));
		}
	}

	protected override void Draw() {
		Raylib.ClearBackground(Color.Black);
	}
}

internal sealed class Menu1OpeningPreviewSurface : Node2D {
	private static readonly Color ButtonMarker = new(102, 208, 142, 255);
	private static readonly Color AnimationMarker = new(214, 120, 228, 255);
	private readonly UtfDbRepository _utfDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly LegacyRcStringResolver _strings;

	public Menu1OpeningPreviewSurface(Menu1OpeningData opening) : base("Menu1OpeningPreviewSurface") {
		ArgumentNullException.ThrowIfNull(opening);
		_utfDbRepository = new UtfDbRepository(RepoPaths.LocateUtfDbPaths());
		_vfxRepository = VfxAnimationDataRepository.LocateFromRepo();
		_strings = LegacyRcStringResolver.LoadFromRepo();

		AddStaticNode("Background", opening.Background);
		AddButtonMarker("Single", opening.Single);
		AddButtonMarker("Multi", opening.Multi);
		AddButtonMarker("Intro", opening.Intro);
		AddButtonMarker("Options", opening.Options);
		AddButtonMarker("Help", opening.Help);
		AddButtonMarker("Quit", opening.Quit);
		AddStaticNode("StaticSingle", opening.StaticSingle);
		AddStaticNode("StaticMulti", opening.StaticMulti);
		AddStaticNode("StaticIntro", opening.StaticIntro);
		AddStaticNode("StaticOptions", opening.StaticOptions);
		AddStaticNode("StaticHelp", opening.StaticHelp);
		AddAnimationMarker("AnimMedia", opening.AnimMedia);
		AddAnimationMarker("AnimSingle", opening.AnimSingle);
		AddAnimationMarker("AnimMulti", opening.AnimMulti);
		AddAnimationMarker("AnimOptions", opening.AnimOptions);
		AddAnimationMarker("AnimQuestion", opening.AnimQuestion);
		AddStaticNode("StaticLegal", opening.StaticLegal);
	}

	private void AddButtonMarker(string label, BUTTON_DATA button) {
		var width = Math.Max(36f, button.ButtonArea.Right - button.ButtonArea.Left);
		var height = Math.Max(18f, button.ButtonArea.Bottom - button.ButtonArea.Top);
		AddMarker(label, new Vector2(button.XOrigin, button.YOrigin), new Vector2(width, height), ButtonMarker);
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

	private void AddAnimationMarker(string label, ANIMATE_DATA animation) {
		AddMarker(label, new Vector2(animation.XOrigin, animation.YOrigin), new Vector2(34f, 34f), AnimationMarker);
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

	private T ReadTypedEntry<T>(string typeName, string fileName) where T : class {
		var details = _utfDbRepository.ReadEntryDetails("GenData.db", typeName, fileName);
		return details.TypedValue as T
			?? throw new InvalidOperationException($"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
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

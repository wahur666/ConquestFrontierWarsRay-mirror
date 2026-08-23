using System;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
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
			_legacyMenuRoot.SetContentRoot(new Menu1OpeningErrorSurface(ex));
		}
	}

	protected override void Draw() {
		Raylib.ClearBackground(Color.Black);
	}
}

internal sealed class Menu1OpeningPreviewSurface : Node2D {
	private static readonly Color ScreenRectOutline = new(90, 153, 220, 255);
	private static readonly Color BackgroundMarker = new(88, 124, 160, 255);
	private static readonly Color ButtonMarker = new(102, 208, 142, 255);
	private static readonly Color StaticMarker = new(230, 190, 92, 255);
	private static readonly Color AnimationMarker = new(214, 120, 228, 255);
	private static readonly Color LegalMarker = new(224, 114, 114, 255);

	public Menu1OpeningPreviewSurface(Menu1OpeningData opening) : base("Menu1OpeningPreviewSurface") {
		ArgumentNullException.ThrowIfNull(opening);

		AddChild(new PanelNode("LegacySurfaceBackground") {
			Position = Vector2.Zero,
			Size = new Vector2(800f, 600f),
			Fill = new Color(16, 21, 28, 255),
			Outline = new Color(54, 67, 84, 255),
			OutlineThickness = 1f
		});

		AddChild(new PanelNode("ScreenRectOutline") {
			Position = new Vector2(opening.ScreenRect.Left, opening.ScreenRect.Top),
			Size = ToSize(opening.ScreenRect, inclusive: true),
			Fill = Color.Blank,
			Outline = ScreenRectOutline,
			OutlineThickness = 2f
		});

		AddStaticMarker("BG", opening.Background.XOrigin, opening.Background.YOrigin, 800f, 600f, BackgroundMarker);
		AddButtonMarker("Single", opening.Single);
		AddButtonMarker("Multi", opening.Multi);
		AddButtonMarker("Intro", opening.Intro);
		AddButtonMarker("Options", opening.Options);
		AddButtonMarker("Help", opening.Help);
		AddButtonMarker("Quit", opening.Quit);
		AddStaticMarker("StaticSingle", opening.StaticSingle, StaticMarker);
		AddStaticMarker("StaticMulti", opening.StaticMulti, StaticMarker);
		AddStaticMarker("StaticIntro", opening.StaticIntro, StaticMarker);
		AddStaticMarker("StaticOptions", opening.StaticOptions, StaticMarker);
		AddStaticMarker("StaticHelp", opening.StaticHelp, StaticMarker);
		AddAnimationMarker("AnimMedia", opening.AnimMedia);
		AddAnimationMarker("AnimSingle", opening.AnimSingle);
		AddAnimationMarker("AnimMulti", opening.AnimMulti);
		AddAnimationMarker("AnimOptions", opening.AnimOptions);
		AddAnimationMarker("AnimQuestion", opening.AnimQuestion);
		AddStaticMarker("StaticLegal", opening.StaticLegal, LegalMarker);
	}

	private void AddButtonMarker(string label, BUTTON_DATA button) {
		var width = Math.Max(36f, button.ButtonArea.Right - button.ButtonArea.Left);
		var height = Math.Max(18f, button.ButtonArea.Bottom - button.ButtonArea.Top);
		AddMarker(label, new Vector2(button.XOrigin, button.YOrigin), new Vector2(width, height), ButtonMarker);
	}

	private void AddStaticMarker(string label, STATIC_DATA data, Color color) {
		var width = Math.Max(40f, data.Width);
		var height = Math.Max(18f, data.Height);
		AddStaticMarker(label, data.XOrigin, data.YOrigin, width, height, color);
	}

	private void AddStaticMarker(string label, int x, int y, float width, float height, Color color) {
		AddMarker(label, new Vector2(x, y), new Vector2(width, height), color);
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

	private static Vector2 ToSize(RECT rect, bool inclusive) {
		var width = rect.Right - rect.Left + (inclusive ? 1 : 0);
		var height = rect.Bottom - rect.Top + (inclusive ? 1 : 0);
		return new Vector2(Math.Max(0f, width), Math.Max(0f, height));
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
			TextStyle = UiTextStyle.Title,
			Tint = new Color(255, 220, 220, 255),
			Text = "Menu1 opening preview failed"
		});
		AddChild(new TextNode("ErrorMessage") {
			Position = new Vector2(24f, 64f),
			FontSize = 16f,
			Tint = new Color(255, 178, 178, 255),
			Text = exception.Message
		});
	}
}

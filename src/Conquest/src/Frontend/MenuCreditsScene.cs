using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class MenuCreditsScene : Node2D {
	private readonly Func<Node> _returnSceneFactory;
	private readonly LegacyMenuRoot _legacyMenuRoot = new("LegacyMenuRoot");

	public MenuCreditsScene(Func<Node> returnSceneFactory) : base("MenuCreditsScene") {
		_returnSceneFactory = returnSceneFactory;
		AddChild(_legacyMenuRoot);
	}

	protected override void OnInitialize() {
		try {
			_legacyMenuRoot.SetContentRoot(new MenuCreditsSurface(_returnSceneFactory));
		} catch (Exception ex) {
			AppLog.Error("MenuCreditsScene", "Failed to initialize credits scene.", ex);
			_legacyMenuRoot.SetContentRoot(new Menu1OpeningErrorSurface(ex));
		}
	}

	protected override void Draw() {
		Raylib.ClearBackground(Color.Black);
	}
}

internal sealed class MenuCreditsSurface : Node2D {
	private readonly Func<Node> _returnSceneFactory;
	private readonly AudioPlayer _musicPlayer = new("CreditsAudioPlayer") {
		Looping = true
	};
	private readonly UtfDbRepository _utfDbRepository;
	private CompressedTexture2D? _backgroundTexture;

	public MenuCreditsSurface(Func<Node> returnSceneFactory) : base("MenuCreditsSurface") {
		_returnSceneFactory = returnSceneFactory;
		_utfDbRepository = new UtfDbRepository(RepoPaths.LocateUtfDbPaths());
		AddChild(_musicPlayer);
	}

	protected override void OnInitialize() {
		var credits = ReadTypedEntry<GT_CREDITS>("GT_CREDITS", "Menu_Credits");
		AddBackground(credits.StaticBackground);
		StartMusic();
		AddChild(new CreditsScrollerNode(
			LoadCreditsEntries(),
			() => Tree.ChangeRoot(_returnSceneFactory())));
	}

	protected override void OnExitTree() {
		_musicPlayer.Stop();
		base.OnExitTree();
	}

	private void AddBackground(STATIC_DATA data) {
		var staticRepo = VfxAnimationDataRepository.LocateFromRepo();
		var archetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", data.StaticType);
		if (string.IsNullOrWhiteSpace(archetype.ShapeFile)) {
			throw new InvalidOperationException($"Credits background '{data.StaticType}' does not define a shape file.");
		}

		var backgroundPath = staticRepo.GetInterfaceAssetPath(new ImageData {
			Filename = archetype.ShapeFile
		});

		_backgroundTexture = new CompressedTexture2D(backgroundPath);
		var node = new Sprite(_backgroundTexture, "CreditsBackground") {
			Position = Vector2.Zero,
			Pivot = Vector2.Zero
		};
		AddChild(node);
	}

	private void StartMusic() {
		var music = Shared.ResourceManager.Audio.OpenMusic(
			"Conquest Frontier Wars soundtrack - Mystery Music.mp3",
			AudioPlaybackBackend.NAudio);
		_musicPlayer.SetOwnedAudio(music);
		_musicPlayer.Play();
	}

	private static IReadOnlyList<CreditsEntry> LoadCreditsEntries() {
		var creditsPath = Path.Combine(RepoPaths.LocateAssetsRoot(), "Credits.txt");
		if (!File.Exists(creditsPath)) {
			throw new FileNotFoundException($"Could not locate credits text at '{creditsPath}'.", creditsPath);
		}

		var entries = new List<CreditsEntry>();
		foreach (var rawLine in File.ReadAllLines(creditsPath)) {
			var line = rawLine.TrimEnd();
			if (string.IsNullOrWhiteSpace(line)) {
				entries.Add(CreditsEntry.Spacing(25f));
				continue;
			}

			if (line.StartsWith("[d]", StringComparison.OrdinalIgnoreCase)) {
				if (int.TryParse(line[3..].Trim(), out var spacing)) {
					entries.Add(CreditsEntry.Spacing(spacing));
				}

				continue;
			}

			if (line.StartsWith("[t]", StringComparison.OrdinalIgnoreCase)) {
				var text = line[3..].Trim();
				if (text.Length > 0) {
					entries.Add(CreditsEntry.Spacing(15f));
					entries.Add(CreditsEntry.Title(text));
				}

				continue;
			}

			entries.Add(CreditsEntry.Name(line));
		}

		return entries;
	}

	private T ReadTypedEntry<T>(string typeName, string fileName) where T : class {
		var details = _utfDbRepository.ReadEntryDetails("GenData.db", typeName, fileName);
		return details.TypedValue as T
		       ?? throw new InvalidOperationException(
			       $"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
	}

	protected override void OnDispose() {
		_backgroundTexture?.Dispose();
		_backgroundTexture = null;
		base.OnDispose();
	}
}

internal sealed class CreditsScrollerNode : Control {
	private const float TextWidth = 800f;
	private const float PaneTop = 104f;
	private const float PaneBottom = 599f;
	private const float StartOffsetY = 540f;
	private const float ScrollSpeedPixelsPerSecond = 60.60606f;
	private static readonly Color TitleShadowColor = new(0, 0, 0, 255);
	private static readonly Color NameShadowColor = new(0, 0, 0, 255);
	private static readonly Color TitleColor = new(49, 90, 198, 255);
	private static readonly Color NameColor = new(160, 200, 240, 255);
	private readonly Action _returnToMenu;
	private readonly List<CreditsRenderLine> _lines;
	private float _scrollOffset;

	public CreditsScrollerNode(IReadOnlyList<CreditsEntry> entries, Action returnToMenu) : base("CreditsScrollerNode") {
		_returnToMenu = returnToMenu;
		Position = new Vector2(0f, PaneTop);
		Size = new Vector2(TextWidth, PaneBottom - PaneTop);
		_lines = BuildLines(entries);
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);
		_scrollOffset += ScrollSpeedPixelsPerSecond * deltaTime;

		if (Input.UiEsc || Raylib.IsMouseButtonReleased(MouseButton.Left)) {
			_returnToMenu();
		}
	}

	protected override void Draw() {
		var clip = GlobalBounds;
		Raylib.BeginScissorMode(
			(int)MathF.Floor(clip.X),
			(int)MathF.Floor(clip.Y),
			(int)MathF.Ceiling(clip.Width),
			(int)MathF.Ceiling(clip.Height));

		try {
			var scale = GlobalScale.X;
			foreach (var line in _lines) {
				var y = GlobalPosition.Y + ((line.Y - _scrollOffset) * scale);
				if (y < clip.Y - 40f || y > clip.Y + clip.Height + 40f) {
					continue;
				}

				var fontSize = line.FontSize * scale;
				var textWidth = UiText.MeasureWidth(line.Text, fontSize, line.Style);
				var x = GlobalPosition.X + ((TextWidth * scale) - textWidth) * 0.5f;
				UiText.Draw(line.Text, x + 2f, y + 2f, fontSize, line.ShadowColor, line.Style);
				UiText.Draw(line.Text, x, y, fontSize, line.Color, line.Style);
			}
		} finally {
			Raylib.EndScissorMode();
		}
	}

	private static List<CreditsRenderLine> BuildLines(IReadOnlyList<CreditsEntry> entries) {
		var lines = new List<CreditsRenderLine>();
		var y = StartOffsetY;
		foreach (var entry in entries) {
			y += entry.AdvanceY;
			if (entry.Kind == CreditsEntryKind.Spacing) {
				continue;
			}

			lines.Add(new CreditsRenderLine(
				entry.Text,
				y,
				entry.Kind == CreditsEntryKind.Title ? 28f : 24f,
				entry.Kind == CreditsEntryKind.Title ? UiTextStyle.Title : UiTextStyle.Body,
				entry.Kind == CreditsEntryKind.Title ? TitleColor : NameColor,
				entry.Kind == CreditsEntryKind.Title ? TitleShadowColor : NameShadowColor));
		}

		return lines;
	}
}

internal enum CreditsEntryKind {
	Spacing,
	Title,
	Name
}

internal readonly record struct CreditsEntry(CreditsEntryKind Kind, string Text, float AdvanceY) {
	public static CreditsEntry Spacing(float amount) => new(CreditsEntryKind.Spacing, string.Empty, amount);
	public static CreditsEntry Title(string text) => new(CreditsEntryKind.Title, text, 25f);
	public static CreditsEntry Name(string text) => new(CreditsEntryKind.Name, text, 25f);
}

internal readonly record struct CreditsRenderLine(
	string Text,
	float Y,
	float FontSize,
	UiTextStyle Style,
	Color Color,
	Color ShadowColor);

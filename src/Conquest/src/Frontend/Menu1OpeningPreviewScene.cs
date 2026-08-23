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

namespace ConquestFrontierWarsRay;

internal sealed class Menu1OpeningPreviewScene : Node2D {
	private readonly UiEventSource _eventSource = new("Menu1OpeningEventSource");
	private readonly LegacyMenuRoot _legacyMenuRoot = new("LegacyMenuRoot");

	public Menu1OpeningPreviewScene() : base("Menu1OpeningPreviewScene") {
		_eventSource.ScopeRoot = this;
		AddChild(_eventSource);
		AddChild(_legacyMenuRoot);
	}

	protected override void OnInitialize() {
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
	private static readonly Color AnimationMarker = new(214, 120, 228, 255);
	private readonly List<AtlasFramesResource> _atlasResources = [];
	private readonly Menu1OpeningData _opening;
	private readonly UtfDbRepository _utfDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly LegacyRcStringResolver _strings;
	private AudioPlayer? _musicPlayer;

	public Menu1OpeningPreviewSurface(Menu1OpeningData opening) : base("Menu1OpeningPreviewSurface") {
		ArgumentNullException.ThrowIfNull(opening);
		_opening = opening;
		_utfDbRepository = new UtfDbRepository(RepoPaths.LocateUtfDbPaths());
		_vfxRepository = VfxAnimationDataRepository.LocateFromRepo();
		_strings = LegacyRcStringResolver.LoadFromRepo();
	}

	protected override void OnInitialize() {
		AddStaticNode("Background", _opening.Background);
		var btnSingle = AddButtonNode("Single", _opening.Single);
		var btnMulti = AddButtonNode("Multi", _opening.Multi);
		var btnIntro = AddButtonNode("Intro", _opening.Intro);
		var btnOptions = AddButtonNode("Options", _opening.Options);
		var btnHelp = AddButtonNode("Help", _opening.Help);
		var btnQuit = AddButtonNode("Quit", _opening.Quit);
		AddStaticNode("StaticSingle", _opening.StaticSingle);
		AddStaticNode("StaticMulti", _opening.StaticMulti);
		AddStaticNode("StaticIntro", _opening.StaticIntro);
		AddStaticNode("StaticOptions", _opening.StaticOptions);
		AddStaticNode("StaticHelp", _opening.StaticHelp);
		var animMedia = AddAnimationNode("AnimMedia", _opening.AnimMedia);
		var animSingle = AddAnimationNode("AnimSingle", _opening.AnimSingle);
		var animMulti = AddAnimationNode("AnimMulti", _opening.AnimMulti);
		var animOptions = AddAnimationNode("AnimOptions", _opening.AnimOptions);
		var animQuestion = AddAnimationNode("AnimQuestion", _opening.AnimQuestion);
		ConnectButtonHover(btnSingle, animSingle);
		ConnectButtonHover(btnMulti, animMulti);
		ConnectButtonHover(btnIntro, animMedia);
		ConnectButtonHover(btnOptions, animOptions);
		ConnectButtonHover(btnHelp, animQuestion);
		btnIntro.Activated += _ => OpenMovie("Assets\\Movies\\cq_intro.mp4");
		btnQuit.Activated += _ => RequestQuit();
		AddStaticNode("StaticLegal", _opening.StaticLegal);
		_musicPlayer = AddMusicPlayer();
	}

	protected override void OnExitTree() {
		_musicPlayer?.Stop();
		base.OnExitTree();
	}

	private void OpenMovie(string moviePath) {
		_musicPlayer?.Stop();
		Tree.ChangeRoot(new MovieScene(moviePath, () => new Menu1OpeningPreviewScene()));
	}

	private AudioPlayer AddMusicPlayer() {
		var audioPlayer = AddChild(new AudioPlayer("AudioPlayer") {
			Looping = true
		});
		var music = new NAudioStreamResource(Path.GetFullPath(Path.Join(RepoPaths.LocateAssetsRoot(), "conquest_frontier_wars_ost", "Conquest Frontier Wars soundtrack - Main Menu Screen Music.mp3")));
		audioPlayer.SetAudio(music, true, true);
		audioPlayer.Play();
		return audioPlayer;
	}

	private static void ConnectButtonHover(LegacyButtonNode button, AnimatedSprite2D? animation) {
		if (animation is null) {
			return;
		}

		button.Entered += _ => {
			animation.Visible = true;
			animation.Play();
		};
		button.Exited += _ => {
			animation.Visible = false;
			animation.Pause();
		};
	}

	private LegacyButtonNode AddButtonNode(string label, BUTTON_DATA data) {
		var archetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", data.ButtonType);
		var node = new LegacyButtonNode(label);

		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.ButtonText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.Text = text;
		}

		AddChild(node);
		return node;
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

	private AnimatedSprite2D? AddAnimationNode(string label, ANIMATE_DATA data) {
		var archetype = ReadTypedEntry<GT_ANIMATE>("GT_ANIMATE", data.AnimateType);
		var vfxData = _vfxRepository.Load();
		if (!vfxData.TryGetAtlasByVfxShapeId(archetype.VfxType, out var atlasEntry)) {
			AddMarker(label, new Vector2(data.XOrigin, data.YOrigin), new Vector2(34f, 34f), AnimationMarker);
			return null;
		}

		var atlas = new AtlasFramesResource(
			_vfxRepository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: false),
			_vfxRepository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: true));
		_atlasResources.Add(atlas);

		var node = new AnimatedSprite2D(atlas.Frames, label) {
			Position = new Vector2(data.XOrigin, data.YOrigin),
			Pivot = Vector2.Zero,
			Loop = true,
			SpeedFps = 15,
			Visible = false
		};
		AddChild(node);
		return node;
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
		       ?? throw new InvalidOperationException(
			       $"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
	}

	protected override void OnDispose() {
		for (var index = _atlasResources.Count - 1; index >= 0; index--) {
			_atlasResources[index].Dispose();
		}

		_atlasResources.Clear();
		base.OnDispose();
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

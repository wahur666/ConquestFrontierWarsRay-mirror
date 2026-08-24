using System;
using System.Collections.Generic;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class Menu1OpeningPreviewScene : Node2D {
	private readonly UiEventSource _eventSource = new("Menu1OpeningEventSource");
	private readonly LegacyMenuRoot _legacyMenuRoot = new("LegacyMenuRoot");
	private readonly bool _showAboutOnInitialize;

	public Menu1OpeningPreviewScene(bool showAboutOnInitialize = false) : base("Menu1OpeningPreviewScene") {
		_showAboutOnInitialize = showAboutOnInitialize;
		_eventSource.ScopeRoot = this;
		AddChild(_eventSource);
		AddChild(_legacyMenuRoot);
	}

	protected override void OnInitialize() {
		try {
			var opening = new Menu1OpeningDataReader().ReadOpening();
			_legacyMenuRoot.SetContentRoot(new Menu1OpeningPreviewSurface(opening, _showAboutOnInitialize));
		} catch (Exception ex) {
			AppLog.Error("Menu1OpeningPreviewScene", "Failed to initialize menu opening preview.", ex);
			_legacyMenuRoot.SetContentRoot(new Menu1OpeningErrorSurface(ex));
		}
	}

	protected override void Draw() {
		Raylib.ClearBackground(Color.Black);
	}
}

internal sealed class Menu1OpeningPreviewSurface : Node2D {
	private const uint ConfirmTitleTextId = 1345;
	private const uint ConfirmQuitMessageTextId = 1406;
	private static readonly Color AnimationMarker = new(214, 120, 228, 255);
	private const float MusicFadeInDurationSeconds = 2f;
	private readonly List<AtlasFramesResource> _atlasResources = [];
	private readonly List<LegacyButtonNode> _openingButtons = [];
	private readonly List<AnimatedSprite2D?> _animatedSprite2Ds = [];
	private readonly Menu1HelpMenuData _helpMenu;
	private readonly Menu1OpeningData _opening;
	private readonly bool _showAboutOnInitialize;
	private readonly GT_MESSAGEBOX _quitMessageBox;
	private readonly UtfDbRepository _utfDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly LegacyRcStringResolver _strings;
	private Menu1HelpModalOverlay? _aboutModal;
	private LegacyMessageBoxModal? _exitModal;
	private AudioPlayer? _musicPlayer;

	public Menu1OpeningPreviewSurface(Menu1OpeningData opening, bool showAboutOnInitialize) : base("Menu1OpeningPreviewSurface") {
		ArgumentNullException.ThrowIfNull(opening);
		_opening = opening;
		_showAboutOnInitialize = showAboutOnInitialize;
		_utfDbRepository = new UtfDbRepository(RepoPaths.LocateUtfDbPaths());
		_vfxRepository = VfxAnimationDataRepository.LocateFromRepo();
		_strings = LegacyRcStringResolver.LoadFromRepo();
		_helpMenu = new Menu1OpeningDataReader().ReadHelpMenu();
		_quitMessageBox = ReadTypedEntry<GT_MESSAGEBOX>("GT_MESSAGEBOX", "CQMessageBox");
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
		_animatedSprite2Ds.AddRange(animSingle, animMulti, animOptions, animQuestion);
		ConnectButtonHover(btnSingle, animSingle);
		ConnectButtonHover(btnMulti, animMulti);
		ConnectButtonHover(btnIntro, animMedia);
		ConnectButtonHover(btnOptions, animOptions);
		ConnectButtonHover(btnHelp, animQuestion);
		btnIntro.Activated += _ => OpenMovie("Assets\\Movies\\cq_intro.mp4");
		btnHelp.Activated += _ => OpenAboutModal();
		btnQuit.Activated += _ => RequestQuit();
		AddStaticNode("StaticLegal", _opening.StaticLegal);
		_musicPlayer = AddMusicPlayer();
		if (_showAboutOnInitialize) {
			OpenAboutModal();
		}
	}

	protected override void OnExitTree() {
		_musicPlayer?.Stop();
		base.OnExitTree();
	}

	private void OpenMovie(string moviePath) {
		_musicPlayer?.Stop();
		Tree.ChangeRoot(new MovieScene(moviePath, () => new Menu1OpeningPreviewScene()));
	}

	protected override void OnUpdate(float deltaTime) {
		FadeInMusic(deltaTime);
		if (_aboutModal is null && _exitModal is null && Input.UiEsc) {
			OpenExitModal();
		}
	}

	private void FadeInMusic(float deltaTime) {
		if (_musicPlayer is not null && _musicPlayer.Volume < 1f) {
			_musicPlayer.Volume = MathF.Min(1f, _musicPlayer.Volume + (deltaTime / MusicFadeInDurationSeconds));
		}
	}

	private AudioPlayer AddMusicPlayer() {
		var audioPlayer = AddChild(new AudioPlayer("AudioPlayer") {
			Looping = true,
			Volume = 0f
		});
		var music = Shared.ResourceManager.Audio.OpenMusic(
			"Conquest Frontier Wars soundtrack - Main Menu Screen Music.mp3",
			AudioPlaybackBackend.NAudio);
		audioPlayer.SetOwnedAudio(music);
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
		_openingButtons.Add(node);
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

	private void OpenAboutModal() {
		if (_aboutModal is not null || _exitModal is not null) {
			return;
		}
		_animatedSprite2Ds.ForEach(x => x?.Visible = false);
		SetOpeningButtonsEnabled(false);
		_aboutModal = AddChild(new Menu1HelpModalOverlay(
			_helpMenu,
			_utfDbRepository,
			_vfxRepository,
			_strings,
			() => new MenuCreditsScene(() => new Menu1OpeningPreviewScene(showAboutOnInitialize: true)),
			CloseAboutModal));
	}

	private void CloseAboutModal() {
		if (_aboutModal is null) {
			return;
		}

		if (RemoveChild(_aboutModal)) {
			_aboutModal.Dispose();
		}

		_aboutModal = null;
		SetOpeningButtonsEnabled(true);
	}

	private void OpenExitModal() {
		if (_aboutModal is not null || _exitModal is not null) {
			return;
		}

		_animatedSprite2Ds.ForEach(x => x?.Visible = false);
		SetOpeningButtonsEnabled(false);
		_exitModal = AddChild(new LegacyMessageBoxModal(
			_quitMessageBox,
			_utfDbRepository,
			_vfxRepository,
			_strings,
			ResolveString(ConfirmTitleTextId, "Confirm Choice"),
			ResolveString(ConfirmQuitMessageTextId, "Do you really want to quit?"),
			LegacyMessageBoxButtons.OkCancel,
			OnExitModalCompleted));
	}

	private void OnExitModalCompleted(bool confirmed) {
		if (_exitModal is not null && RemoveChild(_exitModal)) {
			_exitModal.Dispose();
		}

		_exitModal = null;
		SetOpeningButtonsEnabled(true);
		if (confirmed) {
			RequestQuit();
		}
	}

	private void SetOpeningButtonsEnabled(bool enabled) {
		foreach (var button in _openingButtons) {
			button.EnableButton(enabled);
			button.SetKeyboardFocus(false);
		}
	}

	private string ResolveString(uint id, string fallback) {
		return _strings.TryResolve(id, out var text) && !string.IsNullOrWhiteSpace(text)
			? text
			: fallback;
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

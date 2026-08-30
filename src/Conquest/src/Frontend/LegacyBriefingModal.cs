using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class LegacyBriefingModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private const float CommWidth = 128f;
	private const float CommHeight = 96f;
	private const float ScriptAdvanceSeconds = 7f;

	private readonly Action _closed;
	private readonly List<AtlasFramesResource> _atlasResources = [];
	private readonly LegacyBriefingDefinition _briefing;
	private readonly LegacyRcStringResolver _strings;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly GT_BRIEFING _briefingMenu;

	private LegacyButtonNode? _startButton;
	private LegacyButtonNode? _replayButton;
	private LegacyButtonNode? _cancelButton;
	private LegacyStaticNode? _locationNode;
	private LegacyStaticNode? _objectiveNode;
	private LegacyStaticNode? _transcriptNode;
	private TextNode? _speakerLabel;
	private TextNode? _pageLabel;
	private TextNode? _statusLabel;
	private int _pageIndex;
	private float _pageTimer;

	public LegacyBriefingModal(
		string missionFileName,
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action closed) : base("LegacyBriefingModal", closed) {
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
		_closed = closed;
		_briefingMenu = ReadTypedEntry<GT_BRIEFING>("GT_BRIEFING", "MenuBriefing");
		_briefing = LegacyBriefingLibrary.Resolve(missionFileName);
	}

	protected override void OnInitialize() {
		base.OnInitialize();
		ContentRoot.Position = ResolveScreenPosition(_briefingMenu.ScreenRect);

		ContentRoot.AddChild(new PanelNode("Backdrop") {
			Position = Vector2.Zero,
			Size = new Vector2(LegacyScreenWidth, LegacyScreenHeight),
			Fill = Color.Black,
			Outline = Color.Blank,
			OutlineThickness = 0f
		});
		AddBackgroundNode();
		AddStaticNode("Title", _briefingMenu.Title);
		AddCommPanels();
		AddTeletypeContent();

		_startButton = AddButtonNode("Start", _briefingMenu.Start);
		_replayButton = AddButtonNode("Replay", _briefingMenu.Replay);
		_cancelButton = AddButtonNode("Cancel", _briefingMenu.Cancel);

		_startButton.Activated += _ => ShowStatus($"Mission runtime for '{_briefing.MissionFileName}' is not ported yet.");
		_replayButton.Activated += _ => RestartBriefing();
		_cancelButton.Activated += _ => CloseModal();
		_startButton.SetKeyboardFocus(true);

		RestartBriefing();
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		if (_briefing.Pages.Count <= 1) {
			return;
		}

		_pageTimer += deltaTime;
		if (_pageTimer < ScriptAdvanceSeconds) {
			return;
		}

		_pageTimer = 0f;
		_pageIndex = (_pageIndex + 1) % _briefing.Pages.Count;
		ApplyCurrentPage();
	}

	protected override void OnDispose() {
		for (var index = _atlasResources.Count - 1; index >= 0; index--) {
			_atlasResources[index].Dispose();
		}

		_atlasResources.Clear();
		base.OnDispose();
	}

	private void RestartBriefing() {
		_pageIndex = 0;
		_pageTimer = 0f;
		ShowStatus(string.Empty);
		ApplyCurrentPage();
	}

	private void ApplyCurrentPage() {
		if (_briefing.Pages.Count == 0) {
			return;
		}

		var page = _briefing.Pages[Math.Clamp(_pageIndex, 0, _briefing.Pages.Count - 1)];
		if (_speakerLabel is not null) {
			_speakerLabel.Text = page.Speaker;
		}

		_transcriptNode?.SetText(page.Text);
		if (_pageLabel is not null) {
			_pageLabel.Text = $"{_pageIndex + 1}/{_briefing.Pages.Count}";
		}
	}

	private void AddCommPanels() {
		for (var index = 0; index < _briefingMenu.RcComm.Length; index++) {
			var rect = _briefingMenu.RcComm[index];
			var position = new Vector2(rect.Left, rect.Top);
			ContentRoot.AddChild(new PanelNode($"CommFrame{index}") {
				Position = position,
				Size = new Vector2(CommWidth, CommHeight),
				Fill = new Color(8, 14, 20, 220),
				Outline = new Color(98, 158, 196, 255),
				OutlineThickness = 1f
			});

			AddAnimationNode($"Fuzz{index}", _briefingMenu.AnimFuzz[index]);
		}
	}

	private void AddTeletypeContent() {
		var rect = _briefingMenu.RcTeletype;
		var width = Math.Max(0f, rect.Right - rect.Left);
		var height = Math.Max(0f, rect.Bottom - rect.Top);
		var origin = new Vector2(rect.Left, rect.Top);

		ContentRoot.AddChild(new PanelNode("TeletypeFrame") {
			Position = origin,
			Size = new Vector2(width, height),
			Fill = new Color(0, 6, 12, 180),
			Outline = new Color(80, 144, 180, 255),
			OutlineThickness = 1f
		});

		_locationNode = AddMissionTextNode(
			"Location",
			origin + new Vector2(10f, 8f),
			new Vector2(width - 20f, 28f),
			_briefing.Location,
			new Color(180, 215, 240, 255));

		_objectiveNode = AddMissionTextNode(
			"Objective",
			origin + new Vector2(10f, 38f),
			new Vector2(width - 20f, 32f),
			_briefing.Objective,
			new Color(138, 191, 220, 255));

		_speakerLabel = ContentRoot.AddChild(new TextNode("SpeakerLabel") {
			Position = origin + new Vector2(10f, 78f),
			FontSize = 16f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(225, 196, 120, 255),
			Text = string.Empty
		});

		_transcriptNode = AddMissionTextNode(
			"Transcript",
			origin + new Vector2(8f, 100f),
			new Vector2(width - 16f, height - 124f),
			string.Empty,
			new Color(0, 138, 191, 255));

		_pageLabel = ContentRoot.AddChild(new TextNode("PageLabel") {
			Position = origin + new Vector2(width - 38f, height - 20f),
			FontSize = 14f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(180, 180, 180, 255),
			Text = string.Empty
		});

		_statusLabel = ContentRoot.AddChild(new TextNode("StatusLabel") {
			Position = new Vector2(80f, 507f),
			FontSize = 14f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(200, 180, 140, 255),
			Text = string.Empty
		});
	}

	private void AddBackgroundNode() {
		var scaledBackground = new STATIC_DATA {
			StaticType = _briefingMenu.Background.StaticType,
			StaticText = _briefingMenu.Background.StaticText,
			StaticTooltip = _briefingMenu.Background.StaticTooltip,
			StaticHintbox = _briefingMenu.Background.StaticHintbox,
			Alignment = _briefingMenu.Background.Alignment,
			XOrigin = 0,
			YOrigin = 0,
			Width = (int)LegacyScreenWidth,
			Height = (int)LegacyScreenHeight
		};

		AddStaticNode("Background", scaledBackground);
	}

	private LegacyStaticNode AddMissionTextNode(string label, Vector2 position, Vector2 size, string text, Color color) {
		var archetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", "Static!!MissionText");
		var node = new LegacyStaticNode(label);
		node.ApplyLegacyDefinition(archetype, new STATIC_DATA {
			StaticType = "Static!!MissionText",
			XOrigin = (int)position.X,
			YOrigin = (int)position.Y,
			Width = (int)size.X,
			Height = (int)size.Y,
			Alignment = (uint)LegacyStaticNode.StaticAlignment.TopLeft
		}, _vfxRepository);
		node.SetTextColor(color);
		node.SetText(text);
		ContentRoot.AddChild(node);
		return node;
	}

	private void ShowStatus(string message) {
		if (_statusLabel is not null) {
			_statusLabel.Text = message;
		}
	}

	private void CloseModal() {
		_closed();
	}

	private LegacyButtonNode AddButtonNode(string label, BUTTON_DATA data) {
		var node = CreateButtonNode(label, data);
		ContentRoot.AddChild(node);
		return node;
	}

	private void AddStaticNode(string label, STATIC_DATA data) {
		var node = CreateStaticNode(label, data);
		ContentRoot.AddChild(node);
	}

	private AnimatedSprite2D? AddAnimationNode(string label, ANIMATE_DATA data) {
		var archetype = ReadTypedEntry<GT_ANIMATE>("GT_ANIMATE", data.AnimateType);
		var vfxData = _vfxRepository.Load();
		if (!vfxData.TryGetAtlasByVfxShapeId(archetype.VfxType, out var atlasEntry)) {
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
			SpeedFps = 15f
		};
		ContentRoot.AddChild(node);
		return node;
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

	private sealed record LegacyBriefingPage(string Speaker, string Text);

	private sealed record LegacyBriefingDefinition(
		string MissionFileName,
		string Location,
		string Objective,
		IReadOnlyList<LegacyBriefingPage> Pages);

	private static class LegacyBriefingLibrary {
		public static LegacyBriefingDefinition Resolve(string missionFileName) {
			var normalized = Path.GetFileName(missionFileName);
			if (string.Equals(normalized, "Mantis_Train.dmission", StringComparison.OrdinalIgnoreCase)) {
				return new LegacyBriefingDefinition(
					"Mantis_Train.dmission",
					"Voraak System, Voraak Prime, Omega Sector.",
					"Prepare for Mantis training...",
					[
						new LegacyBriefingPage(
							"Radio Broadcast",
							"I great Mantis General MalMax and grand news have I! To pursue one's foe, to crush their bones beneath one's feet, these things tire the exoskeleton and oppress the brain. For this recommend I MantisAde! Made from the strained fluid of Terran spines. Drink your enemies!"),
						new LegacyBriefingPage(
							"Warlord Mordella",
							"Majesty, Warlord Mordella am I. Great Leader you will be if to me you listen. Weakling peace-lover Terrans may we find, and crush them we will. Crush them and feel their dying warmth. Hail Queen!"),
						new LegacyBriefingPage(
							"Warlord Mordella",
							"Begin we then, Majesty. Before you see three ships and a Cocoon. The larger ship the Weaver is. With your mouse select the Weaver.")
					]);
			}

			if (string.Equals(normalized, "Sol_Train.dmission", StringComparison.OrdinalIgnoreCase)) {
				return new LegacyBriefingDefinition(
					"Sol_Train.dmission",
					"Solar System, 0945 hours",
					"1. Prepare for your Celareon Training.",
					[
						new LegacyBriefingPage(
							"Radio Broadcast",
							"Greetings, Celereon friends. Are you tired of thinking as part of the group? Terran Industries presents 'New You' personality camouflage and a brain blocking chip. For research purposes only."),
						new LegacyBriefingPage(
							"Magistrate Elan",
							"Greetings Commander, I'm Celereon Magistrate Elan. I'll be linking your mind into our collective knowledge base for training purposes. You will learn the necessary decision-making procedures of our appointed ambassadors."),
						new LegacyBriefingPage(
							"Magistrate Elan",
							"Let's begin, Ambassador. You have before you three ships and an Acropolis. We will begin with the larger ship, the Forger. Use your digital selection device, or mouse, and select the Forger.")
					]);
			}

			return new LegacyBriefingDefinition(
				normalized,
				normalized,
				"Briefing data is not authored for this mission yet.",
				[
					new LegacyBriefingPage(
						"System",
						"This briefing route exists, but scripted mission playback for this mission is not ported yet.")
				]);
		}
	}
}

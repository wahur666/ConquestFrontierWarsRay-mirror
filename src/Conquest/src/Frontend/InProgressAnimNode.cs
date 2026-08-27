using System;
using System.Collections.Generic;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class InProgressAnimNode : Node2D {
	private const uint LoadMissionDataTextId = 1546;
	private readonly List<AtlasFramesResource> _atlasResources = [];
	private readonly LegacyRcStringResolver _strings;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private LegacyStaticNode? _backgroundNode;
	private TextNode? _backgroundTitle;
	private LegacyProgressStaticNode? _progressBar;
	private TextNode? _statusLabel;
	private float _elapsedSeconds;
	private int _phaseIndex;
	private float _progress;

	private static readonly LoadingPhase[] Phases = [
		new("Loading mission data", 1.4f, 0.18f),
		new("Preparing interface assets", 1.0f, 0.42f),
		new("Initializing sector state", 1.3f, 0.73f),
		new("Finalizing scene startup", 1.1f, 1f)
	];

	public InProgressAnimNode(
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		string? name = null) : base(name ?? "SinglePlayerLoadingScreenNode") {
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
	}

	public float ScaleFactor {
		get => Scale.X;
		set => Scale = new Vector2(value, value);
	}

	public Vector2 ContentSize { get; private set; } = new(600f, 340f);

	protected override void OnInitialize() {
		_backgroundNode = AddStaticNode("Background", new STATIC_DATA {
			StaticType = "Static!!Progress",
			XOrigin = 0,
			YOrigin = 0,
			StaticText = 0,
			Alignment = (uint)LegacyStaticNode.StaticAlignment.TenBySix
		});

		_backgroundTitle = AddChild(new TextNode("BackgroundTitle") {
			Position = new Vector2(10f, 6f),
			FontSize = 16f,
			TextStyle = UiTextStyle.Body,
			Tint = new Raylib_cs.Color(240, 240, 255, 255),
			Text = ResolveString(LoadMissionDataTextId, "Loading mission data")
		});

		AddAnimationNode("ForegroundAnimation", new ANIMATE_DATA {
			AnimateType = "Animate!!Progress",
			XOrigin = 14,
			YOrigin = 30,
			FuzzEffect = false
		});

		_progressBar = AddChild(new LegacyProgressStaticNode("ProgressBar"));
		_progressBar.ApplyLegacyDefinition(new GT_PROGRESS_STATIC {
			FontName = string.Empty,
			NormalText = new GT_COLOR { Red = 240, Green = 240, Blue = 255 },
			OverText = new GT_COLOR { Red = 240, Green = 240, Blue = 255 },
			Background = new GT_COLOR { Red = 175, Green = 80, Blue = 80 },
			Background2 = new GT_COLOR { Red = 0, Green = 0, Blue = 0 },
			ShapeFile = "progress.shp",
			BackgroundDraw = GT_DRAWTYPE.NODRAW,
			Backdraw = false
		}, new Vector2(15f, 130f), repository: _vfxRepository);
		_progressBar.SetProgress(0, 1000);

		_statusLabel = AddChild(new TextNode("StatusLabel") {
			Position = new Vector2(170f, 100f),
			FontSize = 16f,
			TextStyle = UiTextStyle.Body,
			Tint = new Raylib_cs.Color(240, 240, 255, 255),
			Text = ResolveString(LoadMissionDataTextId, Phases[0].Status)
		});

		if (_backgroundNode is not null) {
			ContentSize = _backgroundNode.Size;
		}

		ApplyProgressState();
	}

	protected override void OnUpdate(float deltaTime) {
		_elapsedSeconds += deltaTime;
		var cycleDuration = 0f;
		foreach (var phase in Phases) {
			cycleDuration += phase.DurationSeconds;
		}

		if (cycleDuration <= 0f) {
			return;
		}

		var loopTime = _elapsedSeconds % cycleDuration;
		var phaseStart = 0f;
		for (var index = 0; index < Phases.Length; index++) {
			var phase = Phases[index];
			var phaseEnd = phaseStart + phase.DurationSeconds;
			if (loopTime <= phaseEnd || index == Phases.Length - 1) {
				_phaseIndex = index;
				var localProgress = phase.DurationSeconds <= 0f
					? 1f
					: Math.Clamp((loopTime - phaseStart) / phase.DurationSeconds, 0f, 1f);
				var previousTarget = index == 0 ? 0f : Phases[index - 1].TargetProgress;
				_progress = previousTarget + ((phase.TargetProgress - previousTarget) * localProgress);
				ApplyProgressState();
				return;
			}

			phaseStart = phaseEnd;
		}
	}

	protected override void OnDispose() {
		for (var index = _atlasResources.Count - 1; index >= 0; index--) {
			_atlasResources[index].Dispose();
		}

		_atlasResources.Clear();
		base.OnDispose();
	}

	private void ApplyProgressState() {
		if (_progressBar is not null) {
			_progressBar.SetProgress((uint)Math.Clamp((int)MathF.Round(_progress * 1000f), 0, 1000), 1000);
		}

		if (_statusLabel is not null) {
			_statusLabel.Text = Phases[Math.Clamp(_phaseIndex, 0, Phases.Length - 1)].Status;
		}
	}

	private LegacyStaticNode AddStaticNode(string label, STATIC_DATA data) {
		var archetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", data.StaticType);
		var node = new LegacyStaticNode(label);
		node.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		if (_strings.TryResolve(data.StaticText, out var text) && !string.IsNullOrWhiteSpace(text)) {
			node.SetText(text);
		}

		AddChild(node);
		return node;
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
		AddChild(node);
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

	private readonly record struct LoadingPhase(string Status, float DurationSeconds, float TargetProgress);
}

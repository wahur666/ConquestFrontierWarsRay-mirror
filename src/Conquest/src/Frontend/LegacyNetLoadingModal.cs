using System;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class LegacyNetLoadingModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private readonly Action _closed;
	private readonly LegacyRcStringResolver _strings;
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private bool _awaitingF1Release = true;
	private PanelNode? _backgroundBackdrop;
	private LegacyStaticNode? _backgroundNode;
	private LegacyStaticNode? _progressFrame;
	private PanelNode? _progressFill;
	private TextNode? _statusLabel;
	private TextNode? _percentLabel;
	private float _elapsedSeconds;
	private int _phaseIndex;
	private float _progress;

	private static readonly LoadingPhase[] Phases = [
		new("Synchronizing multiplayer settings...", 0.9f, 0.12f),
		new("Comparing mission checksums...", 1.1f, 0.28f),
		new("Transferring mission data...", 2.2f, 0.74f),
		new("Initializing net game...", 1.4f, 0.92f),
		new("Waiting for remote players...", 1.1f, 1f)
	];

	public LegacyNetLoadingModal(
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action closed) : base("LegacyNetLoadingModal", closed) {
		ZIndex = 1100;
		_closed = closed;
		_xmlDbRepository = xmlDbRepository;
		_vfxRepository = vfxRepository;
		_strings = strings;
	}

	protected override void OnInitialize() {
		base.OnInitialize();

		_backgroundBackdrop = ContentRoot.AddChild(new PanelNode("BackgroundBackdrop") {
			Position = Vector2.Zero,
			Size = new Vector2(LegacyScreenWidth, LegacyScreenHeight),
			Fill = Color.Black,
			Outline = Color.Blank,
			OutlineThickness = 0f
		});

		_backgroundNode = AddStaticNode("Background", new STATIC_DATA {
			StaticType = "Static!!LoadingBackground",
			XOrigin = 0,
			YOrigin = 0
		});
		ApplyBackgroundLayout();

		AddChild(new TextNode("Title") {
			Position = new Vector2(214f, 98f),
			FontSize = 30f,
			TextStyle = UiTextStyle.Title,
			Tint = new Color(212, 220, 236, 255),
			Text = "Loading Multiplayer Game"
		});

		AddChild(new TextNode("Subtitle") {
			Position = new Vector2(236f, 134f),
			FontSize = 16f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(164, 184, 210, 255),
			Text = "Preview overlay from Menu_netloading.cpp"
		});

		_progressFrame = AddStaticNode("ProgressFrame", new STATIC_DATA {
			StaticType = "Static!!LoadingProgress",
			XOrigin = 180,
			YOrigin = 418,
			Width = 440,
			Height = 28
		});
		_progressFrame.SetText(string.Empty);

		_progressFill = AddChild(new PanelNode("ProgressFill") {
			Position = new Vector2(184f, 422f),
			Size = new Vector2(0f, 20f),
			Fill = new Color(66, 133, 224, 255),
			Outline = Color.Blank,
			OutlineThickness = 0f
		});

		_statusLabel = AddChild(new TextNode("StatusLabel") {
			Position = new Vector2(184f, 378f),
			FontSize = 18f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(232, 236, 240, 255),
			Text = Phases[0].Status
		});

		_percentLabel = AddChild(new TextNode("PercentLabel") {
			Position = new Vector2(632f, 418f),
			FontSize = 18f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(232, 236, 240, 255),
			Text = "0%"
		});

		AddChild(new TextNode("HintLabel") {
			Position = new Vector2(184f, 458f),
			FontSize = 15f,
			TextStyle = UiTextStyle.Body,
			Tint = new Color(164, 184, 210, 255),
			Text = "Press F1 or Esc to return to skirmish staging."
		});

		ApplyProgressVisuals();
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);
		ApplyBackgroundLayout();

		if (_awaitingF1Release) {
			_awaitingF1Release = Raylib.IsKeyDown(KeyboardKey.F1);
			return;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.F1)) {
			_closed();
			return;
		}

		if (_progress >= 1f) {
			return;
		}

		_elapsedSeconds += deltaTime;
		var totalElapsed = 0f;
		for (var index = 0; index < Phases.Length; index++) {
			var phase = Phases[index];
			var phaseEnd = totalElapsed + phase.DurationSeconds;
			if (_elapsedSeconds <= phaseEnd || index == Phases.Length - 1) {
				_phaseIndex = index;
				var localProgress = phase.DurationSeconds <= 0f
					? 1f
					: Math.Clamp((_elapsedSeconds - totalElapsed) / phase.DurationSeconds, 0f, 1f);
				var previousTarget = index == 0 ? 0f : Phases[index - 1].TargetProgress;
				_progress = previousTarget + ((phase.TargetProgress - previousTarget) * localProgress);
				ApplyProgressVisuals();
				return;
			}

			totalElapsed = phaseEnd;
		}
	}

	private void ApplyProgressVisuals() {
		if (_progressFill is not null) {
			_progressFill.Size = new Vector2(432f * Math.Clamp(_progress, 0f, 1f), 20f);
		}

		if (_statusLabel is not null) {
			_statusLabel.Text = Phases[Math.Clamp(_phaseIndex, 0, Phases.Length - 1)].Status;
		}

		if (_percentLabel is not null) {
			_percentLabel.Text = $"{Math.Clamp((int)MathF.Round(_progress * 100f), 0, 100)}%";
		}
	}

	private void ApplyBackgroundLayout() {
		if (_backgroundNode is null || _backgroundNode.Size.X <= 0f || _backgroundNode.Size.Y <= 0f) {
			return;
		}

		var targetRect = ResolveViewportRectInLegacySpace();
		if (_backgroundBackdrop is not null) {
			_backgroundBackdrop.Position = new Vector2(targetRect.X, targetRect.Y);
			_backgroundBackdrop.Size = new Vector2(targetRect.Width, targetRect.Height);
		}

		var scale = MathF.Min(
			targetRect.Width / _backgroundNode.Size.X,
			targetRect.Height / _backgroundNode.Size.Y);

		var scaledSize = _backgroundNode.Size * scale;
		_backgroundNode.Position = new Vector2(
			targetRect.X + ((targetRect.Width - scaledSize.X) * 0.5f),
			targetRect.Y + ((targetRect.Height - scaledSize.Y) * 0.5f));
		_backgroundNode.Scale = new Vector2(scale, scale);
	}

	private Rectangle ResolveViewportRectInLegacySpace() {
		var menuRoot = FindLegacyMenuRoot();
		if (menuRoot is null || menuRoot.UniformScale <= 0f) {
			return new Rectangle(0f, 0f, LegacyScreenWidth, LegacyScreenHeight);
		}

		var inverseScale = 1f / menuRoot.UniformScale;
		return new Rectangle(
			-menuRoot.ContentViewport.X * inverseScale,
			-menuRoot.ContentViewport.Y * inverseScale,
			menuRoot.WindowSize.X * inverseScale,
			menuRoot.WindowSize.Y * inverseScale);
	}

	private LegacyMenuRoot? FindLegacyMenuRoot() {
		for (var current = Parent; current is not null; current = current.Parent) {
			if (current is LegacyMenuRoot menuRoot) {
				return menuRoot;
			}
		}

		return null;
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

	private readonly record struct LoadingPhase(string Status, float DurationSeconds, float TargetProgress);
}

using System;
using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class SinglePlayerLoadingPreviewModal : LegacyModalNode {
	private const float LegacyScreenWidth = 800f;
	private const float LegacyScreenHeight = 600f;
	private readonly Action _closed;
	private readonly InProgressAnimNode _loadingScreen;
	private bool _awaitingF2Release = true;

	public SinglePlayerLoadingPreviewModal(
		XmlDbRepository xmlDbRepository,
		VfxAnimationDataRepository vfxRepository,
		LegacyRcStringResolver strings,
		Action closed) : base("InProgressAnimPreviewModal", closed) {
		ZIndex = 1200;
		_closed = closed;
		_loadingScreen = new InProgressAnimNode(
			xmlDbRepository,
			vfxRepository,
			strings,
			"SinglePlayerLoadingScreen") {
			ScaleFactor = 1.5f
		};
	}

	protected override void OnInitialize() {
		base.OnInitialize();

		ContentRoot.AddChild(new PanelNode("Backdrop") {
			Position = Vector2.Zero,
			Size = new Vector2(LegacyScreenWidth, LegacyScreenHeight),
			Fill = Raylib_cs.Color.Black,
			Outline = Raylib_cs.Color.Blank,
			OutlineThickness = 0f
		});
		ContentRoot.AddChild(_loadingScreen);
		CenterLoadingScreen();
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);
		CenterLoadingScreen();

		if (_awaitingF2Release) {
			_awaitingF2Release = Raylib_cs.Raylib.IsKeyDown(Raylib_cs.KeyboardKey.F2);
			return;
		}

		if (Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.F2)) {
			_closed();
		}
	}

	private void CenterLoadingScreen() {
		var scale = _loadingScreen.ScaleFactor;
		var scaledSize = _loadingScreen.ContentSize * scale;
		_loadingScreen.Position = new Vector2(
			(LegacyScreenWidth - scaledSize.X) * 0.5f,
			(LegacyScreenHeight - scaledSize.Y) * 0.5f);
	}
}

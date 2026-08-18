using System.Numerics;
using Raylib_cs;

namespace RaySharp;

internal static class MainSceneHud {
	internal static Rectangle MediaControlsPanel => new((Raylib.GetScreenWidth() - 520.0f) * 0.5f,
		Raylib.GetScreenHeight() - 76.0f, 520.0f, 58.0f);

	internal static void DrawCommandPlane() {
		DrawLitGroundPlane();
		DrawSmoothGrid();
	}

	internal static void DrawSelectionRectangle(UiState ui) {
		if (!ui.IsSelecting) {
			return;
		}

		Rectangle selection = NormalizeRectangle(ui.SelectionStart, ui.SelectionCurrent);
		Raylib.DrawRectangleRec(selection, new Color(143, 215, 255, 34));
		Raylib.DrawRectangleLinesEx(selection, 2.0f, new Color(143, 215, 255, 210));
	}

	internal static Rectangle NormalizeRectangle(Vector2 start, Vector2 end) {
		float x = MathF.Min(start.X, end.X);
		float y = MathF.Min(start.Y, end.Y);
		float width = MathF.Abs(end.X - start.X);
		float height = MathF.Abs(end.Y - start.Y);
		return new Rectangle(x, y, width, height);
	}

	internal static void DrawUi(UiState ui, UiTextures uiTextures, UiFont uiFont) {
		int screenWidth = Raylib.GetScreenWidth();
		int screenHeight = Raylib.GetScreenHeight();

		Raylib.DrawRectangle(0, 0, screenWidth, AppTheme.HeaderHeight, AppTheme.HeaderBackground);
		Raylib.DrawLine(0, AppTheme.HeaderHeight, screenWidth, AppTheme.HeaderHeight, AppTheme.HeaderBorder);
		DrawUiText(uiFont, "Conquest RTS Prototype", 14, 15, 18.0f, AppTheme.TextPrimary);
		DrawUiText(uiFont, ui.Status, 210, 16, 16.0f, AppTheme.TextMuted);
		DrawUiText(uiFont,
			"Left-drag: select | Right click: move | Wheel: zoom | Q/E: rotate | F8: confine mouse | F9: model wire | F11: fullscreen",
			14, 34, 14.0f, AppTheme.TextMuted);

		if (!ui.MenuOpen) {
			DrawImageStyleButton(UiLayout.MenuButton, "Menu", uiTextures, uiFont, false);
		} else {
			DrawOptionsMenu(uiTextures, uiFont);
		}

		DrawUiText(uiFont, $"FPS: {Raylib.GetFPS()}", 20, screenHeight - 40, 22.0f, AppTheme.TextMuted);
	}

	internal static Vector2 DefaultVideoOverlayPosition(VideoPlayer videoPlayer, bool alignRight = true) {
		const float margin = 24.0f;
		const float maxWidth = 360.0f;
		float scale = maxWidth / videoPlayer.Width;
		float width = videoPlayer.Width * scale;
		float height = videoPlayer.Height * scale;
		return new Vector2(
			alignRight ? Raylib.GetScreenWidth() - width - margin : margin,
			Raylib.GetScreenHeight() - height - (alignRight ? margin : 86.0f));
	}

	internal static Vector2 DefaultVfxAudioOverlayPosition(bool alignRight = false) {
		const float margin = 24.0f;
		const float scale = 3.0f;
		const float width = 62.0f * scale;
		const float height = 79.0f * scale;
		return new Vector2(
			alignRight ? Raylib.GetScreenWidth() - width - margin : margin,
			Raylib.GetScreenHeight() - height - (alignRight ? margin : 86.0f));
	}

	internal static void DrawVideoOverlay(VideoPlayer? videoPlayer, UiFont uiFont, Vector2 position) {
		if (videoPlayer is null) {
			return;
		}

		const float maxWidth = 360.0f;
		float scale = maxWidth / videoPlayer.Width;
		float width = videoPlayer.Width * scale;
		float height = videoPlayer.Height * scale;
		Rectangle destination = new(position.X, position.Y, width, height);

		Raylib.DrawRectangleRec(
			new Rectangle(destination.X - 6.0f, destination.Y - 28.0f, destination.Width + 12.0f,
				destination.Height + 34.0f),
			new Color(0, 0, 0, 150));
		string label = videoPlayer.IsFinished
			? $"Video ended ({VideoLibrary.FormatFrameRate(videoPlayer.FrameRate)})"
			: $"{videoPlayer.FileName} ({VideoLibrary.FormatFrameRate(videoPlayer.FrameRate)})";
		DrawUiText(uiFont, label, destination.X, destination.Y - 22.0f, 16.0f, AppTheme.TextPrimary);
		videoPlayer.Draw(destination, Color.White);
		Raylib.DrawRectangleLinesEx(destination, 1.0f, new Color(255, 255, 255, 130));
	}

	internal static void DrawVfxAudioOverlay(VfxAudioPlayer? vfxAudioPlayer, UiFont uiFont, Vector2 position) {
		if (vfxAudioPlayer is null) {
			return;
		}

		const float scale = 3.0f;
		const float width = 62.0f * scale;
		const float height = 79.0f * scale;
		Rectangle destination = new(position.X, position.Y, width, height);

		Raylib.DrawRectangleRec(
			new Rectangle(destination.X - 10.0f, destination.Y - 30.0f, destination.Width + 20.0f,
				destination.Height + 40.0f),
			new Color(0, 0, 0, 150));
		DrawUiText(uiFont, vfxAudioPlayer.Label, destination.X, destination.Y - 23.0f, 16.0f, AppTheme.TextPrimary);
		vfxAudioPlayer.Draw(destination, Color.White);
		Raylib.DrawRectangleLinesEx(destination, 1.0f, new Color(255, 255, 255, 130));
	}

	internal static void DrawMediaControls(
		ref VideoPlayer? videoPlayer,
		VfxAudioPlayer? vfxAudioPlayer,
		UiFont uiFont,
		UiState ui,
		ref bool stopVideoAfterFrame) {
		Rectangle panel = MediaControlsPanel;
		Raylib.DrawRectangleRec(panel, new Color(13, 16, 20, 220));
		Raylib.DrawRectangleLinesEx(panel, 1.0f, AppTheme.HeaderBorder);

		DrawUiText(uiFont, "Video", panel.X + 14.0f, panel.Y + 10.0f, 15.0f, AppTheme.TextMuted);
		DrawUiText(uiFont, "VFX", panel.X + 318.0f, panel.Y + 10.0f, 15.0f, AppTheme.TextMuted);

		float y = panel.Y + 28.0f;
		if (DrawControlButton(new Rectangle(panel.X + 70.0f, y, 58.0f, 22.0f), "Play", uiFont, true)) {
			videoPlayer ??= VideoLibrary.TryLoadDemoVideo(ui);
			videoPlayer?.Play();
		}

		if (DrawControlButton(new Rectangle(panel.X + 134.0f, y, 64.0f, 22.0f), "Pause", uiFont,
			    videoPlayer is not null)) {
			videoPlayer?.Pause();
		}

		if (DrawControlButton(new Rectangle(panel.X + 204.0f, y, 58.0f, 22.0f), "Stop", uiFont,
			    videoPlayer is not null)) {
			videoPlayer?.Pause();
			stopVideoAfterFrame = true;
			ui.Status = "Video stopped.";
		}

		if (DrawControlButton(new Rectangle(panel.X + 356.0f, y, 58.0f, 22.0f), "Play", uiFont,
			    vfxAudioPlayer is not null)) {
			vfxAudioPlayer?.Play();
		}

		if (DrawControlButton(new Rectangle(panel.X + 420.0f, y, 58.0f, 22.0f), "Stop", uiFont,
			    vfxAudioPlayer is not null)) {
			vfxAudioPlayer?.Stop();
		}
	}

	private static void DrawLitGroundPlane() {
		const float extent = 40.0f;

		Vector3 backLeft = new(-extent, 0.0f, -extent);
		Vector3 frontLeft = new(-extent, 0.0f, extent);
		Vector3 frontRight = new(extent, 0.0f, extent);
		Vector3 backRight = new(extent, 0.0f, -extent);

		LitRenderer.DrawQuad(backLeft, frontLeft, frontRight, backRight, AppTheme.PlaneColor);
	}

	private static void DrawSmoothGrid() {
		const float extent = 40.0f;
		const float step = 2.0f;
		const float y = 0.035f;

		Color minor = new(48, 57, 70, 210);
		Color major = new(83, 97, 116, 235);

		for (int i = -20; i <= 20; i++) {
			float coordinate = i * step;
			Color color = i == 0 ? major : minor;
			Raylib.DrawLine3D(new Vector3(-extent, y, coordinate), new Vector3(extent, y, coordinate), color);
			Raylib.DrawLine3D(new Vector3(coordinate, y, -extent), new Vector3(coordinate, y, extent), color);
		}
	}

	private static void DrawOptionsMenu(UiTextures uiTextures, UiFont uiFont) {
		Rectangle panel = UiLayout.OptionsPanel;
		Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), new Color(0, 0, 0, 95));

		if (uiTextures.HasOptionsPanel) {
			DrawTextureToRect(uiTextures.OptionsPanel, panel, Color.White);
		} else {
			Raylib.DrawRectangleRec(panel, new Color(45, 39, 35, 245));
			Raylib.DrawRectangleLinesEx(panel, 3.0f, new Color(168, 128, 72, 255));
			Raylib.DrawRectangleLinesEx(new Rectangle(panel.X + 10, panel.Y + 10, panel.Width - 20, panel.Height - 20),
				1.0f, new Color(238, 202, 132, 160));
		}

		DrawUiText(uiFont, "Menu", panel.X + 236.0f, panel.Y + 176.0f, 30.0f, new Color(241, 233, 210, 255));
		DrawUiText(uiFont, "Raylib recreation of the Three.js proof-of-concept", panel.X + 72.0f, panel.Y + 236.0f,
			18.0f, new Color(207, 190, 160, 255));
		DrawImageStyleButton(UiLayout.CloseButton, "Close", uiTextures, uiFont, true);
	}

	private static void DrawImageStyleButton(Rectangle rect, string text, UiTextures uiTextures, UiFont uiFont,
		bool raised) {
		Vector2 mouse = Raylib.GetMousePosition();
		bool hot = Raylib.CheckCollisionPointRec(mouse, rect);

		if (uiTextures.HasButtons) {
			DrawTextureToRect(hot ? uiTextures.ButtonHover : uiTextures.ButtonBase, rect, Color.White);
		} else {
			Color fill = hot ? new Color(113, 88, 54, 255) : new Color(74, 58, 42, 255);
			Color border = hot ? new Color(245, 204, 119, 255) : new Color(169, 123, 72, 255);

			Raylib.DrawRectangleRec(rect, fill);
			Raylib.DrawRectangleLinesEx(rect, 2.0f, border);
			if (raised) {
				Raylib.DrawLine((int)rect.X + 3, (int)rect.Y + 3, (int)(rect.X + rect.Width) - 3, (int)rect.Y + 3,
					new Color(245, 226, 170, 120));
			}
		}

		const float fontSize = 15.0f;
		Vector2 textSize = Raylib.MeasureTextEx(uiFont.Font, text, fontSize, UiFont.Spacing);
		DrawUiText(uiFont, text, rect.X + (rect.Width - textSize.X) * 0.5f, rect.Y + 6.0f, fontSize,
			new Color(241, 233, 210, 255));
	}

	private static bool DrawControlButton(Rectangle rect, string text, UiFont uiFont, bool enabled) {
		Vector2 mouse = Raylib.GetMousePosition();
		bool hot = enabled && Raylib.CheckCollisionPointRec(mouse, rect);
		Color fill;
		if (!enabled) {
			fill = new Color(45, 49, 55, 180);
		} else {
			fill = hot ? new Color(85, 123, 154, 255) : new Color(48, 63, 78, 255);
		}

		Color border = hot ? new Color(157, 216, 255, 255) : new Color(91, 108, 126, 255);
		Color textColor = enabled ? AppTheme.TextPrimary : new Color(150, 158, 166, 180);

		Raylib.DrawRectangleRec(rect, fill);
		Raylib.DrawRectangleLinesEx(rect, 1.0f, border);

		const float fontSize = 13.0f;
		Vector2 textSize = Raylib.MeasureTextEx(uiFont.Font, text, fontSize, UiFont.Spacing);
		DrawUiText(uiFont, text, rect.X + (rect.Width - textSize.X) * 0.5f, rect.Y + 4.0f, fontSize, textColor);

		return hot && Raylib.IsMouseButtonPressed(MouseButton.Left);
	}

	private static void DrawUiText(UiFont uiFont, string text, float x, float y, float fontSize, Color color) {
		Raylib.DrawTextEx(uiFont.Font, text, new Vector2(x, y), fontSize, UiFont.Spacing, color);
	}

	private static void DrawTextureToRect(Texture2D texture, Rectangle destination, Color tint) {
		Rectangle source = new(0.0f, 0.0f, texture.Width, texture.Height);
		Raylib.DrawTexturePro(texture, source, destination, Vector2.Zero, 0.0f, tint);
	}
}

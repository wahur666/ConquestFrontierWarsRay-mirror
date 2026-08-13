using System.Globalization;
using System.Numerics;
using Raylib_cs;

namespace RaySharp.Particle;

internal sealed class ParticleEditorUi {
	public const float SampleTreeWidth = 340.0f;
	public const float ParameterPanelWidth = 420.0f;
	private const float BottomHudClearance = 112.0f;
	private const float SampleTreeFontSize = 15.0f;
	private const float SampleTreeRowHeight = SampleTreeFontSize + 11.0f;

	private readonly UiFont uiFont;
	private float scrollOffset;
	private float sampleTreeScrollOffset;
	private int activeSliderId;
	private int activeTextBoxId;
	private int nextSliderId;
	private string activeTextBoxValue = string.Empty;

	public ParticleEditorUi(UiFont uiFont) {
		this.uiFont = uiFont;
	}

	public bool IsTextEditing => activeTextBoxId != 0;

	public ParticleEditorUiResult Draw(
		ParticleParameters? parameters,
		ParticlePreviewSystem? system,
		ref Vector3 mainTransformOrigin,
		ref Vector3 mainTransformRotationDegrees,
		ref Vector3 mainTransformScale,
		string status,
		string stats,
		string sampleStatus,
		IReadOnlyList<string> sampleFiles,
		string? sampleFolder,
		int currentSampleIndex,
		string textureLabel,
		IReadOnlyList<string> logEntries) {
		int screenWidth = Raylib.GetScreenWidth();
		int screenHeight = Raylib.GetScreenHeight();
		Rectangle sampleTreePanel = new(0.0f, 56.0f, SampleTreeWidth, Math.Max(1.0f, screenHeight - 56.0f - BottomHudClearance));
		Rectangle panel = new(screenWidth - ParameterPanelWidth, 56.0f, ParameterPanelWidth, screenHeight - 56.0f);
		Rectangle logPanel = new(panel.X, Math.Max(panel.Y + 160.0f, screenHeight - 138.0f), panel.Width, Math.Min(138.0f, screenHeight - panel.Y));
		Rectangle scrollPanel = new(panel.X, panel.Y, panel.Width, Math.Max(1.0f, logPanel.Y - panel.Y));
		Rectangle contentClip = new(scrollPanel.X + 1.0f, scrollPanel.Y + 42.0f, scrollPanel.Width - 2.0f, Math.Max(1.0f, scrollPanel.Height - 44.0f));
		Rectangle header = new(0.0f, 0.0f, screenWidth, 56.0f);
		ParticleEditorUiResult result = new();
		nextSliderId = 1;

		Raylib.DrawRectangleRec(header, new Color(25, 29, 36, 255));
		Raylib.DrawLine(0, 56, screenWidth, 56, new Color(44, 52, 64, 255));
		DrawText("Conquest Particle Editor", 14.0f, 12.0f, 20.0f, new Color(232, 237, 244, 255));
		DrawText("Drag: orbit | Shift/middle drag: pan | Wheel: zoom | R/F | +/- deploy | IJKL/UO move main | CV yaw | ZX scale | E flag", 14.0f, 34.0f, 13.0f, new Color(185, 196, 210, 255));
		DrawText($"{Raylib.GetFPS()} FPS", screenWidth - 474.0f, 20.0f, 13.0f, new Color(174, 185, 200, 255));

		if (Button(new Rectangle(screenWidth - 402.0f, 14.0f, 82.0f, 28.0f), "Restart")) {
			result.Restart = true;
		}

		if (Button(new Rectangle(screenWidth - 312.0f, 14.0f, 72.0f, 28.0f), "Frame")) {
			result.Frame = true;
		}

		if (Button(new Rectangle(screenWidth - 232.0f, 14.0f, 68.0f, 28.0f), "Open")) {
			result.OpenFile = true;
		}

		if (Button(new Rectangle(screenWidth - 156.0f, 14.0f, 138.0f, 28.0f), "Load blast sample")) {
			result.ReloadSample = true;
		}

		DrawSampleTree(sampleTreePanel, sampleFiles, sampleFolder, currentSampleIndex, ref result);
		DrawHud(status, stats);

		Raylib.DrawRectangleRec(panel, new Color(21, 25, 32, 245));
		Raylib.DrawLine((int)panel.X, (int)panel.Y, (int)panel.X, screenHeight, new Color(44, 52, 64, 255));
		DrawText("Parameters", panel.X + 16.0f, panel.Y + 14.0f, 18.0f, new Color(232, 237, 244, 255));

		float contentStartY = contentClip.Y + 6.0f;
		float y = contentStartY - scrollOffset;
		Raylib.BeginScissorMode((int)contentClip.X, (int)contentClip.Y, (int)contentClip.Width, (int)contentClip.Height);
		DrawText(sampleStatus, panel.X + 16.0f, y, 12.0f, new Color(174, 185, 200, 255));
		y += 24.0f;
		if (Button(new Rectangle(panel.X + 16.0f, y - 4.0f, 54.0f, 24.0f), "Up")) {
			result.PreviousSample = true;
		}

		if (Button(new Rectangle(panel.X + 76.0f, y - 4.0f, 64.0f, 24.0f), "Down")) {
			result.NextSample = true;
		}

		if (Button(new Rectangle(panel.X + 146.0f, y - 4.0f, 92.0f, 24.0f), "Folder")) {
			result.OpenFolder = true;
		}

		y += 36.0f;
		DrawSectionSeparator(panel.X + 16.0f, ref y);

		if (parameters is null) {
			DrawText("No particle system loaded.", panel.X + 16.0f, y, 14.0f, new Color(185, 196, 210, 255));
			y += 34.0f;
			Raylib.EndScissorMode();

			float emptyContentHeight = y + scrollOffset - contentStartY + 80.0f;
			UpdateScroll(contentClip, emptyContentHeight);
			DrawScrollbar(contentClip, emptyContentHeight);
			DrawLog(logPanel, logEntries);
			return result;
		}

		DrawSection("Runtime", panel.X + 16.0f, ref y);
		DrawText(system?.LifecycleText() ?? ParticlePreviewSystem.LifecycleText(parameters), panel.X + 16.0f, y, 13.0f, new Color(185, 196, 210, 255));
		y += 24.0f;
		DrawText($"Texture: {textureLabel}", panel.X + 16.0f, y, 13.0f, new Color(185, 196, 210, 255));
		y += 30.0f;

		DrawSection("Main Transform", panel.X + 16.0f, ref y);
		result.MainTransformChanged |= VectorInputs("Origin", panel.X + 16.0f, ref y, ref mainTransformOrigin);
		result.MainTransformChanged |= VectorInputs("Rotation", panel.X + 16.0f, ref y, ref mainTransformRotationDegrees);
		result.MainTransformChanged |= VectorInputs("Scale", panel.X + 16.0f, ref y, ref mainTransformScale);

		DrawSection("Emitter", panel.X + 16.0f, ref y);
		result.Changed |= Slider("Initial", panel.X + 16.0f, ref y, ref parameters.InitialParticleCount, 0.0f, 256.0f);
		result.Changed |= IntSlider("Max", panel.X + 16.0f, ref y, ref parameters.MaxParticleCount, 0, 20000);
		result.Changed |= Slider("Frequency", panel.X + 16.0f, ref y, ref parameters.Frequency, 0.0f, 2000.0f);
		result.Changed |= Slider("Lifetime", panel.X + 16.0f, ref y, ref parameters.Lifetime, 0.0f, 20.0f);
		result.Changed |= VectorSliders("Direction", panel.X + 16.0f, ref y, ref parameters.EmitterDirection, -5.0f, 5.0f);
		result.Changed |= Slider("Nozzle", panel.X + 16.0f, ref y, ref parameters.EmitterNozzleSize, 0.0f, 100.0f);

		DrawSection("Particles", panel.X + 16.0f, ref y);
		result.Changed |= Slider("Part life", panel.X + 16.0f, ref y, ref parameters.ParticleLifetime, 0.0f, 20.0f);
		result.Changed |= Slider("Size", panel.X + 16.0f, ref y, ref parameters.ParticleSize, 0.0f, 500.0f);
		result.Changed |= Slider("Size vel", panel.X + 16.0f, ref y, ref parameters.ParticleSizeVelocity, -1000.0f, 1000.0f);
		result.Changed |= Slider("Velocity", panel.X + 16.0f, ref y, ref parameters.ParticleVelocity, 0.0f, 5000.0f);
		result.Changed |= Slider("Vel random", panel.X + 16.0f, ref y, ref parameters.ParticleVelocityRandomizer, 0.0f, 5.0f);
		result.Changed |= Slider("Pos random", panel.X + 16.0f, ref y, ref parameters.ParticlePositionRandomizer, 0.0f, 1000.0f);
		result.Changed |= VectorSliders("Gravity", panel.X + 16.0f, ref y, ref parameters.Gravity, -5000.0f, 5000.0f);

		DrawSection("Color Key", panel.X + 16.0f, ref y);
		foreach (int frameIndex in GetKeyedColorFrameIndices(parameters)) {
			result.Changed |= ColorFrameRow(parameters, frameIndex, panel.X + 16.0f, ref y, ref result);
		}
		Raylib.EndScissorMode();

		float contentHeight = y + scrollOffset - contentStartY + 96.0f;
		UpdateScroll(contentClip, contentHeight);
		DrawScrollbar(contentClip, contentHeight);
		DrawLog(logPanel, logEntries);

		return result;
	}

	private void DrawHud(string status, string stats) {
		int screenHeight = Raylib.GetScreenHeight();
		float x = 18.0f;
		DrawPanelText(status, x, screenHeight - 72.0f, 14.0f);
		DrawPanelText(stats, x, screenHeight - 38.0f, 14.0f);
	}

	private void DrawPanelText(string text, float x, float y, float fontSize) {
		Vector2 size = Raylib.MeasureTextEx(uiFont.Font, text, fontSize, UiFont.Spacing);
		float maxWidth = Math.Max(120.0f, SampleTreeWidth - x - 10.0f);
		Raylib.DrawRectangleRec(new Rectangle(x - 8.0f, y - 6.0f, Math.Min(size.X + 16.0f, maxWidth), 26.0f), new Color(16, 18, 22, 214));
		DrawText(text, x, y, fontSize, new Color(219, 226, 236, 255));
	}

	private bool Slider(string label, float x, ref float y, ref float value, float min, float max) {
		int sliderId = nextSliderId++;
		Rectangle bar = new(x + 112.0f, y + 8.0f, 160.0f, 7.0f);
		Rectangle hitBox = new(bar.X, bar.Y - 8.0f, bar.Width, 23.0f);
		float oldValue = value;
		DrawText(label, x, y, 13.0f, new Color(219, 226, 236, 255));
		DrawText(Format(value), x + 278.0f, y, 12.0f, new Color(174, 185, 200, 255));
		Raylib.DrawRectangleRec(bar, new Color(48, 57, 70, 255));
		float t = max > min ? (value - min) / (max - min) : 0.0f;
		t = Math.Clamp(t, 0.0f, 1.0f);
		Rectangle fill = bar;
		fill.Width *= t;
		Raylib.DrawRectangleRec(fill, new Color(93, 183, 255, 255));

		Vector2 mouse = Raylib.GetMousePosition();
		bool hot = Raylib.CheckCollisionPointRec(mouse, hitBox);
		if (Raylib.IsMouseButtonPressed(MouseButton.Left) && hot) {
			activeSliderId = sliderId;
		}

		if (Raylib.IsMouseButtonReleased(MouseButton.Left) && activeSliderId == sliderId) {
			activeSliderId = 0;
		}

		if (Raylib.IsMouseButtonDown(MouseButton.Left) && activeSliderId == sliderId) {
			value = min + Math.Clamp((mouse.X - bar.X) / bar.Width, 0.0f, 1.0f) * (max - min);
		}

		y += 25.0f;
		return Math.Abs(value - oldValue) > 0.0001f;
	}

	private bool ColorFrameRow(ParticleParameters parameters, int frameIndex, float x, ref float y, ref ParticleEditorUiResult result) {
		ParticleColorFrame color = parameters.ColorFrames[frameIndex];
		DrawText($"Frame {frameIndex}", x, y, 13.0f, new Color(219, 226, 236, 255));
		Rectangle swatch = new(x + 70.0f, y - 1.0f, 28.0f, 18.0f);
		Raylib.DrawRectangleRec(swatch, new Color(ToByte(color.R), ToByte(color.G), ToByte(color.B), 255));
		Raylib.DrawRectangleLinesEx(swatch, 1.0f, new Color(91, 108, 126, 255));
		if (Button(new Rectangle(x + 106.0f, y - 4.0f, 70.0f, 24.0f), "Color")) {
			result.ColorPickIndex = frameIndex;
		}

		bool alphaChanged = AlphaSlider(x + 190.0f, ref y, ref color.A);
		if (alphaChanged) {
			result.ColorChanged = true;
		}

		parameters.ColorFrames[frameIndex] = color;
		return alphaChanged;
	}

	private bool AlphaSlider(float x, ref float y, ref float value) {
		int sliderId = nextSliderId++;
		Rectangle bar = new(x + 22.0f, y + 8.0f, 108.0f, 7.0f);
		Rectangle hitBox = new(bar.X, bar.Y - 8.0f, bar.Width, 23.0f);
		float oldValue = value;
		DrawText("A", x, y, 13.0f, new Color(219, 226, 236, 255));
		DrawText(Format(value), x + 136.0f, y, 12.0f, new Color(174, 185, 200, 255));
		Raylib.DrawRectangleRec(bar, new Color(48, 57, 70, 255));
		Rectangle fill = bar;
		fill.Width *= Math.Clamp(value, 0.0f, 1.0f);
		Raylib.DrawRectangleRec(fill, new Color(93, 183, 255, 255));

		Vector2 mouse = Raylib.GetMousePosition();
		if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, hitBox)) {
			activeSliderId = sliderId;
		}

		if (Raylib.IsMouseButtonReleased(MouseButton.Left) && activeSliderId == sliderId) {
			activeSliderId = 0;
		}

		if (Raylib.IsMouseButtonDown(MouseButton.Left) && activeSliderId == sliderId) {
			value = Math.Clamp((mouse.X - bar.X) / bar.Width, 0.0f, 1.0f);
		}

		y += 25.0f;
		return Math.Abs(value - oldValue) > 0.0001f;
	}

	private void UpdateScroll(Rectangle clip, float contentHeight) {
		float maxScroll = Math.Max(0.0f, contentHeight - clip.Height);
		Vector2 mouse = Raylib.GetMousePosition();
		if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(clip.X, clip.Y - 42.0f, clip.Width, clip.Height + 42.0f))) {
			float wheel = Raylib.GetMouseWheelMove();
			if (wheel != 0.0f) {
				scrollOffset -= wheel * 42.0f;
			}
		}

		if (Raylib.IsKeyDown(KeyboardKey.Down)) {
			scrollOffset += 6.0f;
		}

		if (Raylib.IsKeyDown(KeyboardKey.Up)) {
			scrollOffset -= 6.0f;
		}

		scrollOffset = Math.Clamp(scrollOffset, 0.0f, maxScroll);
	}

	private void DrawScrollbar(Rectangle clip, float contentHeight) {
		if (contentHeight <= clip.Height) {
			return;
		}

		float trackX = clip.X + clip.Width - 9.0f;
		Rectangle track = new(trackX, clip.Y + 2.0f, 5.0f, clip.Height - 4.0f);
		float thumbHeight = Math.Max(24.0f, track.Height * clip.Height / contentHeight);
		float maxThumbTravel = track.Height - thumbHeight;
		float maxScroll = contentHeight - clip.Height;
		float thumbY = track.Y + maxThumbTravel * (scrollOffset / maxScroll);
		Raylib.DrawRectangleRec(track, new Color(45, 52, 63, 255));
		Raylib.DrawRectangleRec(new Rectangle(track.X, thumbY, track.Width, thumbHeight), new Color(93, 183, 255, 220));
	}

	private void DrawSampleTree(
		Rectangle panel,
		IReadOnlyList<string> sampleFiles,
		string? sampleFolder,
		int currentSampleIndex,
		ref ParticleEditorUiResult result) {
		Raylib.DrawRectangleRec(panel, new Color(18, 22, 28, 245));
		Raylib.DrawLine((int)(panel.X + panel.Width), (int)panel.Y, (int)(panel.X + panel.Width), (int)(panel.Y + panel.Height), new Color(44, 52, 64, 255));
		DrawText("Samples", panel.X + 16.0f, panel.Y + 14.0f, 18.0f, new Color(232, 237, 244, 255));
		Vector2 mouse = Raylib.GetMousePosition();

		string folderName = string.IsNullOrWhiteSpace(sampleFolder)
			? "No folder loaded"
			: Path.GetFileName(sampleFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		DrawText($"v {folderName}", panel.X + 16.0f, panel.Y + 44.0f, 13.0f, new Color(185, 196, 210, 255));

		Rectangle clip = new(panel.X + 8.0f, panel.Y + 70.0f, panel.Width - 16.0f, Math.Max(1.0f, panel.Height - 78.0f));
		float contentHeight = sampleFiles.Count * SampleTreeRowHeight + 8.0f;
		if (Raylib.CheckCollisionPointRec(mouse, panel)) {
			if (Raylib.IsKeyPressed(KeyboardKey.Left)) {
				result.PreviousSample = true;
				EnsureSampleTreeSelectionVisible(clip, contentHeight, currentSampleIndex - 1);
			} else if (Raylib.IsKeyPressed(KeyboardKey.Right)) {
				result.NextSample = true;
				EnsureSampleTreeSelectionVisible(clip, contentHeight, currentSampleIndex + 1);
			}
		}

		UpdateSampleTreeScroll(clip, contentHeight);

		float y = clip.Y - sampleTreeScrollOffset;
		Raylib.BeginScissorMode((int)clip.X, (int)clip.Y, (int)clip.Width, (int)clip.Height);
		for (int index = 0; index < sampleFiles.Count; index++) {
			Rectangle row = new(clip.X + 4.0f, y, clip.Width - 12.0f, SampleTreeRowHeight - 2.0f);
			bool current = index == currentSampleIndex;
			bool hot = Raylib.CheckCollisionPointRec(mouse, row);
			if (current) {
				Raylib.DrawRectangleRec(row, new Color(51, 86, 111, 255));
			} else if (hot) {
				Raylib.DrawRectangleRec(row, new Color(38, 47, 58, 255));
			}

			DrawText(current ? $"> {Path.GetFileName(sampleFiles[index])}" : $"  {Path.GetFileName(sampleFiles[index])}", row.X + 6.0f, row.Y + 4.0f, SampleTreeFontSize, current ? new Color(241, 244, 248, 255) : new Color(174, 185, 200, 255));
			if (hot && Raylib.IsMouseButtonPressed(MouseButton.Left)) {
				result.SelectedSampleIndex = index;
			}

			y += SampleTreeRowHeight;
		}
		Raylib.EndScissorMode();

		DrawScrollbar(clip, contentHeight, sampleTreeScrollOffset);
	}

	private void UpdateSampleTreeScroll(Rectangle clip, float contentHeight) {
		float maxScroll = Math.Max(0.0f, contentHeight - clip.Height);
		Vector2 mouse = Raylib.GetMousePosition();
		if (Raylib.CheckCollisionPointRec(mouse, clip)) {
			float wheel = Raylib.GetMouseWheelMove();
			if (wheel != 0.0f) {
				sampleTreeScrollOffset -= wheel * 34.0f;
			}
		}

		sampleTreeScrollOffset = Math.Clamp(sampleTreeScrollOffset, 0.0f, maxScroll);
	}

	private void EnsureSampleTreeSelectionVisible(Rectangle clip, float contentHeight, int selectedIndex) {
		if (selectedIndex < 0) {
			selectedIndex = 0;
		}

		float maxScroll = Math.Max(0.0f, contentHeight - clip.Height);
		float rowTop = selectedIndex * SampleTreeRowHeight;
		float rowBottom = rowTop + SampleTreeRowHeight;
		if (rowTop < sampleTreeScrollOffset) {
			sampleTreeScrollOffset = rowTop;
		} else if (rowBottom > sampleTreeScrollOffset + clip.Height) {
			sampleTreeScrollOffset = rowBottom - clip.Height;
		}

		sampleTreeScrollOffset = Math.Clamp(sampleTreeScrollOffset, 0.0f, maxScroll);
	}

	private void DrawScrollbar(Rectangle clip, float contentHeight, float currentScrollOffset) {
		if (contentHeight <= clip.Height) {
			return;
		}

		float trackX = clip.X + clip.Width - 5.0f;
		Rectangle track = new(trackX, clip.Y + 2.0f, 4.0f, clip.Height - 4.0f);
		float thumbHeight = Math.Max(24.0f, track.Height * clip.Height / contentHeight);
		float maxThumbTravel = track.Height - thumbHeight;
		float maxScroll = contentHeight - clip.Height;
		float thumbY = track.Y + maxThumbTravel * (currentScrollOffset / maxScroll);
		Raylib.DrawRectangleRec(track, new Color(45, 52, 63, 255));
		Raylib.DrawRectangleRec(new Rectangle(track.X, thumbY, track.Width, thumbHeight), new Color(93, 183, 255, 220));
	}

	private void DrawLog(Rectangle panel, IReadOnlyList<string> logEntries) {
		Raylib.DrawRectangleRec(panel, new Color(13, 16, 20, 246));
		Raylib.DrawLine((int)panel.X, (int)panel.Y, (int)(panel.X + panel.Width), (int)panel.Y, new Color(44, 52, 64, 255));
		DrawText("Debug Log", panel.X + 16.0f, panel.Y + 10.0f, 14.0f, new Color(232, 237, 244, 255));
		int start = Math.Max(0, logEntries.Count - 5);
		float y = panel.Y + 34.0f;
		for (int i = start; i < logEntries.Count; i++) {
			DrawText(logEntries[i], panel.X + 16.0f, y, 11.0f, new Color(174, 185, 200, 255));
			y += 18.0f;
		}
	}

	private bool IntSlider(string label, float x, ref float y, ref int value, int min, int max) {
		float floatValue = value;
		bool changed = Slider(label, x, ref y, ref floatValue, min, max);
		value = (int)MathF.Round(floatValue);
		return changed;
	}

	private bool VectorSliders(string label, float x, ref float y, ref Vector3 vector, float min, float max) {
		bool changed = false;
		changed |= Slider($"{label} X", x, ref y, ref vector.X, min, max);
		changed |= Slider($"{label} Y", x, ref y, ref vector.Y, min, max);
		changed |= Slider($"{label} Z", x, ref y, ref vector.Z, min, max);
		return changed;
	}

	private bool VectorInputs(string label, float x, ref float y, ref Vector3 vector) {
		bool changed = false;
		DrawText(label, x, y, 13.0f, new Color(219, 226, 236, 255));
		changed |= NumericInput($"{label} X", x + 80.0f, y - 4.0f, ref vector.X);
		changed |= NumericInput($"{label} Y", x + 184.0f, y - 4.0f, ref vector.Y);
		changed |= NumericInput($"{label} Z", x + 288.0f, y - 4.0f, ref vector.Z);
		y += 30.0f;
		return changed;
	}

	private bool NumericInput(string label, float x, float y, ref float value) {
		int inputId = nextSliderId++;
		Rectangle rect = new(x, y, 94.0f, 24.0f);
		Vector2 mouse = Raylib.GetMousePosition();
		bool hot = Raylib.CheckCollisionPointRec(mouse, rect);
		bool active = activeTextBoxId == inputId;
		bool changed = false;

		if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
			if (hot) {
				activeTextBoxId = inputId;
				activeTextBoxValue = Format(value);
				active = true;
			} else if (active) {
				changed |= CommitActiveTextBox(ref value);
				activeTextBoxId = 0;
				active = false;
			}
		}

		if (active) {
			ReadTextBoxInput();
			if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && activeTextBoxValue.Length > 0) {
				activeTextBoxValue = activeTextBoxValue[..^1];
			}

			if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter)) {
				changed |= CommitActiveTextBox(ref value);
				activeTextBoxId = 0;
				active = false;
			} else if (TryParseFloat(activeTextBoxValue, out float parsed) && Math.Abs(parsed - value) > 0.0001f) {
				value = parsed;
				changed = true;
			}
		}

		string text = active ? activeTextBoxValue : Format(value);
		Raylib.DrawRectangleRec(rect, active ? new Color(29, 39, 49, 255) : hot ? new Color(39, 48, 59, 255) : new Color(25, 30, 38, 255));
		Raylib.DrawRectangleLinesEx(rect, 1.0f, active ? new Color(143, 215, 255, 255) : new Color(58, 67, 81, 255));
		DrawText(text, rect.X + 6.0f, rect.Y + 6.0f, 12.0f, new Color(241, 244, 248, 255));
		if (active) {
			Vector2 textSize = Raylib.MeasureTextEx(uiFont.Font, text, 12.0f, UiFont.Spacing);
			float caretX = Math.Min(rect.X + rect.Width - 6.0f, rect.X + 6.0f + textSize.X + 1.0f);
			Raylib.DrawLine((int)caretX, (int)(rect.Y + 5.0f), (int)caretX, (int)(rect.Y + rect.Height - 5.0f), new Color(143, 215, 255, 255));
		}

		return changed;
	}

	private void ReadTextBoxInput() {
		int codepoint = Raylib.GetCharPressed();
		while (codepoint > 0) {
			char character = (char)codepoint;
			if (IsNumericInputCharacter(character) && activeTextBoxValue.Length < 16) {
				activeTextBoxValue += character == ',' ? '.' : character;
			}

			codepoint = Raylib.GetCharPressed();
		}
	}

	private bool CommitActiveTextBox(ref float value) {
		if (!TryParseFloat(activeTextBoxValue, out float parsed) || Math.Abs(parsed - value) <= 0.0001f) {
			return false;
		}

		value = parsed;
		return true;
	}

	private static bool IsNumericInputCharacter(char character) {
		return char.IsDigit(character)
			|| character is '-' or '+' or '.' or ',' or 'e' or 'E';
	}

	private static bool TryParseFloat(string text, out float value) {
		return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}

	private bool Button(Rectangle rect, string text) {
		Vector2 mouse = Raylib.GetMousePosition();
		bool hot = Raylib.CheckCollisionPointRec(mouse, rect);
		Raylib.DrawRectangleRec(rect, hot ? new Color(55, 68, 84, 255) : new Color(36, 42, 52, 255));
		Raylib.DrawRectangleLinesEx(rect, 1.0f, hot ? new Color(143, 215, 255, 255) : new Color(58, 67, 81, 255));
		Vector2 textSize = Raylib.MeasureTextEx(uiFont.Font, text, 13.0f, UiFont.Spacing);
		DrawText(text, rect.X + (rect.Width - textSize.X) * 0.5f, rect.Y + 7.0f, 13.0f, new Color(241, 244, 248, 255));
		return hot && Raylib.IsMouseButtonPressed(MouseButton.Left);
	}

	private static IEnumerable<int> GetKeyedColorFrameIndices(ParticleParameters parameters) {
		return ParticleColorFrameInterpolator.GetKeyedIndices(parameters);
	}

	private void DrawSection(string title, float x, ref float y) {
		Raylib.DrawLine((int)x, (int)y, (int)x + 314, (int)y, new Color(40, 48, 59, 255));
		y += 10.0f;
		DrawText(title.ToUpperInvariant(), x, y, 12.0f, new Color(174, 185, 200, 255));
		y += 22.0f;
	}

	private static void DrawSectionSeparator(float x, ref float y) {
		Raylib.DrawLine((int)x, (int)y, (int)x + 314, (int)y, new Color(40, 48, 59, 255));
		y += 14.0f;
	}

	private void DrawText(string text, float x, float y, float fontSize, Color color) {
		Raylib.DrawTextEx(uiFont.Font, text, new Vector2(x, y), fontSize, UiFont.Spacing, color);
	}

	private static string Format(float value) {
		return value.ToString(MathF.Round(value) == value ? "0" : "0.###", CultureInfo.InvariantCulture);
	}

	private static int ToByte(float value) {
		return (int)(Math.Clamp(value, 0.0f, 1.0f) * 255.0f);
	}
}

internal struct ParticleEditorUiResult {
	public bool Restart;
	public bool Frame;
	public bool OpenFile;
	public bool OpenFolder;
	public bool ReloadSample;
	public bool PreviousSample;
	public bool NextSample;
	public bool Changed;
	public bool ColorChanged;
	public bool MainTransformChanged;
	public int? SelectedSampleIndex;
	public int? ColorPickIndex;
}

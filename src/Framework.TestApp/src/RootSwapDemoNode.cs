using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp;

internal sealed class RootSwapDemoNode : Node {
	private float _elapsed;

	public RootSwapDemoNode() : base("RootSwapDemoRoot") {
	}

	protected override void OnUpdate(float deltaTime) {
		_elapsed += deltaTime;

		if (Input.IsExitRequested()) {
			RequestQuit();
			return;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Tab) || Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space)) {
			Tree.ChangeRoot(new ShowcaseShellNode());
		}
	}

	protected override void OnDraw() {
		Raylib.ClearBackground(new Color(12, 18, 30, 255));

		var center = new Vector2(640f, 360f);
		var orbitRadius = 120f + (MathF.Sin(_elapsed * 1.2f) * 28f);
		var accent = new Color(255, 213, 92, 255);
		var secondary = new Color(96, 176, 255, 255);

		for (var i = 0; i < 3; i++) {
			var angle = _elapsed * (0.7f + (i * 0.2f)) + (i * MathF.Tau / 3f);
			var point = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * orbitRadius;
			Raylib.DrawCircleV(point, 18f + (i * 4f), i == 0 ? accent : secondary);
			Raylib.DrawLineEx(center, point, 3f, new Color(88, 108, 144, 255));
		}

		UiText.Draw("SceneTree Root Swap", 420f, 168f, 40f, new Color(244, 247, 252, 255), UiTextStyle.Title);
		UiText.Draw("This node is a different scene root. The previous showcase root was removed from the active tree and disposed.", 318f, 244f, 20f, new Color(198, 210, 228, 255));
		UiText.Draw("Press Tab, Enter, or Space to rebuild the showcase root and swap back.", 338f, 282f, 20f, accent);
		UiText.Draw("Press Escape or your exit gesture to close the app from this root.", 374f, 316f, 18f, new Color(176, 190, 212, 255));

		DrawInfoChip($"Elapsed: {_elapsed:0.00}s", 514f, 430f);
		DrawInfoChip("Active root: RootSwapDemoNode", 514f, 466f);
		DrawInfoChip("Tree.ChangeRoot(new ShowcaseShellNode()) returns to the menu root.", 514f, 502f);
	}

	private static void DrawInfoChip(string text, float x, float y) {
		var size = UiText.MeasureSize(text, 17f, UiTextStyle.Body);
		var bounds = new Rectangle(x - 8f, y - 4f, size.X + 16f, size.Y + 8f);
		Raylib.DrawRectangleRec(bounds, new Color(10, 14, 22, 224));
		Raylib.DrawRectangleLinesEx(bounds, 1f, new Color(88, 108, 144, 255));
		UiText.Draw(text, x, y, 17f, new Color(218, 226, 238, 255), UiTextStyle.Body);
	}
}

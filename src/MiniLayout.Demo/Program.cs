using ConquestFrontierWarsRay.MiniLayout;
using Raylib_cs;

namespace ConquestFrontierWarsRay.MiniLayout.Demo;

internal static class Program {
	private static readonly Color BackgroundColor = new(24, 28, 36, 255);
	private static readonly Color ContainerColor = new(52, 73, 94, 255);
	private static readonly Color InnerColor = new(34, 45, 58, 255);
	private static readonly Color OutlineColor = new(190, 205, 224, 255);
	private static readonly Color MarginColor = new(230, 126, 34, 80);
	private static readonly Color[] BoxColors = [
		new Color(52, 152, 219, 255),
		new Color(46, 204, 113, 255),
		new Color(155, 89, 182, 255)
	];

	private static void Main() {
		Raylib.SetConfigFlags(ConfigFlags.VSyncHint | ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
		Raylib.InitWindow(1280, 720, "MiniLayout Demo");
		Raylib.SetTargetFPS(60);

		var engine = new BoxLayoutEngine();
		var style = new BoxLayoutStyle {
			Direction = LayoutDirection.Row,
			JustifyContent = LayoutAlignment.Center,
			AlignItems = LayoutAlignment.Center,
			Padding = new Thickness(32f, 32f, 32f, 32f),
			Gap = 16f
		};
		var children = new[] {
			new LayoutNode("Navigation", new LayoutSize(180f, 90f), new Thickness(12f, 10f, 8f, 10f)),
			new LayoutNode("Content", new LayoutSize(260f, 130f), new Thickness(8f, 12f, 8f, 12f)),
			new LayoutNode("Actions", new LayoutSize(160f, 72f), new Thickness(10f, 14f, 10f, 6f))
		};

		try {
			while (!Raylib.WindowShouldClose()) {
				HandleInput(style);

				var container = new LayoutRect(120f, 120f, Raylib.GetScreenWidth() - 240f, Raylib.GetScreenHeight() - 240f);
				var result = engine.Arrange(container, style, children);

				Raylib.BeginDrawing();
				Raylib.ClearBackground(BackgroundColor);

				DrawBackgroundGuides(result.Bounds, style);
				DrawChildBoxes(result, style, children);
				DrawLegend(style);

				Raylib.EndDrawing();
			}
		} finally {
			Raylib.CloseWindow();
		}
	}

	private static void HandleInput(BoxLayoutStyle style) {
		if (Raylib.IsKeyPressed(KeyboardKey.R)) {
			style.Direction = LayoutDirection.Row;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.C)) {
			style.Direction = LayoutDirection.Column;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.One)) {
			style.JustifyContent = LayoutAlignment.Start;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Two)) {
			style.JustifyContent = LayoutAlignment.Center;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Three)) {
			style.JustifyContent = LayoutAlignment.End;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Four)) {
			style.AlignItems = LayoutAlignment.Start;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Five)) {
			style.AlignItems = LayoutAlignment.Center;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Six)) {
			style.AlignItems = LayoutAlignment.End;
		}
	}

	private static void DrawBackgroundGuides(LayoutRect bounds, BoxLayoutStyle style) {
		var inner = new LayoutRect(
			bounds.X + style.Padding.Left,
			bounds.Y + style.Padding.Top,
			MathF.Max(0f, bounds.Width - style.Padding.Horizontal),
			MathF.Max(0f, bounds.Height - style.Padding.Vertical));

		DrawFilledRect(bounds, ContainerColor);
		DrawRectOutline(bounds, OutlineColor, 2);
		DrawFilledRect(inner, InnerColor);
		DrawRectOutline(inner, new Color(120, 140, 160, 255), 1);
	}

	private static void DrawChildBoxes(BoxLayoutResult result, BoxLayoutStyle style, IReadOnlyList<LayoutNode> children) {
		for (var i = 0; i < children.Count; i++) {
			var child = children[i];
			var rect = result.Children[child.Name];
			var outerRect = new LayoutRect(
				rect.X - child.Margin.Left,
				rect.Y - child.Margin.Top,
				rect.Width + child.Margin.Horizontal,
				rect.Height + child.Margin.Vertical);

			DrawFilledRect(outerRect, MarginColor);
			DrawRectOutline(outerRect, new Color(243, 156, 18, 180), 1);
			DrawFilledRect(rect, BoxColors[i % BoxColors.Length]);
			DrawRectOutline(rect, Color.White, 2);

			var label = $"{child.Name}  {rect.Width:0}x{rect.Height:0}";
			Raylib.DrawText(label, (int)rect.X + 10, (int)rect.Y + 10, 20, Color.White);
		}

		var axisLabel = style.Direction == LayoutDirection.Row ? "Main Axis: Horizontal" : "Main Axis: Vertical";
		Raylib.DrawText(axisLabel, 120, Raylib.GetScreenHeight() - 96, 22, new Color(220, 230, 240, 255));
	}

	private static void DrawLegend(BoxLayoutStyle style) {
		var x = 120;
		var y = 36;
		var color = new Color(220, 230, 240, 255);

		Raylib.DrawText("MiniLayout Demo", x, y, 28, Color.White);
		Raylib.DrawText($"Direction: {style.Direction}", x, y + 40, 20, color);
		Raylib.DrawText($"JustifyContent: {style.JustifyContent}", x, y + 66, 20, color);
		Raylib.DrawText($"AlignItems: {style.AlignItems}", x, y + 92, 20, color);
		Raylib.DrawText("R Row   C Column", x + 420, y + 40, 20, color);
		Raylib.DrawText("1 Start   2 Center   3 End", x + 420, y + 66, 20, color);
		Raylib.DrawText("4 Start   5 Center   6 End", x + 420, y + 92, 20, color);
		Raylib.DrawText("Orange area = margins, dark slate = container padding area", x, y + 126, 18, new Color(180, 190, 205, 255));
	}

	private static void DrawFilledRect(LayoutRect rect, Color color) {
		Raylib.DrawRectangleRec(ToRectangle(rect), color);
	}

	private static void DrawRectOutline(LayoutRect rect, Color color, int thickness) {
		Raylib.DrawRectangleLinesEx(ToRectangle(rect), thickness, color);
	}

	private static Rectangle ToRectangle(LayoutRect rect) {
		return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
	}
}

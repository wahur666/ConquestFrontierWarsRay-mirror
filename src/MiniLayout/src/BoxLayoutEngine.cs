namespace ConquestFrontierWarsRay.MiniLayout;

public sealed class BoxLayoutEngine {
	public BoxLayoutResult Arrange(
		LayoutRect bounds,
		BoxLayoutStyle style,
		IReadOnlyList<LayoutNode> children) {
		ArgumentNullException.ThrowIfNull(style);
		ArgumentNullException.ThrowIfNull(children);

		var innerX = bounds.X + style.Padding.Left;
		var innerY = bounds.Y + style.Padding.Top;
		var innerWidth = MathF.Max(0f, bounds.Width - style.Padding.Horizontal);
		var innerHeight = MathF.Max(0f, bounds.Height - style.Padding.Vertical);

		var isRow = style.Direction == LayoutDirection.Row;
		var availableMain = isRow ? innerWidth : innerHeight;
		var availableCross = isRow ? innerHeight : innerWidth;
		var totalGap = children.Count > 1 ? style.Gap * (children.Count - 1) : 0f;

		var totalOuterMain = totalGap;

		foreach (var child in children) {
			totalOuterMain += GetOuterMainSize(child, isRow);
		}

		var mainCursor = GetMainStartOffset(style.JustifyContent, availableMain, totalOuterMain);
		var childBounds = new Dictionary<string, LayoutRect>(children.Count, StringComparer.Ordinal);

		foreach (var child in children) {
			var childMain = isRow ? child.Size.Width : child.Size.Height;
			var childCross = isRow ? child.Size.Height : child.Size.Width;
			var marginMainStart = isRow ? child.Margin.Left : child.Margin.Top;
			var marginMainEnd = isRow ? child.Margin.Right : child.Margin.Bottom;
			var marginCrossStart = isRow ? child.Margin.Top : child.Margin.Left;
			var marginCrossEnd = isRow ? child.Margin.Bottom : child.Margin.Right;

			var outerCross = childCross + marginCrossStart + marginCrossEnd;
			var crossCursor = GetCrossStartOffset(style.AlignItems, availableCross, outerCross) + marginCrossStart;
			var x = isRow ? innerX + mainCursor + marginMainStart : innerX + crossCursor;
			var y = isRow ? innerY + crossCursor : innerY + mainCursor + marginMainStart;

			childBounds.Add(child.Name, new LayoutRect(x, y, child.Size.Width, child.Size.Height));
			mainCursor += marginMainStart + childMain + marginMainEnd + style.Gap;
		}

		return new BoxLayoutResult(bounds, childBounds);
	}

	private static float GetOuterMainSize(LayoutNode child, bool isRow) {
		return isRow
			? child.Size.Width + child.Margin.Left + child.Margin.Right
			: child.Size.Height + child.Margin.Top + child.Margin.Bottom;
	}

	private static float GetMainStartOffset(LayoutAlignment alignment, float availableMain, float usedMain) {
		var freeSpace = MathF.Max(0f, availableMain - usedMain);

		return alignment switch {
			LayoutAlignment.Start => 0f,
			LayoutAlignment.Center => freeSpace / 2f,
			LayoutAlignment.End => freeSpace,
			_ => 0f
		};
	}

	private static float GetCrossStartOffset(LayoutAlignment alignment, float availableCross, float outerCross) {
		var freeSpace = MathF.Max(0f, availableCross - outerCross);

		return alignment switch {
			LayoutAlignment.Start => 0f,
			LayoutAlignment.Center => freeSpace / 2f,
			LayoutAlignment.End => freeSpace,
			_ => 0f
		};
	}
}

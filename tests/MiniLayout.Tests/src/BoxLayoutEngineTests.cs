using ConquestFrontierWarsRay.MiniLayout;

namespace ConquestFrontierWarsRay.MiniLayout.Tests;

public sealed class BoxLayoutEngineTests {
	[Fact]
	public void RowLayout_PlacesChildrenFromStart_WithMarginsAndGap() {
		var engine = new BoxLayoutEngine();
		var style = new BoxLayoutStyle {
			Direction = LayoutDirection.Row,
			JustifyContent = LayoutAlignment.Start,
			AlignItems = LayoutAlignment.Start,
			Gap = 10f
		};
		var children = new[] {
			new LayoutNode("first", new LayoutSize(100f, 40f), new Thickness(5f, 6f, 7f, 8f)),
			new LayoutNode("second", new LayoutSize(50f, 20f), new Thickness(3f, 4f, 2f, 1f))
		};

		var result = engine.Arrange(new LayoutRect(0f, 0f, 400f, 200f), style, children);

		AssertRectEqual(new LayoutRect(5f, 6f, 100f, 40f), result.Children["first"]);
		AssertRectEqual(new LayoutRect(125f, 4f, 50f, 20f), result.Children["second"]);
	}

	[Fact]
	public void RowLayout_CentersSingleChild_OnBothAxes() {
		var engine = new BoxLayoutEngine();
		var style = new BoxLayoutStyle {
			Direction = LayoutDirection.Row,
			JustifyContent = LayoutAlignment.Center,
			AlignItems = LayoutAlignment.Center
		};
		var children = new[] {
			new LayoutNode("centered", new LayoutSize(100f, 40f), Thickness.Zero)
		};

		var result = engine.Arrange(new LayoutRect(0f, 0f, 400f, 200f), style, children);

		AssertRectEqual(new LayoutRect(150f, 80f, 100f, 40f), result.Children["centered"]);
	}

	[Fact]
	public void ColumnLayout_AlignsChildrenToEndAxis() {
		var engine = new BoxLayoutEngine();
		var style = new BoxLayoutStyle {
			Direction = LayoutDirection.Column,
			JustifyContent = LayoutAlignment.Start,
			AlignItems = LayoutAlignment.End
		};
		var children = new[] {
			new LayoutNode("item", new LayoutSize(60f, 30f), new Thickness(4f, 5f, 6f, 7f))
		};

		var result = engine.Arrange(new LayoutRect(10f, 20f, 300f, 150f), style, children);

		AssertRectEqual(new LayoutRect(244f, 25f, 60f, 30f), result.Children["item"]);
	}

	[Fact]
	public void Padding_ReducesAvailableSpace_ForCentering() {
		var engine = new BoxLayoutEngine();
		var style = new BoxLayoutStyle {
			Direction = LayoutDirection.Row,
			JustifyContent = LayoutAlignment.Center,
			AlignItems = LayoutAlignment.Center,
			Padding = new Thickness(20f, 10f, 20f, 10f)
		};
		var children = new[] {
			new LayoutNode("item", new LayoutSize(80f, 40f), Thickness.Zero)
		};

		var result = engine.Arrange(new LayoutRect(0f, 0f, 300f, 120f), style, children);

		AssertRectEqual(new LayoutRect(110f, 40f, 80f, 40f), result.Children["item"]);
	}

	[Fact]
	public void RowLayout_CentersGroup_NotEachChildIndividually() {
		var engine = new BoxLayoutEngine();
		var style = new BoxLayoutStyle {
			Direction = LayoutDirection.Row,
			JustifyContent = LayoutAlignment.Center,
			AlignItems = LayoutAlignment.Start,
			Gap = 10f
		};
		var children = new[] {
			new LayoutNode("left", new LayoutSize(40f, 20f), Thickness.Zero),
			new LayoutNode("right", new LayoutSize(60f, 20f), Thickness.Zero)
		};

		var result = engine.Arrange(new LayoutRect(0f, 0f, 200f, 80f), style, children);

		AssertRectEqual(new LayoutRect(45f, 0f, 40f, 20f), result.Children["left"]);
		AssertRectEqual(new LayoutRect(95f, 0f, 60f, 20f), result.Children["right"]);
	}

	private static void AssertRectEqual(LayoutRect expected, LayoutRect actual, int precision = 4) {
		Assert.Equal(expected.X, actual.X, precision);
		Assert.Equal(expected.Y, actual.Y, precision);
		Assert.Equal(expected.Width, actual.Width, precision);
		Assert.Equal(expected.Height, actual.Height, precision);
	}
}

using System.Numerics;
using System.Text;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class DebugTreeViewTests {
	[Fact]
	public void Print_IncludesHierarchyTransformVisibilityAndDrawOrderState() {
		var root = new Node2D("Root") {
			Position = new Vector2(10f, 20f),
			Rotation = 0.5f,
			Scale = new Vector2(2f, 3f),
			ZIndex = 4,
			YSortEnabled = true
		};
		root.AddChild(new Node2D("Child") {
			Position = new Vector2(5f, 0f),
			Visible = false,
			ZIndex = 2
		});

		var builder = new StringBuilder();
		using var writer = new StringWriter(builder);

		DebugTreeView.Print(root, writer, "Test tree:");

		var output = builder.ToString();

		Assert.Contains("Test tree:", output);
		Assert.Contains("- Root (Node2D)", output);
		Assert.Contains("local=pos=(10, 20) rot=0.5 scale=(2, 3)", output);
		Assert.Contains("global=pos=(10, 20) rot=0.5 scale=(2, 3)", output);
		Assert.Contains("visible=True", output);
		Assert.Contains("z=4", output);
		Assert.Contains("globalZ=4", output);
		Assert.Contains("ySort=True", output);
		Assert.Contains("  - Child (Node2D)", output);
		Assert.Contains("visible=False", output);
		Assert.Contains("globalZ=6", output);
	}
}

using System.Numerics;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class Node2DTests {
	[Fact]
	public void LocalTransform_CombinesScaleRotationAndTranslation() {
		var node = new Node2D("Node") {
			Position = new Vector2(10f, 20f),
			Rotation = MathF.PI / 2f,
			Scale = new Vector2(2f, 3f)
		};

		var expected =
			Matrix3x2.CreateScale(node.Scale) *
			Matrix3x2.CreateRotation(node.Rotation) *
			Matrix3x2.CreateTranslation(node.Position);

		AssertMatrixEqual(expected, node.LocalTransform);
	}

	[Fact]
	public void GlobalProperties_ComposeAcrossParents() {
		var parent = new Node2D("Parent") {
			Position = new Vector2(10f, 0f),
			Rotation = MathF.PI / 2f,
			Scale = new Vector2(2f, 3f)
		};
		var child = parent.AddChild(new Node2D("Child") {
			Position = new Vector2(5f, 0f),
			Rotation = MathF.PI / 4f,
			Scale = new Vector2(4f, 5f)
		});

		var expectedGlobalTransform = child.LocalTransform * parent.GlobalTransform;

		AssertMatrixEqual(expectedGlobalTransform, child.GlobalTransform);
		AssertVectorEqual(Vector2.Transform(Vector2.Zero, expectedGlobalTransform), child.GlobalPosition);
		Assert.Equal(MathF.Atan2(expectedGlobalTransform.M12, expectedGlobalTransform.M11), child.GlobalRotation, 4);

		var expectedXScale = new Vector2(expectedGlobalTransform.M11, expectedGlobalTransform.M12).Length();
		var expectedYScale = new Vector2(expectedGlobalTransform.M21, expectedGlobalTransform.M22).Length();
		AssertVectorEqual(new Vector2(expectedXScale, expectedYScale), child.GlobalScale);
	}

	[Fact]
	public void GlobalZIndex_ComposesAcrossCanvasParents() {
		var root = new Node2D("Root") {
			ZIndex = 5
		};
		var relativeChild = root.AddChild(new Node2D("RelativeChild") {
			ZIndex = 3,
			ZAsRelative = true
		});
		var absoluteChild = root.AddChild(new Node2D("AbsoluteChild") {
			ZIndex = 3,
			ZAsRelative = false
		});

		Assert.Equal(8, relativeChild.GlobalZIndex);
		Assert.Equal(3, absoluteChild.GlobalZIndex);
	}

	[Fact]
	public void DrawRecursive_SkipsInvisibleCanvasSubtrees() {
		var root = new TestCanvasItem("Root");
		var visibleChild = root.AddChild(new TestCanvasItem("VisibleChild"));
		var hiddenChild = root.AddChild(new TestCanvasItem("HiddenChild") {
			Visible = false
		});
		var hiddenGrandchild = hiddenChild.AddChild(new TestCanvasItem("HiddenGrandchild"));

		root.DrawRecursive();

		Assert.Equal(1, root.DrawCount);
		Assert.Equal(1, visibleChild.DrawCount);
		Assert.Equal(0, hiddenChild.DrawCount);
		Assert.Equal(0, hiddenGrandchild.DrawCount);
	}

	[Fact]
	public void DrawRecursive_OrdersCanvasChildrenByGlobalZIndex() {
		var drawOrder = new List<string>();
		var root = new TestCanvasItem("Root") {
			DrawSink = drawOrder
		};

		root.AddChild(new TestCanvasItem("Mid") {
			ZIndex = 5,
			DrawSink = drawOrder
		});
		root.AddChild(new TestCanvasItem("Front") {
			ZIndex = 10,
			DrawSink = drawOrder
		});
		root.AddChild(new TestCanvasItem("Back") {
			ZIndex = -2,
			DrawSink = drawOrder
		});

		root.DrawRecursive();

		Assert.Equal(["Root", "Back", "Mid", "Front"], drawOrder);
	}

	[Fact]
	public void DrawRecursive_CanSortChildrenByYWithinSameZIndex() {
		var drawOrder = new List<string>();
		var root = new TestNode2D("Root") {
			YSortEnabled = true,
			DrawSink = drawOrder
		};

		root.AddChild(new TestNode2D("Low") {
			Position = new Vector2(0f, 100f),
			DrawSink = drawOrder
		});
		root.AddChild(new TestNode2D("High") {
			Position = new Vector2(0f, 20f),
			DrawSink = drawOrder
		});

		root.DrawRecursive();

		Assert.Equal(["Root", "High", "Low"], drawOrder);
	}

	private static void AssertMatrixEqual(Matrix3x2 expected, Matrix3x2 actual, int precision = 4) {
		Assert.Equal(expected.M11, actual.M11, precision);
		Assert.Equal(expected.M12, actual.M12, precision);
		Assert.Equal(expected.M21, actual.M21, precision);
		Assert.Equal(expected.M22, actual.M22, precision);
		Assert.Equal(expected.M31, actual.M31, precision);
		Assert.Equal(expected.M32, actual.M32, precision);
	}

	private static void AssertVectorEqual(Vector2 expected, Vector2 actual, int precision = 4) {
		Assert.Equal(expected.X, actual.X, precision);
		Assert.Equal(expected.Y, actual.Y, precision);
	}

	private sealed class TestCanvasItem(string? name = null) : CanvasItem(name) {
		public int DrawCount { get; private set; }
		public List<string>? DrawSink { get; init; }

		protected override void OnDraw() {
			DrawCount++;
			DrawSink?.Add(Name);
		}
	}

	private sealed class TestNode2D(string? name = null) : Node2D(name) {
		public List<string>? DrawSink { get; init; }

		protected override void Draw() {
			DrawSink?.Add(Name);
		}
	}
}

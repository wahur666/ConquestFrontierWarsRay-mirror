using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class LegacyMenuRootTests {
	[Fact]
	public void RecalculateViewport_UsesBaseResolutionAndCentersScaledContent() {
		var root = new LegacyMenuRoot();

		root.RecalculateViewport(new Vector2(1920f, 1080f));

		Assert.Equal(new Vector2(800f, 600f), root.BaseResolution);
		Assert.Equal(1.8f, root.UniformScale, 4);
		Assert.Equal(new Vector2(240f, 0f), root.Position);
		Assert.Equal(new Vector2(1.8f, 1.8f), root.Scale);
		AssertRectangleEqual(new Rectangle(240f, 0f, 1440f, 1080f), root.ContentViewport);
		Assert.Collection(
			root.BlackBarRects,
			rect => AssertRectangleEqual(new Rectangle(0f, 0f, 240f, 1080f), rect),
			rect => AssertRectangleEqual(new Rectangle(1680f, 0f, 240f, 1080f), rect));
	}

	[Fact]
	public void RecalculateViewport_AddsLetterboxBarsWhenWindowIsTallerThanContent() {
		var root = new LegacyMenuRoot();

		root.RecalculateViewport(new Vector2(800f, 800f));

		Assert.Equal(1f, root.UniformScale, 4);
		Assert.Equal(new Vector2(0f, 100f), root.Position);
		AssertRectangleEqual(new Rectangle(0f, 100f, 800f, 600f), root.ContentViewport);
		Assert.Collection(
			root.BlackBarRects,
			rect => AssertRectangleEqual(new Rectangle(0f, 0f, 800f, 100f), rect),
			rect => AssertRectangleEqual(new Rectangle(0f, 700f, 800f, 100f), rect));
	}

	[Fact]
	public void SetContentRoot_HostsOneAuthoredSubtreeWithoutRelayout() {
		var root = new LegacyMenuRoot();
		var content = new Node2D("OpeningScreen");
		var child = content.AddChild(new Node2D("AuthoredChild") {
			Position = new Vector2(123f, 45f)
		});

		root.SetContentRoot(content);
		root.RecalculateViewport(new Vector2(1600f, 900f));

		Assert.Same(content, Assert.Single(root.Children));
		Assert.Equal(new Vector2(123f, 45f), child.Position);
		Assert.Equal(new Vector2(200f, 0f), content.GlobalPosition);
		Assert.Equal(new Vector2(384.5f, 67.5f), child.GlobalPosition);
	}

	[Fact]
	public void SetContentRoot_ReplacesPreviousHostedSubtree() {
		var root = new LegacyMenuRoot();
		var first = new Node2D("First");
		var second = new Node2D("Second");

		root.SetContentRoot(first);
		root.SetContentRoot(second);

		Assert.Same(second, Assert.Single(root.Children));
		Assert.Null(first.Parent);
	}

	private static void AssertRectangleEqual(Rectangle expected, Rectangle actual, int precision = 4) {
		Assert.Equal(expected.X, actual.X, precision);
		Assert.Equal(expected.Y, actual.Y, precision);
		Assert.Equal(expected.Width, actual.Width, precision);
		Assert.Equal(expected.Height, actual.Height, precision);
	}
}

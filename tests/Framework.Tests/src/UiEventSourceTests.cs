using System.Numerics;
using System.Reflection;
using ConquestFrontierWarsRay.Core.UI;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class UiEventSourceTests {
	[Fact]
	public void ResolveTarget_PrefersChildOverParentAtSharedPoint() {
		var root = new Node("Root");
		var parent = root.AddChild(new HotRectNode("Parent") {
			Size = new Vector2(200f, 200f)
		});
		parent.AddChild(new HotRectNode("Child") {
			Position = new Vector2(120f, 0f),
			Size = new Vector2(80f, 200f)
		});

		var eventSource = new UiEventSource {
			ScopeRoot = root
		};

		var target = ResolveTarget(eventSource, new Vector2(150f, 50f));
		var node = GetPointerTargetNode(target);

		Assert.Same(parent.Children[0], node);
	}

	[Fact]
	public void ResolveTarget_IgnoresControlsUnderInvisibleCanvasParent() {
		var root = new Node("Root");
		var visible = root.AddChild(new HotRectNode("Visible") {
			Size = new Vector2(200f, 200f)
		});
		var hiddenParent = root.AddChild(new Node2D("HiddenParent") {
			Visible = false
		});
		hiddenParent.AddChild(new HotRectNode("HiddenChild") {
			Size = new Vector2(200f, 200f)
		});

		var eventSource = new UiEventSource {
			ScopeRoot = root
		};

		var target = ResolveTarget(eventSource, new Vector2(50f, 50f));
		var node = GetPointerTargetNode(target);

		Assert.Same(visible, node);
	}

	private static object? ResolveTarget(UiEventSource eventSource, Vector2 pointerPosition) {
		var method = typeof(UiEventSource).GetMethod("ResolveTarget", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(method);
		return method!.Invoke(eventSource, [pointerPosition]);
	}

	private static Node GetPointerTargetNode(object? pointerTarget) {
		Assert.NotNull(pointerTarget);
		var property = pointerTarget!.GetType().GetProperty("Node", BindingFlags.Instance | BindingFlags.Public);
		Assert.NotNull(property);
		return Assert.IsAssignableFrom<Node>(property!.GetValue(pointerTarget));
	}
}

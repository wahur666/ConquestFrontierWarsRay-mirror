using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class NodeTests {
	[Fact]
	public void Constructor_UsesTypeName_WhenNameIsMissing() {
		var node = new Node();

		Assert.Equal(nameof(Node), node.Name);
		Assert.Null(node.Parent);
		Assert.Empty(node.Children);
		Assert.False(node.IsInitialized);
		Assert.False(node.IsInTree);
	}

	[Fact]
	public void Constructor_UsesExplicitName() {
		var node = new Node("Root");

		Assert.Equal("Root", node.Name);
	}

	[Fact]
	public void AddChild_AttachesParentAndChild() {
		var parent = new Node("Parent");
		var child = new Node("Child");

		var addedChild = parent.AddChild(child);

		Assert.Same(child, addedChild);
		Assert.Same(parent, child.Parent);
		Assert.Single(parent.Children);
		Assert.Same(child, parent.Children[0]);
	}

	[Fact]
	public void AddChild_RejectsSelfReference() {
		var node = new Node("Node");

		var exception = Assert.Throws<InvalidOperationException>(() => node.AddChild(node));

		Assert.Contains("cannot be added as a child of itself", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void AddChild_RejectsExistingParent() {
		var root = new Node("Root");
		var otherParent = new Node("OtherParent");
		var child = new Node("Child");

		root.AddChild(child);

		var exception = Assert.Throws<InvalidOperationException>(() => otherParent.AddChild(child));

		Assert.Contains("already has a parent", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void AddChild_RejectsCyclesThroughDescendants() {
		var root = new Node("Root");
		var child = root.AddChild(new Node("Child"));
		var grandChild = child.AddChild(new Node("GrandChild"));

		var exception = Assert.Throws<InvalidOperationException>(() => grandChild.AddChild(root));

		Assert.Contains("would create a cycle", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void RemoveChild_DetachesDirectChild() {
		var parent = new Node("Parent");
		var child = parent.AddChild(new Node("Child"));

		var removed = parent.RemoveChild(child);

		Assert.True(removed);
		Assert.Null(child.Parent);
		Assert.Empty(parent.Children);
	}

	[Fact]
	public void RemoveChild_ReturnsFalse_ForNonChild() {
		var parent = new Node("Parent");
		var child = new Node("Child");

		Assert.False(parent.RemoveChild(child));
	}

	[Fact]
	public void RecursiveLifecycle_RunsInExpectedOrder() {
		var root = new TestNode("Root");
		var child = root.AddChild(new TestNode("Child"));
		var grandChild = child.AddChild(new TestNode("GrandChild"));
		var shared = new SharedContext();
		var state = new SharedNodeState("Ready");
		shared.Set(state);
		var tree = new SceneTree(root, new InputManager(), shared);

		root.AttachContextRecursive(new NodeContext(tree, shared));
		root.InitializeRecursive();
		root.EnterTreeRecursive();
		root.UpdateRecursive(0.25f);
		root.DrawRecursive();
		root.ExitTreeRecursive();
		grandChild.ExposedRequestQuit();

		Assert.Same(tree, grandChild.ExposedTree);
		Assert.Same(tree.Input, grandChild.ExposedInput);
		Assert.Same(shared, grandChild.ExposedShared);
		Assert.Same(state, grandChild.ExposedShared.GetRequired<SharedNodeState>());
		Assert.True(tree.IsQuitRequested);
		Assert.Equal(["Root:Initialize", "Root:EnterTree", "Root:ExitTree"], root.LifecycleEvents);
		Assert.Equal(["Child:Initialize", "Child:EnterTree", "Child:ExitTree"], child.LifecycleEvents);
		Assert.Equal(["GrandChild:Initialize", "GrandChild:EnterTree", "GrandChild:ExitTree"], grandChild.LifecycleEvents);
		Assert.Equal([0.25f], root.UpdateDeltas);
		Assert.Equal([0.25f], child.UpdateDeltas);
		Assert.Equal([0.25f], grandChild.UpdateDeltas);
		Assert.Equal(1, root.DrawCount);
		Assert.Equal(1, child.DrawCount);
		Assert.Equal(1, grandChild.DrawCount);
	}

	[Fact]
	public void InputAndQuitApis_ThrowWithoutContext() {
		var node = new TestNode("Node");

		Assert.Throws<InvalidOperationException>(() => _ = node.ExposedInput);
		Assert.Throws<InvalidOperationException>(() => _ = node.ExposedShared);
		Assert.Throws<InvalidOperationException>(() => node.ExposedRequestQuit());
	}

	[Fact]
	public void Dispose_DisposesChildrenAndIsIdempotent() {
		var root = new Node("Root");
		var child = root.AddChild(new Node("Child"));

		root.Dispose();
		root.Dispose();

		Assert.Null(root.Parent);
		Assert.Null(child.Parent);
		Assert.Empty(root.Children);
	}

	private sealed record SharedNodeState(string Status);
}

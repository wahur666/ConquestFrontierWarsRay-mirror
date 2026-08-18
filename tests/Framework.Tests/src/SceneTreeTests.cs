using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class SceneTreeTests {
	[Fact]
	public void StartUpdateDrawStop_RunsWholeTreeLifecycle() {
		var root = new TestNode("Root");
		var child = root.AddChild(new TestNode("Child"));
		var grandChild = child.AddChild(new TestNode("GrandChild"));
		var tree = new SceneTree(root, new InputManager());

		tree.Start();
		tree.Update(0.25f);
		tree.Draw();
		tree.Stop();

		Assert.True(root.IsInitialized);
		Assert.False(root.IsInTree);
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
	public void ChangeRoot_ReplacesRunningRootAndDisposesPreviousRoot_ByDefault() {
		var oldRoot = new TestNode("OldRoot");
		var oldChild = oldRoot.AddChild(new TestNode("OldChild"));
		var newRoot = new TestNode("NewRoot");
		var tree = new SceneTree(oldRoot, new InputManager());

		tree.Start();
		tree.ChangeRoot(newRoot);
		tree.Update(0.5f);
		tree.Draw();

		Assert.True(oldRoot.WasDisposed);
		Assert.True(oldChild.WasDisposed);
		Assert.Contains("OldRoot:ExitTree", oldRoot.LifecycleEvents);
		Assert.Equal(["NewRoot:Initialize", "NewRoot:EnterTree"], newRoot.LifecycleEvents);
		Assert.Equal([0.5f], newRoot.UpdateDeltas);
		Assert.Equal(1, newRoot.DrawCount);
		Assert.Same(newRoot, tree.Root);
	}

	[Fact]
	public void ChangeRoot_RejectsNodeThatAlreadyHasParent() {
		var root = new TestNode("Root");
		var child = root.AddChild(new TestNode("Child"));
		var tree = new SceneTree(root, new InputManager());

		var exception = Assert.Throws<InvalidOperationException>(() => tree.ChangeRoot(child));

		Assert.Contains("already has a parent", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void RequestQuit_CanBeTriggeredThroughNode() {
		var root = new TestNode("Root");
		var child = root.AddChild(new TestNode("Child"));
		var tree = new SceneTree(root, new InputManager());

		tree.Start();
		child.ExposedRequestQuit();

		Assert.True(tree.IsQuitRequested);
	}
}

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Shared runtime services passed to nodes.
/// </summary>
public sealed class NodeContext {
	/// <summary>
	/// Creates a runtime context for one tree.
	/// </summary>
	public NodeContext(SceneTree tree, SharedContext? shared = null) {
		Tree = tree ?? throw new ArgumentNullException(nameof(tree));
		Shared = shared ?? new SharedContext();
	}

	/// <summary>
	/// Owning scene tree for the active node hierarchy.
	/// </summary>
	public SceneTree Tree { get; }

	/// <summary>
	/// Shared input manager for the current app.
	/// </summary>
	public InputManager Input => Tree.Input;

	/// <summary>
	/// Shared data bag visible to every node attached to this tree.
	/// </summary>
	public SharedContext Shared { get; }

	/// <summary>
	/// Callback that asks the app to quit.
	/// </summary>
	public void RequestQuit() {
		Tree.RequestQuit();
	}
}

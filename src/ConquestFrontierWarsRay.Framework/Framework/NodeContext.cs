namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Shared runtime services passed to nodes.
/// </summary>
internal sealed class NodeContext {
	/// <summary>
	/// Creates a runtime context for one tree.
	/// </summary>
	public NodeContext(InputManager input, Action requestQuit) {
		Input = input ?? throw new ArgumentNullException(nameof(input));
		RequestQuit = requestQuit ?? throw new ArgumentNullException(nameof(requestQuit));
	}

	/// <summary>
	/// Shared input manager for the current app.
	/// </summary>
	public InputManager Input { get; }

	/// <summary>
	/// Callback that asks the app to quit.
	/// </summary>
	public Action RequestQuit { get; }
}

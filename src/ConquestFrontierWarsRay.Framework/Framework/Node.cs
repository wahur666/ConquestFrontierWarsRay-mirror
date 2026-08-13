namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Base class for every node in the scene tree.
/// </summary>
internal class Node : IDisposable {
	private readonly List<Node> _children = [];
	private NodeContext? _context;
	private bool _disposed;

	/// <summary>
	/// Creates a node with an optional display name.
	/// </summary>
	public Node(string? name = null) {
		Name = string.IsNullOrWhiteSpace(name) ? GetType().Name : name;
	}

	/// <summary>
	/// Node name used for debugging and errors.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Parent node, or <see langword="null"/> for the root.
	/// </summary>
	public Node? Parent { get; private set; }

	/// <summary>
	/// Direct child nodes.
	/// </summary>
	public IReadOnlyList<Node> Children => _children;

	/// <summary>
	/// True after initialization has run.
	/// </summary>
	public bool IsInitialized { get; private set; }

	/// <summary>
	/// True while the node is inside the active tree.
	/// </summary>
	public bool IsInTree { get; private set; }

	/// <summary>
	/// Shared input state for the current frame.
	/// </summary>
	protected InputManager Input {
		get {
			if (_context is null) {
				throw new InvalidOperationException($"Node '{Name}' is not attached to a runtime context.");
			}

			return _context.Input;
		}
	}

	/// <summary>
	/// Removes the node from the tree and disposes its children.
	/// </summary>
	public void Dispose() {
		if (_disposed) {
			return;
		}

		ExitTreeRecursive();

		for (var i = _children.Count - 1; i >= 0; i--) {
			_children[i].Dispose();
		}

		_children.Clear();
		OnDispose();

		Parent = null;
		_disposed = true;
	}

	/// <summary>
	/// Adds a child node and hooks it into the current runtime state.
	/// </summary>
	public T AddChild<T>(T child) where T : Node {
		ObjectDisposedException.ThrowIf(_disposed, this);

		ArgumentNullException.ThrowIfNull(child);

		if (ReferenceEquals(child, this)) {
			throw new InvalidOperationException("A node cannot be added as a child of itself.");
		}

		if (IsAncestorOf(child, this)) {
			throw new InvalidOperationException(
				$"Node '{child.Name}' cannot be added as a child of '{Name}' because it would create a cycle.");
		}

		if (child.Parent is not null) {
			throw new InvalidOperationException($"Node '{child.Name}' already has a parent.");
		}

		_children.Add(child);
		child.Parent = this;

		if (_context is not null) {
			child.AttachContextRecursive(_context);
		}

		if (IsInitialized) {
			child.InitializeRecursive();
		}

		if (IsInTree) {
			child.EnterTreeRecursive();
		}

		return child;
	}

	/// <summary>
	/// Removes a direct child node.
	/// </summary>
	public bool RemoveChild(Node child) {
		ObjectDisposedException.ThrowIf(_disposed, this);

		ArgumentNullException.ThrowIfNull(child);

		if (!_children.Remove(child)) {
			return false;
		}

		if (child.IsInTree) {
			child.ExitTreeRecursive();
		}

		child.Parent = null;
		return true;
	}

	/// <summary>
	/// Asks the app loop to stop.
	/// </summary>
	protected void RequestQuit() {
		if (_context is null) {
			throw new InvalidOperationException($"Node '{Name}' is not attached to a runtime context.");
		}

		_context.RequestQuit();
	}

	internal void AttachContextRecursive(NodeContext context) {
		ArgumentNullException.ThrowIfNull(context);

		_context = context;

		foreach (var child in _children) {
			child.AttachContextRecursive(context);
		}
	}

	internal void InitializeRecursive() {
		if (IsInitialized) {
			return;
		}

		IsInitialized = true;
		OnInitialize();

		foreach (var child in _children) {
			child.InitializeRecursive();
		}
	}

	internal void EnterTreeRecursive() {
		if (IsInTree) {
			return;
		}

		IsInTree = true;
		OnEnterTree();

		foreach (var child in _children) {
			child.EnterTreeRecursive();
		}
	}

	internal void ExitTreeRecursive() {
		if (!IsInTree) {
			return;
		}

		for (var i = _children.Count - 1; i >= 0; i--) {
			_children[i].ExitTreeRecursive();
		}

		OnExitTree();
		IsInTree = false;
	}

	internal void UpdateRecursive(float deltaTime) {
		OnUpdate(deltaTime);

		foreach (var child in _children) {
			child.UpdateRecursive(deltaTime);
		}
	}

	internal virtual void DrawRecursive() {
		OnDraw();

		foreach (var child in _children) {
			child.DrawRecursive();
		}
	}

	/// <summary>
	/// Runs once before the node starts updating or drawing.
	/// </summary>
	protected virtual void OnInitialize() {
	}

	/// <summary>
	/// Runs when the node enters the active tree.
	/// </summary>
	protected virtual void OnEnterTree() {
	}

	/// <summary>
	/// Runs when the node leaves the active tree.
	/// </summary>
	protected virtual void OnExitTree() {
	}

	/// <summary>
	/// Runs every frame before drawing.
	/// </summary>
	protected virtual void OnUpdate(float deltaTime) {
	}

	/// <summary>
	/// Runs every frame during drawing.
	/// </summary>
	protected virtual void OnDraw() {
	}

	/// <summary>
	/// Runs during disposal after child nodes are disposed.
	/// </summary>
	protected virtual void OnDispose() {
	}

	private static bool IsAncestorOf(Node node, Node candidateDescendant) {
		for (var current = candidateDescendant.Parent; current is not null; current = current.Parent) {
			if (ReferenceEquals(current, node)) {
				return true;
			}
		}

		return false;
	}
}

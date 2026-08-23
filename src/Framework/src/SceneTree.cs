namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Owns the active root node and orchestrates whole-tree lifecycle.
/// </summary>
public sealed class SceneTree : IDisposable {
	private readonly NodeContext _context;
	private Node _root;
	private bool _disposed;

	/// <summary>
	/// Creates a scene tree with one active root node.
	/// </summary>
	public SceneTree(Node root, InputManager input, SharedContext? shared = null) {
		_root = root ?? throw new ArgumentNullException(nameof(root));
		Input = input ?? throw new ArgumentNullException(nameof(input));
		_context = new NodeContext(this, shared);
	}

	/// <summary>
	/// Shared input manager for the whole tree.
	/// </summary>
	public InputManager Input { get; }

	/// <summary>
	/// Shared data bag visible to every node attached to this tree.
	/// </summary>
	public SharedContext Shared => _context.Shared;

	/// <summary>
	/// Current active root node.
	/// </summary>
	public Node Root => _root;

	/// <summary>
	/// True after the active root has entered the tree.
	/// </summary>
	public bool IsRunning { get; private set; }

	/// <summary>
	/// True after any node has requested application shutdown.
	/// </summary>
	public bool IsQuitRequested { get; private set; }

	/// <summary>
	/// Releases the active root node and stops the tree.
	/// </summary>
	public void Dispose() {
		if (_disposed) {
			return;
		}

		_root.Dispose();
		_disposed = true;
	}

	/// <summary>
	/// Starts the active root lifecycle.
	/// </summary>
	public void Start() {
		ThrowIfDisposed();

		if (IsRunning) {
			return;
		}

		_root.AttachContextRecursive(_context);
		_root.InitializeRecursive();
		_root.EnterTreeRecursive();
		IsRunning = true;
	}

	/// <summary>
	/// Stops the active root lifecycle.
	/// </summary>
	public void Stop() {
		ThrowIfDisposed();

		if (!IsRunning) {
			return;
		}

		_root.ExitTreeRecursive();
		IsRunning = false;
	}

	/// <summary>
	/// Replaces the active root node.
	/// </summary>
	public void ChangeRoot(Node newRoot, bool disposeCurrentRoot = true) {
		ThrowIfDisposed();

		ArgumentNullException.ThrowIfNull(newRoot);

		if (newRoot.Parent is not null) {
			throw new InvalidOperationException($"Node '{newRoot.Name}' cannot become the scene root because it already has a parent.");
		}

		if (ReferenceEquals(newRoot, _root)) {
			return;
		}

		var previousRoot = _root;
		var wasRunning = IsRunning;

		if (wasRunning) {
			previousRoot.ExitTreeRecursive();
		}

		if (disposeCurrentRoot) {
			previousRoot.Dispose();
		}

		_root = newRoot;
		_root.AttachContextRecursive(_context);
		_root.InitializeRecursive();

		if (wasRunning) {
			_root.EnterTreeRecursive();
		}
	}

	/// <summary>
	/// Updates the active root and its children.
	/// </summary>
	public void Update(float deltaTime) {
		ThrowIfDisposed();
		_root.UpdateRecursive(deltaTime);
	}

	/// <summary>
	/// Draws the active root and its children.
	/// </summary>
	public void Draw() {
		ThrowIfDisposed();
		_root.DrawRecursive();
	}

	/// <summary>
	/// Marks the tree for shutdown.
	/// </summary>
	public void RequestQuit() {
		ThrowIfDisposed();
		IsQuitRequested = true;
	}

	private void ThrowIfDisposed() {
		ObjectDisposedException.ThrowIf(_disposed, this);
	}
}

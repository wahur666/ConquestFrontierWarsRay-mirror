namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Base class for lazily loaded disposable resources.
/// </summary>
internal abstract class Resource : IDisposable {
	private bool _disposed;

	/// <summary>
	/// Creates a resource with an optional source path.
	/// </summary>
	protected Resource(string? resourcePath = null) {
		ResourcePath = string.IsNullOrWhiteSpace(resourcePath) ? null : resourcePath;
	}

	/// <summary>
	/// Original file path for the resource, when one exists.
	/// </summary>
	public string? ResourcePath { get; }

	/// <summary>
	/// True after the resource data has been loaded.
	/// </summary>
	public bool IsLoaded { get; private set; }

	/// <summary>
	/// True after the resource has been disposed.
	/// </summary>
	public bool IsDisposed => _disposed;

	/// <summary>
	/// Unloads the resource if needed and marks it disposed.
	/// </summary>
	public void Dispose() {
		if (_disposed) {
			return;
		}

		if (IsLoaded) {
			UnloadCore();
			IsLoaded = false;
		}

		_disposed = true;
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Loads the resource on first use.
	/// </summary>
	protected void EnsureLoaded() {
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (IsLoaded) {
			return;
		}

		LoadCore();
		IsLoaded = true;
	}

	/// <summary>
	/// Loads resource data.
	/// </summary>
	protected abstract void LoadCore();

	/// <summary>
	/// Unloads resource data.
	/// </summary>
	protected virtual void UnloadCore() {
	}
}

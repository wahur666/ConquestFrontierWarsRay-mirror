namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Base class for lazily loaded disposable resources.
/// </summary>
public abstract class Resource : IDisposable {
	private readonly List<IDisposable> _ownedResources = [];
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

		List<Exception>? exceptions = null;

		if (IsLoaded) {
			try {
				UnloadCore();
			} catch (Exception ex) {
				exceptions = [ex];
			} finally {
				IsLoaded = false;
			}
		}

		_disposed = true;
		GC.SuppressFinalize(this);
		DisposeOwnedResources(exceptions);
	}

	/// <summary>
	/// Loads the resource on first use.
	/// </summary>
	protected void EnsureLoaded() {
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (IsLoaded) {
			return;
		}

		try {
			LoadCore();
			IsLoaded = true;
		} catch {
			try {
				UnloadCore();
			} catch {
				// Best effort cleanup after a failed partial load.
			}

			throw;
		}
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

	/// <summary>
	/// Registers one child resource or disposable owned by this resource.
	/// </summary>
	protected T Own<T>(T resource) where T : IDisposable {
		ArgumentNullException.ThrowIfNull(resource);
		_ownedResources.Add(resource);
		return resource;
	}

	/// <summary>
	/// Registers multiple child resources or disposables owned by this resource.
	/// </summary>
	protected void OwnRange(IEnumerable<IDisposable> resources) {
		ArgumentNullException.ThrowIfNull(resources);

		foreach (var resource in resources) {
			Own(resource);
		}
	}

	private void DisposeOwnedResources(List<Exception>? exceptions) {
		for (var i = _ownedResources.Count - 1; i >= 0; i--) {
			try {
				_ownedResources[i].Dispose();
			} catch (Exception ex) {
				exceptions ??= [];
				exceptions.Add(ex);
			}
		}

		_ownedResources.Clear();

		if (exceptions is { Count: > 0 }) {
			throw new AggregateException($"Failed to dispose resource '{GetType().Name}'.", exceptions);
		}
	}
}

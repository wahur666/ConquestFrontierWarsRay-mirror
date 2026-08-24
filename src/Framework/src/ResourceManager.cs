namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Default shared framework resource-manager implementation.
/// </summary>
public sealed class ResourceManager : IResourceManager {
	private readonly Dictionary<string, IDisposable> _preloadedResources = new(StringComparer.Ordinal);
	private bool _disposed;

	/// <summary>
	/// Creates one manager from the shared resource locator.
	/// </summary>
	public ResourceManager(IResourceLocator locator) {
		ArgumentNullException.ThrowIfNull(locator);
		Audio = new AudioResourceManager(locator);
		Videos = new VideoResourceManager(locator);
	}

	/// <inheritdoc />
	public IAudioResourceManager Audio { get; }

	/// <inheritdoc />
	public IVideoResourceManager Videos { get; }

	/// <inheritdoc />
	public void Dispose() {
		if (_disposed) {
			return;
		}

		_disposed = true;
		ReleaseAllPreloaded();
	}

	/// <inheritdoc />
	public T Preload<T>(string name, Func<T> factory) where T : class, IDisposable {
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		ArgumentNullException.ThrowIfNull(factory);

		if (_preloadedResources.TryGetValue(name, out var existing)) {
			if (existing is T typedExisting) {
				return typedExisting;
			}

			throw new InvalidOperationException(
				$"Preloaded resource '{name}' already exists as '{existing.GetType().Name}', not '{typeof(T).Name}'.");
		}

		var resource = factory();
		ArgumentNullException.ThrowIfNull(resource);

		try {
			if (resource is Resource frameworkResource) {
				frameworkResource.Load();
			}

			_preloadedResources.Add(name, resource);
			return resource;
		} catch (Exception ex) {
			AppLog.Error("ResourceManager", $"Preload failed for '{name}'. Disposing partial {resource.GetType().Name}.", ex);
			resource.Dispose();
			throw;
		}
	}

	/// <inheritdoc />
	public bool TryGetPreloaded<T>(string name, out T? resource) where T : class, IDisposable {
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		if (_preloadedResources.TryGetValue(name, out var existing) && existing is T typedExisting) {
			resource = typedExisting;
			return true;
		}

		resource = null;
		return false;
	}

	/// <inheritdoc />
	public bool ReleasePreloaded(string name) {
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		if (!_preloadedResources.Remove(name, out var resource)) {
			return false;
		}

		resource.Dispose();
		return true;
	}

	private void ReleaseAllPreloaded() {
		if (_preloadedResources.Count == 0) {
			return;
		}

		foreach (var entry in _preloadedResources.ToArray()) {
			entry.Value.Dispose();
		}

		_preloadedResources.Clear();
	}
}

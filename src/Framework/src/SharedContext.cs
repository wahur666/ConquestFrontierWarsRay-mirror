namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Shared application state bag for named values and typed services.
/// </summary>
public sealed class SharedContext : IDisposable {
	private readonly Dictionary<string, object?> _items = new(StringComparer.Ordinal);
	private readonly Dictionary<Type, object> _services = [];
	private IResourceLocator _resourceLocator = new AssetRootResourceLocator(
		Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Assets")));
	private IResourceManager? _resourceManager;

	/// <summary>
	/// Shared locator for application asset files such as audio and video.
	/// </summary>
	public IResourceLocator ResourceLocator {
		get => _resourceLocator;
		set {
			_resourceLocator = value ?? throw new ArgumentNullException(nameof(value));

			if (_resourceManager is ResourceManager resourceManager) {
				resourceManager.Dispose();
				_resourceManager = null;
			}
		}
	}

	/// <summary>
	/// Shared framework resource manager for asset-oriented services.
	/// </summary>
	public IResourceManager ResourceManager {
		get => _resourceManager ??= new ResourceManager(ResourceLocator);
		set {
			ArgumentNullException.ThrowIfNull(value);

			if (ReferenceEquals(_resourceManager, value)) {
				return;
			}

			_resourceManager?.Dispose();
			_resourceManager = value;
		}
	}

	/// <summary>
	/// Disposes disposable shared services owned by the context.
	/// </summary>
	public void Dispose() {
		_resourceManager?.Dispose();
		_resourceManager = null;
	}

	/// <summary>
	/// Stores or replaces one named value.
	/// </summary>
	public void Set(string key, object? value) {
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		_items[key] = value;
	}

	/// <summary>
	/// Returns true when one named value exists.
	/// </summary>
	public bool Contains(string key) {
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		return _items.ContainsKey(key);
	}

	/// <summary>
	/// Tries to resolve one named value.
	/// </summary>
	public bool TryGet(string key, out object? value) {
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		return _items.TryGetValue(key, out value);
	}

	/// <summary>
	/// Tries to resolve one named value as the requested type.
	/// </summary>
	public bool TryGet<T>(string key, out T? value) {
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		if (_items.TryGetValue(key, out var stored) && stored is T typedValue) {
			value = typedValue;
			return true;
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Gets one named value as the requested type, or throws when it is missing.
	/// </summary>
	public T GetRequired<T>(string key) {
		if (TryGet<T>(key, out var value)) {
			return value!;
		}

		throw new InvalidOperationException($"Shared context does not contain a value for key '{key}'.");
	}

	/// <summary>
	/// Removes one named value.
	/// </summary>
	public bool Remove(string key) {
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		return _items.Remove(key);
	}

	/// <summary>
	/// Stores or replaces one typed shared service under its runtime type.
	/// </summary>
	public void Set<T>(T value) where T : class {
		ArgumentNullException.ThrowIfNull(value);
		_services[typeof(T)] = value;
	}

	/// <summary>
	/// Returns true when a typed shared service of the requested type exists.
	/// </summary>
	public bool Contains<T>() where T : class {
		return _services.ContainsKey(typeof(T));
	}

	/// <summary>
	/// Tries to resolve one typed shared service by type.
	/// </summary>
	public bool TryGet<T>(out T? value) where T : class {
		if (_services.TryGetValue(typeof(T), out var stored) && stored is T typedValue) {
			value = typedValue;
			return true;
		}

		value = null;
		return false;
	}

	/// <summary>
	/// Gets one typed shared service by type, or throws when it is missing.
	/// </summary>
	public T GetRequired<T>() where T : class {
		if (TryGet<T>(out var value)) {
			return value!;
		}

		throw new InvalidOperationException($"Shared context does not contain a value for '{typeof(T).Name}'.");
	}

	/// <summary>
	/// Removes one typed shared service by type.
	/// </summary>
	public bool Remove<T>() where T : class {
		return _services.Remove(typeof(T));
	}
}

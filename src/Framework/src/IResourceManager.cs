namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Shared framework entry point for resource-oriented services.
/// </summary>
public interface IResourceManager : IDisposable {
	/// <summary>
	/// Audio file acquisition service.
	/// </summary>
	IAudioResourceManager Audio { get; }

	/// <summary>
	/// Video file acquisition service.
	/// </summary>
	IVideoResourceManager Videos { get; }

	/// <summary>
	/// Creates and stores one preloaded disposable resource under a stable name.
	/// </summary>
	T Preload<T>(string name, Func<T> factory) where T : class, IDisposable;

	/// <summary>
	/// Tries to get one previously preloaded resource by name.
	/// </summary>
	bool TryGetPreloaded<T>(string name, out T? resource) where T : class, IDisposable;

	/// <summary>
	/// Releases and disposes one previously preloaded resource by name.
	/// </summary>
	bool ReleasePreloaded(string name);
}

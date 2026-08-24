namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Default shared framework resource-manager implementation.
/// </summary>
public sealed class ResourceManager : IResourceManager {
	/// <summary>
	/// Creates one manager from the shared resource locator.
	/// </summary>
	public ResourceManager(IResourceLocator locator) {
		ArgumentNullException.ThrowIfNull(locator);
		Videos = new VideoResourceManager(locator);
	}

	/// <inheritdoc />
	public IVideoResourceManager Videos { get; }
}

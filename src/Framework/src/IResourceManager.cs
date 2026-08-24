namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Shared framework entry point for resource-oriented services.
/// </summary>
public interface IResourceManager {
	/// <summary>
	/// Video file acquisition service.
	/// </summary>
	IVideoResourceManager Videos { get; }
}

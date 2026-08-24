namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Opens disposable video playback sources from application assets.
/// </summary>
public interface IVideoResourceManager {
	/// <summary>
	/// Opens one video source from a file path.
	/// </summary>
	VideoSource OpenFile(string path);

	/// <summary>
	/// Opens one video source from the configured movie asset directory.
	/// </summary>
	VideoSource OpenMovie(string moviePath);
}

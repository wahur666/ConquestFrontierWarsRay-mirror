namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Selects which runtime backend opens one audio file resource.
/// </summary>
public enum AudioPlaybackBackend {
	/// <summary>
	/// Opens audio through the NAudio test-app backend.
	/// </summary>
	NAudio,

	/// <summary>
	/// Opens audio through raylib music streaming.
	/// </summary>
	RaylibMusic,

	/// <summary>
	/// Opens audio through the Media Foundation streaming backend.
	/// </summary>
	MediaFoundation
}

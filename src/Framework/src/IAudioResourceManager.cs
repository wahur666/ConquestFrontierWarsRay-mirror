namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Opens disposable audio resources from application assets.
/// </summary>
public interface IAudioResourceManager {
	/// <summary>
	/// Opens one audio resource from a file path.
	/// </summary>
	AudioStreamResource OpenFile(string path, AudioPlaybackBackend backend = AudioPlaybackBackend.RaylibMusic);

	/// <summary>
	/// Opens one music resource from the configured music asset directory.
	/// </summary>
	AudioStreamResource OpenMusic(string musicPath, AudioPlaybackBackend backend = AudioPlaybackBackend.RaylibMusic);

	/// <summary>
	/// Opens one speech resource from the configured speech asset directory.
	/// </summary>
	AudioStreamResource OpenSpeech(string speechPath, AudioPlaybackBackend backend = AudioPlaybackBackend.RaylibMusic);
}

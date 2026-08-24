namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Opens audio resources from file paths or locator-backed asset roots.
/// </summary>
public sealed class AudioResourceManager : IAudioResourceManager {
	private readonly IResourceLocator _locator;

	public AudioResourceManager(IResourceLocator locator) {
		_locator = locator ?? throw new ArgumentNullException(nameof(locator));
	}

	/// <inheritdoc />
	public AudioStreamResource OpenFile(string path, AudioPlaybackBackend backend = AudioPlaybackBackend.RaylibMusic) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		var fullPath = Path.GetFullPath(path);

		return backend switch {
			AudioPlaybackBackend.NAudio => new NAudioStreamResource(fullPath),
			AudioPlaybackBackend.MediaFoundation => new MediaFoundationAudioStreamResource(fullPath),
			_ => new MusicAudioResource(fullPath)
		};
	}

	/// <inheritdoc />
	public AudioStreamResource OpenMusic(string musicPath, AudioPlaybackBackend backend = AudioPlaybackBackend.RaylibMusic) {
		ArgumentException.ThrowIfNullOrWhiteSpace(musicPath);
		return OpenFile(_locator.ResolveMusicPath(musicPath), backend);
	}

	/// <inheritdoc />
	public AudioStreamResource OpenSpeech(string speechPath, AudioPlaybackBackend backend = AudioPlaybackBackend.RaylibMusic) {
		ArgumentException.ThrowIfNullOrWhiteSpace(speechPath);
		return OpenFile(_locator.ResolveSpeechPath(speechPath), backend);
	}
}

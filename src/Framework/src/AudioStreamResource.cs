namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Base resource for streamed audio playback.
/// </summary>
public abstract class AudioStreamResource : Resource {
	/// <summary>
	/// Creates an audio resource.
	/// </summary>
	protected AudioStreamResource(string? resourcePath = null) : base(resourcePath) {
	}

	/// <summary>
	/// True when the audio stream is configured to loop.
	/// </summary>
	public abstract bool Looping { get; set; }

	/// <summary>
	/// Playback volume in the range [0, 1].
	/// </summary>
	public abstract float Volume { get; set; }

	/// <summary>
	/// Playback pitch multiplier. Values above zero are valid.
	/// </summary>
	public abstract float Pitch { get; set; }

	/// <summary>
	/// Stereo pan in the range [0, 1], where 0.5 is centered.
	/// </summary>
	public abstract float Pan { get; set; }

	/// <summary>
	/// True while the stream is actively playing.
	/// </summary>
	public abstract bool IsPlaying { get; }

	/// <summary>
	/// Current playback position in seconds.
	/// </summary>
	public abstract float TimePlayed { get; }

	/// <summary>
	/// Total audio length in seconds.
	/// </summary>
	public abstract float TimeLength { get; }

	/// <summary>
	/// Starts or resumes playback.
	/// </summary>
	public abstract void Play();

	/// <summary>
	/// Temporarily pauses playback.
	/// </summary>
	public abstract void Pause();

	/// <summary>
	/// Stops playback and rewinds to the beginning.
	/// </summary>
	public abstract void Stop();

	/// <summary>
	/// Seeks to a playback position in seconds.
	/// </summary>
	public abstract void Seek(float positionSeconds);

	/// <summary>
	/// Advances any streaming state needed by the backend.
	/// </summary>
	public abstract void Update();
}

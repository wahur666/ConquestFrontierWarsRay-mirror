namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Non-visual node that owns streamed audio playback.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AudioPlayer"/> owns play, pause, stop, seek, volume, looping,
/// pitch, pan, and assignment of the current <see cref="AudioStreamResource"/>.
/// </para>
/// <para>
/// The node pauses active playback when it leaves the tree and resumes it on
/// re-entry when appropriate.
/// </para>
/// </remarks>
public sealed class AudioPlayer : Node {
	private AudioStreamResource? _audio;
	private bool _ownsAudio;
	private bool _resumeOnEnter;
	private float _volume = 1f;
	private float _pitch = 1f;
	private float _pan = 0.5f;

	/// <summary>
	/// Creates an audio player node.
	/// </summary>
	public AudioPlayer(string? name = null) : base(name) {
	}

	/// <summary>
	/// Currently assigned audio resource, if any.
	/// </summary>
	public AudioStreamResource? Audio => _audio;

	/// <summary>
	/// True when an audio resource is assigned.
	/// </summary>
	public bool HasAudio => _audio is not null;

	/// <summary>
	/// Playback volume in the range [0, 1].
	/// </summary>
	public float Volume {
		get => _volume;
		set {
			_volume = Math.Clamp(value, 0f, 1f);

			if (_audio is not null) {
				_audio.Volume = _volume;
			}
		}
	}

	/// <summary>
	/// Playback pitch multiplier. Values above zero are valid.
	/// </summary>
	public float Pitch {
		get => _pitch;
		set {
			_pitch = value <= 0f ? throw new ArgumentOutOfRangeException(nameof(value), "Pitch must be greater than zero.") : value;

			if (_audio is not null) {
				_audio.Pitch = _pitch;
			}
		}
	}

	/// <summary>
	/// Stereo pan in the range [0, 1], where 0.5 is centered.
	/// </summary>
	public float Pan {
		get => _pan;
		set {
			_pan = Math.Clamp(value, 0f, 1f);

			if (_audio is not null) {
				_audio.Pan = _pan;
			}
		}
	}

	/// <summary>
	/// True when playback should loop.
	/// </summary>
	public bool Looping {
		get;
		set {
			field = value;

			if (_audio is not null) {
				_audio.Looping = value;
			}
		}
	}

	/// <summary>
	/// True while the assigned resource is actively playing.
	/// </summary>
	public bool IsPlaying => _audio?.IsPlaying ?? false;

	/// <summary>
	/// Current playback position in seconds.
	/// </summary>
	public float PlaybackPosition => _audio?.TimePlayed ?? 0f;

	/// <summary>
	/// Total audio length in seconds.
	/// </summary>
	public float PlaybackLength => _audio?.TimeLength ?? 0f;

	/// <summary>
	/// Replaces the assigned borrowed audio resource.
	/// </summary>
	public void SetAudio(AudioStreamResource? audio) {
		AssignAudio(audio, takeOwnership: false);
	}

	/// <summary>
	/// Replaces the assigned audio resource and transfers disposal responsibility to this node.
	/// </summary>
	public void SetOwnedAudio(AudioStreamResource? audio) {
		AssignAudio(audio, takeOwnership: true);
	}

	/// <summary>
	/// Creates and assigns a file-backed streamed audio resource.
	/// </summary>
	public void SetAudioFile(string path) {
		SetOwnedAudio(new MusicAudioResource(path));
	}

	/// <summary>
	/// Disposes the currently assigned audio resource and clears it from the node.
	/// </summary>
	public void DisposeAudio() {
		ReleaseAudio(disposeAssigned: true);
	}

	/// <summary>
	/// Starts or resumes playback.
	/// </summary>
	public void Play() {
		_audio?.Play();
	}

	/// <summary>
	/// Temporarily pauses playback.
	/// </summary>
	public void Pause() {
		_resumeOnEnter = false;
		_audio?.Pause();
	}

	/// <summary>
	/// Stops playback and rewinds to the beginning.
	/// </summary>
	public void Stop() {
		_resumeOnEnter = false;
		_audio?.Stop();
	}

	/// <summary>
	/// Seeks to a playback position in seconds.
	/// </summary>
	public void Seek(float positionSeconds) {
		_audio?.Seek(positionSeconds);
	}

	protected override void OnEnterTree() {
		if (_resumeOnEnter && _audio is not null) {
			_resumeOnEnter = false;
			_audio.Play();
		}
	}

	protected override void OnExitTree() {
		if (_audio?.IsPlaying == true) {
			_resumeOnEnter = true;
			_audio.Pause();
		}
	}

	protected override void OnUpdate(float deltaTime) {
		_audio?.Update();
	}

	protected override void OnDispose() {
		ReleaseAudio(disposeAssigned: _ownsAudio);
	}

	/// <summary>
	/// Clears the current assignment without disposing a borrowed resource.
	/// </summary>
	public void ClearAudio() {
		ReleaseAudio(disposeAssigned: false);
	}

	private void ApplyPlaybackSettings(AudioStreamResource audio) {
		audio.Looping = Looping;
		audio.Volume = _volume;
		audio.Pitch = _pitch;
		audio.Pan = _pan;
	}

	private void AssignAudio(AudioStreamResource? audio, bool takeOwnership) {
		if (ReferenceEquals(_audio, audio)) {
			_ownsAudio = takeOwnership || _ownsAudio;

			if (_audio is not null) {
				ApplyPlaybackSettings(_audio);
			}

			return;
		}

		ReleaseAudio(disposeAssigned: _ownsAudio);
		_audio = audio;
		_ownsAudio = takeOwnership;
		_resumeOnEnter = false;

		if (_audio is not null) {
			ApplyPlaybackSettings(_audio);
		}
	}

	private void ReleaseAudio(bool disposeAssigned) {
		if (_audio is null) {
			_ownsAudio = false;
			_resumeOnEnter = false;
			return;
		}

		if (_audio.IsLoaded) {
			_audio.Stop();
		}

		if (disposeAssigned || _ownsAudio) {
			_audio.Dispose();
		}

		_audio = null;
		_ownsAudio = false;
		_resumeOnEnter = false;
	}
}

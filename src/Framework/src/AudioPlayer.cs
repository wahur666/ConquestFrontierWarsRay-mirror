namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Non-visual node that owns streamed audio playback.
/// </summary>
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
	/// True when the assigned audio resource is owned and will be disposed by this node.
	/// </summary>
	public bool OwnsAudio => _ownsAudio;

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
	/// Replaces the assigned audio resource.
	/// </summary>
	public void SetAudio(AudioStreamResource? audio, bool disposeCurrent = false, bool takeOwnership = false) {
		if (ReferenceEquals(_audio, audio)) {
			_ownsAudio = takeOwnership || _ownsAudio;

			if (_audio is not null) {
				ApplyPlaybackSettings(_audio);
			}

			return;
		}

		ReleaseAudio(disposeCurrent);
		_audio = audio;
		_ownsAudio = takeOwnership;
		_resumeOnEnter = false;

		if (_audio is not null) {
			ApplyPlaybackSettings(_audio);
		}
	}

	/// <summary>
	/// Creates and assigns a file-backed streamed audio resource.
	/// </summary>
	public void SetAudioFile(string path, bool disposeCurrent = true) {
		var audio = new MusicAudioResource(path);
		SetAudio(audio, disposeCurrent, takeOwnership: true);
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

	private void ApplyPlaybackSettings(AudioStreamResource audio) {
		audio.Looping = Looping;
		audio.Volume = _volume;
		audio.Pitch = _pitch;
		audio.Pan = _pan;
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

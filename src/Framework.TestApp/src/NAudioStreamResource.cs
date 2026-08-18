using ConquestFrontierWarsRay.Framework;
using NAudio.Wave;

namespace ConquestFrontierWarsRay.Framework.TestApp;

/// <summary>
/// Windows-only test-app audio resource backed by NAudio.
/// </summary>
internal sealed class NAudioStreamResource : AudioStreamResource {
	private AudioFileReader? _reader;
	private WaveChannel32? _channel;
	private WaveOutEvent? _output;
	private bool _looping;
	private bool _isRestartingLoop;
	private float _volume = 1f;
	private float _pitch = 1f;
	private float _pan = 0.5f;

	public NAudioStreamResource(string path) : base(path) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
	}

	public override bool Looping {
		get => _looping;
		set => _looping = value;
	}

	public override float Volume {
		get => _volume;
		set {
			_volume = Math.Clamp(value, 0f, 1f);

			if (_reader is not null) {
				_reader.Volume = _volume;
			}
		}
	}

	public override float Pitch {
		get => _pitch;
		set => _pitch = value <= 0f ? throw new ArgumentOutOfRangeException(nameof(value), "Pitch must be greater than zero.") : value;
	}

	public override float Pan {
		get => _pan;
		set {
			_pan = Math.Clamp(value, 0f, 1f);

			if (_channel is not null) {
				_channel.Pan = (_pan * 2f) - 1f;
			}
		}
	}

	public override bool IsPlaying {
		get {
			EnsureLoaded();
			return _output?.PlaybackState == PlaybackState.Playing;
		}
	}

	public override float TimePlayed {
		get {
			EnsureLoaded();
			return (float)(_channel?.CurrentTime.TotalSeconds ?? 0d);
		}
	}

	public override float TimeLength {
		get {
			EnsureLoaded();
			return (float)(_channel?.TotalTime.TotalSeconds ?? 0d);
		}
	}

	public override void Play() {
		EnsureLoaded();
		_output!.Play();
	}

	public override void Pause() {
		EnsureLoaded();
		_output!.Pause();
	}

	public override void Stop() {
		EnsureLoaded();
		_output!.Stop();
		_channel!.CurrentTime = TimeSpan.Zero;
	}

	public override void Seek(float positionSeconds) {
		EnsureLoaded();
		var timeLength = _channel!.TotalTime.TotalSeconds;
		var clamped = Math.Clamp(positionSeconds, 0f, (float)timeLength);
		_channel.CurrentTime = TimeSpan.FromSeconds(clamped);
	}

	public override void Update() {
	}

	protected override void LoadCore() {
		var reader = new AudioFileReader(ResourcePath!);
		var channel = new WaveChannel32(reader) {
			PadWithZeroes = false,
			Volume = _volume,
			Pan = (_pan * 2f) - 1f
		};
		var output = new WaveOutEvent();
		output.Init(channel);
		output.PlaybackStopped += OnPlaybackStopped;

		_reader = reader;
		_channel = channel;
		_output = output;
	}

	protected override void UnloadCore() {
		if (_output is not null) {
			_output.PlaybackStopped -= OnPlaybackStopped;
			_output.Stop();
			_output.Dispose();
			_output = null;
		}

		_channel?.Dispose();
		_channel = null;

		_reader?.Dispose();
		_reader = null;
	}

	private void OnPlaybackStopped(object? sender, StoppedEventArgs e) {
		if (_isRestartingLoop || !_looping || _channel is null || _output is null || e.Exception is not null) {
			return;
		}

		if (_channel.CurrentTime < _channel.TotalTime - TimeSpan.FromMilliseconds(50)) {
			return;
		}

		try {
			_isRestartingLoop = true;
			_channel.CurrentTime = TimeSpan.Zero;
			_output.Play();
		} finally {
			_isRestartingLoop = false;
		}
	}
}

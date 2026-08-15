using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// File-backed streamed audio resource implemented with raylib music streams.
/// </summary>
public sealed class MusicAudioResource : AudioStreamResource {
	private Music _music;
	private bool _looping;
	private float _volume = 1f;
	private float _pitch = 1f;
	private float _pan = 0.5f;

	/// <summary>
	/// Creates a streamed audio resource from a file path.
	/// </summary>
	public MusicAudioResource(string path) : base(path) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
	}

	/// <inheritdoc />
	public override bool Looping {
		get => _looping;
		set {
			_looping = value;

			if (IsLoaded && Raylib.IsMusicValid(_music)) {
				_music.Looping = value;
			}
		}
	}

	/// <inheritdoc />
	public override float Volume {
		get => _volume;
		set {
			_volume = Math.Clamp(value, 0f, 1f);

			if (IsLoaded && Raylib.IsMusicValid(_music)) {
				Raylib.SetMusicVolume(_music, _volume);
			}
		}
	}

	/// <inheritdoc />
	public override float Pitch {
		get => _pitch;
		set {
			_pitch = value <= 0f ? throw new ArgumentOutOfRangeException(nameof(value), "Pitch must be greater than zero.") : value;

			if (IsLoaded && Raylib.IsMusicValid(_music)) {
				Raylib.SetMusicPitch(_music, _pitch);
			}
		}
	}

	/// <inheritdoc />
	public override float Pan {
		get => _pan;
		set {
			_pan = Math.Clamp(value, 0f, 1f);

			if (IsLoaded && Raylib.IsMusicValid(_music)) {
				Raylib.SetMusicPan(_music, _pan);
			}
		}
	}

	/// <inheritdoc />
	public override bool IsPlaying {
		get {
			EnsureLoaded();
			return Raylib.IsMusicStreamPlaying(_music);
		}
	}

	/// <inheritdoc />
	public override float TimePlayed {
		get {
			EnsureLoaded();
			return Raylib.GetMusicTimePlayed(_music);
		}
	}

	/// <inheritdoc />
	public override float TimeLength {
		get {
			EnsureLoaded();
			return Raylib.GetMusicTimeLength(_music);
		}
	}

	/// <inheritdoc />
	public override void Play() {
		EnsureLoaded();
		Raylib.PlayMusicStream(_music);
	}

	/// <inheritdoc />
	public override void Pause() {
		EnsureLoaded();
		Raylib.PauseMusicStream(_music);
	}

	/// <inheritdoc />
	public override void Stop() {
		EnsureLoaded();
		Raylib.StopMusicStream(_music);
		Raylib.SeekMusicStream(_music, 0f);
	}

	/// <inheritdoc />
	public override void Seek(float positionSeconds) {
		EnsureLoaded();
		var clampedPosition = Math.Clamp(positionSeconds, 0f, Raylib.GetMusicTimeLength(_music));
		Raylib.SeekMusicStream(_music, clampedPosition);
	}

	/// <inheritdoc />
	public override void Update() {
		EnsureLoaded();
		Raylib.UpdateMusicStream(_music);
	}

	protected override void LoadCore() {
		var music = Raylib.LoadMusicStream(ResourcePath!);

		if (!Raylib.IsMusicValid(music)) {
			throw new InvalidOperationException($"Failed to load music stream resource '{ResourcePath}'.");
		}

		_music = music;
		_music.Looping = _looping;
		Raylib.SetMusicVolume(_music, _volume);
		Raylib.SetMusicPitch(_music, _pitch);
		Raylib.SetMusicPan(_music, _pan);
	}

	protected override void UnloadCore() {
		if (Raylib.IsMusicValid(_music)) {
			Raylib.StopMusicStream(_music);
			Raylib.UnloadMusicStream(_music);
		}

		_music = default;
	}
}

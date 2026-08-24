using System.Numerics;
using Raylib_cs;
using Vortice.MediaFoundation;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Screen-space video playback control backed by Media Foundation decoding.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VideoPlayer"/> owns source loading, play, pause, stop, seek,
/// frame decoding, optional embedded audio playback, and aspect-fit presentation
/// within the control bounds.
/// </para>
/// <para>
/// Media Foundation lifetime is managed inside the framework so callers only
/// interact with the node and its source file.
/// </para>
/// </remarks>
public sealed unsafe class VideoPlayer : Control {
	private const long TicksPerSecond = 10_000_000;

	private readonly AudioPlayer _audioPlayer;
	private VideoSource? _source;
	private byte[]? _rgbaFrame;
	private Raylib_cs.Texture2D _texture;
	private int _stride;
	private bool _sourceIsTopDown;
	private bool _hasPendingFrame;
	private bool _hasDisplayedFrame;
	private bool _endOfStream;
	private bool _isPlaying;
	private bool _resumeOnEnter;
	private long _pendingFrameTimestamp;
	private double _elapsedSeconds;
	private double _durationSeconds;
	private float _volume = 1f;

	public VideoPlayer(string? name = null) : base(name) {
		_audioPlayer = new AudioPlayer($"{Name}.Audio");
	}

	/// <summary>
	/// Path of the currently loaded video file, if any.
	/// </summary>
	public string? SourcePath => _source?.SourcePath;

	/// <summary>
	/// Video width in pixels.
	/// </summary>
	public int VideoWidth { get; private set; }

	/// <summary>
	/// Video height in pixels.
	/// </summary>
	public int VideoHeight { get; private set; }

	/// <summary>
	/// Reported source frame rate, or zero when unavailable.
	/// </summary>
	public double FrameRate { get; private set; }

	/// <summary>
	/// True when a source has been loaded.
	/// </summary>
	public bool HasVideo => _source is not null;

	/// <summary>
	/// True while playback is active.
	/// </summary>
	public bool IsPlaying => _isPlaying;

	/// <summary>
	/// True after the source reaches end of stream.
	/// </summary>
	public bool IsFinished => _endOfStream;

	/// <summary>
	/// Playback tint.
	/// </summary>
	public Color Tint { get; set; } = Color.White;

	/// <summary>
	/// Background fill behind the video rectangle.
	/// </summary>
	public Color Background { get; set; } = Color.Black;

	/// <summary>
	/// When true, the video is aspect-fit inside <see cref="Control.Size"/>.
	/// </summary>
	public bool MaintainAspectRatio { get; set; } = true;

	/// <summary>
	/// When true, playback starts automatically after loading.
	/// </summary>
	public bool AutoPlay { get; set; }

	/// <summary>
	/// Current playback position in seconds.
	/// </summary>
	public double PlaybackPositionSeconds => _elapsedSeconds;

	/// <summary>
	/// Total playback length in seconds, or zero when unavailable.
	/// </summary>
	public double PlaybackLengthSeconds => _durationSeconds;

	/// <summary>
	/// Playback volume in the range [0, 1] for embedded audio.
	/// </summary>
	public float Volume {
		get => _volume;
		set {
			_volume = Math.Clamp(value, 0f, 1f);
			_audioPlayer.Volume = _volume;
		}
	}

	/// <summary>
	/// File name of the current source, or an empty string when no source is loaded.
	/// </summary>
	public string FileName => SourcePath is null ? string.Empty : Path.GetFileName(SourcePath);

	/// <summary>
	/// Loads a video file into this player.
	/// </summary>
	public void SetSourceFile(string path, bool autoPlay = false) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		SetSource(new VideoResourceManager(new AssetRootResourceLocator(Path.GetDirectoryName(Path.GetFullPath(path)) ?? AppContext.BaseDirectory)).OpenFile(path), autoPlay);
	}

	/// <summary>
	/// Loads one already-opened disposable video source into this player.
	/// </summary>
	public void SetSource(VideoSource source, bool autoPlay = false) {
		ArgumentNullException.ThrowIfNull(source);
		LoadSource(source, autoPlay);
	}

	/// <summary>
	/// Clears and disposes the current source.
	/// </summary>
	public void DisposeSource() {
		ReleasePlayback();
	}

	/// <summary>
	/// Starts or resumes playback.
	/// </summary>
	public void Play() {
		if (!HasVideo) {
			return;
		}

		if (_endOfStream) {
			Seek(_durationSeconds > 0d ? _durationSeconds : 0d);
		}

		_isPlaying = true;
		_audioPlayer.Play();
	}

	/// <summary>
	/// Temporarily pauses playback.
	/// </summary>
	public void Pause() {
		_resumeOnEnter = false;
		_isPlaying = false;
		_audioPlayer.Pause();
	}

	/// <summary>
	/// Stops playback and seeks to the beginning without restarting playback.
	/// </summary>
	public void Stop() {
		_resumeOnEnter = false;
		Pause();
		Seek(0d);
	}

	/// <summary>
	/// Seeks to a playback position in seconds.
	/// </summary>
	public void Seek(double positionSeconds) {
		if (_source is null) {
			return;
		}

		var clampedPosition = ClampSeekSeconds(positionSeconds);
		var targetTimestamp = SecondsToTicks(clampedPosition);
		var wasPlaying = _isPlaying;

		_source.Reader.SetCurrentPosition(targetTimestamp);
		_audioPlayer.Seek((float)clampedPosition);
		_elapsedSeconds = clampedPosition;
		_pendingFrameTimestamp = 0;
		_hasPendingFrame = false;
		_hasDisplayedFrame = false;
		_endOfStream = false;
		_isPlaying = false;

		RenderFrameAtOrAfter(targetTimestamp);

		if (wasPlaying && !_endOfStream) {
			_isPlaying = true;
			_audioPlayer.Play();
		}
	}

	/// <summary>
	/// Computes the destination rectangle for aspect-fit presentation.
	/// </summary>
	public static Rectangle FitRectangle(float sourceWidth, float sourceHeight, float targetWidth, float targetHeight) {
		if (sourceWidth <= 0f || sourceHeight <= 0f || targetWidth <= 0f || targetHeight <= 0f) {
			return new Rectangle(0f, 0f, targetWidth, targetHeight);
		}

		var scale = MathF.Min(targetWidth / sourceWidth, targetHeight / sourceHeight);
		var width = sourceWidth * scale;
		var height = sourceHeight * scale;
		return new Rectangle((targetWidth - width) * 0.5f, (targetHeight - height) * 0.5f, width, height);
	}

	protected override void OnEnterTree() {
		if (_resumeOnEnter && HasVideo) {
			_resumeOnEnter = false;
			Play();
		}
	}

	protected override void OnExitTree() {
		if (_isPlaying) {
			_resumeOnEnter = true;
			Pause();
		}
	}

	protected override void OnUpdate(float deltaTime) {
		if (!_isPlaying || _endOfStream || _source is null) {
			return;
		}

		_audioPlayer.Audio?.Update();
		_elapsedSeconds = Math.Min(_elapsedSeconds + deltaTime, _durationSeconds > 0d ? _durationSeconds : double.MaxValue);
		var targetTimestamp = SecondsToTicks(_elapsedSeconds);

		while (!_endOfStream) {
			if (!_hasPendingFrame && !ReadNextFrame()) {
				return;
			}

			if (_hasDisplayedFrame && _pendingFrameTimestamp > targetTimestamp) {
				return;
			}

			UploadCurrentFrame();
			_hasPendingFrame = false;
			_hasDisplayedFrame = true;

			if (_pendingFrameTimestamp >= targetTimestamp) {
				return;
			}
		}
	}

	protected override void Draw() {
		if (Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		Raylib.DrawRectangleRec(bounds, Background);

		if (_texture.Id == 0 || VideoWidth <= 0 || VideoHeight <= 0) {
			return;
		}

		var destination = bounds;
		if (MaintainAspectRatio) {
			var fit = FitRectangle(VideoWidth, VideoHeight, bounds.Width, bounds.Height);
			destination = new Rectangle(bounds.X + fit.X, bounds.Y + fit.Y, fit.Width, fit.Height);
		}

		var source = new Rectangle(0f, 0f, VideoWidth, VideoHeight);
		Raylib.DrawTexturePro(_texture, source, destination, Vector2.Zero, 0f, Tint);
	}

	protected override void OnDispose() {
		ReleasePlayback();
	}

	private void LoadSource(VideoSource source, bool autoPlay) {
		ReleasePlayback();
		try {
			_source = source;
			_audioPlayer.SetAudio(source.EmbeddedAudio, disposeCurrent: true, takeOwnership: false);
			_audioPlayer.Volume = _volume;
			VideoWidth = source.Width;
			VideoHeight = source.Height;
			FrameRate = source.FrameRate;
			_durationSeconds = source.DurationSeconds;
			_stride = Math.Abs(source.Stride);
			_sourceIsTopDown = source.SourceIsTopDown;
			_rgbaFrame = new byte[source.Width * source.Height * 4];
			_elapsedSeconds = 0d;
			_pendingFrameTimestamp = 0;
			_hasPendingFrame = false;
			_hasDisplayedFrame = false;
			_endOfStream = false;
			_isPlaying = false;
			_resumeOnEnter = false;
			EnsureTexture(source.Width, source.Height);

			if (Size.X <= 0f || Size.Y <= 0f) {
				Size = new Vector2(source.Width, source.Height);
			}

			Seek(0d);
			if (autoPlay || AutoPlay) {
				Play();
			}
		} catch {
			ReleasePlayback();
			throw;
		}
	}

	private void EnsureTexture(int width, int height) {
		if (_texture.Id != 0) {
			Raylib.UnloadTexture(_texture);
			_texture = default;
		}

		var image = Raylib.GenImageColor(width, height, Color.Blank);
		_texture = Raylib.LoadTextureFromImage(image);
		Raylib.UnloadImage(image);
	}

	private void ReleasePlayback() {
		_resumeOnEnter = false;
		_isPlaying = false;

		if (_texture.Id != 0) {
			Raylib.UnloadTexture(_texture);
			_texture = default;
		}

		_audioPlayer.SetAudio(null, disposeCurrent: false);
		_source?.Dispose();
		_source = null;

		_rgbaFrame = null;
		_stride = 0;
		_sourceIsTopDown = false;
		_pendingFrameTimestamp = 0;
		_hasPendingFrame = false;
		_hasDisplayedFrame = false;
		_endOfStream = false;
		_elapsedSeconds = 0d;
		_durationSeconds = 0d;
		VideoWidth = 0;
		VideoHeight = 0;
		FrameRate = 0d;
	}

	private bool ReadNextFrame() {
		if (_source is null) {
			return false;
		}

		var sample = _source.Reader.ReadSample(SourceReaderIndex.FirstVideoStream, SourceReaderControlFlag.None, out _, out var flags, out var timestamp);

		if ((flags & SourceReaderFlag.EndOfStream) != 0) {
			_endOfStream = true;
			_isPlaying = false;
			_audioPlayer.Pause();
		}

		if ((flags & SourceReaderFlag.Error) != 0) {
			throw new InvalidOperationException($"Media Foundation reported a read error for '{SourcePath}'.");
		}

		if (sample is null) {
			return false;
		}

		_pendingFrameTimestamp = timestamp;
		CopySampleToFrame(sample);
		_hasPendingFrame = true;
		return true;
	}

	private static long SecondsToTicks(double seconds) {
		return (long)Math.Round(seconds * TicksPerSecond);
	}

	private double ClampSeekSeconds(double positionSeconds) {
		if (_durationSeconds <= 0d) {
			return Math.Max(0d, positionSeconds);
		}

		return Math.Clamp(positionSeconds, 0d, _durationSeconds);
	}

	private void RenderFrameAtOrAfter(long targetTimestamp) {
		while (!_endOfStream) {
			if (!ReadNextFrame()) {
				return;
			}

			UploadCurrentFrame();
			_hasPendingFrame = false;
			_hasDisplayedFrame = true;

			if (_pendingFrameTimestamp >= targetTimestamp) {
				return;
			}
		}
	}

	private void UploadCurrentFrame() {
		if (_rgbaFrame is null) {
			return;
		}

		fixed (byte* pixels = _rgbaFrame) {
			Raylib.UpdateTexture(_texture, pixels);
		}
	}

	private void CopySampleToFrame(IMFSample sample) {
		var buffer = sample.ConvertToContiguousBuffer();
		try {
			buffer.Lock(out var sourcePointer, out _, out var currentLength);
			CopyBgr32ToRgba((byte*)sourcePointer, currentLength);
		} finally {
			buffer.Unlock();
			buffer.Dispose();
			sample.Dispose();
		}
	}

	private void CopyBgr32ToRgba(byte* source, int sourceLength) {
		if (_rgbaFrame is null || VideoWidth <= 0 || VideoHeight <= 0) {
			return;
		}

		var rowBytes = VideoWidth * 4;
		if (sourceLength < rowBytes * VideoHeight) {
			return;
		}

		fixed (byte* destinationBase = _rgbaFrame) {
			for (var y = 0; y < VideoHeight; y++) {
				var sourceY = _sourceIsTopDown ? VideoHeight - 1 - y : y;
				var sourceRow = source + (sourceY * _stride);
				var destinationRow = destinationBase + (y * rowBytes);

				for (var x = 0; x < VideoWidth; x++) {
					var sourcePixel = sourceRow + (x * 4);
					var destinationPixel = destinationRow + (x * 4);

					destinationPixel[0] = sourcePixel[2];
					destinationPixel[1] = sourcePixel[1];
					destinationPixel[2] = sourcePixel[0];
					destinationPixel[3] = 255;
				}
			}
		}
	}
}

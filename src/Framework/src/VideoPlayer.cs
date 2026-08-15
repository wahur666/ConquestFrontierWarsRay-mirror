using System.Numerics;
using System.Runtime.InteropServices;
using Raylib_cs;
using SharpGen.Runtime;
using Vortice.MediaFoundation;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Screen-space video playback control backed by Media Foundation decoding.
/// </summary>
public sealed unsafe class VideoPlayer : Control {
	private const long TicksPerSecond = 10_000_000;

	private IMFSourceReader? _videoReader;
	private AudioTrack? _audioTrack;
	private MediaFoundationRuntime.Lease _mediaFoundationLease;
	private byte[]? _rgbaFrame;
	private Raylib_cs.Texture2D _texture;
	private string? _sourcePath;
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
	}

	/// <summary>
	/// Path of the currently loaded video file, if any.
	/// </summary>
	public string? SourcePath => _sourcePath;

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
	public bool HasVideo => _videoReader is not null;

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
			_audioTrack?.SetVolume(_volume);
		}
	}

	/// <summary>
	/// File name of the current source, or an empty string when no source is loaded.
	/// </summary>
	public string FileName => _sourcePath is null ? string.Empty : Path.GetFileName(_sourcePath);

	/// <summary>
	/// Loads a video file into this player.
	/// </summary>
	public void SetSourceFile(string path, bool autoPlay = false) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		LoadSource(path, autoPlay);
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
		_audioTrack?.Play();
	}

	/// <summary>
	/// Temporarily pauses playback.
	/// </summary>
	public void Pause() {
		_resumeOnEnter = false;
		_isPlaying = false;
		_audioTrack?.Pause();
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
		if (_videoReader is null) {
			return;
		}

		var clampedPosition = ClampSeekSeconds(positionSeconds);
		var targetTimestamp = SecondsToTicks(clampedPosition);
		var wasPlaying = _isPlaying;

		_videoReader.SetCurrentPosition(targetTimestamp);
		_audioTrack?.Seek(targetTimestamp);
		_elapsedSeconds = clampedPosition;
		_pendingFrameTimestamp = 0;
		_hasPendingFrame = false;
		_hasDisplayedFrame = false;
		_endOfStream = false;
		_isPlaying = false;

		RenderFrameAtOrAfter(targetTimestamp);

		if (wasPlaying && !_endOfStream) {
			_isPlaying = true;
			_audioTrack?.Play();
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
		if (!_isPlaying || _endOfStream || _videoReader is null) {
			return;
		}

		_audioTrack?.Update();
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

	private void LoadSource(string path, bool autoPlay) {
		var fullPath = Path.GetFullPath(path);
		ReleasePlayback();

		_mediaFoundationLease = MediaFoundationRuntime.Acquire();
		try {
			var attributes = MediaFactory.MFCreateAttributes(1);
			attributes.Set(SourceReaderAttributeKeys.EnableVideoProcessing, true).CheckError();

			var reader = MediaFactory.MFCreateSourceReaderFromURL(fullPath, attributes);
			reader.SetStreamSelection(SourceReaderIndex.AllStreams, false);
			reader.SetStreamSelection(SourceReaderIndex.FirstVideoStream, true);

			var outputType = MediaFactory.MFCreateMediaType();
			outputType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video).CheckError();
			outputType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Rgb32).CheckError();
			reader.SetCurrentMediaType(SourceReaderIndex.FirstVideoStream, outputType);

			var currentType = reader.GetCurrentMediaType(SourceReaderIndex.FirstVideoStream);
			var packedFrameSize = currentType.GetUInt64(MediaTypeAttributeKeys.FrameSize);
			var width = (int)(packedFrameSize >> 32);
			var height = (int)(packedFrameSize & 0xffffffff);
			var stride = width * 4;
			var frameRate = ReadPackedRatio(currentType, MediaTypeAttributeKeys.FrameRate);

			var strideResult = currentType.GetUInt32(MediaTypeAttributeKeys.DefaultStride, out var mediaStride);
			if (strideResult.Success) {
				stride = unchecked((int)mediaStride);
			}

			if (width <= 0 || height <= 0) {
				reader.Dispose();
				throw new InvalidOperationException($"Could not read a valid video size from '{fullPath}'.");
			}

			_videoReader = reader;
			_audioTrack = AudioTrack.TryOpen(fullPath);
			_audioTrack?.SetVolume(_volume);
			_sourcePath = fullPath;
			VideoWidth = width;
			VideoHeight = height;
			FrameRate = frameRate;
			_durationSeconds = ReadDurationSeconds(reader);
			_stride = Math.Abs(stride);
			_sourceIsTopDown = stride < 0;
			_rgbaFrame = new byte[width * height * 4];
			_elapsedSeconds = 0d;
			_pendingFrameTimestamp = 0;
			_hasPendingFrame = false;
			_hasDisplayedFrame = false;
			_endOfStream = false;
			_isPlaying = false;
			_resumeOnEnter = false;
			EnsureTexture(width, height);

			if (Size.X <= 0f || Size.Y <= 0f) {
				Size = new Vector2(width, height);
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

		_videoReader?.Dispose();
		_videoReader = null;

		_audioTrack?.Dispose();
		_audioTrack = null;

		_mediaFoundationLease.Dispose();
		_mediaFoundationLease = default;

		_rgbaFrame = null;
		_sourcePath = null;
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
		if (_videoReader is null) {
			return false;
		}

		var sample = _videoReader.ReadSample(SourceReaderIndex.FirstVideoStream, SourceReaderControlFlag.None, out _, out var flags, out var timestamp);

		if ((flags & SourceReaderFlag.EndOfStream) != 0) {
			_endOfStream = true;
			_isPlaying = false;
			_audioTrack?.Pause();
		}

		if ((flags & SourceReaderFlag.Error) != 0) {
			throw new InvalidOperationException($"Media Foundation reported a read error for '{_sourcePath}'.");
		}

		if (sample is null) {
			return false;
		}

		_pendingFrameTimestamp = timestamp;
		CopySampleToFrame(sample);
		_hasPendingFrame = true;
		return true;
	}

	private static double ReadPackedRatio(IMFAttributes attributes, Guid key) {
		var result = attributes.GetUInt64(key, out var packedRatio);
		if (result.Failure) {
			return 0d;
		}

		var numerator = (uint)(packedRatio >> 32);
		var denominator = (uint)(packedRatio & 0xffffffff);
		return denominator == 0 ? 0d : (double)numerator / denominator;
	}

	private static double ReadDurationSeconds(IMFSourceReader reader) {
		try {
			var duration = reader.GetPresentationAttribute(SourceReaderIndex.MediaSource, PresentationDescriptionAttributeKeys.Duration);
			var ticks = Convert.ToInt64(duration.Value);
			return ticks <= 0 ? 0d : ticks / (double)TicksPerSecond;
		} catch {
			return 0d;
		}
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

	private sealed unsafe class AudioTrack : IDisposable {
		private const int BufferFrameCount = 4096;

		private readonly IMFSourceReader _reader;
		private readonly Queue<short> _queuedSamples = new();
		private AudioStream _stream;
		private bool _endOfStream;
		private bool _started;
		private int _channels;
		private float _volume = 1f;

		private AudioTrack(IMFSourceReader reader, AudioStream stream) {
			_reader = reader;
			_stream = stream;
			_channels = (int)stream.Channels;
			FillNextAudioBuffer();
			FillNextAudioBuffer();
		}

		public static AudioTrack? TryOpen(string path) {
			try {
				var reader = MediaFactory.MFCreateSourceReaderFromURL(path, null);
				reader.SetStreamSelection(SourceReaderIndex.AllStreams, false);
				reader.SetStreamSelection(SourceReaderIndex.FirstAudioStream, true);

				var outputType = MediaFactory.MFCreateMediaType();
				outputType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Audio).CheckError();
				outputType.Set(MediaTypeAttributeKeys.Subtype, AudioFormatGuids.Pcm).CheckError();
				outputType.Set(MediaTypeAttributeKeys.AudioBitsPerSample, 16).CheckError();
				reader.SetCurrentMediaType(SourceReaderIndex.FirstAudioStream, outputType);

				var currentType = reader.GetCurrentMediaType(SourceReaderIndex.FirstAudioStream);
				var sampleRate = currentType.GetUInt32(MediaTypeAttributeKeys.AudioSamplesPerSecond);
				var channels = currentType.GetUInt32(MediaTypeAttributeKeys.AudioNumChannels);

				if (sampleRate == 0 || channels == 0) {
					reader.Dispose();
					return null;
				}

				Raylib.SetAudioStreamBufferSizeDefault(BufferFrameCount);
				var stream = Raylib.LoadAudioStream(sampleRate, 16, channels);
				return new AudioTrack(reader, stream);
			} catch {
				return null;
			}
		}

		public void Update() {
			if (_endOfStream || _stream.Buffer == 0) {
				return;
			}

			while (Raylib.IsAudioStreamProcessed(_stream)) {
				if (!FillNextAudioBuffer()) {
					return;
				}
			}
		}

		public void Play() {
			if (_stream.Buffer == 0) {
				return;
			}

			if (!_started) {
				Raylib.PlayAudioStream(_stream);
				_started = true;
				return;
			}

			Raylib.ResumeAudioStream(_stream);
		}

		public void Seek(long positionTicks) {
			_reader.SetCurrentPosition(positionTicks);
			_queuedSamples.Clear();
			_endOfStream = false;
			FillNextAudioBuffer();
			FillNextAudioBuffer();
		}

		public void SetVolume(float volume) {
			_volume = Math.Clamp(volume, 0f, 1f);

			if (_stream.Buffer != 0) {
				Raylib.SetAudioStreamVolume(_stream, _volume);
			}
		}

		public void Pause() {
			if (_stream.Buffer != 0) {
				Raylib.PauseAudioStream(_stream);
			}
		}

		public void Dispose() {
			if (_stream.Buffer != 0) {
				Raylib.StopAudioStream(_stream);
				Raylib.UnloadAudioStream(_stream);
				_stream = default;
			}

			_reader.Dispose();
		}

		private bool FillNextAudioBuffer() {
			var targetSampleCount = BufferFrameCount * _channels;
			QueueDecodedSamples(targetSampleCount);

			if (_queuedSamples.Count == 0) {
				return false;
			}

			var samples = new short[targetSampleCount];
			var writableSamples = Math.Min(samples.Length, _queuedSamples.Count);
			for (var i = 0; i < writableSamples; i++) {
				samples[i] = _queuedSamples.Dequeue();
			}

			Raylib.UpdateAudioStream(_stream, samples, BufferFrameCount);
			return true;
		}

		private void QueueDecodedSamples(int minimumSampleCount) {
			while (!_endOfStream && _queuedSamples.Count < minimumSampleCount) {
				var samples = ReadNextSamples();
				if (samples is null) {
					return;
				}

				foreach (var sample in samples) {
					_queuedSamples.Enqueue(sample);
				}
			}
		}

		private short[]? ReadNextSamples() {
			var sample = _reader.ReadSample(SourceReaderIndex.FirstAudioStream, SourceReaderControlFlag.None, out _, out var flags, out _);

			if ((flags & SourceReaderFlag.EndOfStream) != 0) {
				_endOfStream = true;
			}

			if ((flags & SourceReaderFlag.Error) != 0) {
				throw new InvalidOperationException("Media Foundation reported an audio read error.");
			}

			if (sample is null) {
				return null;
			}

			var buffer = sample.ConvertToContiguousBuffer();
			try {
				buffer.Lock(out var sourcePointer, out _, out var currentLength);
				var sampleCount = currentLength / sizeof(short);
				var samples = new short[sampleCount];
				Marshal.Copy(sourcePointer, samples, 0, sampleCount);
				return samples;
			} finally {
				buffer.Unlock();
				buffer.Dispose();
				sample.Dispose();
			}
		}
	}
}

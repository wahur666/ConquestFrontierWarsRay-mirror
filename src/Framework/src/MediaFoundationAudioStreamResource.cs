using System.Runtime.InteropServices;
using Raylib_cs;
using SharpGen.Runtime;
using Vortice.MediaFoundation;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Media Foundation-backed streamed audio resource for embedded media audio.
/// </summary>
public sealed class MediaFoundationAudioStreamResource : AudioStreamResource {
	private const int BufferFrameCount = 4096;
	private const long TicksPerSecond = 10_000_000;

	private readonly Queue<short> _queuedSamples = new();
	private IMFSourceReader? _reader;
	private AudioStream _stream;
	private bool _endOfStream;
	private bool _started;
	private int _channels;
	private float _timeLength;
	private float _timePlayed;
	private float _volume = 1f;

	/// <summary>
	/// Creates an audio stream resource from a Media Foundation-compatible file.
	/// </summary>
	public MediaFoundationAudioStreamResource(string path) : base(path) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
	}

	/// <inheritdoc />
	public override bool Looping {
		get => false;
		set {
		}
	}

	/// <inheritdoc />
	public override float Volume {
		get => _volume;
		set {
			_volume = Math.Clamp(value, 0f, 1f);

			if (_stream.Buffer != 0) {
				Raylib.SetAudioStreamVolume(_stream, _volume);
			}
		}
	}

	/// <inheritdoc />
	public override float Pitch {
		get => 1f;
		set {
			if (value <= 0f) {
				throw new ArgumentOutOfRangeException(nameof(value), "Pitch must be greater than zero.");
			}

			if (Math.Abs(value - 1f) > float.Epsilon) {
				throw new NotSupportedException("Media Foundation audio playback only supports a pitch of 1.0.");
			}
		}
	}

	/// <inheritdoc />
	public override float Pan {
		get => 0.5f;
		set {
			var clampedValue = Math.Clamp(value, 0f, 1f);
			if (Math.Abs(clampedValue - 0.5f) > float.Epsilon) {
				throw new NotSupportedException("Media Foundation audio playback only supports centered pan.");
			}
		}
	}

	/// <inheritdoc />
	public override bool IsPlaying {
		get {
			EnsureLoaded();
			return _started && _stream.Buffer != 0 && Raylib.IsAudioStreamPlaying(_stream);
		}
	}

	/// <inheritdoc />
	public override float TimePlayed => _timePlayed;

	/// <inheritdoc />
	public override float TimeLength => _timeLength;

	/// <inheritdoc />
	public override void Play() {
		EnsureLoaded();

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

	/// <inheritdoc />
	public override void Pause() {
		EnsureLoaded();

		if (_stream.Buffer != 0) {
			Raylib.PauseAudioStream(_stream);
		}
	}

	/// <inheritdoc />
	public override void Stop() {
		EnsureLoaded();

		if (_stream.Buffer != 0) {
			Raylib.StopAudioStream(_stream);
		}

		_started = false;
		Seek(0f);
	}

	/// <inheritdoc />
	public override void Seek(float positionSeconds) {
		EnsureLoaded();

		if (_reader is null) {
			return;
		}

		var clampedPosition = Math.Clamp(positionSeconds, 0f, TimeLength > 0f ? TimeLength : float.MaxValue);
		_reader.SetCurrentPosition(SecondsToTicks(clampedPosition));
		_queuedSamples.Clear();
		_endOfStream = false;
		_timePlayed = clampedPosition;
		FillNextAudioBuffer();
		FillNextAudioBuffer();
	}

	/// <inheritdoc />
	public override void Update() {
		EnsureLoaded();

		if (_endOfStream || _stream.Buffer == 0) {
			return;
		}

		while (Raylib.IsAudioStreamProcessed(_stream)) {
			if (!FillNextAudioBuffer()) {
				return;
			}
		}
	}

	protected override void LoadCore() {
		var reader = MediaFactory.MFCreateSourceReaderFromURL(ResourcePath!, null);
		try {
			reader.SetStreamSelection(SourceReaderIndex.AllStreams, false);
			reader.SetStreamSelection(SourceReaderIndex.FirstAudioStream, true);

			using var outputType = MediaFactory.MFCreateMediaType();
			outputType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Audio).CheckError();
			outputType.Set(MediaTypeAttributeKeys.Subtype, AudioFormatGuids.Pcm).CheckError();
			outputType.Set(MediaTypeAttributeKeys.AudioBitsPerSample, 16).CheckError();
			reader.SetCurrentMediaType(SourceReaderIndex.FirstAudioStream, outputType);

			using var currentType = reader.GetCurrentMediaType(SourceReaderIndex.FirstAudioStream);
			var sampleRate = currentType.GetUInt32(MediaTypeAttributeKeys.AudioSamplesPerSecond);
			var channels = currentType.GetUInt32(MediaTypeAttributeKeys.AudioNumChannels);

			if (sampleRate == 0 || channels == 0) {
				throw new InvalidOperationException($"Failed to read a valid audio stream from '{ResourcePath}'.");
			}

			Raylib.SetAudioStreamBufferSizeDefault(BufferFrameCount);
			var stream = Raylib.LoadAudioStream(sampleRate, 16, channels);

			_reader = reader;
			_stream = stream;
			_channels = (int)channels;
			_endOfStream = false;
			_started = false;
			_timePlayed = 0f;
			_timeLength = ReadDurationSeconds(reader);
			Raylib.SetAudioStreamVolume(_stream, _volume);
			FillNextAudioBuffer();
			FillNextAudioBuffer();
		} catch {
			reader.Dispose();
			throw;
		}
	}

	protected override void UnloadCore() {
		if (_stream.Buffer != 0) {
			Raylib.StopAudioStream(_stream);
			Raylib.UnloadAudioStream(_stream);
			_stream = default;
		}

		_reader?.Dispose();
		_reader = null;
		_queuedSamples.Clear();
		_endOfStream = false;
		_started = false;
		_channels = 0;
		_timePlayed = 0f;
		_timeLength = 0f;
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
		if (_reader is null) {
			return null;
		}

		var sample = _reader.ReadSample(SourceReaderIndex.FirstAudioStream, SourceReaderControlFlag.None, out _, out var flags, out var timestamp);

		if ((flags & SourceReaderFlag.EndOfStream) != 0) {
			_endOfStream = true;
		}

		if ((flags & SourceReaderFlag.Error) != 0) {
			throw new InvalidOperationException("Media Foundation reported an audio read error.");
		}

		if (sample is null) {
			return null;
		}

		_timePlayed = timestamp / (float)TicksPerSecond;

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

	private static float ReadDurationSeconds(IMFSourceReader reader) {
		try {
			var duration = reader.GetPresentationAttribute(SourceReaderIndex.MediaSource, PresentationDescriptionAttributeKeys.Duration);
			var ticks = Convert.ToInt64(duration.Value);
			return ticks <= 0 ? 0f : ticks / (float)TicksPerSecond;
		} catch {
			return 0f;
		}
	}

	private static long SecondsToTicks(float seconds) {
		return (long)Math.Round(seconds * TicksPerSecond);
	}
}

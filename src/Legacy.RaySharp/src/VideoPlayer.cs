using System.Numerics;
using System.Runtime.InteropServices;
using Raylib_cs;
using SharpGen.Runtime;
using Vortice.MediaFoundation;

namespace RaySharp;

internal sealed unsafe class VideoPlayer : IDisposable
{
    private const long TicksPerSecond = 10_000_000;

    private readonly IMFSourceReader reader;
    private readonly byte[] rgbaFrame;
    private readonly int stride;
    private readonly bool sourceIsTopDown;
    private readonly string sourcePath;
    private readonly AudioTrack? audioTrack;
    private Texture2D texture;
    private double elapsedSeconds;
    private long pendingFrameTimestamp;
    private bool hasPendingFrame;
    private bool hasDisplayedFrame;
    private bool endOfStream;
    private bool isPlaying;

    private VideoPlayer(string sourcePath, IMFSourceReader reader, AudioTrack? audioTrack, int width, int height, int stride, double frameRate)
    {
        this.sourcePath = sourcePath;
        this.reader = reader;
        this.audioTrack = audioTrack;
        Width = width;
        Height = height;
        FrameRate = frameRate;
        this.stride = Math.Abs(stride);
        sourceIsTopDown = stride < 0;
        rgbaFrame = new byte[Width * Height * 4];

        Image image = Raylib.GenImageColor(Width, Height, Color.Blank);
        texture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
    }

    public int Width { get; }

    public int Height { get; }

    public double FrameRate { get; }

    public bool IsFinished => endOfStream;

    public bool IsPlaying => isPlaying;

    public Texture2D Texture => texture;

    public string FileName => Path.GetFileName(sourcePath);

    public static VideoPlayer Open(string path)
    {
        string fullPath = Path.GetFullPath(path);
        IMFAttributes attributes = MediaFactory.MFCreateAttributes(1);
        attributes.Set(SourceReaderAttributeKeys.EnableVideoProcessing, true).CheckError();

        IMFSourceReader reader = MediaFactory.MFCreateSourceReaderFromURL(fullPath, attributes);
        reader.SetStreamSelection(SourceReaderIndex.AllStreams, false);
        reader.SetStreamSelection(SourceReaderIndex.FirstVideoStream, true);

        IMFMediaType outputType = MediaFactory.MFCreateMediaType();
        outputType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video).CheckError();
        outputType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Rgb32).CheckError();
        reader.SetCurrentMediaType(SourceReaderIndex.FirstVideoStream, outputType);

        IMFMediaType currentType = reader.GetCurrentMediaType(SourceReaderIndex.FirstVideoStream);
        ulong packedFrameSize = currentType.GetUInt64(MediaTypeAttributeKeys.FrameSize);
        int width = (int)(packedFrameSize >> 32);
        int height = (int)(packedFrameSize & 0xffffffff);
        int stride = width * 4;
        double frameRate = ReadPackedRatio(currentType, MediaTypeAttributeKeys.FrameRate);

        Result strideResult = currentType.GetUInt32(MediaTypeAttributeKeys.DefaultStride, out uint mediaStride);
        if (strideResult.Success)
        {
            stride = unchecked((int)mediaStride);
        }

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException($"Could not read a valid video size from '{fullPath}'.");
        }

        return new VideoPlayer(fullPath, reader, AudioTrack.TryOpen(fullPath), width, height, stride, frameRate);
    }

    public void Update(float deltaTime)
    {
        if (!isPlaying || endOfStream)
        {
            return;
        }

        audioTrack?.Update();
        elapsedSeconds += deltaTime;
        long targetTimestamp = (long)(elapsedSeconds * TicksPerSecond);

        while (!endOfStream)
        {
            if (!hasPendingFrame && !ReadNextFrame())
            {
                return;
            }

            if (hasDisplayedFrame && pendingFrameTimestamp > targetTimestamp)
            {
                return;
            }

            fixed (byte* pixels = rgbaFrame)
            {
                Raylib.UpdateTexture(texture, pixels);
            }

            hasPendingFrame = false;
            hasDisplayedFrame = true;

            if (pendingFrameTimestamp >= targetTimestamp)
            {
                return;
            }
        }
    }

    public void Play()
    {
        if (endOfStream)
        {
            return;
        }

        isPlaying = true;
        audioTrack?.Play();
    }

    public void Pause()
    {
        isPlaying = false;
        audioTrack?.Pause();
    }

    public void Draw(Rectangle destination, Color tint)
    {
        if (texture.Id == 0)
        {
            return;
        }

        Rectangle source = new(0.0f, 0.0f, Width, Height);
        Raylib.DrawTexturePro(texture, source, destination, Vector2.Zero, 0.0f, tint);
    }

    public void Dispose()
    {
        if (texture.Id != 0)
        {
            Raylib.UnloadTexture(texture);
            texture = default;
        }

        reader.Dispose();
        audioTrack?.Dispose();
    }

    private bool ReadNextFrame()
    {
        IMFSample? sample = reader.ReadSample(SourceReaderIndex.FirstVideoStream, SourceReaderControlFlag.None, out _, out SourceReaderFlag flags, out long timestamp);

        if ((flags & SourceReaderFlag.EndOfStream) != 0)
        {
            endOfStream = true;
            isPlaying = false;
            audioTrack?.Pause();
        }

        if ((flags & SourceReaderFlag.Error) != 0)
        {
            throw new InvalidOperationException($"Media Foundation reported a read error for '{sourcePath}'.");
        }

        if (sample is null)
        {
            return false;
        }

        pendingFrameTimestamp = timestamp;
        CopySampleToFrame(sample);
        hasPendingFrame = true;
        return true;
    }

    private static double ReadPackedRatio(IMFAttributes attributes, Guid key)
    {
        Result result = attributes.GetUInt64(key, out ulong packedRatio);
        if (result.Failure)
        {
            return 0.0;
        }

        uint numerator = (uint)(packedRatio >> 32);
        uint denominator = (uint)(packedRatio & 0xffffffff);
        return denominator == 0 ? 0.0 : (double)numerator / denominator;
    }

    private void CopySampleToFrame(IMFSample sample)
    {
        IMFMediaBuffer buffer = sample.ConvertToContiguousBuffer();
        try
        {
            buffer.Lock(out nint sourcePointer, out _, out int currentLength);
            CopyBgr32ToRgba((byte*)sourcePointer, currentLength);
        }
        finally
        {
            buffer.Unlock();
            buffer.Dispose();
            sample.Dispose();
        }
    }

    private void CopyBgr32ToRgba(byte* source, int sourceLength)
    {
        int rowBytes = Width * 4;
        if (sourceLength < rowBytes * Height)
        {
            return;
        }

        fixed (byte* destinationBase = rgbaFrame)
        {
            for (int y = 0; y < Height; y++)
            {
                int sourceY = sourceIsTopDown ? Height - 1 - y : y;
                byte* sourceRow = source + (sourceY * stride);
                byte* destinationRow = destinationBase + (y * rowBytes);

                for (int x = 0; x < Width; x++)
                {
                    byte* sourcePixel = sourceRow + (x * 4);
                    byte* destinationPixel = destinationRow + (x * 4);

                    destinationPixel[0] = sourcePixel[2];
                    destinationPixel[1] = sourcePixel[1];
                    destinationPixel[2] = sourcePixel[0];
                    destinationPixel[3] = 255;
                }
            }
        }
    }

    private sealed unsafe class AudioTrack : IDisposable
    {
        private const int BufferFrameCount = 4096;

        private readonly IMFSourceReader reader;
        private readonly Queue<short> queuedSamples = new();
        private AudioStream stream;
        private bool endOfStream;
        private bool started;
        private int channels;

        private AudioTrack(IMFSourceReader reader, AudioStream stream)
        {
            this.reader = reader;
            this.stream = stream;
            channels = (int)stream.Channels;
            FillNextAudioBuffer();
            FillNextAudioBuffer();
        }

        public static AudioTrack? TryOpen(string path)
        {
            try
            {
                IMFSourceReader reader = MediaFactory.MFCreateSourceReaderFromURL(path, null);
                reader.SetStreamSelection(SourceReaderIndex.AllStreams, false);
                reader.SetStreamSelection(SourceReaderIndex.FirstAudioStream, true);

                IMFMediaType outputType = MediaFactory.MFCreateMediaType();
                outputType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Audio).CheckError();
                outputType.Set(MediaTypeAttributeKeys.Subtype, AudioFormatGuids.Pcm).CheckError();
                outputType.Set(MediaTypeAttributeKeys.AudioBitsPerSample, 16).CheckError();
                reader.SetCurrentMediaType(SourceReaderIndex.FirstAudioStream, outputType);

                IMFMediaType currentType = reader.GetCurrentMediaType(SourceReaderIndex.FirstAudioStream);
                uint sampleRate = currentType.GetUInt32(MediaTypeAttributeKeys.AudioSamplesPerSecond);
                uint channels = currentType.GetUInt32(MediaTypeAttributeKeys.AudioNumChannels);

                if (sampleRate == 0 || channels == 0)
                {
                    reader.Dispose();
                    return null;
                }

                Raylib.SetAudioStreamBufferSizeDefault(BufferFrameCount);
                AudioStream stream = Raylib.LoadAudioStream(sampleRate, 16, channels);
                return new AudioTrack(reader, stream);
            }
            catch
            {
                return null;
            }
        }

        public void Update()
        {
            if (endOfStream || stream.Buffer == 0)
            {
                return;
            }

            while (Raylib.IsAudioStreamProcessed(stream))
            {
                if (!FillNextAudioBuffer())
                {
                    return;
                }
            }
        }

        public void Play()
        {
            if (stream.Buffer == 0)
            {
                return;
            }

            if (!started)
            {
                Raylib.PlayAudioStream(stream);
                started = true;
                return;
            }

            Raylib.ResumeAudioStream(stream);
        }

        public void Pause()
        {
            if (stream.Buffer != 0)
            {
                Raylib.PauseAudioStream(stream);
            }
        }

        public void Dispose()
        {
            if (stream.Buffer != 0)
            {
                Raylib.StopAudioStream(stream);
                Raylib.UnloadAudioStream(stream);
                stream = default;
            }

            reader.Dispose();
        }

        private bool FillNextAudioBuffer()
        {
            int targetSampleCount = BufferFrameCount * channels;
            QueueDecodedSamples(targetSampleCount);

            if (queuedSamples.Count == 0)
            {
                return false;
            }

            short[] samples = new short[targetSampleCount];
            int writableSamples = Math.Min(samples.Length, queuedSamples.Count);
            for (int i = 0; i < writableSamples; i++)
            {
                samples[i] = queuedSamples.Dequeue();
            }

            Raylib.UpdateAudioStream(stream, samples, BufferFrameCount);
            return true;
        }

        private void QueueDecodedSamples(int minimumSampleCount)
        {
            while (!endOfStream && queuedSamples.Count < minimumSampleCount)
            {
                short[]? samples = ReadNextSamples();
                if (samples is null)
                {
                    return;
                }

                foreach (short sample in samples)
                {
                    queuedSamples.Enqueue(sample);
                }
            }
        }

        private short[]? ReadNextSamples()
        {
            IMFSample? sample = reader.ReadSample(SourceReaderIndex.FirstAudioStream, SourceReaderControlFlag.None, out _, out SourceReaderFlag flags, out _);

            if ((flags & SourceReaderFlag.EndOfStream) != 0)
            {
                endOfStream = true;
            }

            if ((flags & SourceReaderFlag.Error) != 0)
            {
                throw new InvalidOperationException("Media Foundation reported an audio read error.");
            }

            if (sample is null)
            {
                return null;
            }

            IMFMediaBuffer buffer = sample.ConvertToContiguousBuffer();
            try
            {
                buffer.Lock(out nint sourcePointer, out _, out int currentLength);
                int sampleCount = currentLength / sizeof(short);
                short[] samples = new short[sampleCount];
                Marshal.Copy(sourcePointer, samples, 0, sampleCount);
                return samples;
            }
            finally
            {
                buffer.Unlock();
                buffer.Dispose();
                sample.Dispose();
            }
        }
    }
}

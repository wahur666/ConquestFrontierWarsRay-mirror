using System.Numerics;
using System.Text.Json;
using Raylib_cs;

namespace RaySharp;

internal sealed class VfxAudioPlayer : IDisposable
{
    private const int TimingFps = 30;

    private readonly Texture2D atlas;
    private readonly Music audio;
    private readonly AtlasFrame[] frames;
    private readonly TimingRow[] timingRows;
    private int currentFrameIndex;
    private int lastTimingFrame = -1;
    private bool isPlaying;
    private bool finished;

    private VfxAudioPlayer(Texture2D atlas, Music audio, AtlasFrame[] frames, TimingRow[] timingRows)
    {
        this.atlas = atlas;
        this.audio = audio;
        this.frames = frames;
        this.timingRows = timingRows;
        currentFrameIndex = ImageIndexAtTimingFrame(0);
        this.audio.Looping = false;
    }

    public string Label => $"Blackwell VFX frame {currentFrameIndex}";

    public bool IsPlaying => isPlaying;

    public static VfxAudioPlayer? TryLoad()
    {
        string? root = FindAssetRoot();
        if (root is null)
        {
            return null;
        }

        string atlasJsonPath = Path.Combine(root, "talkBlackwell2_atlas.json");
        string atlasPngPath = Path.Combine(root, "talkBlackwell2_atlas.png");
        string timingPath = Path.Combine(root, "m01bl03.txt");
        string audioPath = Path.Combine(root, "m01bl03_pcm.wav");

        if (!File.Exists(atlasJsonPath) || !File.Exists(atlasPngPath) || !File.Exists(timingPath) || !File.Exists(audioPath))
        {
            return null;
        }

        try
        {
            AtlasFrame[] frames = LoadFrames(atlasJsonPath);
            TimingRow[] timingRows = LoadTimingRows(timingPath);
            Texture2D atlas = Raylib.LoadTexture(atlasPngPath);
            Music audio = Raylib.LoadMusicStream(audioPath);

            if (atlas.Id == 0 || frames.Length == 0 || timingRows.Length == 0)
            {
                if (atlas.Id != 0)
                {
                    Raylib.UnloadTexture(atlas);
                }

                if (Raylib.IsMusicValid(audio))
                {
                    Raylib.UnloadMusicStream(audio);
                }

                return null;
            }

            Raylib.SetTextureFilter(atlas, TextureFilter.Bilinear);
            return new VfxAudioPlayer(atlas, audio, frames, timingRows);
        }
        catch
        {
            return null;
        }
    }

    public void Update()
    {
        if (!isPlaying || finished)
        {
            return;
        }

        Raylib.UpdateMusicStream(audio);

        float audioTime = Raylib.GetMusicTimePlayed(audio);
        int timingFrame = (int)MathF.Floor(audioTime * TimingFps);
        ApplyTimingFrame(timingFrame);

        if (!Raylib.IsMusicStreamPlaying(audio) && audioTime >= Raylib.GetMusicTimeLength(audio) - 0.01f)
        {
            finished = true;
            isPlaying = false;
        }
    }

    public void Play()
    {
        if (!Raylib.IsMusicValid(audio))
        {
            return;
        }

        if (finished)
        {
            Stop();
        }

        isPlaying = true;
        Raylib.PlayMusicStream(audio);
    }

    public void Stop()
    {
        if (!Raylib.IsMusicValid(audio))
        {
            return;
        }

        Raylib.StopMusicStream(audio);
        Raylib.SeekMusicStream(audio, 0.0f);
        isPlaying = false;
        finished = false;
        lastTimingFrame = -1;
        currentFrameIndex = ImageIndexAtTimingFrame(0);
    }

    public void Draw(Rectangle destination, Color tint)
    {
        if (atlas.Id == 0 || frames.Length == 0)
        {
            return;
        }

        AtlasFrame frame = frames[Math.Clamp(currentFrameIndex, 0, frames.Length - 1)];
        Rectangle source = new(frame.X, frame.Y, frame.Width, frame.Height);
        Raylib.DrawTexturePro(atlas, source, destination, Vector2.Zero, 0.0f, tint);
    }

    public void Dispose()
    {
        if (Raylib.IsMusicValid(audio))
        {
            Raylib.StopMusicStream(audio);
            Raylib.UnloadMusicStream(audio);
        }

        if (atlas.Id != 0)
        {
            Raylib.UnloadTexture(atlas);
        }
    }

    private void ApplyTimingFrame(int timingFrame)
    {
        if (timingFrame == lastTimingFrame)
        {
            return;
        }

        lastTimingFrame = timingFrame;
        if ((uint)timingFrame >= timingRows.Length)
        {
            finished = true;
            return;
        }

        int sourceIndex = timingRows[timingFrame].SourceIndex;
        if (sourceIndex >= 0)
        {
            currentFrameIndex = Math.Clamp(sourceIndex, 0, frames.Length - 1);
        }
    }

    private int ImageIndexAtTimingFrame(int timingFrame)
    {
        int imageIndex = 0;
        int rowCount = Math.Min(timingRows.Length, timingFrame + 1);

        for (int i = 0; i < rowCount; i++)
        {
            int sourceIndex = timingRows[i].SourceIndex;
            if (sourceIndex >= 0)
            {
                imageIndex = sourceIndex;
            }
        }

        return Math.Clamp(imageIndex, 0, frames.Length - 1);
    }

    private static AtlasFrame[] LoadFrames(string jsonPath)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(jsonPath));
        JsonElement framesElement = document.RootElement.GetProperty("frames");
        List<AtlasFrame> frames = [];

        foreach (JsonElement frame in framesElement.EnumerateArray())
        {
            frames.Add(new AtlasFrame(
                frame.GetProperty("index").GetInt32(),
                frame.GetProperty("x").GetSingle(),
                frame.GetProperty("y").GetSingle(),
                frame.GetProperty("width").GetSingle(),
                frame.GetProperty("height").GetSingle()));
        }

        return frames
            .OrderBy(frame => frame.Index)
            .ToArray();
    }

    private static TimingRow[] LoadTimingRows(string timingPath)
    {
        return File.ReadLines(timingPath)
            .Skip(1)
            .Select(ParseTimingRow)
            .Where(row => row is not null)
            .Select(row => row!.Value)
            .ToArray();
    }

    private static TimingRow? ParseTimingRow(string line)
    {
        string[] parts = line.Split(',', 2);
        if (parts.Length == 0 || !int.TryParse(parts[0], out int frame))
        {
            return null;
        }

        string channel = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        int sourceIndex = -1;

        if (channel.StartsWith("test", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(channel[4..], out int parsedIndex))
        {
            sourceIndex = parsedIndex;
        }

        return new TimingRow(frame, sourceIndex);
    }

    private static string? FindAssetRoot()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "web-vfx-player"),
            Path.Combine(Environment.CurrentDirectory, "web-vfx-player"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "web-vfx-player"),
            Path.Combine(Environment.CurrentDirectory, "..", "web-vfx-player")
        ];

        foreach (string candidate in candidates)
        {
            string fullPath = Path.GetFullPath(candidate);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    private readonly record struct AtlasFrame(int Index, float X, float Y, float Width, float Height);

    private readonly record struct TimingRow(int Frame, int SourceIndex);
}

using System.Numerics;
using System.Text.Json;
using Raylib_cs;

namespace RaySharp;

internal sealed class VfxAudioPlayer : IDisposable {
	private const int TimingFps = 30;
	private const int FuzzFps = 30;
	private const float LegacyPortraitWidth = 62.0f;
	private const float LegacyPortraitHeight = 79.0f;
	private const float BorderLeftInset = 5.0f;
	private const float BorderTopInset = 8.0f;
	private const float BorderRightInset = 13.0f;
	private const float BorderBottomInset = 2.0f;
	private const int FuzzTileColumns = 4;
	private const int FuzzTileRows = 4;

	private readonly Texture2D atlas;
	private readonly Texture2D borderAtlas;
	private readonly Texture2D fuzzTexture;
	private readonly Music audio;
	private readonly AtlasFrame[] frames;
	private readonly AtlasFrame[] borderFrames;
	private readonly TimingRow[] timingRows;
	private int currentFrameIndex;
	private int lastTimingFrame = -1;
	private int lastFuzzFrame = -1;
	private int fuzzIndex;
	private bool isPlaying;
	private bool finished;

	private VfxAudioPlayer(
		Texture2D atlas,
		Texture2D borderAtlas,
		Texture2D fuzzTexture,
		Music audio,
		AtlasFrame[] frames,
		AtlasFrame[] borderFrames,
		TimingRow[] timingRows) {
		this.atlas = atlas;
		this.borderAtlas = borderAtlas;
		this.fuzzTexture = fuzzTexture;
		this.audio = audio;
		this.frames = frames;
		this.borderFrames = borderFrames;
		this.timingRows = timingRows;
		currentFrameIndex = ImageIndexAtTimingFrame(0);
		this.audio.Looping = false;
	}

	public string Label => $"Blackwell VFX frame {currentFrameIndex}";

	public bool IsPlaying => isPlaying;

	public static VfxAudioPlayer? TryLoad() {
		string? atlasJsonPath = FindOptionalAssetPath("vfx", "talkBlackwell2_atlas.json");
		string? atlasPngPath = FindOptionalAssetPath("vfx", "talkBlackwell2_atlas.png");
		string? timingPath = FindOptionalAssetPath("vfx", "m01bl03.txt");
		string? audioPath = FindOptionalAssetPath("vfx", "m01bl03_pcm.wav");
		string? borderAtlasJsonPath = FindOptionalAssetPath("interface", "talkingHeadBorder_atlas.json");
		string? borderAtlasPngPath = FindOptionalAssetPath("interface", "talkingHeadBorder_atlas.png");
		string? fuzzTexturePath = FindOptionalAssetPath("textures", "videoFX.png");

		List<string?> assets = [atlasJsonPath, atlasPngPath, timingPath, audioPath, borderAtlasJsonPath, borderAtlasPngPath, fuzzTexturePath];
		
		
		if (!assets.All(File.Exists)) {
			Console.WriteLine("Missing files");
			return null;
		}

		try {
			AtlasFrame[] frames = LoadFrames(atlasJsonPath);
			AtlasFrame[] borderFrames = LoadOptionalFrames(borderAtlasJsonPath);
			TimingRow[] timingRows = LoadTimingRows(timingPath);
			Texture2D atlas = Raylib.LoadTexture(atlasPngPath);
			Texture2D borderAtlas = LoadOptionalTexture(borderAtlasPngPath);
			Texture2D fuzzTexture = LoadOptionalTexture(fuzzTexturePath);
			Music audio = Raylib.LoadMusicStream(audioPath);

			if (atlas.Id == 0 || frames.Length == 0 || timingRows.Length == 0) {
				if (atlas.Id != 0) {
					Raylib.UnloadTexture(atlas);
				}

				if (Raylib.IsMusicValid(audio)) {
					Raylib.UnloadMusicStream(audio);
				}

				if (borderAtlas.Id != 0) {
					Raylib.UnloadTexture(borderAtlas);
				}

				if (fuzzTexture.Id != 0) {
					Raylib.UnloadTexture(fuzzTexture);
				}

				return null;
			}

			Raylib.SetTextureFilter(atlas, TextureFilter.Bilinear);
			if (borderAtlas.Id != 0) {
				Raylib.SetTextureFilter(borderAtlas, TextureFilter.Bilinear);
			}

			if (fuzzTexture.Id != 0) {
				Raylib.SetTextureFilter(fuzzTexture, TextureFilter.Point);
			}

			return new VfxAudioPlayer(atlas, borderAtlas, fuzzTexture, audio, frames, borderFrames, timingRows);
		} catch {
			return null;
		}
	}

	public void Update() {
		if (!isPlaying || finished) {
			return;
		}

		Raylib.UpdateMusicStream(audio);

		float audioTime = Raylib.GetMusicTimePlayed(audio);
		int timingFrame = (int)MathF.Floor(audioTime * TimingFps);
		int fuzzFrame = (int)MathF.Floor(audioTime * FuzzFps);
		ApplyTimingFrame(timingFrame);
		ApplyFuzzFrame(fuzzFrame);

		if (!Raylib.IsMusicStreamPlaying(audio) && audioTime >= Raylib.GetMusicTimeLength(audio) - 0.01f) {
			finished = true;
			isPlaying = false;
		}
	}

	public void Play() {
		if (!Raylib.IsMusicValid(audio)) {
			return;
		}

		if (finished) {
			Stop();
		}

		isPlaying = true;
		Raylib.PlayMusicStream(audio);
	}

	public void Stop() {
		if (!Raylib.IsMusicValid(audio)) {
			return;
		}

		Raylib.StopMusicStream(audio);
		Raylib.SeekMusicStream(audio, 0.0f);
		isPlaying = false;
		finished = false;
		lastTimingFrame = -1;
		lastFuzzFrame = -1;
		currentFrameIndex = ImageIndexAtTimingFrame(0);
		fuzzIndex = 0;
	}

	public void Draw(Rectangle destination, Color tint) {
		if (atlas.Id == 0 || frames.Length == 0) {
			return;
		}

		AtlasFrame frame = frames[Math.Clamp(currentFrameIndex, 0, frames.Length - 1)];
		Rectangle source = new(frame.X, frame.Y, frame.Width, frame.Height);
		Raylib.DrawTexturePro(atlas, source, destination, Vector2.Zero, 0.0f, tint);

		DrawFuzz(destination);
		DrawBorder(destination, tint);
	}

	public void Dispose() {
		if (Raylib.IsMusicValid(audio)) {
			Raylib.StopMusicStream(audio);
			Raylib.UnloadMusicStream(audio);
		}

		if (atlas.Id != 0) {
			Raylib.UnloadTexture(atlas);
		}

		if (borderAtlas.Id != 0) {
			Raylib.UnloadTexture(borderAtlas);
		}

		if (fuzzTexture.Id != 0) {
			Raylib.UnloadTexture(fuzzTexture);
		}
	}

	private void ApplyTimingFrame(int timingFrame) {
		if (timingFrame == lastTimingFrame) {
			return;
		}

		lastTimingFrame = timingFrame;
		if ((uint)timingFrame >= timingRows.Length) {
			finished = true;
			return;
		}

		int sourceIndex = timingRows[timingFrame].SourceIndex;
		if (sourceIndex >= 0) {
			currentFrameIndex = Math.Clamp(sourceIndex, 0, frames.Length - 1);
		}
	}

	private int ImageIndexAtTimingFrame(int timingFrame) {
		int imageIndex = 0;
		int rowCount = Math.Min(timingRows.Length, timingFrame + 1);

		for (int i = 0; i < rowCount; i++) {
			int sourceIndex = timingRows[i].SourceIndex;
			if (sourceIndex >= 0) {
				imageIndex = sourceIndex;
			}
		}

		return Math.Clamp(imageIndex, 0, frames.Length - 1);
	}

	private void DrawFuzz(Rectangle destination) {
		if (fuzzTexture.Id == 0) {
			return;
		}

		float tileWidth = fuzzTexture.Width / (float)FuzzTileColumns;
		float tileHeight = fuzzTexture.Height / (float)FuzzTileRows;
		float tileX = (fuzzIndex % FuzzTileColumns) * tileWidth;
		float tileY = (fuzzIndex / FuzzTileColumns) * tileHeight;
		Rectangle source = new(tileX, tileY, tileWidth, tileHeight);

		Raylib.BeginBlendMode(BlendMode.Additive);
		Raylib.DrawTexturePro(fuzzTexture, source, destination, Vector2.Zero, 0.0f, Color.White);
		Raylib.EndBlendMode();
	}

	private void ApplyFuzzFrame(int fuzzFrame) {
		if (fuzzFrame == lastFuzzFrame) {
			return;
		}

		lastFuzzFrame = fuzzFrame;
		fuzzIndex = fuzzFrame % (FuzzTileColumns * FuzzTileRows);
	}

	private void DrawBorder(Rectangle destination, Color tint) {
		if (borderAtlas.Id == 0 || borderFrames.Length == 0) {
			return;
		}

		AtlasFrame borderFrame = borderFrames[0];
		Rectangle source = new(borderFrame.X, borderFrame.Y, borderFrame.Width, borderFrame.Height);
		float scaleX = destination.Width / LegacyPortraitWidth;
		float scaleY = destination.Height / LegacyPortraitHeight;
		Rectangle borderDestination = new(
			destination.X - (BorderLeftInset * scaleX),
			destination.Y - (BorderTopInset * scaleY),
			destination.Width + ((BorderLeftInset + BorderRightInset) * scaleX),
			destination.Height + ((BorderTopInset + BorderBottomInset) * scaleY));

		Raylib.DrawTexturePro(borderAtlas, source, borderDestination, Vector2.Zero, 0.0f, tint);
	}

	private static AtlasFrame[] LoadFrames(string jsonPath) {
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(jsonPath));
		JsonElement framesElement = document.RootElement.GetProperty("frames");
		List<AtlasFrame> frames = [];

		foreach (JsonElement frame in framesElement.EnumerateArray()) {
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

	private static AtlasFrame[] LoadOptionalFrames(string? jsonPath) {
		return jsonPath is not null && File.Exists(jsonPath)
			? LoadFrames(jsonPath)
			: [];
	}

	private static TimingRow[] LoadTimingRows(string timingPath) {
		return File.ReadLines(timingPath)
			.Skip(1)
			.Select(ParseTimingRow)
			.Where(row => row is not null)
			.Select(row => row!.Value)
			.ToArray();
	}

	private static TimingRow? ParseTimingRow(string line) {
		string[] parts = line.Split(',', 2);
		if (parts.Length == 0 || !int.TryParse(parts[0], out int frame)) {
			return null;
		}

		string channel = parts.Length > 1 ? parts[1].Trim() : string.Empty;
		int sourceIndex = -1;

		if (channel.StartsWith("test", StringComparison.OrdinalIgnoreCase) &&
		    int.TryParse(channel[4..], out int parsedIndex)) {
			sourceIndex = parsedIndex;
		}

		return new TimingRow(frame, sourceIndex);
	}

	private static Texture2D LoadOptionalTexture(string? texturePath) {
		return texturePath is not null && File.Exists(texturePath)
			? Raylib.LoadTexture(texturePath)
			: default;
	}

	private static string? FindOptionalAssetPath(string folder, string fileName) {
		string[] candidates = [
			Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "assets", folder, fileName)),
			Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "assets", folder, fileName)),
			Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "assets", folder,
				fileName)),
			Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "assets", folder, fileName)),
			Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..",
				"ConquestFrontierWarsRay", "assets", folder, fileName)),
			Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "ConquestFrontierWarsRay", "assets", folder,
				fileName))
		];

		return candidates.FirstOrDefault(File.Exists);
	}

	private readonly record struct AtlasFrame(int Index, float X, float Y, float Width, float Height);

	private readonly record struct TimingRow(int Frame, int SourceIndex);
}

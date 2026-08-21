using System.Globalization;

using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.TalkingHead;

internal static class TalkingHeadClipLoader {
	private const int DefaultFuzzColumns = 4;
	private const int DefaultFuzzRows = 4;

	public static TalkingHeadClip LoadBlackwellDemo(string assetRoot) {
		var vfxRoot = Path.Combine(assetRoot, "vfx");
		var interfaceRoot = Path.Combine(assetRoot, "interface");
		var texturesRoot = Path.Combine(assetRoot, "textures");

		var faceTexture = new CompressedTexture2D(Path.Combine(vfxRoot, "talkBlackwell2_atlas.png")) {
			Filter = TextureFilter.Bilinear
		};
		var faceAtlas = new AtlasDefinitionResource(Path.Combine(vfxRoot, "talkBlackwell2_atlas.json"));
		var faceFrames = SpriteFrames.FromAtlas(faceTexture, faceAtlas);

		var borderTexture = new CompressedTexture2D(Path.Combine(interfaceRoot, "talkingHeadBorder_atlas.png")) {
			Filter = TextureFilter.Bilinear
		};
		var borderAtlas = new AtlasDefinitionResource(Path.Combine(interfaceRoot, "talkingHeadBorder_atlas.json"));
		var borderFrames = SpriteFrames.FromAtlas(borderTexture, borderAtlas);

		var fuzzTexture = new CompressedTexture2D(Path.Combine(texturesRoot, "videoFX.png")) {
			Filter = TextureFilter.Point
		};
		var fuzzFrames = SpriteFrames.FromGrid(fuzzTexture, (int)(fuzzTexture.Size.X / DefaultFuzzColumns), (int)(fuzzTexture.Size.Y / DefaultFuzzRows), frameCount: DefaultFuzzColumns * DefaultFuzzRows, columns: DefaultFuzzColumns, rows: DefaultFuzzRows);

		var voiceAudio = new MusicAudioResource(Path.Combine(vfxRoot, "m01bl03_pcm.wav"));
		var timeline = LoadTimingRows(Path.Combine(vfxRoot, "m01bl03.txt"));

		return new TalkingHeadClip(
			faceFrames,
			voiceAudio,
			timeline,
			fuzzFrames,
			borderFrames,
			ownedResources: [voiceAudio, fuzzFrames, fuzzTexture, borderFrames, borderAtlas, borderTexture, faceFrames, faceAtlas, faceTexture]);
	}

	public static IReadOnlyList<TalkingHeadTimingRow> LoadTimingRows(string timingPath) {
		return File.ReadLines(timingPath)
			.Skip(1)
			.Select(ParseTimingRow)
			.Where(static row => row.HasValue)
			.Select(static row => row!.Value)
			.ToArray();
	}

	private static TalkingHeadTimingRow? ParseTimingRow(string line) {
		if (string.IsNullOrWhiteSpace(line)) {
			return null;
		}

		var parts = line.Split(',', 2);
		if (parts.Length == 0 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) {
			return null;
		}

		var channel = parts.Length > 1 ? parts[1].Trim() : string.Empty;
		var sourceIndex = -1;
		if (channel.StartsWith("test", StringComparison.OrdinalIgnoreCase) &&
		    int.TryParse(channel.AsSpan(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedIndex)) {
			sourceIndex = parsedIndex;
		}

		return new TalkingHeadTimingRow(sourceIndex);
	}
}

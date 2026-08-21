using System.Globalization;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.TalkingHead;

internal static class TalkingHeadClipLoader {
	private const int DefaultFuzzColumns = 4;
	private const int DefaultFuzzRows = 4;

	public static TalkingHeadClip LoadBlackwellDemo() {
		var vfxRepository = VfxAnimationDataRepository.LocateFromRepo();
		var vfxData = vfxRepository.Load();
		var assetRoot = RepoPaths.LocateAssetsRoot();
		var vfxRoot = Path.Combine(assetRoot, "vfx");
		var texturesRoot = Path.Combine(assetRoot, "textures");

		const string talkBlackwell2 = "VFXShape!!AnimateBlackwell2";
		if (!vfxData.TryGetAtlasByVfxShapeId(talkBlackwell2, out var blackwell2Atlas)) {
			throw new InvalidOperationException($"Could not find atlas entry for '{talkBlackwell2}' in vfx-animation-data.json.");
		}
		
		const string talkingHeadBorder = "talkingHeadBorder";
		if (!vfxData.TryGetAtlasByVfxShapeId(talkingHeadBorder, out var talkingHeadBorderAtlas)) {
			throw new InvalidOperationException($"Could not find atlas entry for '{talkingHeadBorder}' in vfx-animation-data.json.");
		}

		var faceResource = vfxRepository.CreateAtlasFramesResource(blackwell2Atlas.Value, TextureFilter.Bilinear);
		var borderResource = vfxRepository.CreateAtlasFramesResource(talkingHeadBorderAtlas.Value, TextureFilter.Bilinear);

		var fuzzTexture = new CompressedTexture2D(Path.Combine(texturesRoot, "videoFX.png")) {
			Filter = TextureFilter.Point
		};
		var fuzzFrames = SpriteFrames.FromGrid(fuzzTexture, (int)(fuzzTexture.Size.X / DefaultFuzzColumns), (int)(fuzzTexture.Size.Y / DefaultFuzzRows), frameCount: DefaultFuzzColumns * DefaultFuzzRows, columns: DefaultFuzzColumns, rows: DefaultFuzzRows);

		var voiceAudio = new MusicAudioResource(Path.Combine(vfxRoot, "m01bl03_pcm.wav"));
		var timeline = LoadTimingRows(Path.Combine(vfxRoot, "m01bl03.txt"));

		return new TalkingHeadClip(
			faceResource.Frames,
			voiceAudio,
			timeline,
			fuzzFrames,
			borderResource.Frames,
			ownedResources: [voiceAudio, fuzzFrames, fuzzTexture, borderResource, faceResource]);
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

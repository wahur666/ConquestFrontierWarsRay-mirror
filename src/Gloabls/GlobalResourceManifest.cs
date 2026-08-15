namespace ConquestFrontierWarsRay.Globals;

public sealed class GlobalResourceManifest {
	public int ScreenHIdealWidth { get; init; }
	public int ScreenHIdealHeight { get; init; }
	public required IReadOnlyList<RgbColor> DefaultColorTable { get; init; }
	public required IReadOnlyList<RgbColor> SectorColorTable { get; init; }

	public static GlobalResourceManifest LoadDefault() {
		return new GlobalResourceManifest {
			ScreenHIdealWidth = 1024,
			ScreenHIdealHeight = 768,
			DefaultColorTable = [
				new RgbColor(100, 100, 100),
				new RgbColor(255, 255, 0),
				new RgbColor(240, 0, 0),
				new RgbColor(56, 52, 255),
				new RgbColor(255, 0, 255),
				new RgbColor(18, 200, 0),
				new RgbColor(255, 150, 0),
				new RgbColor(128, 0, 255),
				new RgbColor(85, 218, 240),
			],
			SectorColorTable = [
				new RgbColor(255, 255, 255),
				new RgbColor(255, 0, 0),
				new RgbColor(0, 255, 0),
				new RgbColor(0, 0, 255),
				new RgbColor(255, 255, 0),
				new RgbColor(0, 255, 255),
				new RgbColor(255, 0, 255),
				new RgbColor(0, 128, 255),
				new RgbColor(255, 0, 128),
				new RgbColor(128, 255, 0),
				new RgbColor(128, 0, 255),
				new RgbColor(255, 128, 0),
				new RgbColor(0, 255, 128),
				new RgbColor(128, 128, 255),
				new RgbColor(255, 128, 128),
				new RgbColor(128, 255, 128),
			],
		};
	}
}

public readonly record struct RgbColor(int R, int G, int B);

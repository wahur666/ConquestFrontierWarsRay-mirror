using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConquestFrontierWarsRay.Globals;

public sealed class GlobalResourceManifest {
	public int ScreenHIdealWidth { get; init; }
	public int ScreenHIdealHeight { get; init; }
	public required IReadOnlyList<RgbColor> DefaultColorTable { get; init; }
	public required IReadOnlyList<RgbColor> SectorColorTable { get; init; }

	public static GlobalResourceManifest LoadDefault() {
		var assembly = typeof(GlobalResourceManifest).Assembly;
		const string resourceName = "ConquestFrontierWarsRay.Globals.Resources.globals-resource-manifest.json";

		using var stream = assembly.GetManifestResourceStream(resourceName)
		                  ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
		using var reader = new StreamReader(stream);
		var json = reader.ReadToEnd();
		var manifest = JsonSerializer.Deserialize<GlobalResourceManifestDto>(json, new JsonSerializerOptions {
			PropertyNameCaseInsensitive = true,
		})
		              ?? throw new InvalidOperationException("Could not deserialize globals resource manifest.");

		return new GlobalResourceManifest {
			ScreenHIdealWidth = manifest.ScreenHIdealWidth,
			ScreenHIdealHeight = manifest.ScreenHIdealHeight,
			DefaultColorTable = manifest.DefaultColorTable.Select(static color => new RgbColor(color[0], color[1], color[2])).ToArray(),
			SectorColorTable = manifest.SectorColorTable.Select(static color => new RgbColor(color[0], color[1], color[2])).ToArray(),
		};
	}

	private sealed class GlobalResourceManifestDto {
		[JsonPropertyName("screenHIdealWidth")]
		public int ScreenHIdealWidth { get; init; }
		[JsonPropertyName("screenHIdealHeight")]
		public int ScreenHIdealHeight { get; init; }
		[JsonPropertyName("defaultColorTable")]
		public required int[][] DefaultColorTable { get; init; }
		[JsonPropertyName("sectorColorTable")]
		public required int[][] SectorColorTable { get; init; }
	}
}

public readonly record struct RgbColor(int R, int G, int B);

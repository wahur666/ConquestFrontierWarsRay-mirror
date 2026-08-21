using System.Text.Json.Serialization;

namespace ConquestFrontierWarsRay.Data.VfxAnimation;

public sealed class VfxAnimationData {
	[JsonPropertyName("image")]
	public Dictionary<string, ImageData> Image { get; init; } = new(StringComparer.OrdinalIgnoreCase);

	[JsonPropertyName("atlas")]
	public Dictionary<string, AtlasData> Atlas { get; init; } = new(StringComparer.OrdinalIgnoreCase);

	public bool TryGetImageByVfxShapeId(string vfxShapeId, out KeyValuePair<string, ImageData> match) {
		ArgumentException.ThrowIfNullOrWhiteSpace(vfxShapeId);

		foreach (var entry in Image) {
			if (string.Equals(entry.Value.VfxShapeId, vfxShapeId, StringComparison.OrdinalIgnoreCase)) {
				match = entry;
				return true;
			}
		}

		match = default;
		return false;
	}

	public bool TryGetAtlasByVfxShapeId(string vfxShapeId, out KeyValuePair<string, AtlasData> match) {
		ArgumentException.ThrowIfNullOrWhiteSpace(vfxShapeId);

		foreach (var entry in Atlas) {
			if (string.Equals(entry.Value.VfxShapeId, vfxShapeId, StringComparison.OrdinalIgnoreCase)) {
				match = entry;
				return true;
			}
		}

		match = default;
		return false;
	}
}

public class ImageData {
	[JsonPropertyName("vfxShapeId")]
	public string VfxShapeId { get; init; } = string.Empty;

	[JsonPropertyName("filename")]
	public string Filename { get; init; } = string.Empty;
}

public sealed class AtlasData : ImageData {
	[JsonPropertyName("metaJson")]
	public string MetaJson { get; init; } = string.Empty;
}

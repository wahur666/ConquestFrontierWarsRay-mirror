using System.Text.Json;

namespace ConquestFrontierWarsRay.Data.VfxAnimation;

public sealed class VfxAnimationDataRepository {
	private static readonly JsonSerializerOptions JsonOptions = new() {
		PropertyNameCaseInsensitive = true
	};

	private readonly string _interfaceAssetsPath;
	private readonly string _jsonPath;

	public VfxAnimationDataRepository(string jsonPath, string interfaceAssetsPath) {
		if (string.IsNullOrWhiteSpace(jsonPath)) {
			throw new ArgumentException("JSON path must not be null or whitespace.", nameof(jsonPath));
		}

		if (string.IsNullOrWhiteSpace(interfaceAssetsPath)) {
			throw new ArgumentException("Interface assets path must not be null or whitespace.", nameof(interfaceAssetsPath));
		}

		_jsonPath = jsonPath;
		_interfaceAssetsPath = interfaceAssetsPath;
	}

	public static VfxAnimationDataRepository LocateFromRepo() {
		return new VfxAnimationDataRepository(
			RepoPaths.LocateVfxAnimationDataPath(),
			RepoPaths.LocateInterfaceAssetsPath());
	}

	public VfxAnimationData Load() {
		if (!File.Exists(_jsonPath)) {
			throw new FileNotFoundException($"Could not locate VFX animation data JSON '{_jsonPath}'.", _jsonPath);
		}

		var json = File.ReadAllText(_jsonPath);
		var data = JsonSerializer.Deserialize<VfxAnimationData>(json, JsonOptions);
		if (data is null) {
			throw new InvalidDataException($"Failed to deserialize VFX animation data from '{_jsonPath}'.");
		}

		return data;
	}

	public bool TryGetAtlasByShapeFile(string shapeFile, out KeyValuePair<string, AtlasData> match) {
		ArgumentException.ThrowIfNullOrWhiteSpace(shapeFile);

		var normalized = Path.GetFileNameWithoutExtension(shapeFile);
		if (string.IsNullOrWhiteSpace(normalized)) {
			match = default;
			return false;
		}

		var data = Load();

		foreach (var entry in data.Atlas) {
			if (string.Equals(entry.Key, normalized, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(entry.Value.VfxShapeId, normalized, StringComparison.OrdinalIgnoreCase)) {
				match = entry;
				return true;
			}
		}

		match = default;
		return false;
	}

	public string GetInterfaceAssetPath(ImageData imageData) {
		ArgumentNullException.ThrowIfNull(imageData);
		return Path.Combine(_interfaceAssetsPath, imageData.Filename);
	}

	public string GetInterfaceAssetPath(AtlasData atlasData, bool metaJson) {
		ArgumentNullException.ThrowIfNull(atlasData);
		return Path.Combine(_interfaceAssetsPath, metaJson ? atlasData.MetaJson : atlasData.Filename);
	}
}

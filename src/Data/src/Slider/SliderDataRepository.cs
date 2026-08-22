using System.Drawing;
using System.Text.Json;
using ConquestFrontierWarsRay.Data.Models.GT;

namespace ConquestFrontierWarsRay.Data.Slider;

public sealed class SliderDataRepository {
	private static readonly JsonSerializerOptions JsonOptions = new() {
		PropertyNameCaseInsensitive = true
	};

	private readonly string _jsonPath;

	public SliderDataRepository(string jsonPath) {
		if (string.IsNullOrWhiteSpace(jsonPath)) {
			throw new ArgumentException("JSON path must not be null or whitespace.", nameof(jsonPath));
		}

		_jsonPath = jsonPath;
	}

	public static SliderDataRepository LocateFromRepo() {
		return new SliderDataRepository(RepoPaths.LocateSliderDataPath());
	}

	public SliderDataCatalog Load() {
		if (!File.Exists(_jsonPath)) {
			throw new FileNotFoundException($"Could not locate slider data JSON '{_jsonPath}'.", _jsonPath);
		}

		var json = File.ReadAllText(_jsonPath);
		var raw = JsonSerializer.Deserialize<Dictionary<string, RawSliderEntry>>(json, JsonOptions);
		if (raw is null || raw.Count == 0) {
			throw new InvalidDataException($"Failed to deserialize slider data from '{_jsonPath}'.");
		}

		var entries = new Dictionary<string, GT_SLIDER>(StringComparer.OrdinalIgnoreCase);
		foreach (var (key, value) in raw) {
			entries[key] = new GT_SLIDER {
				DisabledColor = ParseColor(value.DisabledColor, nameof(value.DisabledColor), key),
				NormalColor = ParseColor(value.NormalColor, nameof(value.NormalColor), key),
				HighlightColor = ParseColor(value.HighlightColor, nameof(value.HighlightColor), key),
				AlertColor = ParseColor(value.AlertColor, nameof(value.AlertColor), key),
				Vertical = value.Vertical,
				Indent = value.Indent,
				ShapeFile = value.ShapeFile ?? string.Empty
			};
		}

		return new SliderDataCatalog(entries);
	}

	private static Color ParseColor(string? hex, string propertyName, string sliderKey) {
		if (string.IsNullOrWhiteSpace(hex)) {
			throw new InvalidDataException($"Slider entry '{sliderKey}' is missing required color '{propertyName}'.");
		}

		try {
			return ColorTranslator.FromHtml(hex);
		} catch (Exception ex) {
			throw new InvalidDataException($"Slider entry '{sliderKey}' has invalid color '{hex}' for '{propertyName}'.", ex);
		}
	}

	private sealed class RawSliderEntry {
		public string DisabledColor { get; init; } = string.Empty;
		public string NormalColor { get; init; } = string.Empty;
		public string HighlightColor { get; init; } = string.Empty;
		public string AlertColor { get; init; } = string.Empty;
		public bool Vertical { get; init; }
		public uint Indent { get; init; }
		public string ShapeFile { get; init; } = string.Empty;
	}
}

public sealed class SliderDataCatalog {
	private readonly IReadOnlyDictionary<string, GT_SLIDER> _entries;

	public SliderDataCatalog(IReadOnlyDictionary<string, GT_SLIDER> entries) {
		_entries = entries ?? throw new ArgumentNullException(nameof(entries));
	}

	public IReadOnlyDictionary<string, GT_SLIDER> Entries => _entries;

	public GT_SLIDER GetRequired(string key) {
		if (!_entries.TryGetValue(key, out var entry)) {
			throw new KeyNotFoundException($"Could not find slider entry '{key}'.");
		}

		return entry;
	}

	public bool TryGet(string key, out GT_SLIDER entry) {
		return _entries.TryGetValue(key, out entry!);
	}
}

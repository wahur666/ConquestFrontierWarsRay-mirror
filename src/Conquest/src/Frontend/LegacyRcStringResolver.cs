using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using ConquestFrontierWarsRay.Data;

namespace ConquestFrontierWarsRay;

internal sealed class LegacyRcStringResolver {
	private readonly IReadOnlyDictionary<uint, string> _valuesById;

	private LegacyRcStringResolver(IReadOnlyDictionary<uint, string> valuesById) {
		_valuesById = valuesById;
	}

	public static LegacyRcStringResolver LoadFromRepo() {
		var path = Path.Combine(
			RepoPaths.LocateRepoRoot(),
			"src",
			"Gloabls",
			"Generated",
			"LegacyGlobalsRc.json");

		if (!File.Exists(path)) {
			throw new FileNotFoundException($"Could not locate legacy RC string data at '{path}'.", path);
		}

		using var document = JsonDocument.Parse(File.ReadAllText(path));
		if (!document.RootElement.TryGetProperty("strings", out var stringsElement) ||
		    !stringsElement.TryGetProperty("entries", out var entriesElement)) {
			throw new InvalidDataException($"Legacy RC string data at '{path}' is missing strings.entries.");
		}

		var valuesById = new Dictionary<uint, string>();
		foreach (var entry in entriesElement.EnumerateArray()) {
			if (!entry.TryGetProperty("id", out var idElement) ||
			    idElement.ValueKind != JsonValueKind.Number ||
			    !idElement.TryGetUInt32(out var id) ||
			    !entry.TryGetProperty("value", out var valueElement)) {
				continue;
			}

			valuesById[id] = valueElement.GetString() ?? string.Empty;
		}

		return new LegacyRcStringResolver(valuesById);
	}

	public bool TryResolve(uint id, out string value) {
		if (id == 0) {
			value = string.Empty;
			return false;
		}

		return _valuesById.TryGetValue(id, out value!);
	}
}

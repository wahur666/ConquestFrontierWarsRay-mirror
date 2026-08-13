namespace ConquestFrontierWarsRay.Data.DosFile;

internal static class DosPath {
	public static string NormalizeSwitchChars(string path) {
		return path.Replace('/', '\\');
	}

	public static string NormalizeContainerPath(string path) {
		var normalized = NormalizeSwitchChars(path);
		if (string.IsNullOrEmpty(normalized)) {
			return "\\";
		}

		if (!normalized.StartsWith('\\')) {
			normalized = '\\' + normalized;
		}

		return normalized;
	}

	public static string NormalizeDirectoryPath(string path) {
		var normalized = NormalizeContainerPath(path);
		return normalized.EndsWith('\\') ? normalized : normalized + '\\';
	}

	public static string CombineContainerPath(string baseDirectory, string input) {
		var normalizedInput = NormalizeSwitchChars(input);
		if (string.IsNullOrEmpty(normalizedInput)) {
			return NormalizeDirectoryPath(baseDirectory);
		}

		if (normalizedInput.StartsWith('\\')) {
			return NormalizePathSegments(normalizedInput);
		}

		return NormalizePathSegments(NormalizeDirectoryPath(baseDirectory) + normalizedInput);
	}

	public static string NormalizePathSegments(string path) {
		var normalized = NormalizeSwitchChars(path);
		var rooted = normalized.StartsWith('\\');
		var parts = normalized.Split('\\', StringSplitOptions.RemoveEmptyEntries);
		var stack = new Stack<string>();

		foreach (var part in parts) {
			if (part == ".") {
				continue;
			}

			if (part == "..") {
				if (stack.Count > 0) {
					stack.Pop();
				}

				continue;
			}

			stack.Push(part);
		}

		var ordered = stack.Reverse().ToArray();
		var prefix = rooted ? "\\" : string.Empty;
		return prefix + string.Join('\\', ordered);
	}

	public static bool PatternMatch(string value, string pattern) {
		var normalizedValue = value.Contains('.') ? value : value + '.';
		var normalizedPattern = pattern.Contains('.')
			? (pattern.StartsWith('.') ? "*" + pattern : pattern)
			: pattern + ".*";

		return PatternMatchCore(normalizedValue, normalizedPattern);
	}

	private static bool PatternMatchCore(ReadOnlySpan<char> value, ReadOnlySpan<char> pattern) {
		if (pattern.IsEmpty) {
			return value.IsEmpty;
		}

		if (pattern[0] == '*') {
			var remainder = pattern[1..];
			if (PatternMatchCore(value, remainder)) {
				return true;
			}

			for (var i = 0; i < value.Length; i++) {
				if (PatternMatchCore(value[(i + 1)..], remainder)) {
					return true;
				}
			}

			return false;
		}

		if (value.IsEmpty) {
			return false;
		}

		if (pattern[0] != '?' &&
		    char.ToUpperInvariant(pattern[0]) != char.ToUpperInvariant(value[0])) {
			return false;
		}

		return PatternMatchCore(value[1..], pattern[1..]);
	}
}

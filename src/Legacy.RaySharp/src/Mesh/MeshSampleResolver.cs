namespace RaySharp.Mesh;

internal static class MeshSampleResolver {
	public static string? Resolve(string fileName) {
		string[] candidates = BuildDirectoryCandidates()
			.Select(directory => Path.Combine(directory, fileName))
			.ToArray();

		return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
	}

	public static string? ResolveSamplesDirectory() {
		string[] candidates = BuildDirectoryCandidates();

		return candidates.Select(Path.GetFullPath).FirstOrDefault(Directory.Exists);
	}

	private static string[] BuildDirectoryCandidates() {
		HashSet<string> candidates = new(StringComparer.OrdinalIgnoreCase);
		void Add(string path) {
			if (!string.IsNullOrWhiteSpace(path)) {
				candidates.Add(Path.GetFullPath(path));
			}
		}

		foreach (string root in EnumerateSearchRoots()) {
			Add(Path.Combine(root, "assets", "models_xml"));
			Add(Path.Combine(root, "assets", "xml_dump"));
			Add(Path.Combine(root, "models_xml"));
			Add(Path.Combine(root, "xml_dump"));
			Add(Path.Combine(root, "RaySharp", "assets", "models_xml"));
			Add(Path.Combine(root, "RaySharp", "assets", "xml_dump"));
		}

		return candidates.ToArray();
	}

	private static IEnumerable<string> EnumerateSearchRoots() {
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
		foreach (string start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory }) {
			if (string.IsNullOrWhiteSpace(start)) {
				continue;
			}

			for (DirectoryInfo? current = new DirectoryInfo(start); current is not null; current = current.Parent) {
				if (seen.Add(current.FullName)) {
					yield return current.FullName;
				}
			}
		}
	}
}

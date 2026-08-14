namespace RaySharp.Mesh;

internal static class MeshSampleResolver {
	public static string? Resolve(string fileName) {
		string[] candidates = [
			Path.Combine(Environment.CurrentDirectory, "assets", "xml_dump", fileName),
			Path.Combine(Environment.CurrentDirectory, "RaySharp", "assets", "xml_dump", fileName),
			Path.Combine(AppContext.BaseDirectory, "assets", "xml_dump", fileName),
			Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "xml_dump", fileName)
		];

		return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
	}

	public static string? ResolveSamplesDirectory() {
		string[] candidates = [
			Path.Combine(Environment.CurrentDirectory, "assets", "xml_dump"),
			Path.Combine(Environment.CurrentDirectory, "RaySharp", "assets", "xml_dump"),
			Path.Combine(AppContext.BaseDirectory, "assets", "xml_dump"),
			Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "xml_dump")
		];

		return candidates.Select(Path.GetFullPath).FirstOrDefault(Directory.Exists);
	}
}

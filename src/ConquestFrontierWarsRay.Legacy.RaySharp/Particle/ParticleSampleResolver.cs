namespace RaySharp.Particle;

internal static class ParticleSampleResolver {
	public static string? Resolve(string fileName) {
		string[] candidates =
		[
			Path.Combine(Environment.CurrentDirectory, "assets", "xml_unified", fileName),
			Path.Combine(Environment.CurrentDirectory, "RaySharp", "assets", "xml_unified", fileName),
			Path.Combine(AppContext.BaseDirectory, "assets", "xml_unified", fileName),
			Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "xml_unified", fileName)
		];

		return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
	}

	public static string? ResolveSamplesDirectory() {
		string[] candidates =
		[
			Path.Combine(Environment.CurrentDirectory, "assets", "xml_unified"),
			Path.Combine(Environment.CurrentDirectory, "RaySharp", "assets", "xml_unified"),
			Path.Combine(AppContext.BaseDirectory, "assets", "xml_unified"),
			Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "xml_unified")
		];

		return candidates.Select(Path.GetFullPath).FirstOrDefault(Directory.Exists);
	}
}

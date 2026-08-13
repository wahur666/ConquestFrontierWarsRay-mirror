namespace RaySharp;

internal static class AppAssets {
	internal static string? ResolveAssetPath(string fileName) {
		string[] candidates = [
			Path.Combine(AppContext.BaseDirectory, "assets", fileName),
			Path.Combine(Environment.CurrentDirectory, "assets", fileName),
			Path.Combine(Environment.CurrentDirectory, "RaySharp", "assets", fileName)
		];

		return candidates.FirstOrDefault(File.Exists);
	}
}
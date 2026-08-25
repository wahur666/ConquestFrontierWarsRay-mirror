using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Windowing;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp;

internal static class Program {
	private static void Main(string[] args) {
		var options = new WindowOptions(
			1280,
			720,
			"Framework Test App",
			60,
			ConfigFlags.VSyncHint | ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);

		using var app = new RaylibApplication(options);
		app.Bootstrap();
		app.Shared.ResourceLocator = new RepoResourceLocator();

		var startupSceneIndex = TryParseSceneIndex(args);
		var root = new ShowcaseShellNode(startupSceneIndex);
		var tree = new SceneTree(root, new InputManager(), app.Shared);
		app.Run(tree);
	}

	private static int? TryParseSceneIndex(string[] args) {
		if (args.Length == 0) {
			return null;
		}

		return int.TryParse(args[0], out var sceneIndex) ? sceneIndex : null;
	}
}

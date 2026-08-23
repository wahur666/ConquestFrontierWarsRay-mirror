using ConquestFrontierWarsRay.Framework;
using ConquestFrontierWarsRay.Framework.App;
using ConquestFrontierWarsRay.Windowing;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp;

internal static class Program {
	private static void Main() {
		var options = new WindowOptions(
			1280,
			720,
			"Framework Test App",
			60,
			ConfigFlags.VSyncHint | ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);

		using var app = new RaylibApplication(options);
		app.Bootstrap();

		var root = new ShowcaseShellNode();
		var tree = new SceneTree(root, new InputManager());
		app.Run(tree);
	}
}

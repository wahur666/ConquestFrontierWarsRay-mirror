using ConquestFrontierWarsRay.Framework;
using ConquestFrontierWarsRay.Framework.App;
using ConquestFrontierWarsRay.Windowing;
using Raylib_cs;

namespace ConquestFrontierWarsRay;

internal static class Program {
	private static void Main() {
		var options = new WindowOptions(
			1280,
			720,
			"Conquest Frontier Wars - Menu1 Opening Preview",
			60,
			ConfigFlags.VSyncHint | ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);

		using var app = new RaylibApplication(options);
		app.Bootstrap();

		var root = new Menu1OpeningPreviewScene();
		var tree = new SceneTree(root, new InputManager());
		app.Run(tree);
	}
}

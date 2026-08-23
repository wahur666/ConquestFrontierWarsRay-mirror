using System;
using System.Linq;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Framework;
using ConquestFrontierWarsRay.Framework.App;
using ConquestFrontierWarsRay.Windowing;
using Raylib_cs;

namespace ConquestFrontierWarsRay;

internal static class Program {
	private static void Main(string[] args) {
		var options = new WindowOptions(
			1280,
			720,
			"Conquest Frontier Wars - Menu1 Opening Preview",
			60,
			ConfigFlags.VSyncHint | ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);

		using var app = new RaylibApplication(options);
		app.Bootstrap();

		var root = CreateStartupRoot(args);
		var tree = new SceneTree(root, new InputManager());
		app.Run(tree);
	}

	private static Node CreateStartupRoot(string[] args) {
		return HasArgument(args, "--no-skip-intro")
			? CreateIntroSequenceRoot()
			: new Menu1OpeningPreviewScene();
	}

	private static Node CreateIntroSequenceRoot() {
		return new MovieScene(
			@"Assets\Movies\UbiLogo.mp4",
			() => new MovieScene(
				@"Assets\Movies\FPS_Logo.mp4",
				() => new Menu1OpeningPreviewScene()));
	}

	private static bool HasArgument(string[] args, string argument) {
		return args.Any(arg => string.Equals(arg, argument, StringComparison.OrdinalIgnoreCase));
	}
}

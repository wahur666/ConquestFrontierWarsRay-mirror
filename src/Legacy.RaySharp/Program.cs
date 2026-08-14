using Raylib_cs;

namespace RaySharp;

internal static class Program {
	internal const int ScreenWidth = 1280;
	internal const int ScreenHeight = 720;

	// STAThread is required if you deploy using NativeAOT on Windows
	// See https://github.com/raylib-cs/raylib-cs/issues/301
	[STAThread]
	public static void Main(string[] args) {
		bool startFullScreen = HasArgument(args, "--full-screen");
		bool skipIntro = HasArgument(args, "--skip-intro");
		bool startParticleEditor = HasArgument(args, "--particle");
		bool startMeshViewer = HasArgument(args, "--mesh");
		using NativeSplash? splash = NativeSplash.TryShow(AppAssets.ResolveAssetPath("00000409.256.bmp"));

		using MediaFoundationRuntime mediaFoundation = MediaFoundationRuntime.Start();

		Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint | ConfigFlags.HighDpiWindow);
		Raylib.InitWindow(ScreenWidth, ScreenHeight, "Conquest RTS Prototype - Raylib");
		Raylib.InitAudioDevice();
		Raylib.SetTargetFPS(60);

		UiState ui = new();
		UiTextures uiTextures = UiTextures.Load();
		UiFont uiFont = UiFont.Load();
		LitRenderer.LoadLighting();
		WindowState windowState = new(ScreenWidth, ScreenHeight);
		AppWindow.ApplyStartupOptions(windowState, ui, startFullScreen);
		SceneContext sceneContext = new(windowState, ui, uiTextures, uiFont);
		SceneRegistry sceneRegistry = SceneRegistry.Discover(sceneContext);
		IScene currentScene = sceneRegistry.CreateStartupScene(skipIntro, startMeshViewer, startParticleEditor);

		splash?.Dispose();

		while (!Raylib.WindowShouldClose()) {
			float deltaTime = Math.Min(0.05f, Raylib.GetFrameTime());
			SceneRequest? nextScene = currentScene.Update(deltaTime);
			if (nextScene is null) {
				continue;
			}

			currentScene.Dispose();
			currentScene = sceneRegistry.Create(nextScene.Value);
		}

		AppWindow.ReleaseMouseConfinement();
		currentScene.Dispose();
		LitRenderer.UnloadLighting();
		uiFont.Unload();
		uiTextures.Unload();
		Raylib.CloseAudioDevice();
		Raylib.CloseWindow();
	}

	private static bool HasArgument(string[] args, string argument) {
		return args.Any(arg => string.Equals(arg, argument, StringComparison.OrdinalIgnoreCase));
	}
}

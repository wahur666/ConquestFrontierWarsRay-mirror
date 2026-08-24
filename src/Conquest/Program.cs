using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Framework;
using ConquestFrontierWarsRay.Frontend;
using ConquestFrontierWarsRay.Windowing;
using Raylib_cs;

namespace ConquestFrontierWarsRay;

internal static class Program {
	private const string LogDirectoryName = "logs";

	[STAThread]
	private static void Main(string[] args) {
		InitializeLogging();

		using NativeSplash? splash = HasArgument(args, "--splash")
			? NativeSplash.TryShow(GetSplashImagePath())
			: null;

		try {
			AppLog.Info("Program", $"Current directory: {Environment.CurrentDirectory}");
			AppLog.Info("Program", $"Arguments: {(args.Length == 0 ? "<none>" : string.Join(' ', args))}");

			var options = new WindowOptions(
				1280,
				720,
				"Conquest Frontier Wars - Menu1 Opening Preview",
				60,
				ConfigFlags.VSyncHint | ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow,
				true);

			using var app = new RaylibApplication(options);
			app.Bootstrap();
			Win32Window.TrySetWindowIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "cur", "Icon1.ico"));
			app.Shared.ResourceLocator = new RepoResourceLocator();
			app.Shared.Set(options);
			app.Shared.Set("startup.args", args);

			var root = CreateStartupRoot(args);
			var tree = new SceneTree(root, new InputManager(), app.Shared);
			splash?.Dispose();
			app.Run(tree);
		} catch (Exception ex) {
			AppLog.Error("Program", "Unhandled fatal exception.", ex);
			throw;
		} finally {
			AppLog.Info("Program", "Process shutdown.");
			AppLog.Close();
		}
	}

	private static Node CreateStartupRoot(string[] args) {
		return HasArgument(args, "--no-skip-intro")
			? CreateIntroSequenceRoot()
			: new Menu1OpeningPreviewScene();
	}

	private static Node CreateIntroSequenceRoot() {
		return new MovieScene(@"Assets\Movies\UbiLogo.mp4", FpsMovieScene);
		Menu1OpeningPreviewScene MenuScene() => new();
		MovieScene FpsMovieScene() => new(@"Assets\Movies\FPS_Logo.mp4", MenuScene);
	}

	private static bool HasArgument(string[] args, string argument) {
		return args.Any(arg => string.Equals(arg, argument, StringComparison.OrdinalIgnoreCase));
	}

	private static string GetSplashImagePath() {
		return Path.Combine(RepoPaths.LocateInterfaceAssetsPath(), "00000409.256.bmp");
	}

	private static string InitializeLogging() {
		var logDirectory = Path.Combine(Environment.CurrentDirectory, LogDirectoryName);
		var fileName = $"conquest-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.log";
		var logPath = Path.Combine(logDirectory, fileName);
		AppLog.ConfigureFile(logPath, mirrorToConsole: true);
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		return logPath;
	}

	private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args) {
		if (args.ExceptionObject is Exception exception) {
			AppLog.Error("AppDomain", $"Unhandled exception (terminating={args.IsTerminating}).", exception);
			return;
		}

		AppLog.Error("AppDomain", $"Unhandled non-exception object (terminating={args.IsTerminating}).");
	}

	private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args) {
		AppLog.Error("TaskScheduler", "Unobserved task exception.", args.Exception);
	}
}

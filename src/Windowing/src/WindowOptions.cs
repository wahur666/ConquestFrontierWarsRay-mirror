using Raylib_cs;

namespace ConquestFrontierWarsRay.Windowing;

/// <summary>
/// Basic window settings for the app.
/// </summary>
/// <param name="Width">Window width in pixels.</param>
/// <param name="Height">Window height in pixels.</param>
/// <param name="Title">Window title text.</param>
/// <param name="TargetFps">Target frames per second.</param>
/// <param name="Flags">Raylib window flags to apply before startup.</param>
/// <param name="DrawFps">Draw the Raylib default fps counter into the top left corner</param>
public sealed record WindowOptions(
	int Width,
	int Height,
	string Title,
	int TargetFps = 60,
	ConfigFlags Flags = ConfigFlags.VSyncHint,
	bool DrawFps = false
) {
	/// <summary>
	/// Startup flags with runtime stability requirements applied.
	/// </summary>
	public ConfigFlags StartupFlags => Flags | ConfigFlags.AlwaysRunWindow;
}

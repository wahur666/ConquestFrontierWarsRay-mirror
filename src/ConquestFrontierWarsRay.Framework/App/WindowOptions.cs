using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.App;

/// <summary>
/// Basic window settings for the app.
/// </summary>
/// <param name="Width">Window width in pixels.</param>
/// <param name="Height">Window height in pixels.</param>
/// <param name="Title">Window title text.</param>
/// <param name="TargetFps">Target frames per second.</param>
/// <param name="Flags">Raylib window flags to apply before startup.</param>
internal sealed record WindowOptions(
	int Width,
	int Height,
	string Title,
	int TargetFps = 60,
	ConfigFlags Flags = ConfigFlags.VSyncHint
) {
	/// <summary>
	/// Startup flags with runtime stability requirements applied.
	/// </summary>
	public ConfigFlags StartupFlags => Flags | ConfigFlags.AlwaysRunWindow;
}

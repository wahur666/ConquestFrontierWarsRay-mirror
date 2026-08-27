namespace ConquestFrontierWarsRay.Data.UserProfiles;

public sealed class UserProfilesData {
	public string CurrentUser { get; init; } = string.Empty;
	public IReadOnlyList<UserProfileData> Users { get; init; } = [];
	public PlayerOptionsData Player { get; init; } = new();
	public GraphicsOptionsData Graphics { get; init; } = new();
	public SoundOptionsData Sound { get; init; } = new();
}

public sealed class UserProfileData {
	public string Name { get; init; } = string.Empty;
}

public sealed class PlayerOptionsData {
	public bool DirectInput { get; init; }
	public int MouseSensitivity { get; init; } = 7;
	public int GameSpeed { get; init; } = 5;
	public int ScrollSpeed { get; init; } = 6;
	public bool ShowStatusInfo { get; init; } = true;
	public bool EnableRolloverHelp { get; init; } = true;
	public bool UseSectorMapTexture { get; init; } = true;
	public bool EnableRightClickMenu { get; init; } = true;
	public bool ShowSubtitles { get; init; } = true;
}

public sealed class GraphicsOptionsData {
	public bool Use3DHardware { get; init; } = true;
	public string Resolution { get; init; } = "1024x768";
	public string Device { get; init; } = "Primary Display Driver";
	public int Gamma { get; init; } = 6;
	public int DrawDistance { get; init; } = 7;
	public int Ships3DDetail { get; init; } = 8;
	public bool EnableTrails { get; init; } = true;
	public bool EnableEmissiveLighting { get; init; } = true;
	public bool EnableDetailTextures { get; init; } = true;
}

public sealed class SoundOptionsData {
	public bool SoundEnabled { get; init; } = true;
	public int SoundVolume { get; init; } = 5;
	public bool MusicEnabled { get; init; } = true;
	public int MusicVolume { get; init; } = 5;
	public bool CommEnabled { get; init; } = true;
	public int CommVolume { get; init; } = 5;
	public int ChatVolume { get; init; } = 5;
}

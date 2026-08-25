namespace ConquestFrontierWarsRay.Data.UserProfiles;

public sealed class UserProfilesData {
	public string CurrentUser { get; init; } = string.Empty;
	public IReadOnlyList<UserProfileData> Users { get; init; } = [];
}

public sealed class UserProfileData {
	public string Name { get; init; } = string.Empty;
}

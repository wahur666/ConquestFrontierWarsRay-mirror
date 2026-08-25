using System.Text.Json;

namespace ConquestFrontierWarsRay.Data.UserProfiles;

public sealed class UserProfilesRepository {
	private static readonly JsonSerializerOptions JsonOptions = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true
	};

	private readonly string _jsonPath;

	public UserProfilesRepository(string jsonPath) {
		if (string.IsNullOrWhiteSpace(jsonPath)) {
			throw new ArgumentException("JSON path must not be null or whitespace.", nameof(jsonPath));
		}

		_jsonPath = jsonPath;
	}

	public string JsonPath => _jsonPath;

	public static UserProfilesRepository LocateFromRepo() {
		return new UserProfilesRepository(RepoPaths.LocateUserProfilesPath());
	}

	public UserProfilesData Load() {
		EnsureExists();
		var json = File.ReadAllText(_jsonPath);
		var data = JsonSerializer.Deserialize<UserProfilesData>(json, JsonOptions);
		if (data is null) {
			throw new InvalidDataException($"Failed to deserialize user profiles from '{_jsonPath}'.");
		}

		return Normalize(data);
	}

	public bool HasUsers() {
		return Load().Users.Count > 0;
	}

	public bool TryCreateUser(string? name, out UserProfilesData savedData, out string errorMessage) {
		var normalizedName = NormalizeName(name);
		if (string.IsNullOrWhiteSpace(normalizedName)) {
			savedData = Load();
			errorMessage = "Enter a user name.";
			return false;
		}

		var current = Load();
		if (current.Users.Any(user => string.Equals(user.Name, normalizedName, StringComparison.OrdinalIgnoreCase))) {
			savedData = current;
			errorMessage = "That user already exists.";
			return false;
		}

		var users = current.Users.ToList();
		users.Add(new UserProfileData { Name = normalizedName });
		savedData = new UserProfilesData {
			CurrentUser = normalizedName,
			Users = users
		};
		Save(savedData);
		errorMessage = string.Empty;
		return true;
	}

	public void Save(UserProfilesData data) {
		ArgumentNullException.ThrowIfNull(data);
		EnsureDirectoryExists();
		var normalized = Normalize(data);
		var json = JsonSerializer.Serialize(normalized, JsonOptions);
		File.WriteAllText(_jsonPath, json);
	}

	private void EnsureExists() {
		if (File.Exists(_jsonPath)) {
			return;
		}

		Save(new UserProfilesData());
	}

	private void EnsureDirectoryExists() {
		var directory = Path.GetDirectoryName(_jsonPath);
		if (!string.IsNullOrWhiteSpace(directory)) {
			Directory.CreateDirectory(directory);
		}
	}

	private static UserProfilesData Normalize(UserProfilesData data) {
		var users = (data.Users ?? [])
			.Where(user => !string.IsNullOrWhiteSpace(user?.Name))
			.Select(user => new UserProfileData { Name = NormalizeName(user.Name) })
			.Where(user => user.Name.Length > 0)
			.ToList();

		var currentUser = NormalizeName(data.CurrentUser);
		return new UserProfilesData {
			CurrentUser = currentUser,
			Users = users
		};
	}

	private static string NormalizeName(string? name) {
		return string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
	}
}

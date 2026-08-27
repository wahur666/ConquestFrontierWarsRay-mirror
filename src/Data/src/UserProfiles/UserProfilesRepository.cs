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
			Users = users,
			Player = current.Player,
			Graphics = current.Graphics,
			Sound = current.Sound
		};
		Save(savedData);
		errorMessage = string.Empty;
		return true;
	}

	public bool TryRenameUser(string? existingName, string? newName, out UserProfilesData savedData, out string errorMessage) {
		var current = Load();
		var normalizedExisting = NormalizeName(existingName);
		var normalizedNew = NormalizeName(newName);
		if (string.IsNullOrWhiteSpace(normalizedExisting)) {
			savedData = current;
			errorMessage = "Select a user first.";
			return false;
		}

		if (string.IsNullOrWhiteSpace(normalizedNew)) {
			savedData = current;
			errorMessage = "Enter a user name.";
			return false;
		}

		var index = current.Users.ToList().FindIndex(user => string.Equals(user.Name, normalizedExisting, StringComparison.OrdinalIgnoreCase));
		if (index < 0) {
			savedData = current;
			errorMessage = "That user no longer exists.";
			return false;
		}

		if (current.Users.Any(user =>
			    !string.Equals(user.Name, normalizedExisting, StringComparison.OrdinalIgnoreCase) &&
			    string.Equals(user.Name, normalizedNew, StringComparison.OrdinalIgnoreCase))) {
			savedData = current;
			errorMessage = "That user already exists.";
			return false;
		}

		var users = current.Users.ToList();
		users[index] = new UserProfileData { Name = normalizedNew };
		savedData = new UserProfilesData {
			CurrentUser = string.Equals(current.CurrentUser, normalizedExisting, StringComparison.OrdinalIgnoreCase)
				? normalizedNew
				: current.CurrentUser,
			Users = users,
			Player = current.Player,
			Graphics = current.Graphics,
			Sound = current.Sound
		};
		Save(savedData);
		errorMessage = string.Empty;
		return true;
	}

	public bool TryDeleteUser(string? name, out UserProfilesData savedData, out string errorMessage) {
		var current = Load();
		var normalizedName = NormalizeName(name);
		if (string.IsNullOrWhiteSpace(normalizedName)) {
			savedData = current;
			errorMessage = "Select a user first.";
			return false;
		}

		var users = current.Users
			.Where(user => !string.Equals(user.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (users.Count == current.Users.Count) {
			savedData = current;
			errorMessage = "That user no longer exists.";
			return false;
		}

		var currentUser = string.Equals(current.CurrentUser, normalizedName, StringComparison.OrdinalIgnoreCase)
			? users.FirstOrDefault()?.Name ?? string.Empty
			: current.CurrentUser;
		savedData = new UserProfilesData {
			CurrentUser = currentUser,
			Users = users,
			Player = current.Player,
			Graphics = current.Graphics,
			Sound = current.Sound
		};
		Save(savedData);
		errorMessage = string.Empty;
		return true;
	}

	public void SetCurrentUser(string? name) {
		var normalizedName = NormalizeName(name);
		var current = Load();
		if (!current.Users.Any(user => string.Equals(user.Name, normalizedName, StringComparison.OrdinalIgnoreCase))) {
			return;
		}

		Save(new UserProfilesData {
			CurrentUser = normalizedName,
			Users = current.Users,
			Player = current.Player,
			Graphics = current.Graphics,
			Sound = current.Sound
		});
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
			Users = users,
			Player = NormalizePlayer(data.Player),
			Graphics = NormalizeGraphics(data.Graphics),
			Sound = NormalizeSound(data.Sound)
		};
	}

	private static PlayerOptionsData NormalizePlayer(PlayerOptionsData? data) {
		data ??= new PlayerOptionsData();
		return new PlayerOptionsData {
			DirectInput = data.DirectInput,
			MouseSensitivity = ClampSlider(data.MouseSensitivity),
			GameSpeed = ClampSlider(data.GameSpeed),
			ScrollSpeed = ClampSlider(data.ScrollSpeed),
			ShowStatusInfo = data.ShowStatusInfo,
			EnableRolloverHelp = data.EnableRolloverHelp,
			UseSectorMapTexture = data.UseSectorMapTexture,
			EnableRightClickMenu = data.EnableRightClickMenu,
			ShowSubtitles = data.ShowSubtitles
		};
	}

	private static GraphicsOptionsData NormalizeGraphics(GraphicsOptionsData? data) {
		data ??= new GraphicsOptionsData();
		return new GraphicsOptionsData {
			Use3DHardware = data.Use3DHardware,
			Resolution = NormalizeOptionValue(data.Resolution, new GraphicsOptionsData().Resolution),
			Device = NormalizeOptionValue(data.Device, new GraphicsOptionsData().Device),
			Gamma = ClampSlider(data.Gamma),
			DrawDistance = ClampSlider(data.DrawDistance),
			Ships3DDetail = ClampSlider(data.Ships3DDetail),
			EnableTrails = data.EnableTrails,
			EnableEmissiveLighting = data.EnableEmissiveLighting,
			EnableDetailTextures = data.EnableDetailTextures
		};
	}

	private static SoundOptionsData NormalizeSound(SoundOptionsData? data) {
		data ??= new SoundOptionsData();
		return new SoundOptionsData {
			SoundEnabled = data.SoundEnabled,
			SoundVolume = ClampSlider(data.SoundVolume),
			MusicEnabled = data.MusicEnabled,
			MusicVolume = ClampSlider(data.MusicVolume),
			CommEnabled = data.CommEnabled,
			CommVolume = ClampSlider(data.CommVolume),
			ChatVolume = ClampSlider(data.ChatVolume)
		};
	}

	private static int ClampSlider(int value) {
		return Math.Clamp(value, 0, 10);
	}

	private static string NormalizeName(string? name) {
		return string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
	}

	private static string NormalizeOptionValue(string? value, string fallback) {
		var normalized = NormalizeName(value);
		return normalized.Length > 0 ? normalized : fallback;
	}
}

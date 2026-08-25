using System.Text.Json;
using ConquestFrontierWarsRay.Data.UserProfiles;

namespace ConquestFrontierWarsRay.Data.Tests.UserProfiles;

public sealed class UserProfilesRepositoryTests {
	[Fact]
	public void Load_Creates_Default_File_When_Missing() {
		using var tempDirectory = new TempDirectory();
		var jsonPath = Path.Combine(tempDirectory.Path, "SavedGame", "users.json");
		var repository = new UserProfilesRepository(jsonPath);

		var data = repository.Load();

		Assert.True(File.Exists(jsonPath));
		Assert.Equal(string.Empty, data.CurrentUser);
		Assert.Empty(data.Users);
	}

	[Fact]
	public void TryCreateUser_Appends_User_And_Sets_CurrentUser() {
		using var tempDirectory = new TempDirectory();
		var jsonPath = Path.Combine(tempDirectory.Path, "SavedGame", "users.json");
		var repository = new UserProfilesRepository(jsonPath);

		var created = repository.TryCreateUser("MyUser", out var data, out var errorMessage);
		var persisted = repository.Load();

		Assert.True(created);
		Assert.Equal(string.Empty, errorMessage);
		Assert.Equal("MyUser", data.CurrentUser);
		Assert.Single(data.Users);
		Assert.Equal("MyUser", data.Users[0].Name);
		Assert.Equal("MyUser", persisted.CurrentUser);
		Assert.Single(persisted.Users);
		Assert.Equal("MyUser", persisted.Users[0].Name);
	}

	[Fact]
	public void TryCreateUser_Rejects_Duplicate_Name_Ignoring_Case() {
		using var tempDirectory = new TempDirectory();
		var jsonPath = Path.Combine(tempDirectory.Path, "SavedGame", "users.json");
		var repository = new UserProfilesRepository(jsonPath);
		repository.TryCreateUser("MyUser", out _, out _);

		var created = repository.TryCreateUser("myuser", out var data, out var errorMessage);

		Assert.False(created);
		Assert.Equal("That user already exists.", errorMessage);
		Assert.Equal("MyUser", data.CurrentUser);
		Assert.Single(data.Users);
	}

	[Fact]
	public void Save_Writes_Expected_Json_Shape() {
		using var tempDirectory = new TempDirectory();
		var jsonPath = Path.Combine(tempDirectory.Path, "SavedGame", "users.json");
		var repository = new UserProfilesRepository(jsonPath);

		repository.Save(new UserProfilesData {
			CurrentUser = "MyUser",
			Users = [new UserProfileData { Name = "MyUser" }]
		});

		using var document = JsonDocument.Parse(File.ReadAllText(jsonPath));
		var root = document.RootElement;
		Assert.Equal("MyUser", root.GetProperty("currentUser").GetString());
		var users = root.GetProperty("users");
		Assert.Equal(1, users.GetArrayLength());
		Assert.Equal("MyUser", users[0].GetProperty("name").GetString());
	}

	private sealed class TempDirectory : IDisposable {
		public TempDirectory() {
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ConquestFrontierWarsRay.Tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Path);
		}

		public string Path { get; }

		public void Dispose() {
			if (Directory.Exists(Path)) {
				Directory.Delete(Path, recursive: true);
			}
		}
	}
}

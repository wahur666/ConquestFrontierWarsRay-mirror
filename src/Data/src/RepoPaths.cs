namespace ConquestFrontierWarsRay.Data;

public static class RepoPaths {
	public static string LocateRepoRoot() {
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null) {
			if (LooksLikeRepoRoot(current.FullName)) {
				return current.FullName;
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException(
			$"Could not locate repository root from the application directory '{AppContext.BaseDirectory}'.");
	}

	public static string LocateAssetsRoot() {
		return Path.Combine(LocateRepoRoot(), "assets");
	}

	public static string LocateInterfaceAssetsPath() {
		return Path.Combine(LocateAssetsRoot(), "interface");
	}

	public static string LocateVfxAnimationDataPath() {
		return Path.Combine(LocateAssetsRoot(), "DB", "vfx-animation-data.json");
	}

	public static (string DatabasePath, string XmlRootPath, string RepoRoot) LocateStringPackPaths() {
		var repoRoot = LocateRepoRoot();
		var dbRoot = Path.Combine(repoRoot, "assets", "DB");
		var dbPath = Path.Combine(dbRoot, "StringPack.db");
		var xmlRootPath = Path.Combine(dbRoot, "xml", "StringPack.db");
		if (File.Exists(dbPath) && Directory.Exists(xmlRootPath)) {
			return (dbPath, xmlRootPath, repoRoot);
		}

		throw new DirectoryNotFoundException(
			$"Could not locate assets/DB/StringPack.db and assets/DB/xml/StringPack.db under repo root '{repoRoot}'.");
	}

	public static IReadOnlyList<UtfDb.UtfDbDatabaseSpec> LocateUtfDbPaths() {
		var repoRoot = LocateRepoRoot();
		var dbRoot = Path.Combine(repoRoot, "assets", "DB");
		var xmlRoot = Path.Combine(dbRoot, "xml");
		if (Directory.Exists(dbRoot) && Directory.Exists(xmlRoot)) {
			var databases = new List<UtfDb.UtfDbDatabaseSpec>();
			foreach (var file in Directory.EnumerateFiles(dbRoot, "*.db", SearchOption.TopDirectoryOnly)) {
				var name = Path.GetFileName(file);
				var xmlPath = Path.Combine(xmlRoot, name);
				databases.Add(new UtfDb.UtfDbDatabaseSpec(name, file, Directory.Exists(xmlPath) ? xmlPath : null));
			}

			return databases
				.OrderBy(database => database.Name, StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}

		throw new DirectoryNotFoundException(
			$"Could not locate assets/DB and assets/DB/xml under repo root '{repoRoot}'.");
	}

	private static bool LooksLikeRepoRoot(string path) {
		if (!Directory.Exists(Path.Combine(path, "assets")) || !Directory.Exists(Path.Combine(path, "src"))) {
			return false;
		}

		if (Directory.Exists(Path.Combine(path, ".git"))) {
			return true;
		}

		return File.Exists(Path.Combine(path, "ConquestFrontierWarsRay.slnx"))
			|| File.Exists(Path.Combine(path, "README.md"));
	}
}

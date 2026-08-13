namespace ConquestFrontierWarsRay.Data;

public static class RepoPaths {
	public static (string DatabasePath, string XmlRootPath, string RepoRoot) LocateStringPackPaths() {
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null) {
			var dbPath = Path.Combine(current.FullName, "DB", "StringPack.db");
			var xmlRootPath = Path.Combine(current.FullName, "DB", "xml", "StringPack.db");
			if (File.Exists(dbPath) && Directory.Exists(xmlRootPath)) {
				return (dbPath, xmlRootPath, current.FullName);
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException(
			"Could not locate DB/StringPack.db and DB/xml/StringPack.db from the application directory.");
	}

	public static IReadOnlyList<UtfDb.UtfDbDatabaseSpec> LocateUtfDbPaths() {
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null) {
			var dbRoot = Path.Combine(current.FullName, "DB");
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

			current = current.Parent;
		}

		throw new DirectoryNotFoundException(
			"Could not locate DB and DB/xml from the application directory.");
	}
}

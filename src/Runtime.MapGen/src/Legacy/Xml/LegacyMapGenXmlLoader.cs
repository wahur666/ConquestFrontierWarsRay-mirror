namespace ConquestFrontierWarsRay.Runtime.MapGen.Legacy;

public static class LegacyMapGenXmlLoader {
	public static BT_MAP_GEN LoadFromFile(string xmlPath) {
		ArgumentException.ThrowIfNullOrWhiteSpace(xmlPath);
		LegacyParserBootstrap.EnsureRegistered();
		return LegacyParserRegistry.Parse<BT_MAP_GEN>(File.ReadAllText(xmlPath));
	}

	public static string LocateDefaultXmlPath() {
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null) {
			var candidate = Path.Combine(current.FullName, "assets", "DB", "xml", "GameTypes.db", "BT_MAP_GEN", "MAPGEN!!Map.xml");
			if (File.Exists(candidate)) {
				return candidate;
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException(
			"Could not locate assets/DB/xml/GameTypes.db/BT_MAP_GEN/MAPGEN!!Map.xml from the application directory.");
	}
}

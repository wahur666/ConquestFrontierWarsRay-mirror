namespace RaySharp.Particle;

internal sealed class ParticleSampleCatalog {
	private readonly List<string> files = [];
	private int currentIndex = -1;

	public IReadOnlyList<string> Files => files;

	public string? Folder { get; private set; }

	public int CurrentIndex => currentIndex;

	public int Count => files.Count;

	public string? CurrentPath => currentIndex >= 0 && currentIndex < files.Count ? files[currentIndex] : null;

	public string Summary {
		get {
			if (files.Count == 0) {
				return "No unified samples indexed";
			}

			return $"{currentIndex + 1}/{files.Count} {Path.GetFileName(CurrentPath)}";
		}
	}

	public bool LoadFolder(string folder, string? preferredPath = null) {
		files.Clear();
		currentIndex = -1;
		Folder = Path.GetFullPath(folder);

		if (!Directory.Exists(Folder)) {
			return false;
		}

		files.AddRange(Directory
			.EnumerateFiles(Folder, "*.pte.unified.xml", SearchOption.TopDirectoryOnly)
			.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase));

		if (files.Count == 0) {
			return false;
		}

		currentIndex = 0;
		if (!string.IsNullOrWhiteSpace(preferredPath)) {
			SetCurrent(preferredPath);
		}

		return true;
	}

	public bool SetCurrent(string path) {
		string fullPath = Path.GetFullPath(path);
		int index = files.FindIndex(file => string.Equals(Path.GetFullPath(file), fullPath, StringComparison.OrdinalIgnoreCase));
		if (index < 0) {
			return false;
		}

		currentIndex = index;
		return true;
	}

	public bool SetCurrentIndex(int index) {
		if (index < 0 || index >= files.Count) {
			return false;
		}

		currentIndex = index;
		return true;
	}

	public string? GetPath(int index) {
		return index >= 0 && index < files.Count ? files[index] : null;
	}

	public string? MoveNext() {
		if (files.Count == 0 || currentIndex >= files.Count - 1) {
			return null;
		}

		currentIndex++;
		return CurrentPath;
	}

	public string? MovePrevious() {
		if (files.Count == 0 || currentIndex <= 0) {
			return null;
		}

		currentIndex--;
		return CurrentPath;
	}
}

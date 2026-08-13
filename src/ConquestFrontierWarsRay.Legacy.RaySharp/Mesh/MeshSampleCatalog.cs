namespace RaySharp.Mesh;

internal readonly record struct MeshSampleTreeRow(string Name, bool IsFolder, int FileIndex, int Indent);

internal sealed class MeshSampleCatalog {
	private readonly List<string> files = [];
	private readonly List<MeshSampleTreeRow> treeRows = [];
	private int currentIndex = -1;

	public IReadOnlyList<string> Files => files;
	public IReadOnlyList<MeshSampleTreeRow> TreeRows => treeRows;
	public string? Folder { get; private set; }
	public int CurrentIndex => currentIndex;
	public int Count => files.Count;
	public string? CurrentPath => currentIndex >= 0 && currentIndex < files.Count ? files[currentIndex] : null;
	public int CurrentRowIndex => treeRows.FindIndex(row => !row.IsFolder && row.FileIndex == currentIndex);

	public string Summary => files.Count == 0
		? "No mesh samples indexed"
		: $"{currentIndex + 1}/{files.Count} {Path.GetFileName(CurrentPath)}";

	public bool LoadFolder(string folder, string? preferredPath = null) {
		files.Clear();
		treeRows.Clear();
		currentIndex = -1;
		Folder = Path.GetFullPath(folder);

		if (!Directory.Exists(Folder)) {
			return false;
		}

		BuildTreeRows();

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

	private void BuildTreeRows() {
		if (Folder is null) {
			return;
		}

		AddFiles(EnumerateMeshFiles(Folder), indent: 0);

		foreach (string directory in Directory
			.EnumerateDirectories(Folder, "*", SearchOption.AllDirectories)
			.OrderBy(RelativeDirectoryName, StringComparer.OrdinalIgnoreCase)) {
			string[] directoryFiles = EnumerateMeshFiles(directory).ToArray();
			if (directoryFiles.Length == 0) {
				continue;
			}

			treeRows.Add(new MeshSampleTreeRow(RelativeDirectoryName(directory), IsFolder: true, FileIndex: -1, Indent: 0));
			AddFiles(directoryFiles, indent: 1);
		}
	}

	private static IEnumerable<string> EnumerateMeshFiles(string directory) {
		return Directory
			.EnumerateFiles(directory, "*.3db.xml", SearchOption.TopDirectoryOnly)
			.Concat(Directory.EnumerateFiles(directory, "*.cmp.xml", SearchOption.TopDirectoryOnly))
			.Concat(Directory.EnumerateFiles(directory, "*.shield.xml", SearchOption.TopDirectoryOnly))
			.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
	}

	private void AddFiles(IEnumerable<string> paths, int indent) {
		foreach (string path in paths) {
			int fileIndex = files.Count;
			files.Add(path);
			treeRows.Add(new MeshSampleTreeRow(Path.GetFileName(path), IsFolder: false, fileIndex, indent));
		}
	}

	private string RelativeDirectoryName(string directory) {
		return Folder is null ? Path.GetFileName(directory) : Path.GetRelativePath(Folder, directory);
	}
}

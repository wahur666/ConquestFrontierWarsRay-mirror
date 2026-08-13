namespace ConquestFrontierWarsRay.Data.DosFile;

public sealed class DosFileReader {
	private readonly string _sourcePath;
	private readonly UtfDirectoryNode? _utfRoot;
	private readonly bool _isDirectory;

	public DosFileReader(string path) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		_sourcePath = Path.GetFullPath(path);
		if (Directory.Exists(_sourcePath)) {
			_isDirectory = true;
			return;
		}

		if (!File.Exists(_sourcePath)) {
			throw new FileNotFoundException($"The path '{_sourcePath}' does not exist.", _sourcePath);
		}

		var bytes = File.ReadAllBytes(_sourcePath);
		if (LooksLikeUtfArchive(bytes)) {
			_utfRoot = UtfArchive.Parse(bytes);
			return;
		}
	}

	public IReadOnlyList<FileSystemEntry> FindFiles(string relativePath = "", string pattern = "*") {
		if (_utfRoot is not null) {
			var node = ResolveUtfNode(relativePath);
			if (node is not UtfDirectoryNode directory) {
				return DosPath.PatternMatch(node.Name, pattern)
					? [CreateUtfEntry(node, GetUtfPath(relativePath))]
					: [];
			}

			return directory.Children.Values
				.Where(node => DosPath.PatternMatch(node.Name, pattern))
				.Select(node => CreateUtfEntry(node, GetUtfPath(relativePath, node.Name)))
				.ToArray();
		}

		var hostPath = ResolveHostPath(relativePath);
		if (File.Exists(hostPath)) {
			var info = new FileInfo(hostPath);
			return DosPath.PatternMatch(info.Name, pattern) ? [CreateHostEntry(info)] : [];
		}

		if (!Directory.Exists(hostPath)) {
			throw new DirectoryNotFoundException(hostPath);
		}

		return Directory.EnumerateFileSystemEntries(hostPath)
			.Select(CreateHostEntry)
			.Where(entry => DosPath.PatternMatch(entry.Name, pattern))
			.ToArray();
	}

	public byte[] ReadAllBytes(string relativePath) {
		ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

		if (_utfRoot is not null) {
			var node = ResolveUtfNode(relativePath) as UtfFileNode
				?? throw new InvalidOperationException($"'{relativePath}' is a directory.");
			return node.Data.ToArray();
		}

		var hostPath = ResolveHostPath(relativePath);
		if (Directory.Exists(hostPath)) {
			throw new InvalidOperationException($"'{relativePath}' is a directory.");
		}

		return File.ReadAllBytes(hostPath);
	}

	private string ResolveHostPath(string relativePath) {
		if (_utfRoot is not null) {
			throw new InvalidOperationException("The reader is attached to a UTF archive.");
		}

		if (string.IsNullOrWhiteSpace(relativePath)) {
			if (!_isDirectory) {
				return _sourcePath;
			}

			return _sourcePath;
		}

		if (!_isDirectory) {
			throw new InvalidOperationException("The reader is attached to a file, not a directory.");
		}

		return Path.GetFullPath(Path.Combine(_sourcePath, DosPath.NormalizeSwitchChars(relativePath)));
	}

	private UtfNode ResolveUtfNode(string relativePath) {
		if (_utfRoot is null) {
			throw new InvalidOperationException("The reader is not attached to a UTF archive.");
		}

		var normalized = string.IsNullOrWhiteSpace(relativePath)
			? "\\"
			: DosPath.CombineContainerPath("\\", relativePath);

		if (normalized == "\\") {
			return _utfRoot;
		}

		var parts = normalized.TrimStart('\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);
		UtfNode current = _utfRoot;
		foreach (var part in parts) {
			if (current is not UtfDirectoryNode directory ||
			    !directory.Children.TryGetValue(part, out var next)) {
				throw new FileNotFoundException(
					$"Entry '{normalized}' was not found in UTF container '{_sourcePath}'.",
					normalized);
			}

			current = next;
		}

		return current;
	}

	private static bool LooksLikeUtfArchive(byte[] bytes) {
		return bytes.Length >= 8 &&
		       BitConverter.ToUInt32(bytes, 0) == UtfConstants.Identifier &&
		       BitConverter.ToUInt32(bytes, 4) == UtfConstants.Version;
	}

	private static FileSystemEntry CreateHostEntry(string path) {
		if (Directory.Exists(path)) {
			return CreateHostEntry(new DirectoryInfo(path));
		}

		return CreateHostEntry(new FileInfo(path));
	}

	private static FileSystemEntry CreateHostEntry(FileSystemInfo info) {
		var isDirectory = (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
		var length = info is FileInfo fileInfo ? fileInfo.Length : 0;
		return new FileSystemEntry(
			info.Name,
			info.FullName,
			isDirectory,
			length,
			info.CreationTimeUtc,
			info.LastAccessTimeUtc,
			info.LastWriteTimeUtc,
			info.Attributes);
	}

	private static FileSystemEntry CreateUtfEntry(UtfNode node, string fullPath) {
		var length = node is UtfFileNode file ? file.Data.LongLength : 0;
		return new FileSystemEntry(
			node.Name,
			fullPath,
			node.IsDirectory,
			length,
			node.CreationTimeUtc,
			node.LastAccessTimeUtc,
			node.LastWriteTimeUtc,
			node.Attributes);
	}

	private static string GetUtfPath(string relativePath, string? childName = null) {
		var path = string.IsNullOrWhiteSpace(relativePath)
			? "\\"
			: DosPath.CombineContainerPath("\\", relativePath);
		if (string.IsNullOrWhiteSpace(childName)) {
			return path;
		}

		return DosPath.CombineContainerPath(path, childName);
	}
}

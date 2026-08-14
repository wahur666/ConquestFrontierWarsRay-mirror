namespace ConquestFrontierWarsRay.Data.DosFile;

public sealed record FileSystemEntry(
	string Name,
	string FullPath,
	bool IsDirectory,
	long Length,
	DateTime CreationTimeUtc,
	DateTime LastAccessTimeUtc,
	DateTime LastWriteTimeUtc,
	FileAttributes Attributes);

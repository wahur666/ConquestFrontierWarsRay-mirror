namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Resolves asset paths from one configured asset-root directory.
/// </summary>
public class AssetRootResourceLocator : IResourceLocator {
	private const string AssetsDirectoryName = "assets";

	/// <summary>
	/// Creates a locator rooted at one asset directory.
	/// </summary>
	public AssetRootResourceLocator(string assetRootPath) {
		ArgumentException.ThrowIfNullOrWhiteSpace(assetRootPath);
		AssetRootPath = Path.GetFullPath(assetRootPath);
	}

	/// <inheritdoc />
	public string AssetRootPath { get; }

	/// <inheritdoc />
	public string ResolveAssetPath(string relativeAssetPath) {
		ArgumentException.ThrowIfNullOrWhiteSpace(relativeAssetPath);

		if (Path.IsPathRooted(relativeAssetPath)) {
			return Path.GetFullPath(relativeAssetPath);
		}

		var relativePath = StripLeadingDirectory(relativeAssetPath, AssetsDirectoryName);
		return Path.GetFullPath(Path.Combine(AssetRootPath, relativePath));
	}

	/// <inheritdoc />
	public string ResolveMoviePath(string moviePath) {
		return ResolvePathWithinAssetSubdirectory(moviePath, "Movies");
	}

	/// <inheritdoc />
	public string ResolveMusicPath(string musicPath) {
		return ResolvePathWithinAssetSubdirectory(musicPath, "conquest_frontier_wars_ost");
	}

	/// <inheritdoc />
	public string ResolveSpeechPath(string speechPath) {
		return ResolvePathWithinAssetSubdirectory(speechPath, "mspeech");
	}

	private string ResolvePathWithinAssetSubdirectory(string path, string subdirectoryName) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		if (Path.IsPathRooted(path)) {
			return Path.GetFullPath(path);
		}

		var relativePath = StripLeadingDirectory(path, AssetsDirectoryName);
		relativePath = StripLeadingDirectory(relativePath, subdirectoryName);
		return Path.GetFullPath(Path.Combine(AssetRootPath, subdirectoryName, relativePath));
	}

	private static string StripLeadingDirectory(string path, string directoryName) {
		var trimmed = path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var prefix = directoryName + Path.DirectorySeparatorChar;
		var altPrefix = directoryName + Path.AltDirectorySeparatorChar;

		if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
			return trimmed[prefix.Length..];
		}

		if (trimmed.StartsWith(altPrefix, StringComparison.OrdinalIgnoreCase)) {
			return trimmed[altPrefix.Length..];
		}

		return trimmed;
	}
}

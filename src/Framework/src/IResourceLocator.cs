namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Resolves logical application asset paths to concrete file-system paths.
/// </summary>
public interface IResourceLocator {
	/// <summary>
	/// Root path used for the application's asset tree.
	/// </summary>
	string AssetRootPath { get; }

	/// <summary>
	/// Resolves one path relative to the asset root.
	/// </summary>
	string ResolveAssetPath(string relativeAssetPath);

	/// <summary>
	/// Resolves one movie file path.
	/// </summary>
	string ResolveMoviePath(string moviePath);

	/// <summary>
	/// Resolves one music file path.
	/// </summary>
	string ResolveMusicPath(string musicPath);

	/// <summary>
	/// Resolves one speech file path.
	/// </summary>
	string ResolveSpeechPath(string speechPath);
}

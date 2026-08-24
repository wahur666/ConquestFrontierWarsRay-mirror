using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class AudioResourceManagerTests {
	[Fact]
	public void OpenFile_UsesRequestedBackend_AndCanonicalPath() {
		var manager = new AudioResourceManager(new AssetRootResourceLocator(Path.Combine(Path.GetTempPath(), "assets")));
		var relativePath = Path.Combine(".", "assets", "sample.wav");

		using var nAudio = manager.OpenFile(relativePath, AudioPlaybackBackend.NAudio);
		using var raylib = manager.OpenFile(relativePath, AudioPlaybackBackend.RaylibMusic);
		using var mediaFoundation = manager.OpenFile(relativePath, AudioPlaybackBackend.MediaFoundation);

		Assert.IsType<NAudioStreamResource>(nAudio);
		Assert.IsType<MusicAudioResource>(raylib);
		Assert.IsType<MediaFoundationAudioStreamResource>(mediaFoundation);
		Assert.Equal(Path.GetFullPath(relativePath), nAudio.ResourcePath);
		Assert.Equal(Path.GetFullPath(relativePath), raylib.ResourcePath);
		Assert.Equal(Path.GetFullPath(relativePath), mediaFoundation.ResourcePath);
	}

	[Fact]
	public void OpenMusic_ResolvesThroughLocator() {
		var root = Path.Combine(Path.GetTempPath(), "resource-manager-audio");
		var manager = new AudioResourceManager(new AssetRootResourceLocator(root));

		using var audio = manager.OpenMusic("theme.mp3");

		Assert.Equal(Path.Combine(root, "conquest_frontier_wars_ost", "theme.mp3"), audio.ResourcePath);
		Assert.IsType<MusicAudioResource>(audio);
	}

	[Fact]
	public void OpenSpeech_ResolvesThroughLocator() {
		var root = Path.Combine(Path.GetTempPath(), "resource-manager-audio");
		var manager = new AudioResourceManager(new AssetRootResourceLocator(root));

		using var audio = manager.OpenSpeech("briefing.wav", AudioPlaybackBackend.NAudio);

		Assert.Equal(Path.Combine(root, "mspeech", "briefing.wav"), audio.ResourcePath);
		Assert.IsType<NAudioStreamResource>(audio);
	}
}

using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class AudioPlayerTests {
	[Fact]
	public void SetAudio_AppliesCachedPlaybackSettingsToAssignedResource() {
		var player = new AudioPlayer();
		var audio = new FakeAudioStreamResource("clip.wav");

		player.Volume = 0.25f;
		player.Pitch = 1.5f;
		player.Pan = 0.75f;
		player.Looping = true;

		player.SetAudio(audio);

		Assert.Same(audio, player.Audio);
		Assert.Equal(0.25f, audio.Volume);
		Assert.Equal(1.5f, audio.Pitch);
		Assert.Equal(0.75f, audio.Pan);
		Assert.True(audio.Looping);
		Assert.False(player.OwnsAudio);
	}

	[Fact]
	public void PlaybackMethods_ForwardToAssignedResource() {
		var player = new AudioPlayer();
		var audio = new FakeAudioStreamResource("clip.wav", timeLength: 12f);

		player.SetAudio(audio);
		player.Play();
		player.Seek(4.5f);
		player.Pause();
		player.Play();
		player.Stop();

		Assert.Equal(2, audio.PlayCount);
		Assert.Equal(1, audio.PauseCount);
		Assert.Equal(1, audio.StopCount);
		Assert.Equal(1, audio.SeekCount);
		Assert.Equal(12f, player.PlaybackLength);
		Assert.Equal(0f, player.PlaybackPosition);
	}

	[Fact]
	public void Update_AdvancesAssignedStream() {
		var player = new AudioPlayer();
		var audio = new FakeAudioStreamResource("clip.wav");
		var tree = new SceneTree(player, new InputManager());

		player.SetAudio(audio);
		tree.Start();
		tree.Update(1f / 60f);
		tree.Stop();

		Assert.Equal(1, audio.UpdateCount);
	}

	[Fact]
	public void ExitTree_PausesPlayingAudio_AndEnterTreeResumesIt() {
		var player = new AudioPlayer();
		var audio = new FakeAudioStreamResource("clip.wav");
		var tree = new SceneTree(player, new InputManager());

		player.SetAudio(audio);
		tree.Start();
		player.Play();

		tree.Stop();
		Assert.Equal(1, audio.PauseCount);
		Assert.False(audio.IsPlaying);

		tree.Start();
		Assert.Equal(2, audio.PlayCount);
		Assert.True(audio.IsPlaying);
	}

	[Fact]
	public void DisposeAudio_DisposesAssignedResourceAndClearsNode() {
		var player = new AudioPlayer();
		var audio = new FakeAudioStreamResource("clip.wav");

		player.SetAudio(audio, takeOwnership: true);
		player.DisposeAudio();

		Assert.Null(player.Audio);
		Assert.False(player.HasAudio);
		Assert.True(audio.IsDisposed);
		Assert.Equal(0, audio.UnloadCount);
	}

	[Fact]
	public void Dispose_DoesNotDisposeExternalResource() {
		var player = new AudioPlayer();
		var audio = new FakeAudioStreamResource("clip.wav");

		player.SetAudio(audio, takeOwnership: false);
		player.Dispose();

		Assert.False(audio.IsDisposed);
	}
}

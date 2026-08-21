using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class AnimatedSprite2DTests {
	[Fact]
	public void Constructor_UsesExpectedDefaults() {
		var frames = CreateFrames(3);

		var animatedSprite = new AnimatedSprite2D(frames, "AnimatedSprite");

		Assert.Equal("AnimatedSprite", animatedSprite.Name);
		Assert.Same(frames, animatedSprite.Frames);
		Assert.Equal(0, animatedSprite.Frame);
		Assert.Equal(8f, animatedSprite.SpeedFps);
		Assert.True(animatedSprite.Playing);
		Assert.True(animatedSprite.Loop);
	}

	[Fact]
	public void Update_AdvancesFramesWhilePlaying() {
		var animatedSprite = new AnimatedSprite2D(CreateFrames(3));
		var tree = new SceneTree(animatedSprite, new InputManager());

		tree.Start();
		tree.Update(0.125f);
		tree.Update(0.125f);

		Assert.Equal(2, animatedSprite.Frame);
	}

	[Fact]
	public void Update_StopsOnLastFrameWhenLoopIsDisabled() {
		var animatedSprite = new AnimatedSprite2D(CreateFrames(3)) {
			Loop = false,
			SpeedFps = 4f
		};
		var tree = new SceneTree(animatedSprite, new InputManager());

		tree.Start();
		tree.Update(1f);

		Assert.Equal(2, animatedSprite.Frame);
		Assert.False(animatedSprite.Playing);
	}

	[Fact]
	public void PauseAndPlay_ControlPlaybackWithoutResettingFrame() {
		var animatedSprite = new AnimatedSprite2D(CreateFrames(3));
		var tree = new SceneTree(animatedSprite, new InputManager());

		tree.Start();
		animatedSprite.Pause();
		tree.Update(0.5f);

		Assert.Equal(0, animatedSprite.Frame);

		animatedSprite.Play();
		tree.Update(0.125f);

		Assert.Equal(1, animatedSprite.Frame);
	}

	[Fact]
	public void Stop_ResetsPlaybackToFirstFrame() {
		var animatedSprite = new AnimatedSprite2D(CreateFrames(3));
		var tree = new SceneTree(animatedSprite, new InputManager());

		tree.Start();
		tree.Update(0.25f);

		animatedSprite.Stop();

		Assert.Equal(0, animatedSprite.Frame);
		Assert.False(animatedSprite.Playing);
	}

	[Fact]
	public void SetFrames_ReplacesFrameCollectionAndResetsFrameIndex() {
		var firstFrames = CreateFrames(3);
		var secondFrames = CreateFrames(2, xOffset: 100f);
		var animatedSprite = new AnimatedSprite2D(firstFrames);

		animatedSprite.Frame = 2;
		animatedSprite.SetFrames(secondFrames);

		Assert.Same(secondFrames, animatedSprite.Frames);
		Assert.Equal(0, animatedSprite.Frame);
	}

	[Fact]
	public void ConstructorAndSetters_RejectInvalidArguments() {
		Assert.Throws<ArgumentNullException>(() => new AnimatedSprite2D(null!));

		var animatedSprite = new AnimatedSprite2D(CreateFrames(2));

		Assert.Throws<ArgumentNullException>(() => animatedSprite.SetFrames(null!));
		Assert.Throws<ArgumentOutOfRangeException>(() => animatedSprite.Frame = -1);
		Assert.Throws<ArgumentOutOfRangeException>(() => animatedSprite.Frame = 2);
	}

	private static SpriteFrames CreateFrames(int count, float xOffset = 0f) {
		var texture = new FakeTexture(new Texture2D.TextureSlice(
			new Raylib_cs.Texture2D(),
			new Rectangle(0f, 0f, 256f, 32f)));
		var frames = Enumerable.Range(0, count)
			.Select(index => new Rectangle(xOffset + index * 16f, 0f, 16f, 16f))
			.ToArray();
		return new SpriteFrames(texture, frames);
	}
}

using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Texture-backed 2D node that advances through indexed sprite frames over time.
/// </summary>
public sealed class AnimatedSprite2D : Node2D {
	private SpriteFrames _frames;
	private AtlasTexture _frameTexture;
	private int _frame;
	private float _frameTimeAccumulator;

	/// <summary>
	/// Creates an animated sprite from a shared frame collection.
	/// </summary>
	public AnimatedSprite2D(SpriteFrames frames, string? name = null) : base(name) {
		_frames = frames ?? throw new ArgumentNullException(nameof(frames));
		_frameTexture = _frames.GetFrameTexture(0);
	}

	/// <summary>
	/// Normalized pivot point used for rotation and placement.
	/// </summary>
	public Vector2 Pivot { get; set; } = new(0.5f, 0.5f);

	/// <summary>
	/// Tint color applied when drawing.
	/// </summary>
	public Color Modulate { get; set; } = Color.White;

	/// <summary>
	/// Blend mode applied while drawing this animated sprite.
	/// </summary>
	public BlendMode BlendMode { get; set; } = BlendMode.Alpha;

	/// <summary>
	/// Mirrors the sprite horizontally when drawing.
	/// </summary>
	public bool FlipH { get; set; }

	/// <summary>
	/// Mirrors the sprite vertically when drawing.
	/// </summary>
	public bool FlipV { get; set; }

	/// <summary>
	/// Shared frame collection used for playback.
	/// </summary>
	public SpriteFrames Frames => _frames;

	/// <summary>
	/// Current frame index.
	/// </summary>
	public int Frame {
		get => _frame;
		set => SetFrame(value);
	}

	/// <summary>
	/// Playback speed in frames per second.
	/// </summary>
	public float SpeedFps { get; set; } = 8f;

	/// <summary>
	/// True while playback is advancing automatically.
	/// </summary>
	public bool Playing { get; private set; } = true;

	/// <summary>
	/// True when playback wraps back to the first frame.
	/// </summary>
	public bool Loop { get; set; } = true;

	/// <summary>
	/// Raised when non-looping playback reaches the final frame and stops.
	/// </summary>
	public event Action<AnimatedSprite2D>? PlaybackCompleted;

	/// <summary>
	/// Starts or resumes playback.
	/// </summary>
	public void Play() {
		Playing = true;
	}

	/// <summary>
	/// Pauses playback without changing the current frame.
	/// </summary>
	public void Pause() {
		Playing = false;
	}

	/// <summary>
	/// Stops playback and returns to the first frame.
	/// </summary>
	public void Stop() {
		Playing = false;
		_frameTimeAccumulator = 0f;
		SetFrame(0);
	}

	/// <summary>
	/// Returns to the first frame and starts playback immediately.
	/// </summary>
	public void Restart() {
		_frameTimeAccumulator = 0f;
		SetFrame(0);
		Playing = true;
	}

	/// <summary>
	/// Replaces the current frame collection and resets playback to the first frame.
	/// </summary>
	public void SetFrames(SpriteFrames frames) {
		_frames = frames ?? throw new ArgumentNullException(nameof(frames));
		_frameTimeAccumulator = 0f;
		SetFrame(0);
	}

	protected override void OnUpdate(float deltaTime) {
		if (!Playing || _frames.Count <= 1 || SpeedFps <= 0f) {
			return;
		}

		_frameTimeAccumulator += deltaTime;
		var frameDuration = 1f / SpeedFps;

		while (_frameTimeAccumulator >= frameDuration) {
			_frameTimeAccumulator -= frameDuration;

			if (_frame < _frames.Count - 1) {
				SetFrame(_frame + 1);
				continue;
			}

			if (Loop) {
				SetFrame(0);
				continue;
			}

			SetFrame(_frames.Count - 1);
			Playing = false;
			_frameTimeAccumulator = 0f;
			PlaybackCompleted?.Invoke(this);
			break;
		}
	}

	protected override void OnDraw() {
		var slice = _frameTexture.GetSlice();
		var source = slice.Source;

		if (FlipH) {
			source.X += source.Width;
			source.Width = -source.Width;
		}

		if (FlipV) {
			source.Y += source.Height;
			source.Height = -source.Height;
		}

		var globalScale = GlobalScale;
		var destinationSize = new Vector2(slice.Source.Width * globalScale.X, slice.Source.Height * globalScale.Y);
		var destination = new Rectangle(
			GlobalPosition.X,
			GlobalPosition.Y,
			destinationSize.X,
			destinationSize.Y);
		var origin = new Vector2(destinationSize.X * Pivot.X, destinationSize.Y * Pivot.Y);
		var rotationDegrees = GlobalRotation * (180f / MathF.PI);

		Raylib.BeginBlendMode(BlendMode);
		Raylib.DrawTexturePro(slice.Texture, source, destination, origin, rotationDegrees, Modulate);
		Raylib.EndBlendMode();
	}

	private void SetFrame(int frame) {
		if (frame < 0 || frame >= _frames.Count) {
			throw new ArgumentOutOfRangeException(nameof(frame), $"Frame index {frame} is outside the valid range 0..{_frames.Count - 1}.");
		}

		_frame = frame;
		_frameTexture = _frames.GetFrameTexture(frame);
	}
}

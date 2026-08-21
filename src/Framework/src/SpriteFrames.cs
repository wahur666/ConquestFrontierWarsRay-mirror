using System.Collections.ObjectModel;
using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Indexed collection of drawable frames that all share one backing texture.
/// </summary>
public sealed class SpriteFrames : Resource {
	private readonly ReadOnlyCollection<Rectangle> _frames;

	/// <summary>
	/// Creates a frame collection over one texture and one or more regions.
	/// </summary>
	public SpriteFrames(Texture2D texture, IReadOnlyList<Rectangle> frames, string? resourcePath = null) : base(resourcePath) {
		Texture = texture ?? throw new ArgumentNullException(nameof(texture));
		ArgumentNullException.ThrowIfNull(frames);

		if (frames.Count == 0) {
			throw new ArgumentException("Sprite frames must contain at least one frame.", nameof(frames));
		}

		var copy = new Rectangle[frames.Count];

		for (var i = 0; i < frames.Count; i++) {
			var frame = frames[i];

			if (frame.Width <= 0f || frame.Height <= 0f) {
				throw new ArgumentOutOfRangeException(nameof(frames), $"Frame at index {i} must have positive width and height.");
			}

			copy[i] = frame;
		}

		_frames = Array.AsReadOnly(copy);
	}

	/// <summary>
	/// Backing texture shared by every frame.
	/// </summary>
	public Texture2D Texture { get; }

	/// <summary>
	/// Indexed frame regions.
	/// </summary>
	public IReadOnlyList<Rectangle> Frames => _frames;

	/// <summary>
	/// Number of indexed frames.
	/// </summary>
	public int Count => _frames.Count;

	/// <summary>
	/// Creates a one-frame collection that covers the whole texture.
	/// </summary>
	public static SpriteFrames FromTexture(Texture2D texture) {
		ArgumentNullException.ThrowIfNull(texture);
		var size = texture.Size;
		return new SpriteFrames(texture, [new Rectangle(0f, 0f, size.X, size.Y)], texture.ResourcePath);
	}

	/// <summary>
	/// Creates a frame collection from atlas JSON metadata and a texture.
	/// </summary>
	public static SpriteFrames FromAtlas(Texture2D texture, AtlasDefinitionResource atlasDefinition) {
		ArgumentNullException.ThrowIfNull(texture);
		ArgumentNullException.ThrowIfNull(atlasDefinition);

		var frames = atlasDefinition.Frames
			.Select(frame => new Rectangle(frame.X, frame.Y, frame.Width, frame.Height))
			.ToArray();

		return new SpriteFrames(texture, frames, atlasDefinition.ResourcePath ?? texture.ResourcePath);
	}

	/// <summary>
	/// Creates a frame collection by slicing a texture on a regular grid.
	/// </summary>
	public static SpriteFrames FromGrid(
		Texture2D texture,
		int frameWidth,
		int frameHeight,
		int startX = 0,
		int startY = 0,
		int marginX = 0,
		int marginY = 0,
		int spacingX = 0,
		int spacingY = 0,
		int? frameCount = null,
		int? columns = null,
		int? rows = null) {
		ArgumentNullException.ThrowIfNull(texture);

		if (frameWidth <= 0) {
			throw new ArgumentOutOfRangeException(nameof(frameWidth), "Frame width must be positive.");
		}

		if (frameHeight <= 0) {
			throw new ArgumentOutOfRangeException(nameof(frameHeight), "Frame height must be positive.");
		}

		if (startX < 0 || startY < 0 || marginX < 0 || marginY < 0 || spacingX < 0 || spacingY < 0) {
			throw new ArgumentOutOfRangeException(nameof(startX), "Grid slicing values cannot be negative.");
		}

		if (frameCount is <= 0) {
			throw new ArgumentOutOfRangeException(nameof(frameCount), "Frame count must be positive when provided.");
		}

		if (columns is <= 0) {
			throw new ArgumentOutOfRangeException(nameof(columns), "Column count must be positive when provided.");
		}

		if (rows is <= 0) {
			throw new ArgumentOutOfRangeException(nameof(rows), "Row count must be positive when provided.");
		}

		var size = texture.Size;
		var maxWidth = (int)size.X;
		var maxHeight = (int)size.Y;
		var frames = new List<Rectangle>();
		var maxFrames = frameCount ?? int.MaxValue;
		var limitColumns = columns ?? int.MaxValue;
		var limitRows = rows ?? int.MaxValue;
		var initialX = startX + marginX;
		var initialY = startY + marginY;
		var stepX = frameWidth + spacingX;
		var stepY = frameHeight + spacingY;

		for (var row = 0; row < limitRows; row++) {
			var y = initialY + row * stepY;

			if (y + frameHeight > maxHeight) {
				break;
			}

			for (var column = 0; column < limitColumns; column++) {
				var x = initialX + column * stepX;

				if (x + frameWidth > maxWidth) {
					break;
				}

				frames.Add(new Rectangle(x, y, frameWidth, frameHeight));

				if (frames.Count >= maxFrames) {
					return new SpriteFrames(texture, frames, texture.ResourcePath);
				}
			}
		}

		if (frames.Count == 0) {
			throw new InvalidOperationException("Grid slicing did not produce any frames.");
		}

		return new SpriteFrames(texture, frames, texture.ResourcePath);
	}

	/// <summary>
	/// Gets one frame region by index.
	/// </summary>
	public Rectangle GetFrameRegion(int index) {
		EnsureLoaded();
		return _frames[index];
	}

	/// <summary>
	/// Creates a one-frame texture view for the requested frame index.
	/// </summary>
	public AtlasTexture GetFrameTexture(int index) {
		return new AtlasTexture(Texture, GetFrameRegion(index));
	}

	protected override void LoadCore() {
		Texture.GetSlice();
	}
}

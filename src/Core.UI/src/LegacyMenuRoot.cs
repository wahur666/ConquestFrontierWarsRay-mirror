using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored menu root that presents one fixed-resolution subtree inside
/// the current window using centered scale-to-fit pillarboxing.
/// </summary>
public sealed class LegacyMenuRoot : Node2D {
	private Node2D? _contentRoot;

	public LegacyMenuRoot(string? name = null) : base(name) {
		BaseResolution = new Vector2(800f, 600f);
		ContentViewport = new Rectangle(0f, 0f, BaseResolution.X, BaseResolution.Y);
		WindowSize = BaseResolution;
	}

	/// <summary>
	/// Fixed authored resolution for the hosted legacy content.
	/// </summary>
	public Vector2 BaseResolution { get; }

	/// <summary>
	/// Current window size used to compute the viewport transform.
	/// </summary>
	public Vector2 WindowSize { get; private set; }

	/// <summary>
	/// Current centered content viewport in screen coordinates.
	/// </summary>
	public Rectangle ContentViewport { get; private set; }

	/// <summary>
	/// Current uniform scale applied to the hosted legacy subtree.
	/// </summary>
	public float UniformScale { get; private set; } = 1f;

	/// <summary>
	/// Current screen-space rectangles occupied by the black bars around content.
	/// </summary>
	public IReadOnlyList<Rectangle> BlackBarRects => _blackBarRects;

	private List<Rectangle> _blackBarRects { get; } = [];

	/// <summary>
	/// Replaces the currently hosted legacy screen subtree.
	/// </summary>
	public T SetContentRoot<T>(T contentRoot) where T : Node2D {
		ArgumentNullException.ThrowIfNull(contentRoot);

		if (ReferenceEquals(contentRoot, this)) {
			throw new InvalidOperationException("LegacyMenuRoot cannot host itself as content.");
		}

		if (_contentRoot is not null) {
			RemoveChild(_contentRoot);
			_contentRoot.Dispose();
		}

		_contentRoot = AddChild(contentRoot);
		return contentRoot;
	}

	/// <summary>
	/// Recomputes the viewport transform using the supplied window size.
	/// </summary>
	public void RecalculateViewport(Vector2 windowSize) {
		var width = Math.Max(1f, windowSize.X);
		var height = Math.Max(1f, windowSize.Y);

		WindowSize = new Vector2(width, height);
		UniformScale = MathF.Min(width / BaseResolution.X, height / BaseResolution.Y);

		var contentWidth = BaseResolution.X * UniformScale;
		var contentHeight = BaseResolution.Y * UniformScale;
		var offsetX = (width - contentWidth) * 0.5f;
		var offsetY = (height - contentHeight) * 0.5f;

		Position = new Vector2(offsetX, offsetY);
		Scale = new Vector2(UniformScale, UniformScale);
		ContentViewport = new Rectangle(offsetX, offsetY, contentWidth, contentHeight);
		RebuildBlackBars(width, height);
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);
		RecalculateViewport(new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight()));
	}

	protected override void Draw() {
		foreach (var rect in _blackBarRects) {
			Raylib.DrawRectangleRec(rect, Color.Black);
		}
	}

	private void RebuildBlackBars(float windowWidth, float windowHeight) {
		_blackBarRects.Clear();

		var leftWidth = Math.Max(0f, ContentViewport.X);
		if (leftWidth > 0f) {
			_blackBarRects.Add(new Rectangle(0f, 0f, leftWidth, windowHeight));
		}

		var rightX = ContentViewport.X + ContentViewport.Width;
		var rightWidth = Math.Max(0f, windowWidth - rightX);
		if (rightWidth > 0f) {
			_blackBarRects.Add(new Rectangle(rightX, 0f, rightWidth, windowHeight));
		}

		var topHeight = Math.Max(0f, ContentViewport.Y);
		if (topHeight > 0f) {
			_blackBarRects.Add(new Rectangle(ContentViewport.X, 0f, ContentViewport.Width, topHeight));
		}

		var bottomY = ContentViewport.Y + ContentViewport.Height;
		var bottomHeight = Math.Max(0f, windowHeight - bottomY);
		if (bottomHeight > 0f) {
			_blackBarRects.Add(new Rectangle(ContentViewport.X, bottomY, ContentViewport.Width, bottomHeight));
		}
	}
}

using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Drawing-oriented base node for the 2D canvas stack.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CanvasItem"/> sits between <see cref="Node"/> and concrete 2D
/// nodes such as <see cref="Node2D"/> and <see cref="Control"/>.
/// </para>
/// <para>
/// It adds visibility, z ordering, optional Y sorting, and local-space helper
/// drawing methods that are transformed through <see cref="CanvasTransform"/>.
/// </para>
/// </remarks>
public abstract class CanvasItem : Node {
	/// <summary>
	/// Creates a drawable canvas node.
	/// </summary>
	protected CanvasItem(string? name = null) : base(name) {
	}

	/// <summary>
	/// Controls whether this canvas item and its subtree are drawn.
	/// </summary>
	public bool Visible { get; set; } = true;

	/// <summary>
	/// Draw order offset for this canvas item.
	/// </summary>
	public int ZIndex { get; set; }

	/// <summary>
	/// When enabled, this item inherits parent z ordering.
	/// </summary>
	public bool ZAsRelative { get; set; } = true;

	/// <summary>
	/// Final z index after parent ordering is applied.
	/// </summary>
	public int GlobalZIndex {
		get {
			if (!ZAsRelative) {
				return ZIndex;
			}

			return ZIndex + ((Parent as CanvasItem)?.GlobalZIndex ?? 0);
		}
	}

	/// <summary>
	/// Sort direct canvas children by their world Y position when z values match.
	/// </summary>
	public bool YSortEnabled { get; set; }

	/// <summary>
	/// Transform applied to local drawing points.
	/// </summary>
	protected virtual Matrix3x2 CanvasTransform => Matrix3x2.Identity;

	public override void DrawRecursive() {
		if (!Visible) {
			return;
		}

		OnDraw();

		foreach (var child in GetDrawOrderedChildren()) {
			child.DrawRecursive();
		}
	}

	/// <summary>
	/// Draws a circle in local space.
	/// </summary>
	protected void DrawCircle(Vector2 center, float radius, Color color) {
		Raylib.DrawCircleV(TransformPoint(center), radius * GetTransformScale(), color);
	}

	/// <summary>
	/// Draws a line in local space.
	/// </summary>
	protected void DrawLine(Vector2 start, Vector2 end, Color color) {
		Raylib.DrawLineV(TransformPoint(start), TransformPoint(end), color);
	}

	/// <summary>
	/// Draws connected lines in local space.
	/// </summary>
	protected void DrawPolyline(IReadOnlyList<Vector2> points, Color color, bool closed = false) {
		ArgumentNullException.ThrowIfNull(points);

		if (points.Count < 2) {
			return;
		}

		for (var i = 0; i < points.Count - 1; i++) {
			DrawLine(points[i], points[i + 1], color);
		}

		if (closed) {
			DrawLine(points[^1], points[0], color);
		}
	}

	private Vector2 TransformPoint(Vector2 point) {
		return Vector2.Transform(point, CanvasTransform);
	}

	private float GetTransformScale() {
		var xScale = new Vector2(CanvasTransform.M11, CanvasTransform.M12).Length();
		var yScale = new Vector2(CanvasTransform.M21, CanvasTransform.M22).Length();

		return (xScale + yScale) * 0.5f;
	}

	private IEnumerable<Node> GetDrawOrderedChildren() {
		return Children
			.Select((child, index) => new OrderedChild(child, index, GetSortY(child)))
			.OrderBy(item => GetSortZ(item.Child))
			.ThenBy(item => item.SortY)
			.ThenBy(item => item.Index)
			.Select(item => item.Child);
	}

	private float GetSortY(Node child) {
		if (!YSortEnabled || child is not Node2D node2D) {
			return 0f;
		}

		return node2D.GlobalPosition.Y;
	}

	private static int GetSortZ(Node child) {
		return child is CanvasItem canvasItem ? canvasItem.GlobalZIndex : 0;
	}

	private readonly record struct OrderedChild(Node Child, int Index, float SortY);
}

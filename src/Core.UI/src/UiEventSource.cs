using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Primitive pointer event source that samples Raylib input and bubbles events up the node tree.
/// </summary>
public sealed class UiEventSource : Node {
	private readonly List<PointerTarget> _targets = [];
	private Vector2 _lastPointerPosition;
	private bool _hasPointerPosition;
	private Node? _pressedTarget;
	private Node? _hoveredTarget;

	public UiEventSource(string? name = null) : base(name ?? "UiEventSource") {
	}

	/// <summary>
	/// Optional subtree root to dispatch within. Defaults to the active scene root.
	/// </summary>
	public Node? ScopeRoot { get; set; }

	protected override void OnUpdate(float deltaTime) {
		var pointerPosition = Raylib.GetMousePosition();
		var target = ResolveTarget(pointerPosition);
		var targetNode = target?.Node;

		if (!ReferenceEquals(targetNode, _hoveredTarget)) {
			if (_hoveredTarget is not null) {
				Dispatch(_hoveredTarget, UiPointerEventKind.Leave, pointerPosition, null, 0f);
			}

			_hoveredTarget = targetNode;

			if (_hoveredTarget is not null) {
				Dispatch(_hoveredTarget, UiPointerEventKind.Enter, pointerPosition, null, 0f);
			}
		}

		if (!_hasPointerPosition || pointerPosition != _lastPointerPosition) {
			_lastPointerPosition = pointerPosition;
			_hasPointerPosition = true;

			if (targetNode is not null) {
				Dispatch(targetNode, UiPointerEventKind.Move, pointerPosition, null, 0f);
			}
		}

		if (Raylib.IsMouseButtonPressed(MouseButton.Left) && targetNode is not null) {
			_pressedTarget = targetNode;
			Dispatch(targetNode, UiPointerEventKind.Down, pointerPosition, MouseButton.Left, 0f);
		}

		if (Raylib.IsMouseButtonReleased(MouseButton.Left)) {
			if (_pressedTarget is not null) {
				Dispatch(_pressedTarget, UiPointerEventKind.Up, pointerPosition, MouseButton.Left, 0f);

				if (targetNode is not null && ReferenceEquals(targetNode, _pressedTarget)) {
					Dispatch(_pressedTarget, UiPointerEventKind.Click, pointerPosition, MouseButton.Left, 0f);
				}
			}

			_pressedTarget = null;
		}

		var wheelDelta = Raylib.GetMouseWheelMove();
		if (MathF.Abs(wheelDelta) > float.Epsilon && targetNode is not null) {
			Dispatch(targetNode, UiPointerEventKind.Wheel, pointerPosition, null, wheelDelta);
		}
	}

	private PointerTarget? ResolveTarget(Vector2 pointerPosition) {
		_targets.Clear();
		CollectTargets(ResolveScopeRoot(), _targets);

		for (var i = _targets.Count - 1; i >= 0; i--) {
			var target = _targets[i];
			if (target.Handler.IsPointerInputEnabled && target.Handler.HitTest(pointerPosition)) {
				return target;
			}
		}

		return null;
	}

	private Node ResolveScopeRoot() {
		return ScopeRoot ?? Tree.Root;
	}

	private static void CollectTargets(Node node, List<PointerTarget> targets) {
		foreach (var child in node.Children) {
			CollectTargets(child, targets);
		}

		if (node is IUiPointerEventHandler handler) {
			targets.Add(new PointerTarget(node, handler));
		}
	}

	private static void Dispatch(Node targetNode, UiPointerEventKind kind, Vector2 pointerPosition, MouseButton? button, float wheelDelta) {
		var pointerEvent = new UiPointerEvent(kind, pointerPosition, button, wheelDelta, targetNode);

		for (Node? current = targetNode; current is not null; current = current.Parent) {
			if (current is not IUiPointerEventHandler handler || !handler.IsPointerInputEnabled) {
				continue;
			}

			pointerEvent.CurrentTarget = current;
			handler.OnPointerEvent(pointerEvent);

			if (pointerEvent.Handled) {
				break;
			}
		}
	}

	private readonly record struct PointerTarget(Node Node, IUiPointerEventHandler Handler);
}

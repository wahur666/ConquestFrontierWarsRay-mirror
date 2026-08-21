using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Primitive pointer event source that samples Raylib input and bubbles events up the node tree.
/// </summary>
public sealed class UiEventSource : Node {
	private static readonly MouseButton[] RoutedButtons = [MouseButton.Left, MouseButton.Middle, MouseButton.Right];
	private readonly List<PointerTarget> _targets = [];
	private readonly Dictionary<MouseButton, Node> _pressedTargets = [];
	private Vector2 _lastPointerPosition;
	private bool _hasPointerPosition;
	private Node? _hoveredTarget;

	public UiEventSource(string? name = null) : base(name ?? "UiEventSource") {
	}

	/// <summary>
	/// Optional subtree root to dispatch within. Defaults to the active scene root.
	/// </summary>
	public Node? ScopeRoot { get; set; }

	protected override void OnUpdate(float deltaTime) {
		var inputSnapshot = Input.CaptureUiSnapshot();
		var pointerPosition = inputSnapshot.PointerPosition;
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

		foreach (var button in RoutedButtons) {
			if (IsPressed(inputSnapshot, button) && targetNode is not null) {
				_pressedTargets[button] = targetNode;
				Dispatch(targetNode, UiPointerEventKind.Down, pointerPosition, button, 0f);
			}

			if (!IsReleased(inputSnapshot, button)) {
				continue;
			}

			if (_pressedTargets.TryGetValue(button, out var pressedTarget)) {
				Dispatch(pressedTarget, UiPointerEventKind.Up, pointerPosition, button, 0f);

				if (targetNode is not null && ReferenceEquals(targetNode, pressedTarget)) {
					Dispatch(pressedTarget, UiPointerEventKind.Click, pointerPosition, button, 0f);
				}
			}

			_pressedTargets.Remove(button);
		}

		var wheelDelta = inputSnapshot.WheelDelta;
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

			pointerEvent.RouteTo(current);
			handler.OnPointerEvent(pointerEvent);

			if (pointerEvent.Handled) {
				break;
			}
		}
	}

	private static bool IsPressed(UiInputSnapshot inputSnapshot, MouseButton button) {
		return button switch {
			MouseButton.Left => inputSnapshot.LeftPressed,
			MouseButton.Middle => inputSnapshot.MiddlePressed,
			MouseButton.Right => inputSnapshot.RightPressed,
			_ => false
		};
	}

	private static bool IsReleased(UiInputSnapshot inputSnapshot, MouseButton button) {
		return button switch {
			MouseButton.Left => inputSnapshot.LeftReleased,
			MouseButton.Middle => inputSnapshot.MiddleReleased,
			MouseButton.Right => inputSnapshot.RightReleased,
			_ => false
		};
	}

	private readonly record struct PointerTarget(Node Node, IUiPointerEventHandler Handler);
}

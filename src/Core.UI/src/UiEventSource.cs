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
	private readonly Dictionary<MouseButton, Node> _capturedTargets = [];
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

			DispatchCapturedMoves(pointerPosition, targetNode);
		}

		foreach (var button in RoutedButtons) {
			if (IsPressed(inputSnapshot, button) && targetNode is not null) {
				_pressedTargets[button] = targetNode;
				var pointerEvent = Dispatch(targetNode, UiPointerEventKind.Down, pointerPosition, button, 0f);
				if (pointerEvent.CaptureRequested && pointerEvent.CaptureTarget is not null) {
					_capturedTargets[button] = pointerEvent.CaptureTarget;
				}
			}

			if (!IsReleased(inputSnapshot, button)) {
				continue;
			}

			var releaseTarget = ResolveReleaseTarget(button);
			if (releaseTarget is not null) {
				Dispatch(releaseTarget, UiPointerEventKind.Up, pointerPosition, button, 0f);
			}

			if (_pressedTargets.TryGetValue(button, out var pressedTarget)) {
				if (targetNode is not null && ReferenceEquals(targetNode, pressedTarget)) {
					Dispatch(pressedTarget, UiPointerEventKind.Click, pointerPosition, button, 0f);
				}
			}

			_pressedTargets.Remove(button);
			_capturedTargets.Remove(button);
		}

		var wheelDelta = inputSnapshot.WheelDelta;
		if (MathF.Abs(wheelDelta) > float.Epsilon && targetNode is not null) {
			Dispatch(targetNode, UiPointerEventKind.Wheel, pointerPosition, null, wheelDelta);
		}
	}

	private PointerTarget? ResolveTarget(Vector2 pointerPosition) {
		_targets.Clear();
		CollectTargets(ResolveScopeRoot(), _targets);

		foreach (var target in GetHitTestOrderedTargets()) {
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
		if (node is IUiPointerEventHandler handler) {
			targets.Add(new PointerTarget(node, handler));
		}

		foreach (var child in node.Children) {
			CollectTargets(child, targets);
		}
	}

	private IEnumerable<PointerTarget> GetHitTestOrderedTargets() {
		return _targets
			.Select((target, index) => new OrderedPointerTarget(target, index))
			.OrderByDescending(item => item.ZIndex)
			.ThenByDescending(item => item.Index)
			.Select(item => item.Target);
	}

	private void DispatchCapturedMoves(Vector2 pointerPosition, Node? hoverTarget) {
		var dispatchedTargets = new HashSet<Node>(ReferenceEqualityComparer.Instance);
		if (hoverTarget is not null) {
			dispatchedTargets.Add(hoverTarget);
		}

		foreach (var capturedTarget in _capturedTargets.Values) {
			if (!capturedTarget.IsInTree || !dispatchedTargets.Add(capturedTarget)) {
				continue;
			}

			Dispatch(capturedTarget, UiPointerEventKind.Move, pointerPosition, null, 0f);
		}
	}

	private Node? ResolveReleaseTarget(MouseButton button) {
		if (_capturedTargets.TryGetValue(button, out var capturedTarget) && capturedTarget.IsInTree) {
			return capturedTarget;
		}

		if (_pressedTargets.TryGetValue(button, out var pressedTarget) && pressedTarget.IsInTree) {
			return pressedTarget;
		}

		return null;
	}

	private static UiPointerEvent Dispatch(Node targetNode, UiPointerEventKind kind, Vector2 pointerPosition, MouseButton? button, float wheelDelta) {
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

		return pointerEvent;
	}

	private sealed class ReferenceEqualityComparer : IEqualityComparer<Node> {
		public static readonly ReferenceEqualityComparer Instance = new();

		public bool Equals(Node? x, Node? y) {
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(Node obj) {
			return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
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
	private readonly record struct OrderedPointerTarget(PointerTarget Target, int Index) {
		public int ZIndex => Target.Node is CanvasItem canvasItem ? canvasItem.GlobalZIndex : 0;
	}
}

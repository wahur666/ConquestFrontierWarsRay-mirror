using System.Numerics;
using System.Globalization;
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
	public bool LogPointerDispatch { get; set; }
	public bool LogPointerMoveDispatch { get; set; }

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
				var moveEvent = Dispatch(targetNode, UiPointerEventKind.Move, pointerPosition, null, 0f);
				if (ShouldLogMove(inputSnapshot)) {
					LogPointerMoveSummary(moveEvent, "Move");
				}
			}

			DispatchCapturedMoves(pointerPosition, targetNode, inputSnapshot);
		}

		foreach (var button in RoutedButtons) {
			if (IsPressed(inputSnapshot, button) && targetNode is not null) {
				_pressedTargets[button] = targetNode;
				var pointerEvent = Dispatch(targetNode, UiPointerEventKind.Down, pointerPosition, button, 0f);
				LogPointerDispatchSummary(pointerEvent);
				if (pointerEvent.CaptureRequested && pointerEvent.CaptureTarget is not null) {
					_capturedTargets[button] = pointerEvent.CaptureTarget;
				}
			} else if (IsPressed(inputSnapshot, button)) {
				LogPointerMiss(UiPointerEventKind.Down, pointerPosition, button);
			}

			if (!IsReleased(inputSnapshot, button)) {
				continue;
			}

			var releaseTarget = ResolveReleaseTarget(button);
			if (releaseTarget is not null) {
				LogPointerDispatchSummary(Dispatch(releaseTarget, UiPointerEventKind.Up, pointerPosition, button, 0f));
			} else {
				LogPointerMiss(UiPointerEventKind.Up, pointerPosition, button);
			}

			if (_pressedTargets.TryGetValue(button, out var pressedTarget)) {
				if (targetNode is not null && ReferenceEquals(targetNode, pressedTarget)) {
					LogPointerDispatchSummary(Dispatch(pressedTarget, UiPointerEventKind.Click, pointerPosition, button, 0f));
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
		if (node is CanvasItem { Visible: false }) {
			return;
		}

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

	private void DispatchCapturedMoves(Vector2 pointerPosition, Node? hoverTarget, UiInputSnapshot inputSnapshot) {
		var dispatchedTargets = new HashSet<Node>(ReferenceEqualityComparer.Instance);
		if (hoverTarget is not null) {
			dispatchedTargets.Add(hoverTarget);
		}

		foreach (var capturedTarget in _capturedTargets.Values) {
			if (!capturedTarget.IsInTree || !dispatchedTargets.Add(capturedTarget)) {
				continue;
			}

			var moveEvent = Dispatch(capturedTarget, UiPointerEventKind.Move, pointerPosition, null, 0f);
			if (ShouldLogMove(inputSnapshot)) {
				LogPointerMoveSummary(moveEvent, "CapturedMove");
			}
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

	private void LogPointerDispatchSummary(UiPointerEvent pointerEvent) {
		if (!LogPointerDispatch || pointerEvent.Button is null || !IsClickDiagnosticKind(pointerEvent.Kind)) {
			return;
		}

		var capture = pointerEvent.CaptureTarget is null ? "<none>" : GetNodePath(pointerEvent.CaptureTarget);
		AppLog.Info(
			Name,
			string.Create(
				CultureInfo.InvariantCulture,
				$"Pointer {pointerEvent.Kind} {pointerEvent.Button} at ({pointerEvent.Position.X:0.00},{pointerEvent.Position.Y:0.00}) "
				+ $"target='{GetNodePath(pointerEvent.OriginalTarget)}' lastReceiver='{GetNodePath(pointerEvent.CurrentTarget)}' "
				+ $"handled={pointerEvent.Handled} capture='{capture}'."));
	}

	private void LogPointerMoveSummary(UiPointerEvent pointerEvent, string label) {
		if (!LogPointerDispatch || !LogPointerMoveDispatch) {
			return;
		}

		AppLog.Info(
			Name,
			string.Create(
				CultureInfo.InvariantCulture,
				$"Pointer {label} at ({pointerEvent.Position.X:0.00},{pointerEvent.Position.Y:0.00}) "
				+ $"target='{GetNodePath(pointerEvent.OriginalTarget)}' lastReceiver='{GetNodePath(pointerEvent.CurrentTarget)}' "
				+ $"handled={pointerEvent.Handled}."));
	}

	private void LogPointerMiss(UiPointerEventKind kind, Vector2 pointerPosition, MouseButton button) {
		if (!LogPointerDispatch || !IsClickDiagnosticKind(kind)) {
			return;
		}

		AppLog.Info(
			Name,
			string.Create(
				CultureInfo.InvariantCulture,
				$"Pointer {kind} {button} at ({pointerPosition.X:0.00},{pointerPosition.Y:0.00}) target='<none>'."));
	}

	private static bool IsClickDiagnosticKind(UiPointerEventKind kind) {
		return kind is UiPointerEventKind.Down or UiPointerEventKind.Up or UiPointerEventKind.Click;
	}

	private bool ShouldLogMove(UiInputSnapshot inputSnapshot) {
		return LogPointerDispatch
			&& LogPointerMoveDispatch
			&& (inputSnapshot.LeftDown || inputSnapshot.MiddleDown || inputSnapshot.RightDown || _capturedTargets.Count > 0);
	}

	private static string GetNodePath(Node node) {
		var names = new Stack<string>();
		for (var current = node; current is not null; current = current.Parent) {
			names.Push(current.Name);
		}

		return string.Join("/", names);
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

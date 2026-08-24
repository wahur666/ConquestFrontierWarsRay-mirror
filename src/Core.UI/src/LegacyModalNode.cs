using System.Numerics;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Reusable authored-space modal overlay for legacy menu composition.
/// </summary>
public class LegacyModalNode : Node2D {
	private readonly Action? _closeRequested;
	private readonly ModalInputBlockerNode _inputBlocker;

	public LegacyModalNode(string? name = null, Action? closeRequested = null) : base(name ?? "LegacyModalNode") {
		_closeRequested = closeRequested;
		ZIndex = 1000;
		_inputBlocker = new ModalInputBlockerNode("ModalInputBlocker") {
			Size = new Vector2(800f, 600f)
		};
		ContentRoot = new Node2D("ModalContentRoot");
	}

	public Node2D ContentRoot { get; }
	public bool CloseOnEscape { get; set; } = true;

	protected override void OnInitialize() {
		AddChild(_inputBlocker);
		AddChild(ContentRoot);
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);
		if (CloseOnEscape && Input.IsActionJustPressed(InputManager.UiEscapeAction)) {
			RequestClose();
		}
	}

	protected void RequestClose() {
		_closeRequested?.Invoke();
	}
}

internal sealed class ModalInputBlockerNode : Control, IUiPointerEventHandler {
	public ModalInputBlockerNode(string? name = null) : base(name) {
	}

	public bool IsPointerInputEnabled => Visible;

	public bool HitTest(Vector2 screenPoint) {
		return ContainsPoint(screenPoint);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		pointerEvent.MarkHandled();
	}
}

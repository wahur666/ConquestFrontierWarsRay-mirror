# UI Event Consumption Proposal

Tracking checklist: [ui-event-system-checklist.md](./ui-event-system-checklist.md)

## Problem

The current UI framework requires scenes to poll controls manually and decide
ordering themselves. That creates inconsistent input ownership:

- multiple controls can react to the same click if callers poll them in the
  wrong order
- controls cannot reliably block parents or siblings
- sliders and other drag interactions have no framework-level pointer capture
- dropdowns and future modals have no central outside-click or focus policy

This is not only a `bool consumed` problem. It is a missing framework-owned
dispatch model.

Stealing good ideas from the Godot Engine is permitted!

## Current Spike Status

A first vertical slice now exists in `Core.UI` as an experiment surface.

Implemented spike pieces:

- `UiEventSource`
- `UiPointerEvent`
- `IUiPointerEventHandler`
- `HotRectNode`
- `Framework.TestApp` scene `Hot Rect Events`

The spike currently proves:

- topmost target resolution inside a scoped node subtree
- upward bubbling through the node parent chain
- stop-propagation through `Handled`
- visible/logged event traces for debugging
- a practical first replacement for the `BaseHotRect` idea of:
  - rectangular hit ownership
  - root-attached event sampling
  - upward event routing

The spike is intentionally narrow. It is not yet the final framework-wide UI
dispatcher.

## Goal

Move UI input ownership into the framework so controls receive routed events and
can explicitly consume them. Scenes should stop deciding which control gets the
mouse first.

## Proposed Solution

Add a framework-owned `UiInputDispatcher` that runs once per frame and routes
input to `Control` subtrees.

### Dispatcher Responsibilities

1. Read raw input from Raylib once per frame.
2. Build a small UI input snapshot:
   - mouse position
   - mouse button pressed/down/released
   - wheel delta
   - named UI actions from `InputManager`
3. Resolve the active target control using front-to-back hit order.
4. Dispatch pointer events to the target control.
5. Stop propagation when a control marks the event handled.
6. Maintain framework-owned pointer capture for drag interactions.
7. Maintain framework-owned focus ownership for keyboard/gamepad navigation.

## Event Model

Add lightweight event objects instead of direct polling from each control.

### Pointer Events

Suggested event kinds:

- `Enter`
- `Leave`
- `Move`
- `Down`
- `Up`
- `Click`
- `Wheel`

Suggested event fields:

- `Position`
- `Button`
- `WheelDelta`
- `OriginalTarget`
- `CurrentTarget`
- `Handled`
- `CaptureRequested`
- `FocusRequested`

### Spike-Observed Pointer Semantics

The current spike emits and logs these pointer events:

- `Enter`: pointer moved from no target or a different target into the resolved target
- `Leave`: pointer moved from the previous resolved target to no target or a different target
- `Move`: pointer position changed while a target is resolved
- `Down`: a routed mouse button pressed on the resolved target
- `Up`: a routed mouse button released and routed to the node that received `Down`
- `Click`: a routed mouse button released over the same resolved target that received `Down`
- `Wheel`: wheel moved while a target is resolved

This is good enough for a first dispatcher slice because it exposes the real
questions clearly:

- which node becomes the resolved target
- how bubbling should work
- when a node may stop propagation
- where pointer capture must take over from plain hit-testing

### Keyboard / Navigation Events

Suggested event kinds:

- `Navigate`
- `Accept`
- `Back`
- `Cancel`

These should route to the focused control first, with optional parent bubbling
only if unhandled.

## Control API Changes

Extend `Control` with framework-facing input hooks instead of scene-owned
polling.

Suggested additions:

- `IsEnabled`
- `IsFocusable`
- `HasFocus`
- `BlocksPointerBelow`

Suggested virtual handlers:

- `OnPointerDown(UiPointerEvent e)`
- `OnPointerUp(UiPointerEvent e)`
- `OnPointerMove(UiPointerEvent e)`
- `OnPointerWheel(UiPointerEvent e)`
- `OnNavigate(UiNavigationEvent e)`
- `OnActivate(UiCommandEvent e)`

Suggested events raised by controls:

- `Pressed`
- `Released`
- `Clicked`
- `ValueChanged`
- `SelectionChanged`
- `FocusChanged`

## Dispatch Rules

### Pointer Targeting

- Hit-test controls in visual front-to-back order.
- Dispatch to the first eligible control under the pointer.
- If the event is handled, do not continue to siblings or parents.

### Bubbling

- Start dispatch at the resolved target.
- Then walk upward through `Parent`.
- Only nodes implementing the pointer-handler contract participate.
- Update `CurrentTarget` at each step.
- Stop when `Handled` becomes `true`.

This bubbling rule is now proven by the `Hot Rect Events` test scene:

- one child target allows bubbling into its parent
- one child target marks the event handled and blocks the parent route

### Pointer Capture

- A control may request capture during pointer down.
- While captured, that control receives move and up events even when the cursor
  leaves its bounds.
- Release capture automatically on pointer up or when the control leaves the
  tree.

This is required for sliders, drag handles, splitters, and similar controls.

### Focus Ownership

- A control may request focus when clicked or navigated to.
- Only one control owns focus at a time.
- Keyboard/gamepad UI actions route to the focused control.

### Modal Blocking

- Introduce a dispatcher-level concept of an active modal or input scope.
- When a modal is active, controls outside that scope do not receive UI events.

This will later support dialogs, dropdown popups, overlays, and pause menus.

## Why A Simple `bool` Return Is Not Enough

A plain `HandleInput() -> bool` convention does not solve the broader ownership
problem because:

- dispatch order still lives in scenes
- drag interactions still need capture
- keyboard input still needs focus ownership
- modals still need scope blocking
- outside-click behavior still needs framework policy

The fix must move event routing into the framework, not just standardize return
values on existing polling methods.

## Recommended Implementation Slice

Implement the smallest useful vertical slice first:

1. Keep the existing `Core.UI` spike as the experimental seam.
2. Rename or stabilize `UiEventSource` as the framework-owned dispatcher shape.
3. Promote `UiPointerEvent` and the pointer-handler contract to the intended long-term package boundary.
4. Convert one existing framework control, preferably `ButtonNode`, to dispatched pointer click handling.
5. Convert `SliderNode` next to prove pointer capture.
6. Add framework-owned focus state after pointer routing is stable.

This is the minimum slice that moves the current spike toward a real framework
solution instead of leaving it as a parallel experiment.

## Immediate Next Step

The next implementation target should be:

1. adapt `ButtonNode` to `IUiPointerEventHandler`
2. let `UiEventSource` route to real framework controls, not only `HotRectNode`
3. preserve the `Hot Rect Events` scene as the debugger scene for routed events
4. add pointer capture only when `SliderNode` is converted

This ordering matters. The current system first needs one normal control using
dispatch cleanly before adding capture, focus, modal scopes, or full migration.

## Migration Plan

1. Keep existing `HandleInput()` methods temporarily as compatibility shims.
2. Introduce dispatcher-driven handlers on `Control`.
3. Convert `ButtonNode`, `SliderNode`, `DropdownNode`, and `ListViewNode`.
4. Update test app scenes to stop polling controls manually.
5. Remove public polling as the primary integration path once the test app is
   using framework dispatch.

## Expected Outcome

After this change:

- scenes no longer decide control polling order
- controls can reliably consume input
- dragging works through framework capture
- focus becomes a framework concept instead of app logic
- modal and popup behavior has a viable base to build on

This aligns with the existing pain point that UI reuse is blocked by missing
event dispatch, focus ownership, and input consumption semantics.

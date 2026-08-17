# UI Event Consumption Proposal

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

- `Move`
- `Down`
- `Up`
- `Click`
- `Wheel`

Suggested event fields:

- `Position`
- `Button`
- `WheelDelta`
- `Handled`
- `CaptureRequested`
- `FocusRequested`

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

1. Add `UiInputDispatcher`.
2. Add `UiPointerEvent`.
3. Add `Handled` and `CaptureRequested`.
4. Store focused control in `SceneTree`.
5. Convert `ButtonNode` to dispatched pointer click handling.
6. Convert `SliderNode` next to prove pointer capture.

This is the minimum slice that actually resolves the event consumption problem
instead of relocating it.

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

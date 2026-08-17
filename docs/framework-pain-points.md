# Framework Pain Points

This document tracks the current implementation gaps that still limit how
reusable the framework components are. It is intentionally about live missing
systems and rough integration surfaces, not about already-resolved direction
decisions or documentation cleanups.

Related: [Plan](./plan.md)

## Proposal Pages

- [UI Event Consumption Proposal](./ui-event-consumption-proposal.md)
- [Basic Layout Containers Proposal](./basic-layout-containers-proposal.md)

## Current Assessment

The framework has a usable foundation: node lifecycle, tree ownership, root
switching, transform propagation, named input actions, resource wrappers, simple
UI widgets, sprite/audio/video nodes, and a test app that exercises the pieces
together.

The main weakness is still system-level reuse. Many features are reusable as
individual classes, but not yet reusable as complete application-building
systems. The test app still has to manually coordinate UI input, layout,
screen composition, 3D rendering, and asset ownership.

## 1. UI Controls Require Manual Polling

### Evidence

- `ButtonNode`, `DropdownNode`, `SliderNode`, and `ListViewNode` expose
  `HandleInput()` methods.
- Scenes call each control manually from their own `OnUpdate()` methods.
- Modal behavior is coordinated by app-side ordering rather than a
  framework-level UI event model.

### Why This Hurts Reuse

Every screen has to remember which controls exist, in what order to poll them,
and when one control should block another. This works for small showcase scenes,
but it does not scale well to real menus, dialogs, HUDs, inspectors, or nested
panels.

### Missing Framework Pieces

- Pointer event routing.
- Keyboard/gamepad focus ownership.
- Focus traversal between controls.
- Press/release/click events owned by the control itself.
- Pointer capture for dragging.
- Disabled/read-only states.
- Modal input blocking.
- A consistent way for controls to consume input so parents and siblings do not
  also react.

### Suggested Work Items

1. Add a UI input dispatch pass for `Control` subtrees.
2. Add focus state to `Control`.
3. Add event hooks such as `Pressed`, `ValueChanged`, `SelectionChanged`, and
   `FocusChanged`.
4. Convert existing controls away from public `HandleInput()` polling as the
   main integration path.
5. Keep direct polling only as a low-level escape hatch or testing helper.

Related proposal: [UI Event Consumption Proposal](./ui-event-consumption-proposal.md)

## 2. No Layout System

### Evidence

- Test app scenes hard-code almost every `Position`, `Size`, and text
  coordinate.
- `Control` only provides `Size`, `GlobalBounds`, and hit testing.

### Why This Hurts Reuse

Reusable controls are still difficult to compose because every caller must
manually place them. Any new screen size, scaling mode, sidebar width, list
length, or localization change becomes manual coordinate work.

### Missing Framework Pieces

- Anchors or margins.
- Layout containers.
- Minimum/preferred size.
- Parent-driven child layout.
- Padding and spacing.
- Basic text wrapping or measured label sizing.
- Responsive handling for window resize.

### Suggested Work Items

1. Add `Control` layout properties such as minimum size, margin, and
   anchor/preset.
2. Add a simple `VBoxContainer` and `HBoxContainer`.
3. Add a `PanelContainer` or content-wrapper pattern for padding.
4. Add measured text helpers to `TextNode`.
5. Convert one test app scene from hard-coded coordinates to containers as a
   proof point.

Related proposal: [Basic Layout Containers Proposal](./basic-layout-containers-proposal.md)

## 3. 3D Still Lacks Reusable Renderables

### Evidence

- `Node3D`, `Camera3DNode`, `Socket3D`, `Hardpoint3D`, `Light3D`, and
  `LightShader3D` are implemented.
- The test app still creates render traversal, cube mesh drawing, and gizmos
  inside `ThreeDScene`.
- The next missing layer is still `VisualInstance3D` and a first concrete
  reusable renderable such as `MeshInstance3D`.

### Why This Hurts Reuse

The current 3D stack proves transform propagation and basic scene ownership,
but it still does not let game code build reusable renderable 3D scene objects.
Every 3D consumer still has to invent render traversal and renderable node
conventions in app code.

### Missing Framework Pieces

- Active camera selection.
- 3D draw traversal or render pass ownership.
- `VisualInstance3D`.
- `MeshInstance3D`.
- Mesh/model resource abstraction.
- 3D visibility/layers.

### Suggested Work Items

1. Add active camera resolution through `SceneTree` or a 3D viewport node.
2. Add `VisualInstance3D` as the base renderable 3D node.
3. Move the test app cube renderer into a first framework renderable node.
4. Add tests for 3D transform inheritance through sockets and renderable
   children.

## 4. Resource Management Is Local, Not Centralized

### Evidence

- `Resource` provides lazy load and disposal.
- Scenes manually create and dispose resources.
- There is no cache, asset root, preload queue, or shared ownership model.

### Why This Hurts Reuse

Reusable resource types exist, but applications still have to decide when
identical paths share loaded assets, who owns them, and how assets are found.
That can lead to duplicate loads, inconsistent disposal, and hard-coded asset
paths.

### Missing Framework Pieces

- Asset root abstraction.
- Resource cache.
- Shared resource handles or reference counting.
- Preload API.
- Async or staged loading, if needed later.
- Consistent path resolution for app assets.

### Suggested Work Items

1. Add an asset path resolver or asset root service.
2. Add a minimal resource cache keyed by type and path.
3. Define ownership rules for resources passed into nodes.
4. Convert one sprite/audio/video scene to use the cache.
5. Add tests for cache reuse and disposal behavior.

## 5. Scene and UI Composition Still Lives Mostly in the Test App

### Evidence

- `ShowcaseShellNode` manually owns scene switching, sidebar rendering, pause
  dialog routing, and root swapping.
- `ShowcaseScene` is a test app abstraction rather than a framework scene
  concept.

### Why This Hurts Reuse

The framework has `SceneTree`, but does not yet provide reusable scene
navigation, stack, modal, overlay, or screen composition patterns. Each app
will likely rebuild this shell logic.

### Missing Framework Pieces

- Scene stack or screen manager.
- Overlay/modal layer conventions.
- Transition hooks.
- Pause/menu flow as nodes rather than external helper objects.
- Reusable shell/sidebar/menu composition primitives.

### Suggested Work Items

1. Decide whether scene stack management belongs in the framework.
2. If yes, add a `SceneStack` or `ScreenManagerNode`.
3. Convert `PauseDialog` into a node-based modal control.
4. Make menu/sidebar composition use framework layout and input routing once
   those exist.

## Recommended Order

1. UI event dispatch, focus, and input consumption.
2. Basic layout containers.
3. Node-based modal/dialog/menu patterns.
4. First reusable 3D renderable stack.
5. Resource cache and asset path service.

This order should reduce manual wiring first, then make the test app prove real
reuse by deleting code from scenes rather than only adding new framework
classes.

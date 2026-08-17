# Framework Pain Points

This document tracks the implementation gaps that currently limit how reusable the framework components are. It is intentionally about missing reusable systems and rough integration surfaces, not about whether the implemented pieces are valuable.

## Current Assessment

The framework has a usable foundation: node lifecycle, tree ownership, root switching, transform propagation, resource wrappers, simple UI widgets, sprite/audio/video nodes, and a test app that exercises the pieces together.

The main weakness is that many features are reusable as individual classes, but not yet reusable as complete application-building systems. The test app still has to manually coordinate input, layout, interaction, and 3D rendering.

## 1. UI Controls Require Manual Polling

### Evidence

- `ButtonNode`, `DropdownNode`, `SliderNode`, and `ListViewNode` expose `HandleInput()` methods.
- Scenes call each control manually from their own `OnUpdate()` methods.
- Modal behavior is coordinated by app-side ordering rather than a framework-level UI event model.

### Why This Hurts Reuse

Every screen has to remember which controls exist, in what order to poll them, and when one control should block another. This works for small showcase scenes, but it does not scale well to real menus, dialogs, HUDs, inspectors, or nested panels.

### Missing Framework Pieces

- Pointer event routing.
- Keyboard/gamepad focus ownership.
- Focus traversal between controls.
- Press/release/click events owned by the control itself.
- Pointer capture for dragging.
- Disabled/read-only states.
- Modal input blocking.
- A consistent way for controls to consume input so parents and siblings do not also react.

### Suggested Work Items

1. Add a UI input dispatch pass for `Control` subtrees.
2. Add focus state to `Control`.
3. Add event hooks such as `Pressed`, `ValueChanged`, `SelectionChanged`, and `FocusChanged`.
4. Convert existing controls away from public `HandleInput()` polling as the main integration path.
5. Keep direct polling only as a low-level escape hatch or testing helper.

## 2. No Layout System

### Evidence

- Test app scenes hard-code almost every `Position`, `Size`, and text coordinate.
- `Control` only provides `Size`, `GlobalBounds`, and hit testing.

### Why This Hurts Reuse

Reusable controls are still difficult to compose because every caller must manually place them. Any new screen size, scaling mode, sidebar width, list length, or localization change becomes manual coordinate work.

### Missing Framework Pieces

- Anchors or margins.
- Layout containers.
- Minimum/preferred size.
- Parent-driven child layout.
- Padding and spacing.
- Basic text wrapping or measured label sizing.
- Responsive handling for window resize.

### Suggested Work Items

1. Add `Control` layout properties such as minimum size, margin, and anchor/preset.
2. Add a simple `VBoxContainer` and `HBoxContainer`.
3. Add a `PanelContainer` or content-wrapper pattern for padding.
4. Add measured text helpers to `TextNode`.
5. Convert one test app scene from hard-coded coordinates to containers as a proof point.

## 3. Input Actions Need Broader Binding Coverage

### Evidence

- `InputManager` now exposes public action registration.
- `SetupHotkeys()` registers the built-in UI navigation actions.
- UI navigation now goes through named actions rather than bypassing them.
- The remaining coverage is still narrow compared to a full reusable input map.

### Why This Hurts Reuse

The framework now has a usable action-registration entry point, but it still is
not broad enough for a fully reusable input map. Consumers can define keyboard
and basic gamepad navigation actions, but they still lack the wider binding and
configuration surface needed for gameplay-scale input setup.

### Missing Framework Pieces

- Public action unregistration or rebinding.
- Mouse button bindings.
- Axis actions or analog values.
- Action groups or contexts.
- Serializable input map configuration.

### Suggested Work Items

1. Add action removal and rebinding APIs with validation.
2. Add mouse bindings and broader analog/axis action support where continuous values matter.
3. Add tests for rebinding and multiple input devices.
4. Consider input contexts for gameplay, UI, debug, and modal states.

## 4. 3D Still Lacks Reusable Renderables

### Evidence

- `Node3D`, `Camera3DNode`, `Socket3D`, `Hardpoint3D`, `Light3D`, and `LightShader3D` are implemented.
- The test app still creates render traversal, cube mesh drawing, and gizmos inside `ThreeDScene`.
- The next missing layer is still `VisualInstance3D` and a first concrete reusable renderable such as `MeshInstance3D`.

### Why This Hurts Reuse

The current 3D stack proves transform propagation and basic scene ownership, but
it still does not let game code build reusable renderable 3D scene objects.
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
4. Add tests for 3D transform inheritance through sockets and renderable children.

## 5. Rendering Backend Boundary Is Raylib-Only By Design

### Evidence

- Framework drawing nodes call `Raylib` directly.
- `CanvasItem`, UI controls, `Sprite`, `AudioPlayer`, `VideoPlayer`, and `RaylibApplication` are coupled to Raylib APIs.

### Decision

Raylib is the current and only backend the framework should support. Backend independence is not a goal right now.

The original project already paid enough complexity cost through DirectX 9 and SharpDX 9. The current framework should treat Raylib as the practical engine layer and build reusable components on top of it, not spend effort hiding it behind broad renderer abstractions.

DX11 should only be considered later if Raylib becomes a concrete blocker. That should be treated as a deliberate future rendering backend project, not as a reason to abstract everything today.

### Why This Still Matters

Raylib coupling is acceptable inside framework implementation code, but the boundary should be documented clearly so we do not keep reopening this as an architectural concern. Reuse means reusable inside the Raylib-based framework.

The remaining useful work here is not engine abstraction. It is keeping Raylib contained behind structured framework concepts so application code does not collapse back into the `Legacy.RaySharp` pattern: scenes and static helpers directly mixing input polling, layout, UI state, media controls, domain behavior, and drawing calls.

### Missing Framework Pieces

- Documentation that states Raylib is the supported backend.
- Clear separation between reusable framework nodes and demo-only Raylib drawing.
- Framework-owned wrappers for concepts that otherwise invite unstructured Raylib usage.
- Optional lightweight draw helpers where repeated Raylib call patterns appear.
- Tests for pure geometry/layout calculations that feed Raylib drawing.

### Suggested Work Items

1. Update `framework.md` to state that Raylib is the only supported backend.
2. Keep public framework APIs structured around framework concepts first; do not expose Raylib types just because it is convenient.
3. Allow Raylib types at narrow backend edges where the framework is explicitly wrapping a Raylib-owned concept, such as window flags, native textures, cameras, shaders, or low-level drawing resources.
4. Avoid broad renderer abstractions unless a specific repeated pain point appears.
5. Prefer testing math, layout, event routing, and resource ownership without trying to fake the whole renderer.
6. Revisit DX11 only if Raylib becomes a specific blocker with known requirements.

## 6. Resource Management Is Local, Not Centralized

### Evidence

- `Resource` provides lazy load and disposal.
- Scenes manually create and dispose resources.
- There is no cache, asset root, preload queue, or shared ownership model.

### Why This Hurts Reuse

Reusable resource types exist, but applications still have to decide when identical paths share loaded assets, who owns them, and how assets are found. That can lead to duplicate loads, inconsistent disposal, and hard-coded asset paths.

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

## 7. Scene and UI Composition Still Lives Mostly in the Test App

### Evidence

- `ShowcaseShellNode` manually owns scene switching, sidebar rendering, pause dialog routing, and root swapping.
- `ShowcaseScene` is a test app abstraction rather than a framework scene concept.

### Why This Hurts Reuse

The framework has `SceneTree`, but does not yet provide reusable scene navigation, stack, modal, overlay, or screen composition patterns. Each app will likely rebuild this shell logic.

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
4. Make menu/sidebar composition use framework layout and input routing once those exist.

## 8. Documentation Boundary Is Slightly Blurry

### Evidence

- `framework.md` describes `RaylibApplication` as part of the framework runtime.
- The actual app runner lives in the separate `Windowing` project.
- The test app references both `Framework` and `Windowing`.

### Why This Hurts Reuse

The current split is reasonable, but the docs should make it clear which features belong to `Framework`, which belong to `Windowing`, and which are only demonstrated by `Framework.TestApp`.

### Missing Documentation Pieces

- Project boundary summary.
- "Framework core" versus "Windowing runner" versus "Test app demos."
- A reusable-component maturity table.
- Explicit "implemented but basic" notes for UI, resources, and 3D.

### Suggested Work Items

1. Update `framework.md` to distinguish `Framework` from `Windowing`.
2. Add a maturity table for major subsystems.
3. Mark demo-only code paths clearly.
4. Keep this pain point document linked from the docs index.

## Recommended Order

1. Public input action registration and UI navigation through actions.
2. UI event dispatch, focus, and input consumption.
3. Basic layout containers.
4. Node-based modal/dialog/menu patterns.
5. `Camera3DNode` and first reusable 3D renderable.
6. `Socket3D` for attachment-driven game objects.
7. Resource cache and asset path service.
8. Raylib backend boundary documentation.

This order should reduce manual wiring first, then make the test app prove real reuse by deleting code from scenes rather than only adding new framework classes.

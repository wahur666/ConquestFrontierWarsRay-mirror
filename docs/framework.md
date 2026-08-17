# Framework Status

This document tracks framework status, scope, and direction.

Node-specific behavior now belongs on the owning framework types under
`src/Framework/src/` as XML documentation. This file should stay at the
framework-summary level rather than duplicating per-node API docs.

## Implemented

### Runtime

- `RaylibApplication` owns the window lifecycle and main loop.
- `WindowOptions` configures window setup.
- `SceneTree` owns:
  - the active root
  - quit flow
  - root switching
  - whole-tree update/draw orchestration
- The main loop:
  - updates input state once per frame
  - updates the active `SceneTree`
  - begins drawing
  - draws the active `SceneTree`
  - ends drawing
- The application exits when:
  - the active `SceneTree` receives a quit request
  - or `Raylib.WindowShouldClose()` returns `true`

### Node System

- The framework node stack is implemented and documented in code:
  - `Node`
  - `CanvasItem`
  - `Node2D`
  - `Control`
  - `PanelNode`
  - `TextNode`
  - `ButtonNode`
  - `DropdownNode`
  - `SliderNode`
  - `ListViewNode`
  - `Sprite`
  - `Node3D`
  - `Camera3DNode`
  - `Socket3D`
  - `Hardpoint3D`
  - `Light3D`
  - `LightShader3D`
  - `AudioPlayer`
  - `VideoPlayer`
- Lifecycle, composition, transform behavior, and node-specific usage notes
  should now be maintained on those classes instead of here.

### Input

- `InputManager` supports named actions with one or more key bindings.
- `UiEscape` is registered by default and maps to `Escape`.
- The input manager tracks per-frame action state.
- The following queries are implemented:
  - `IsActionJustPressed`
  - `IsActionJustReleased`
  - `IsActionPressed`
  - `IsActionReleased`
- The input manager also exposes:
  - primary-gamepad polling through `GamepadState`
  - one-shot UI navigation properties such as `UiUp`, `UiDown`, and `UiAccept`
  - exit gesture detection
- `MenuNavigation` turns per-frame input into reusable menu intents.

### 3D Direction

- `SceneTree` remains the framework owner for:
  - the active root
  - current root switching
  - global update/draw orchestration
  - quit flow
- The framework lighting boundary is split into:
  1. `Light3D` for reusable scene/node light state
  2. `LightShader3D` for reusable raylib shader upload and lighting evaluation
- The current lighting split is:
  1. ambient contribution through the `Light3D.Ambient` flag and shader ambient upload
  2. directional, point, and spot contribution through the compiled dynamic light array
- The next most useful Godot-like 3D additions are:
  1. `VisualInstance3D` with a first concrete `MeshInstance3D`
  2. broader render-facing 3D nodes on top of the current transform, camera, socket, hardpoint, and light stack
- This order is preferred because the project still needs more reusable 3D
  composition nodes before deeper specialized systems.

### Hardpoint Direction

- The framework now implements sockets and hardpoints as first-class 3D nodes.
- The split remains:
  1. `Socket3D` for named attachment ownership and local/world transforms
  2. `Hardpoint3D` for optional native-style constraint metadata
- The next target should be:
  1. populate `Socket3D` / `Hardpoint3D` directly from mesh hardpoint definitions during asset-instance spawn
  2. expose broader socket lookup from mesh/part instance owners, not only from socket subtrees
  3. move hardpoint debug drawing into a reusable renderer instead of scene-local showcase code
  4. add full native parity only when physics/collision integration actually needs runtime joint creation

### Debugging

- `DebugTreeView` prints any node subtree.
- It includes hierarchy plus 2D transform, visibility, and draw-order state for debugging.

### Resources

- `Resource` is the shared framework base type for disposable loadable assets.
- The texture stack includes:
  - `Texture2D`
  - `CompressedTexture2D`
  - `AtlasDefinitionResource`
  - `AtlasTexture`
- The audio stack includes:
  - `AudioStreamResource`
  - `MusicAudioResource`
- `RaylibApplication` initializes and closes the raylib audio device alongside the main app lifetime.

### UI Primitives

- `MenuList` provides vertical menu selection logic.
- `PauseDialog` provides a small modal dialog pattern on top of `MenuList`.

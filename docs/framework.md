# Framework Status

This document tracks what is currently implemented in the node framework.

## Implemented

### Runtime

- `RaylibApplication` owns the window lifecycle and main loop.
- Window setup is configured through `WindowOptions`.
- The main loop:
  - updates input state once per frame
  - updates the node tree
  - begins drawing
  - draws the node tree
  - ends drawing
- The application exits when:
  - a node requests quit through `RequestQuit()`
  - or `Raylib.WindowShouldClose()` returns `true`

### Node System

- `Node` is the base type for the framework.
- Nodes support lifecycle hooks:
  - `OnInitialize()`
  - `OnEnterTree()`
  - `OnExitTree()`
  - `OnUpdate(float deltaTime)`
  - `OnDraw()`
  - `OnDispose()`
- Nodes support parent/child composition.
- Child nodes automatically inherit:
  - runtime context
  - initialization
  - tree entry
- Recursive update and draw traversal are built into `Node`.
- Nodes receive a `NodeContext`.
- Nodes can request application shutdown through `RequestQuit()`.

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

### 2D Node Stack

- `CanvasItem` is the drawing-oriented base node between `Node` and `Node2D`.
- `Transform2D` owns 2D transform composition and matrix-backed decomposition.
- `Node2D` adds transform properties:
  - `Position`
  - `Rotation`
  - `Scale`
  - `Visible`
  - `ZIndex`
  - `ZAsRelative`
  - `YSortEnabled`
- `Node2D` computes:
  - `LocalTransform2D`
  - `GlobalTransform2D`
  - `LocalTransform`
  - `GlobalTransform`
  - `GlobalPosition`
  - `GlobalRotation`
  - `GlobalScale`
  - `GlobalZIndex`
- Parent-child 2D transform composition is implemented through `Transform2D`.
- Parent-child draw ordering is implemented through `GlobalZIndex` and optional Y sorting.

### Debugging

- `DebugTreeView` prints any node subtree.
- It includes hierarchy plus 2D transform, visibility, and draw-order state for debugging.

### Drawing Helpers

- `CanvasItem` currently exposes:
  - `DrawCircle`
  - `DrawLine`
  - `DrawPolyline`
- Draw helpers operate in local space and are transformed through the node canvas transform.
- `DrawCircle` applies the current transform scale to the radius.

### Texture Resources

- `Resource` is the shared framework base type for disposable loadable assets.
- `Texture2D` inherits from `Resource` as the framework texture resource base type.
- `CompressedTexture2D` represents a file-backed texture resource.
- `AtlasDefinitionResource` represents atlas frame metadata loaded from JSON.
- `AtlasTexture` represents a sub-rectangle of another framework `Texture2D`.
- `Sprite` depends on the framework texture abstraction rather than file paths or raw Raylib resources.

### UI Primitives

- `Control` adds size and hit-testing to `Node2D`.
- `PanelNode`, `TextNode`, and `ButtonNode` provide basic lightweight UI drawing.
- `MenuList` provides vertical menu selection logic.
- `PauseDialog` provides a small modal dialog pattern on top of `MenuList`.

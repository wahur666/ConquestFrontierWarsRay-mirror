# Framework Status

This document tracks what is currently implemented in the node framework.

## Implemented

### Runtime

- `RaylibApplication` owns the window lifecycle and main loop.
- Window setup is configured through `WindowOptions`.
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

- `Node` is the base type for the framework.
- The currently available concrete or subclassable node types are:
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
  - `AudioPlayer`
  - `VideoPlayer`
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
- `NodeContext` exposes the owning `SceneTree`.
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
- The currently available 2D node types are:
  - `Node2D`
  - `Control`
  - `PanelNode`
  - `TextNode`
  - `ButtonNode`
  - `DropdownNode`
  - `SliderNode`
  - `ListViewNode`
  - `Sprite`
  - `VideoPlayer`

### 3D Node Stack

- `Transform3D` owns 3D transform composition and matrix-backed decomposition.
- `Node3D` adds transform properties:
  - `Position`
  - `Rotation`
  - `Scale`
- `Node3D` computes:
  - `LocalTransform3D`
  - `GlobalTransform3D`
  - `LocalTransform`
  - `GlobalTransform`
  - `GlobalPosition`
  - `GlobalRotation`
  - `GlobalScale`
- Parent-child 3D transform composition is implemented through `Transform3D`.
- The current 3D stack is still transform-only and does not yet provide a
  full scene-graph owner or reusable renderable 3D node types.
- The currently available 3D node types are:
  - `Node3D`
  - `Camera3DNode`
  - `Light3D`
  - `Socket3D`
  - `Hardpoint3D`
- `Camera3DNode` mirrors raylib's `Camera3D` shape:
  - `Position`
  - `Target`
  - `Up`
  - `FovY`
  - `Projection`
- `Camera3DNode` also provides:
  - `LookAt`
  - `FrameSphere`
  - `BeginMode`
  - `EndMode`
  - `TryPointToScreen`
  - `ScreenToPoint`
  - `ObjectVisibility` for bounding spheres
  - `ObjectVisibility` for axis-aligned bounding boxes
- `Camera3DNode` uses raylib helpers where available for:
  - camera matrix generation
  - world-to-screen projection
  - screen-to-world ray reconstruction
- Coarse frustum culling is now implemented through explicit frustum-plane
  tests for spheres and AABBs.
- `Socket3D` is now the reusable named attachment node for:
  - mesh-part sockets
  - weapon mounts
  - dock points
  - particle anchors
- `Light3D` is now the reusable 3D lighting node for:
  - ambient-tagged scene baseline lights
  - directional lights
  - point lights
  - spot lights
  - transform-derived world-space position and direction
  - raylib-style position/target snapshots for shader upload
- `LightShader3D` is now the reusable framework shader resource for:
  - ambient baseline upload through raylib shader uniforms
  - forward-lit 3D shading from `Light3DState` snapshots
  - directional, point, and spot light evaluation
  - configurable compiled light count through `maxLights`
  - clamping `maxLights` to the current framework cap of `8`
  - a stable `Begin(...)` / draw / `End()` call pattern for renderer code
- `LightShader3D` should be used in this order:
  1. create one shader resource for the renderer, scene, or view that owns the lit pass
  2. choose `maxLights` up front; it changes the compiled shader shape, so it is not a per-frame toggle
  3. each frame, gather `Light3DState` values from the active `Light3D` nodes with `ToLightState()`
  4. start the camera 3D pass
  5. call `LightShader3D.Begin(cameraPosition, lights)`
  6. draw the geometry that should receive this lighting
  7. call `LightShader3D.End()`
  8. dispose the shader resource when the owning scene or renderer is torn down
- `LightShader3D` currently expects the caller to provide:
  - the camera/view position for shader upload
  - the ordered list of active lights for the pass
  - geometry that already provides positions, normals, and vertex colors
- Ambient-tagged `Light3D` nodes are accumulated into the shader ambient baseline.
- Ambient-tagged `Light3D` nodes do not consume one of the compiled directional/point/spot light slots.
- `LightShader3D` currently does not:
  - discover `Light3D` nodes automatically
  - select the best subset of lights for a mesh or region
  - bind textures or materials by itself
  - manage multiple passes or mixed lighting models
- The practical rule is:
  - use `Light3D` for reusable world-space light data
  - use the `Light3D.Ambient` flag when a light should contribute only to the scene ambient baseline
  - use `LightShader3D` for the actual shader-backed draw pass
  - keep light collection and pass ownership in the scene or renderer layer above them
- `Hardpoint3D` now extends `Socket3D` with native-style constraint metadata:
  - `JointType`
  - `Axis`
  - `Min0`
  - `Max0`
  - `SpringConstant`
  - `DampingConstant`
  - `RestLength`
- `Hardpoint3D` now exports immutable `HardpointInfo3D` values.
- `Hardpoint3D` now derives native-style parent/child attachment descriptors through `BuildConnectionTo(...)`.

### 3D Direction

- `SceneTree` now provides the framework owner for:
  - the active root
  - current root switching
  - global update/draw orchestration
  - quit flow
- The next most useful Godot-like 3D additions are:
  1. `VisualInstance3D` with a first concrete `MeshInstance3D`
  2. broader render-facing 3D nodes on top of the current transform, camera, socket, hardpoint, and light stack
- The framework lighting boundary is now split into:
  1. `Light3D` for reusable scene/node light state
  2. `LightShader3D` for reusable raylib shader upload and lighting evaluation
- The current lighting split is:
  1. ambient contribution through the `Light3D.Ambient` flag and the shader `ambientColor` upload
  2. directional, point, and spot contribution through the compiled dynamic light array
- This keeps lighting data separate from scene-local demo code and makes the
  first renderer-facing lighting component reusable across test scenes and
  future mesh nodes.
- This order is preferred because the project needs more reusable 3D
  composition nodes before adding deeper specialized systems.
- `Socket3D` is especially important for Conquest-style compound ships,
  weapon mounts, particle anchors, and other attachment-driven content.
- `Socket3D` should be harvested from the existing `Legacy.RaySharp`
  hardpoint behavior instead of invented from scratch.

### Hardpoint Direction

- The framework now implements hardpoints and sockets as first-class 3D nodes.
- `Legacy.RaySharp` already defines the minimum durable hardpoint asset shape:
  - `Name`
  - `GroupName`
  - `ParentName`
  - `Position`
  - `Orientation`
- The current XML loader reads hardpoints from:
  - `Hardpoints/<group>/<hardpoint>/Position`
  - `Hardpoints/<group>/<hardpoint>/Orientation`
- Hardpoint `Position` is scaled through the same `0.01f` world-unit conversion used by mesh positions.
- Missing hardpoint orientation currently defaults to identity.
- Compound mesh loading already preserves attachment ownership by rewriting each part hardpoint with that part's `ParentName`.
- In `Legacy.RaySharp`, hardpoint world position is resolved as:
  - hardpoint local position
  - then owning part world transform when `ParentName` is set
  - then model/world transform
- This means the framework should not store hardpoints only as flat world markers.
- The correct framework model is:
  - immutable hardpoint definitions on the loaded mesh asset
  - named socket ownership on the relevant part node
  - runtime world transforms derived from the node hierarchy
- `Legacy.RaySharp` also shows that particle emitters and hardpoints should follow the same part-local attachment rules.
- The debug/viewer behavior already worth preserving is:
  - bounds-scaled hardpoint marker radius
  - 3D markers drawn at resolved world positions
  - screen-space labels
  - visibility toggling
- That debug behavior belongs in a dedicated `HardpointDebugRenderer`, not in core scene classes.
- The native `IHardpoint` contract contains more than just debug-anchor data.
- The original hardpoint info model also carries:
  - joint type
  - point
  - orientation
  - axis
  - min/max constraint values
  - spring constant
  - damping constant
  - rest length
- The native connection behavior uses the child's hardpoint type to decide how a parent and child instance connect.
- The original implemented connection semantics that still matter are:
  - `JT_FIXED`: align parent and child hardpoint positions and orientations
  - `JT_REVOLUTE`: attach with parent point, child point, relative orientation, axis, and min/max
  - `JT_PRISMATIC`: same data shape as revolute, but translation-constrained instead of rotation-constrained
- The current `Legacy.RaySharp` loader/viewer only uses the spatial socket subset:
  - name
  - group
  - parent part
  - position
  - orientation
- So the framework should split the concern into two layers:
  1. `Socket3D`
     - name
     - group/tag
     - local transform
     - owning node/part
  2. optional hardpoint constraint metadata
     - joint type
     - axis
     - limits
     - spring/damping/rest data
- This split matches both the current managed implementation and the original native contract.
- The framework now has the first isolated hardpoint layer:
  1. named `Socket3D` child nodes for attachment ownership
  2. `Hardpoint3D` for native-style joint metadata
  3. immutable `HardpointInfo3D` export
  4. native-style connection descriptor generation for fixed, revolute, and prismatic child hardpoints
- The next framework target should therefore be:
  1. populate `Socket3D` / `Hardpoint3D` directly from mesh hardpoint definitions during asset-instance spawn
  2. expose broader socket lookup from mesh/part instance owners, not only from socket subtrees
  3. move hardpoint debug drawing into a reusable renderer instead of scene-local showcase code
  4. add full native parity only when physics/collision integration actually needs runtime joint creation

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

### Audio

- `AudioStreamResource` is the shared framework base type for streamed audio playback resources.
- `MusicAudioResource` represents a file-backed raylib music stream resource.
- `AudioPlayer` is a non-visual node that owns:
  - play
  - pause
  - stop
  - seek
  - volume
  - looping
  - pitch
  - pan
  - assigning and disposing audio resources
- `RaylibApplication` now initializes and closes the raylib audio device alongside the main app lifetime.

### Video

- `VideoPlayer` is a screen-space `Control` node for video playback.
- `VideoPlayer` owns:
  - loading a file-backed source
  - play
  - pause
  - stop/reopen from frame zero
  - frame decoding
  - audio playback for embedded audio tracks when available
  - aspect-fit presentation inside control bounds
- Media Foundation startup and shutdown are now managed inside the framework so video playback does not depend on `Legacy.RaySharp`.

### UI Primitives

- `Control` adds size and hit-testing to `Node2D`.
- `PanelNode` draws a filled rectangle with an optional outline.
- `TextNode` draws a text label in the 2D node tree.
- `ButtonNode` provides a clickable button with hover feedback.
- `DropdownNode` provides a mouse-driven single-select dropdown list.
- `SliderNode` provides a mouse-driven horizontal slider with normalized values.
- `ListViewNode` provides a mouse-driven single-selection list of clickable rows.
- `MenuList` provides vertical menu selection logic.
- `PauseDialog` provides a small modal dialog pattern on top of `MenuList`.

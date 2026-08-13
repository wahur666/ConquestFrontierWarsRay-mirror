# RaySharp Foundation Harvest Plan

This note defines the project priority and harvest order for the Conquest
rewrite foundation.

The priority is:

1. `RaySharp` is the primary host and final destination.
2. `testapp-ray-cs` is the primary donor project for reusable framework code.
3. `EventHorizon` is the secondary donor project and reference project.
4. `ConquestSharp` is a selective logic donor only for missing runtime
   subsystems and the mandatory shared data model.

That order matters. `RaySharp` is where the Conquest rewrite actually lives.
The other projects only matter insofar as they improve `RaySharp`.

This document builds on:

- [RaySharp scene graph refactor checklist](/D:/git2/Conquest-Frontier-Wars-Source2/docs/RaySharp-scene-graph-refactor-checklist.md)
- [ConquestSharp reimplementation tracker](/D:/git2/Conquest-Frontier-Wars-Source2/docs/ConquestSharp-reimplementation-tracker.md)
- [Conquest core packages overview](/D:/git2/Conquest-Frontier-Wars-Source2/docs/Conquest-core-packages-overview.md)

## Project roles

## `RaySharp`

`RaySharp` is the main rewrite project.

It already contains the Conquest-specific work that matters:

- mesh loading and viewer runtime
- compound hierarchy handling
- hardpoints
- particle preview/runtime
- animation playback
- video decode integration
- asset and tool-side migration work

Its current weakness is not domain coverage. Its weakness is structure:

- too much scene-local orchestration
- too many static `Load(...)` entry points
- runtime state mixed with immutable asset data
- too much direct drawing inside scene classes

That means `RaySharp` should not be replaced. It should be refactored and
strengthened.

## `testapp-ray-cs`

`testapp-ray-cs` is the primary donor project.

It is the closest existing C# raylib framework in the repo to what `RaySharp`
needs as a foundation.

It provides:

- app shell ownership
- node lifecycle
- runtime context propagation
- transform composition
- action-based input
- resource abstraction
- texture and atlas abstraction
- sprite/resource ownership patterns
- debug tree inspection
- tests around the framework pieces

Useful files:

- [testapp-ray-cs/src/App/RaylibApplication.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/App/RaylibApplication.cs)
- [testapp-ray-cs/src/App/WindowOptions.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/App/WindowOptions.cs)
- [testapp-ray-cs/src/Framework/Node.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/Node.cs)
- [testapp-ray-cs/src/Framework/NodeContext.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/NodeContext.cs)
- [testapp-ray-cs/src/Framework/Node2D.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/Node2D.cs)
- [testapp-ray-cs/src/Framework/Transform2D.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/Transform2D.cs)
- [testapp-ray-cs/src/Framework/CanvasItem.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/CanvasItem.cs)
- [testapp-ray-cs/src/Framework/InputManager.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/InputManager.cs)
- [testapp-ray-cs/src/Framework/Resource.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/Resource.cs)
- [testapp-ray-cs/src/Framework/Texture2D.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/Texture2D.cs)
- [testapp-ray-cs/src/Framework/CompressedTexture2D.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/CompressedTexture2D.cs)
- [testapp-ray-cs/src/Framework/AtlasDefinitionResource.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/AtlasDefinitionResource.cs)
- [testapp-ray-cs/src/Framework/AtlasTexture.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/AtlasTexture.cs)
- [testapp-ray-cs/src/Framework/Sprite.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/Sprite.cs)
- [testapp-ray-cs/src/Framework/DebugTreeView.cs](/D:/git2/Conquest-Frontier-Wars-Source2/testapp-ray-cs/src/Framework/DebugTreeView.cs)

`testapp-ray-cs` should be treated as the base reference for framework
extraction unless `RaySharp` already has a better equivalent.

## `EventHorizon`

`EventHorizon` is the secondary donor project.

It is still useful, but mostly as a reference for:

- scene lifecycle shape
- simple scene-controller design
- small-game shell composition
- UI/control composition patterns

It is not the main framework donor anymore because most of its reusable
scene-graph work is either duplicated in weaker form or superseded by
`testapp-ray-cs`.

Useful files:

- [EventHorizon/Core/Game.cs](/D:/git2/Conquest-Frontier-Wars-Source2/EventHorizon/EventHorizon/Core/Game.cs)
- [EventHorizon/Core/IScene.cs](/D:/git2/Conquest-Frontier-Wars-Source2/EventHorizon/EventHorizon/Core/IScene.cs)
- [EventHorizon/SceneGraph/Node.cs](/D:/git2/Conquest-Frontier-Wars-Source2/EventHorizon/EventHorizon/SceneGraph/Node.cs)
- [EventHorizon/SceneGraph/Node2D.cs](/D:/git2/Conquest-Frontier-Wars-Source2/EventHorizon/EventHorizon/SceneGraph/Node2D.cs)
- [EventHorizon/SceneGraph/Control.cs](/D:/git2/Conquest-Frontier-Wars-Source2/EventHorizon/EventHorizon/SceneGraph/Control.cs)
- [EventHorizon/SceneGraph/ButtonNode.cs](/D:/git2/Conquest-Frontier-Wars-Source2/EventHorizon/EventHorizon/SceneGraph/ButtonNode.cs)

`EventHorizon` should only win over `testapp-ray-cs` when it offers a cleaner
scene-level idea that `testapp-ray-cs` does not cover.

## `ConquestSharp`

`ConquestSharp` is not a framework donor. It is a selective subsystem donor and
the mandatory donor for the shared Conquest data model.

It should only be used when `RaySharp` is still missing a real Conquest-facing
runtime capability or the shared typed data model that the rewrite needs.

The rule for `ConquestSharp` is:

- take the shared data model from `Common`
- take domain/runtime logic
- do not take DACOM architecture
- do not take Direct3D-era render architecture
- do not take plugin/container orchestration
- do not take COM-style interface patterns

The earlier project-by-project assessment still holds in broad terms for the
current direction:

- `DACOM` glue: discard
- `Math3D`: replace with `System.Numerics`
- `System`: discard in favor of a raylib-native app shell
- `DOSFile`: only salvage data-access pieces that are still genuinely needed
- `Anim` + `Channel`: already reimplemented in `RaySharp`
- `Physics`: genuinely useful
- `Collision`: genuinely useful and still missing on the `RaySharp` side
- `EngineCameras`: raylib-side camera model is enough
- `EngineLights`: raylib-side light handling is enough for the current rewrite
- `Hardpoint`: already implemented in `RaySharp`
- `RenderPipeline`: discard
- `RenderBatcher`: defer unless profiling proves a need
- `Engine` component orchestration: discard
- `MeshManager`: likely replaceable; do not adopt blindly
- `Streamer`: raylib/native audio path is enough
- `VertexBufferManager`: discard as architecture
- `VideoSystem`: already covered by the newer direction
- `RPUL`: discard
- `LightManager`: only tiny orchestration ideas if needed
- `TextureLibrary`: already rewritten as part of the newer path
- `PolyMesh`: rewritten in `RaySharp`
- `Optics`: rewritten in `RaySharp`
- `MaterialManager`: rewritten in `RaySharp`
- `TextureManager`: likely small helper ideas only
- `Docuview`: do not treat as foundation
- `DAHOTKEY`: still a real missing subsystem
- `ParticleEffect`: data-side ideas only if something is still missing
- `ParticleEditor`: already migrated in practice through newer work

One major correction must be explicit:

- `Common` is not optional
- `Common` is the absolute base of the data model
- `Common` should be treated as a required foundation import, not as a late
  selective salvage task

Useful files:

- `ConquestSharp/Common/*`
- [ConquestSharp/Collision/README.md](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestSharp/Collision/README.md)
- [ConquestSharp/Physics/README.md](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestSharp/Physics/README.md)
- [ConquestSharp/DOSFile/README.md](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestSharp/DOSFile/README.md)
- [ConquestSharp reimplementation tracker](/D:/git2/Conquest-Frontier-Wars-Source2/docs/ConquestSharp-reimplementation-tracker.md)

## Practical donor policy

When deciding what to adopt into `RaySharp`, use this rule:

1. Keep existing `RaySharp` code if it already solves the Conquest-specific
   problem cleanly.
2. Otherwise, prefer `testapp-ray-cs` for framework-level code and patterns.
3. Use `EventHorizon` only when it has a useful scene/UI idea not already
   covered better by `testapp-ray-cs`.
4. Use `ConquestSharp` only for missing subsystem logic, never as a competing
   architectural base, except for the mandatory shared data model in `Common`.

This avoids merging four competing foundations into one mess.

## What `testapp-ray-cs` should donate

These parts are primary harvest candidates.

### App shell

- one owner of window startup
- one owner of main-loop execution
- one owner of frame timing
- one owner of quit signaling

This maps directly onto `RaySharp.App`.

### Node lifecycle

- initialize
- enter tree
- exit tree
- update
- draw
- dispose

This is stronger than the current loose `RenderableObject` pattern in
`RaySharp`.

### Runtime context propagation

- shared input
- shared quit/request services
- later extendable to shared asset caches, diagnostics, and app services

### Input abstraction

- named actions
- per-frame action state
- just-pressed / just-released / held queries
- event-style input notifications

### Resource abstraction

- lazy-loaded disposable resources
- texture wrappers
- atlas definition resources
- atlas texture slices
- resource-backed sprite patterns

### Debug inspection

- tree printer
- transform and visibility inspection
- draw-order debugging patterns

### Tests

This is one of the biggest advantages over `EventHorizon`.

The framework pieces in `testapp-ray-cs` are already exercised by tests, which
makes them far safer as a donor base.

## What `EventHorizon` should donate

These parts are secondary harvest candidates.

### Scene contract shape

The `IScene` lifecycle is very readable:

- `Load`
- `HandleInput`
- `Update`
- `Draw`
- `Unload`

This is still useful as a shape for `RaySharp` scenes.

### Scene controller ideas

- explicit scene switching
- small-game shell clarity
- less framework noise than reflection-driven scene discovery

### UI composition examples

- control nodes
- simple buttons and panels
- practical examples of node-based UI usage in an actual small game

## What `ConquestSharp` should donate

These are the required and selective harvest candidates.

### Common

This is mandatory.

`ConquestSharp/Common` is the base data-model layer and should be treated as a
foundation dependency for the rewrite, not as an optional migration aid.

What is worth taking:

- typed data models
- shared enums
- shared structs
- UTF DB typed projections
- parsers and helpers that define the real shape of Conquest data
- anything in `Common` that establishes canonical field names, value layouts,
  and schema expectations

What is not worth taking:

- any surrounding architectural assumptions that drag DACOM-style runtime
  composition into the new foundation
- any unnecessary porting scaffolding whose only purpose was managed parity
  auditing

Target use inside `RaySharp`:

- `RaySharp.Data`
- `RaySharp.Data.Models`
- `RaySharp.Data.UtfDb`
- shared canonical types used by runtime, assets, tools, and future game
  modules

Practical rule:

- `Common` should be imported early
- `Common` should become the canonical typed model layer
- new `RaySharp` asset/runtime code should build on that typed model layer
  instead of inventing competing representations

### Priority inside `ConquestSharp`

The internal priority for `ConquestSharp` harvest should now be:

1. `Common`
2. `Collision`
3. `Physics`
4. `DOSFile` only as minimal importer/backend support if still needed
5. `DAHOTKEY` later when gameplay-side input/event work needs it

### Collision

This is the clearest missing subsystem on the `RaySharp` side.

What is worth taking:

- extent-tree loading rules
- collision extent data model
- hierarchy ray-query behavior
- narrow-phase logic that is already implemented
- mesh/triangle query behavior
- bounding helpers and collision math

What is not worth taking:

- DACOM registration
- engine-component plumbing
- profile-driven loading architecture

Target use inside `RaySharp`:

- `RaySharp.Runtime.Collision`
- `CollisionAsset`
- `CollisionInstance`
- raycast / overlap / contact services exposed through the new runtime model

### Physics

This is still one of the few genuinely good reusable parts.

What is worth taking:

- solver interfaces
- Euler / RK4 / trapezoidal solver behavior
- fixed-step update rules
- force / impulse / momentum state handling
- `min_dt` stepping logic

What is not worth taking:

- DACOM-facing `IPhysics` factory shape
- engine-component loading contracts

Target use inside `RaySharp`:

- `RaySharp.Runtime.Physics`
- explicit simulation services
- optionally physics components or simulation nodes

### DOSFile

This should be treated carefully and minimally.

The stated goal is not to rebuild the old UTF-centered runtime as the
foundation. The current rewrite direction is XML-first where possible.

What is worth taking only if still needed:

- UTF parser/serializer logic
- nested container navigation rules
- memory-backed filesystem ideas
- search-path ideas if some remaining data import still depends on them

What should not be taken as architecture:

- `IFileSystem` / DACOM creation flow
- DOS/UTF/MEM subsystem role as a runtime foundation
- generalized old-engine filesystem orchestration

Practical rule:

- use `DOSFile` as an importer/backend helper only
- do not let it become the central runtime file model for `RaySharp`

### DAHOTKEY

This is still a real missing subsystem according to the tracker and earlier
assessment.

What is worth taking:

- runtime hotkey/event concepts
- action/event wiring expectations from the original game

What is not worth taking:

- old DACOM module architecture
- recorder/editor assumptions as the main design

Target use inside `RaySharp`:

- `RaySharp.Runtime.Input`
- named actions
- hotkey tables
- game-side event dispatch

### Smaller selective reference value

These may still provide tiny useful ideas, but they should not drive the
architecture:

- `LightManager`
- `TextureManager`
- `ParticleEffect`
- `MeshManager`

Only harvest from them when a concrete missing feature appears.

## What is duplicated or superseded

If `testapp-ray-cs` is the main donor, then most of `EventHorizon`'s
framework-level scene graph should be treated as superseded.

Superseded or near-superseded:

- `EventHorizon` `Node`
- `EventHorizon` `Node2D`
- `EventHorizon` `Sprite`
- most of `EventHorizon`'s framework-level tree and drawing mechanics

Why:

- `testapp-ray-cs` has richer lifecycle handling
- `testapp-ray-cs` has context propagation
- `testapp-ray-cs` has stronger disposal behavior
- `testapp-ray-cs` has resource abstractions
- `testapp-ray-cs` has tests

So `EventHorizon` should not be merged as a competing framework base.

Likewise, `ConquestSharp` should not be merged as a competing framework base.
It is even more important here because its old managed architecture still
carries the DACOM/component-porting mindset that the current direction is
trying to leave behind.

## Recommended architecture target inside `RaySharp`

The resulting structure should be:

```text
RaySharp/
  App/
    app shell
    window bootstrap
    scene switching

  SceneGraph/
    Node
    Node3D
    UiNode
    ControlNode
    attachment semantics

  Assets/
    Mesh assets
    Material assets
    Texture assets
    Particle assets
    Animation assets
    shared resource wrappers

  Runtime/
    Animation playback
    Particle simulation
    Collision
    Physics
    Input abstraction
    Hotkeys
    Audio/video services

  Rendering/
    Render queue
    Mesh renderer
    Particle renderer
    UI renderer
    Debug renderer

  Tools/
    Mesh viewer
    Particle editor
    diagnostics
```

## Immediate harvest plan

1. Keep `RaySharp` as the host.
   - do not fork into a fourth prototype shell

2. Introduce `RaySharp.App`.
   - use `testapp-ray-cs` `RaylibApplication` as the main donor
   - use `EventHorizon.Game` only as a scene-controller reference

3. Replace reflection-driven scene discovery with an explicit scene API.
   - shape inspired by `EventHorizon`
   - implementation discipline inspired by `testapp-ray-cs`

4. Replace `RenderableObject` as the main structural base.
   - introduce `Node`
   - introduce `Node3D`
   - later add `UiNode` / `ControlNode`

5. Split current runtime blobs.
   - `MeshViewerModel` -> `MeshAsset` + `MeshInstance` + `MeshRenderer`
   - particle runtime -> asset + instance + renderer

6. Introduce resource wrappers.
   - adopt the `testapp-ray-cs` resource model
   - add texture and atlas ownership patterns to `RaySharp`

7. Introduce a real input layer.
   - named actions
   - controller/keyboard/mouse support
   - keep touch out unless it becomes necessary again

8. Harvest missing runtime subsystem logic from `ConquestSharp`.
   - `Common` first
   - collision second
   - physics third
   - minimal UTF/DOSFile import helpers only if still required
   - hotkey/event work later when game-side needs demand it

9. Introduce diagnostics helpers.
   - tree inspection
   - debug dumps
   - capture on demand

10. Move tool-side UI to reusable nodes and controls.
   - use `EventHorizon` as reference for simple composition
   - keep `testapp-ray-cs` as framework base

11. Build the Conquest foundation modules on top.
    - `Globals`
    - `Trim`
    - `Mission`
    - keep `ZBatcher` deferred until profiling proves a need

## `ZBatcher` position

Nothing in `testapp-ray-cs` or `EventHorizon` changes the current `ZBatcher`
assessment.

Current recommendation remains:

- do not implement `ZBatcher` first
- first build:
  - node tree
  - asset/instance split
  - render queue
  - clean runtime boundaries
- then profile real Conquest-facing workloads

`ZBatcher` stays:

- deferred
- profiling-driven
- not part of the first clean foundation pass

## Final summary

The correct mental model is:

- `RaySharp` is the rewrite.
- `testapp-ray-cs` is the primary framework donor.
- `EventHorizon` is the secondary scene/UI reference project.
- `ConquestSharp` is the selective donor for missing subsystem logic only.
  with one exception: `Common` is mandatory as the base data model.

So the consolidation strategy is:

- keep all Conquest-specific runtime and asset work in `RaySharp`
- harvest framework rigor, resource patterns, and lifecycle ideas from
  `testapp-ray-cs`
- harvest smaller scene/UI ideas from `EventHorizon`
- harvest only the still-useful missing subsystem logic from `ConquestSharp`
- harvest the `Common` data-model layer from `ConquestSharp` as a required base
- avoid carrying two competing scene-graph frameworks forward
- avoid carrying the old DACOM/component architecture forward

That is the cleanest path to a usable foundation for the Conquest rewrite.

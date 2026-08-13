# RaySharp Scene Graph Refactor Checklist

This note captures the current recommendation for restructuring `RaySharp`
into reusable, scene-creatable objects instead of scene-local loaders and
renderers.

It is written for the current migration goal:

- keep the useful runtime and asset behavior
- remove old COM-style / DACOM-style architectural baggage where it no longer
  helps
- build a cleaner foundation for the four core Conquest modules
- defer `ZBatcher` until there is profiling evidence that raylib alone is not
  enough

Related context:

- [ConquestSharp reimplementation tracker](/D:/git2/Conquest-Frontier-Wars-Source2/docs/ConquestSharp-reimplementation-tracker.md)
- [Conquest core packages overview](/D:/git2/Conquest-Frontier-Wars-Source2/docs/Conquest-core-packages-overview.md)

## Current concern

The current `RaySharp` codebase already has some useful structural pieces:

- `Transform`
- `RenderableObject`
- scene classes
- mesh and particle runtime code

But object creation is still too direct and too scene-local. In practice:

- assets are often loaded from static `Load(...)` entry points
- runtime state and asset data are mixed together
- scene classes do too much orchestration and drawing directly
- mesh, particle, animation, and editor behavior are not yet split cleanly

This is the main risk to future extensibility.

## Goal state

The target structure is:

```text
Assets -> immutable loaded data
Scene graph -> nodes and attachments
Runtime -> animation / particles / physics / collision
Rendering -> queues and renderers
Tools -> editors / viewers / diagnostics
```

The practical rule is:

- asset data should be immutable
- instance state should be mutable
- scenes should assemble and control objects
- renderers should draw objects
- loaders should not live inside runtime object classes

## Refactor checklist

1. Define one object model and use it everywhere.
   - `SceneNode`
   - `SceneComponent`
   - `Scene`
   - `AssetHandle<T>`
   - `Archetype`
   - `Instance`

2. Split mesh runtime types into asset, instance, and renderer layers.
   - `MeshAsset`
   - `MeshInstance`
   - `MeshRenderer`

3. Split particle runtime types the same way.
   - `ParticleEffectAsset`
   - `ParticleSystemInstance`
   - `ParticleRenderer`

4. Stop loading from constructors and static `Load(...)` methods on runtime
   objects.
   - replace scene-local object creation with factories
   - use loaders for immutable data and factories for instances

5. Introduce a real scene graph.
   - parent/child node relationships
   - local/world transforms
   - named attachment nodes
   - child components for mesh, particle, camera, light, and debug behavior

6. Move part hierarchy logic out of viewer-specific classes.
   - compound parts become child nodes
   - hardpoints become named sockets or child nodes
   - manual part rotation becomes a node transform override, not a dictionary
     mutation inside the mesh object

7. Separate update systems from render systems.
   - animation update system
   - particle simulation system
   - mesh renderer
   - particle renderer
   - debug renderer

8. Separate immutable asset data from mutable runtime state.
   - immutable:
     - geometry
     - texture references
     - materials
     - animation clips
     - particle definitions
     - hardpoint definitions
   - mutable:
     - transform
     - current animation state
     - part overrides
     - live particles
     - visibility
     - bounds cache

9. Build one object spawn API.
   - `SpawnMesh(...)`
   - `SpawnCompound(...)`
   - `SpawnParticleEffect(...)`
   - `SpawnCamera(...)`
   - `SpawnLight(...)`

   Every spawn call should return a `SceneNode` or a typed node wrapper.

10. Move editor and viewer behavior out of runtime object classes.
    - sample browser
    - transform panel
    - diagnostics capture
    - UV mode toggles
    - per-part debug controls

11. Add a render queue abstraction before adding more systems.
    - opaque
    - alpha
    - emissive
    - billboards
    - debug lines

12. Create package boundaries in `RaySharp`.
    - `Assets`
    - `SceneGraph`
    - `Runtime`
    - `Rendering`
    - `Tools`

## First concrete cuts

These are the best first refactors because they reduce risk without requiring
the whole engine to be redesigned first.

### 1. Split `MeshViewerModel`

Break the current mesh-viewer object into:

- `MeshAsset`
  - immutable loaded mesh data
  - textures
  - hardpoints
  - animation clips
  - particle emitter definitions
- `MeshInstance`
  - transform
  - current animation state
  - part overrides
  - runtime bounds
- `MeshRenderer`
  - draw submission and material pass logic

### 2. Split `MeshViewerScene`

Extract:

- sample loading into a controller
- direct mesh drawing into `MeshRenderer`
- particle drawing into `ParticleRenderer`
- hardpoint drawing into `HardpointDebugRenderer`
- transform/editor panel logic into a tool-side controller

### 3. Replace static loads with loaders and factories

Replace this pattern:

```csharp
MeshViewerModel.Load(path, uvMode);
```

With this pattern:

```csharp
MeshAsset asset = meshAssetLoader.Load(path, uvMode);
MeshInstance instance = meshFactory.CreateInstance(asset);
SceneNode node = scene.Spawn(instance);
```

### 4. Turn compound hierarchy into node hierarchy

Do not keep compound part transforms only as mesh-local dictionaries.

Instead:

- each part becomes a node or sub-node
- emitters attach to part nodes
- hardpoints attach to part nodes
- animation updates node-local transforms

This gives a much cleaner path to reuse outside the mesh viewer.

## What the native/original structure still gives value

The original source still has useful architectural ideas. These should not be
discarded just because the old implementation style was painful.

### Keep these ideas

- archetype vs instance split
- explicit separation of loaded asset data from live runtime state
- compound hierarchy and attachment semantics
- hardpoints as named sockets / joints
- animation as data plus runtime playback state
- engine/runtime layering below game/module logic
- physics and collision as real runtime services

These are durable concepts, not artifacts of old C++ or COM.

## What should be treated as old-age relics

These were coping mechanisms for the original engine/toolchain and should not
be preserved unless a very specific compatibility reason appears later.

- DACOM
- COM-style interface discovery
- DLL-per-subsystem thinking
- string-keyed `QueryInterface` patterns
- plugin-style system bootstrapping for everything
- WinForms/Win32 wrapper-first architecture
- Direct3D-era public render-pipeline abstractions
- public vertex-buffer manager architecture
- render setup that assumes the app must own the full graphics backend

For the current `RaySharp` direction, these are mostly baggage.

## Recommended foundation for the four core modules

The proposed base for future work is:

### Assets

- XML-derived game data
- strings and resources
- meshes
- textures
- materials
- particles
- animations
- audio/video references

### Scene graph

- nodes
- transforms
- parent/child relationships
- named sockets
- bounds
- visibility

### Runtime

- animation
- particles
- collision
- physics
- audio
- input / hotkeys

### Rendering

- render queue
- mesh renderer
- particle renderer
- UI renderer
- debug renderer

This is enough to support:

- `Globals` as shared resources/contracts
- `Trim` as front-end/UI/runtime support
- `Mission` as gameplay/session logic
- `ZBatcher` only if it proves necessary later

## `ZBatcher` assessment

`ZBatcher` should currently be treated as unknown, not assumed necessary.

The right rule is:

- do not design `ZBatcher` first
- first build a render queue abstraction on top of raylib
- then profile real `Trim` and `Mission` workloads
- only add a custom batching layer if there is measured evidence that:
  - draw call count is a real problem
  - state bucketing matters
  - raylib's default behavior is insufficient for the actual game workload

Current recommendation:

- `ZBatcher = deferred`
- `ZBatcher = profiling-driven`
- `ZBatcher = not part of the minimum clean architecture`

## Practical decision summary

### Safe to keep as concepts

- archetypes
- instances
- hierarchy
- attachments
- animation state
- collision
- physics

### Safe to discard as architecture

- DACOM
- COM-like interface plumbing
- old render backend abstractions
- system-wide plugin orchestration for every runtime service

### Immediate next refactor

If only one structural task is done next, it should be:

- split `MeshViewerModel` into `MeshAsset` + `MeshInstance` + `MeshRenderer`

That is the smallest change that creates reusable scene-creatable objects and
unlocks the rest of the cleanup.

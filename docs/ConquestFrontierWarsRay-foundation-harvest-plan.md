# ConquestFrontierWarsRay Foundation Harvest Plan

This document is the short tracker for the rewrite foundation.

It has two jobs:

1. explain what each migrated module is doing in the current codebase
2. track what has already happened and what still needs migration

Related context:

- [ConquestFrontierWarsRay scene graph refactor checklist](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/docs/ConquestFrontierWarsRay-scene-graph-refactor-checklist.md)
- [ConquestSharp reimplementation tracker](/D:/git2/Conquest-Frontier-Wars-Source2/docs/ConquestSharp-reimplementation-tracker.md)
- [Conquest core packages overview](/D:/git2/Conquest-Frontier-Wars-Source2/docs/Conquest-core-packages-overview.md)

## Host project

`ConquestFrontierWarsRay` is the active target project.

All remaining migration work should land here. Older projects are sources and
references, not final destinations.

## Current foundation status

The foundation is only partially migrated.

Already in place:

- `Common` is imported
- the UTF reader/import path is imported
- both are connected to `ConquestFrontierWarsRay`
- DACOM-style dependency wiring was removed in favor of direct usable classes
  such as `DosFileReader`

Not fully done yet:

- imported code is not fully validated
- some code is still too close to original donor-project structure
- subsystem behavior still needs explicit verification

## Finished or active foundation modules

### 1. `Common`

Role in the code:

- canonical shared data model
- typed enums, structs, and domain models
- stable shape for Conquest data used by runtime, assets, and tools

Current migration state:

- imported into `ConquestFrontierWarsRay`
- connected as a foundation dependency
- should now be treated as the canonical model layer

Noteworthy changes from the original code:

- kept as the data-model base
- old DACOM architectural expectations are not part of the target foundation

Tests / verification:

- integration present
- functional validation status still incomplete

### 2. UTF reader / data access path

Role in the code:

- reads UTF-backed Conquest data
- provides the data-access path used by the imported model layer
- supports the current foundation import

Current migration state:

- imported into `ConquestFrontierWarsRay`
- wired to the main project
- available for current data access work

Noteworthy changes from the original code:

- DACOM/plugin-style indirection removed
- direct reader usage is preferred
- the UTF path is treated as a data reader, not as the central runtime
  architecture

Tests / verification:

- integration present
- end-to-end validation status still incomplete

### 3. `RaySharp` code already worth keeping

Role in the code:

- predecessor implementation containing important Conquest-specific runtime and
  tool behavior
- current source for useful mesh, particle, animation, hardpoint, and viewer
  logic

Current migration state:

- not the target project anymore
- remains a migration source for validated code
- useful code still needs selective harvest into
  `ConquestFrontierWarsRay`

Noteworthy changes from the original code:

- it is no longer the final host
- future work should split scene-local runtime blobs into cleaner asset,
  instance, and renderer boundaries

Tests / verification:

- code exists and is useful
- migration validation into `ConquestFrontierWarsRay` is still pending

### 4. Framework base

Role in the code:

- app shell, node lifecycle, input, resource ownership, and debug-tree patterns
  now live directly in `ConquestFrontierWarsRay.Framework`

Current migration state:

- harvested into the local framework project
- donor project separation is no longer required for the foundation layer

Tests / verification:

- local framework tests now cover the harvested behavior

### 5. Lightweight UI/menu layer

Role in the code:

- simple UI/control composition on top of the local framework

Current migration state:

- useful pieces have been folded into `ConquestFrontierWarsRay.Framework`
- duplicate scene-graph and input layers were removed

Tests / verification:

- framework coverage exists for the inherited base layers
- direct tests for the harvested menu/UI layer are still a follow-up item

## Planned migrations

These are the next self-contained migration regions. Each one states what it
is supposed to contribute to the target codebase.

### 1. App shell

Need:

- one owner of window startup
- one owner of the main loop
- one owner of frame timing and quit flow

Likely source:

- local `ConquestFrontierWarsRay.Framework`

### 2. Scene graph

Need:

- node hierarchy
- local/world transforms
- attach points
- reusable scene-creatable objects instead of scene-local loaders

Likely source:

- validated local framework patterns
- structure guided by the refactor checklist

### 3. Resource layer

Need:

- lazy-loaded disposable resources
- texture wrappers
- atlas ownership patterns
- resource-backed sprite patterns

Likely source:

- local `ConquestFrontierWarsRay.Framework`

### 4. Input layer

Need:

- named actions
- just-pressed / held / released state
- runtime hotkey support later

Likely source:

- local framework input model
- later selective behavior from `DAHOTKEY`

### 5. Mesh runtime split

Need:

- `MeshAsset`
- `MeshInstance`
- `MeshRenderer`

Reason:

- current viewer/runtime code is still too blob-like and scene-local

Likely source:

- selective harvest from `RaySharp`

### 6. Particle runtime split

Need:

- particle asset
- particle instance
- particle renderer

Reason:

- particle logic should follow the same asset/runtime/render split as meshes

Likely source:

- selective harvest from `RaySharp`

### 7. Collision

Need:

- extent-tree loading
- collision data model
- ray-query and mesh-query behavior

Likely source:

- `ConquestSharp/Collision`

Migration rule:

- keep the collision logic
- do not import DACOM registration or old engine plumbing

Current migration state:

- imported into `ConquestFrontierWarsRay.Runtime.Collision`
- DACOM factory/component wiring removed
- collision loading now uses the local `DosFileReader`
- runtime now uses direct construction such as `new CollisionService()`
- local collision tests ported into
  `ConquestFrontierWarsRay.Runtime.Collision.Tests`
- AppHost contains a visual sphere-collision demo driven by the runtime API

Noteworthy changes from the original code:

- `CollisionRuntime.Register`, `IDacomFactory`, and aggregate-component
  creation were removed
- archetype/model access is explicit through direct service methods
- the collision assembly now depends on local `Data` and `Math3D` projects
  instead of the old DACOM/engine plumbing path

Tests / verification:

- local collision tests are passing in `ConquestFrontierWarsRay`
- AppHost demo provides visual verification of contact point and normal output

### 8. Physics

Need:

- solver behavior
- fixed-step updates
- force / impulse / momentum handling

Likely source:

- `ConquestSharp/Physics`

Migration rule:

- keep the useful simulation behavior
- drop old factory/loading contracts

Current migration state:

- imported into `ConquestFrontierWarsRay.Runtime.Physics`
- DACOM/COM-style descriptor and factory wiring removed
- runtime now uses direct construction such as `new PhysicsService()` and
  `new PhysicsService(PhysicsSolverKind.Rk4)`
- `ConquestSharp` project references removed from the physics project
- solver tests ported into `ConquestFrontierWarsRay.Runtime.Physics.Tests`

Noteworthy changes from the original code:

- `IODESolver` factory indirection replaced by static solver dispatch over
  `PhysicsSolverKind`
- engine/system/file-system bridge code removed from the physics assembly
- the redundant Trap/Trapezoidal alias was removed because it compiled down to
  the same forward-Euler step

Tests / verification:

- ported runtime tests are passing in `ConquestFrontierWarsRay`
- direct-construction behavior is covered by the new local tests

### 8a. `Math3D`

Role in the code:

- minimal math support retained to keep the harvested physics code mostly
  intact
- provides `Matrix3`, `Transform3`, persistence structs, and the numerics-based
  math engine

Current migration state:

- imported into `ConquestFrontierWarsRay.Math3D`
- DACOM/COM-less version only
- namespace kept as `Math3D` to minimize churn in harvested code
- `ConquestSharp` project references removed from the math-dependent physics
  path

Tests / verification:

- `ConquestSharp/Math3D.Tests` ported into
  `ConquestFrontierWarsRay.Math3D.Tests`
- local math tests are passing

### 9. DOS/UTF import helpers

Need:

- only the minimal importer/backend support still required after the current
  UTF reader import

Likely source:

- selective pieces from `ConquestSharp/DOSFile`

Migration rule:

- use as importer support only
- do not let it become the runtime file-system architecture

### 10. Diagnostics and tool-side controls

Need:

- tree inspection
- debug dumps
- capture on demand
- reusable tool-side controls

Likely source:

- local framework debug-tree and UI controls

### 11. Future game-facing modules

Need:

- `Globals`
- `Trim`
- `Mission`

Rule:

- these build on top of the cleaned foundation, not before it

## Deferred items

### `ZBatcher`

Status:

- deferred

Rule:

- do not build it first
- first finish the cleaner node, asset, runtime, and render structure
- only revisit after profiling real workloads

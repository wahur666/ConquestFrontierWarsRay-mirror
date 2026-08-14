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

### 4. `testapp-ray-cs` framework donor

Role in the code:

- main framework donor for the new host
- reference for app shell, node lifecycle, input, resource ownership, and
  debug-tree patterns

Current migration state:

- still a donor/reference project
- not migrated as a whole
- selected framework pieces should be harvested as needed

Noteworthy changes from the original code:

- none required at the project level
- when harvested, the goal is to keep the framework concepts and not import
  unrelated project structure

Tests / verification:

- donor project has framework tests
- harvested integration inside `ConquestFrontierWarsRay` still needs its own
  verification

### 5. `EventHorizon` scene/UI reference

Role in the code:

- secondary reference for scene shape, scene switching, and simple UI/control
  composition

Current migration state:

- reference only
- not a foundation base

Noteworthy changes from the original code:

- most of its framework scene-graph layer is considered superseded by
  `testapp-ray-cs`

Tests / verification:

- not relevant as a migrated module yet

## Planned migrations

These are the next self-contained migration regions. Each one states what it
is supposed to contribute to the target codebase.

### 1. App shell

Need:

- one owner of window startup
- one owner of the main loop
- one owner of frame timing and quit flow

Likely source:

- primarily `testapp-ray-cs`
- use `EventHorizon` only as a scene-controller reference

### 2. Scene graph

Need:

- node hierarchy
- local/world transforms
- attach points
- reusable scene-creatable objects instead of scene-local loaders

Likely source:

- validated patterns from `testapp-ray-cs`
- structure guided by the refactor checklist

### 3. Resource layer

Need:

- lazy-loaded disposable resources
- texture wrappers
- atlas ownership patterns
- resource-backed sprite patterns

Likely source:

- `testapp-ray-cs`

### 4. Input layer

Need:

- named actions
- just-pressed / held / released state
- runtime hotkey support later

Likely source:

- base input model from `testapp-ray-cs`
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
  `new PhysicsService(new PhysicsOptions { Solver = PhysicsSolverKind.Rk4 })`
- `ConquestSharp` project references removed from the physics project
- solver tests ported into `ConquestFrontierWarsRay.Runtime.Physics.Tests`

Noteworthy changes from the original code:

- `IODESolver` factory indirection replaced by direct solver classes
- engine/system/file-system bridge code removed from the physics assembly
- the active forward-only Trap behavior remains preserved in
  `TrapezoidalSolver`

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

- debug-tree ideas from `testapp-ray-cs`
- simple UI composition ideas from `EventHorizon`

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

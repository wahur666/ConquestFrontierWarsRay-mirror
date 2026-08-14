# ConquestFrontierWarsRay Foundation Harvest Plan

This document tracks the current rewrite foundation inside `ConquestFrontierWarsRay`.

It has two jobs:

1. describe what the active foundation projects in this solution already own
2. track which foundation migrations are finished, active, or still pending

Related context:

- [ConquestFrontierWarsRay scene graph refactor checklist](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/docs/ConquestFrontierWarsRay-scene-graph-refactor-checklist.md)
- [Framework status](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/docs/framework.md)
- [Framework plan](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/docs/plan.md)

## Host solution

`ConquestFrontierWarsRay` is the active rewrite host.

The current solution is split into these source projects:

- `Conquest`: executable host, currently a collision demo application
- `Framework`: reusable raylib-oriented app, node, resource, and UI base layer
- `Data`: UTF/DOS readers plus typed Conquest data models
- `Math3D`: stripped-down math support retained for harvested runtime code
- `Runtime.Collision`: cleaned collision runtime
- `Runtime.Physics`: cleaned physics runtime
- `Legacy.RaySharp`: older executable/runtime source still used as a harvest reference

The current automated coverage lives in:

- `Data.Tests`
- `Framework.Tests`
- `Math3D.Tests`
- `Runtime.Collision.Tests`
- `Runtime.Physics.Tests`

## Current foundation status

The foundation is no longer just a partial import stub. A real local base now exists.

Already in place:

- the solution has dedicated local projects for framework, data, math, collision, and physics
- UTF/DOS reading and typed data-model code live under `src/Data`
- the local framework owns the app loop, node tree lifecycle, 2D nodes, resources, and lightweight UI primitives
- cleaned collision and physics runtimes live in their own projects with local tests
- the executable host in `src/Conquest` already exercises the collision runtime through a visual demo

Still not finished:

- the main executable is still a focused demo host, not a game shell
- `Legacy.RaySharp` still contains a large amount of Conquest-specific runtime and tool logic that has not yet been split into cleaner reusable modules
- framework-level follow-up work called out in `docs/plan.md` is still open, especially `SceneTree`, `AnimatedSprite2D`, `ResourceCache`, and more UI test coverage
- end-to-end validation against real game content remains incomplete

## Finished or active foundation modules

### 1. `Data`

Role in the code:

- canonical local home for UTF/DOS reading
- typed Conquest data models under `Models`, including `BT`, `GT`, `MT`, and shared structs
- repository helpers such as `StringPackRepository` and repo-path helpers such as `RepoPaths`

Current state:

- active local project in the solution
- no donor-project wiring layer is visible in the current structure
- supports both runtime code and parity-style data tests

Tests / verification:

- local `Data.Tests` cover DOS file reading and UTF DB/XML parity paths

### 2. `Framework`

Role in the code:

- reusable app shell around raylib
- node lifecycle and tree composition
- 2D transform and draw-order foundation
- reusable texture/resource ownership
- lightweight debug-tree and menu/UI primitives

Current state:

- active local project in the solution
- this is already the main owner of the reusable non-game-specific runtime base
- donor framework separation is no longer the important boundary; the local project is

Tests / verification:

- local `Framework.Tests` cover app, node, transform, input, resource, texture, and debug-tree behavior
- direct coverage for some harvested UI/menu pieces is still listed as follow-up work in `docs/plan.md`

### 3. `Math3D`

Role in the code:

- keeps the minimal math surface needed by harvested runtime code
- provides `Matrix3`, `Transform3`, persistence structs, and `MathEngine`

Current state:

- active local project in the solution
- namespace intentionally remains `Math3D` to reduce churn in harvested code

Tests / verification:

- local `Math3D.Tests` are present for the retained math engine behavior

### 4. `Runtime.Collision`

Role in the code:

- collision data model and runtime queries
- extent collision behavior used by the current executable demo

Current state:

- active local project in the solution
- depends only on local `Data` and `Math3D`
- direct construction is the current usage model

Noteworthy current usage:

- `src/Conquest/Program.cs` runs a visual sphere-collision demo using `CollisionService`

Tests / verification:

- local `Runtime.Collision.Tests` are present
- the executable demo gives a lightweight visual verification path

### 5. `Runtime.Physics`

Role in the code:

- fixed-step simulation support
- ODE solver selection
- force, impulse, momentum, and runtime instance/archetype handling

Current state:

- active local project in the solution
- depends only on local `Math3D`
- direct construction through `PhysicsService` is the current usage model

Tests / verification:

- local `Runtime.Physics.Tests` are present

### 6. `Legacy.RaySharp`

Role in the code:

- large donor/reference surface for older Conquest-specific runtime behavior
- still contains mesh, particle, scene, video, viewer, and UI code that may be worth selective harvest

Current state:

- still part of the solution as an executable/reference project
- not the desired final home for the rewrite foundation
- remains the main harvest source for Conquest-specific runtime behavior that has not yet been given cleaner local boundaries

Tests / verification:

- no dedicated local test project exists for this module in the current solution
- code here should be treated as source material to split and validate, not as finished foundation

### 7. `Conquest` executable host

Role in the code:

- current executable entry point
- small host for validating harvested runtime pieces in isolation

Current state:

- active local executable project
- currently focused on a collision demo rather than a general game shell

Tests / verification:

- validation is currently manual through the demo behavior

## Planned foundation migrations

These are the next foundation regions that still need deliberate structure work.

### 1. Executable host and scene ownership

Need:

- promote `Conquest` from a focused demo into a reusable game host
- introduce a real active-tree owner such as the planned `SceneTree`
- separate demo code from the long-term application shell

Primary source:

- local `Framework`
- current `docs/plan.md`

### 2. Scene graph completion

Need:

- finish the scene ownership model
- define clearer scene switching and root-node orchestration
- carry forward attach-point and reusable scene-object patterns where they are still needed

Primary source:

- local framework patterns
- scene graph refactor checklist

### 3. Shared resource/cache layer

Need:

- shared resource cache for textures and atlas definitions
- less scene-local/manual resource ownership
- cleaner disposal rules across reusable runtime code

Primary source:

- local `Framework`

### 4. Animation and richer 2D runtime primitives

Need:

- `AnimatedSprite2D`
- atlas-driven frame playback
- more reusable 2D presentation primitives so scene code stops owning low-level animation glue

Primary source:

- local `Framework`
- selective ideas from `Legacy.RaySharp`

### 5. Mesh runtime split

Need:

- separate asset loading from instance state and rendering
- define cleaner mesh-facing boundaries instead of scene-local viewer blobs

Likely target shape:

- `MeshAsset`
- `MeshInstance`
- `MeshRenderer`

Primary source:

- selective harvest from `Legacy.RaySharp/Mesh*` and `MeshViewer`

### 6. Particle runtime split

Need:

- separate particle asset definitions, runtime instances, preview/editor plumbing, and rendering
- avoid keeping the particle system as one scene-local cluster

Likely target shape:

- `ParticleAsset`
- `ParticleInstance`
- `ParticleRenderer`

Primary source:

- selective harvest from `Legacy.RaySharp/Particle`

### 7. Tool-side diagnostics

Need:

- keep improving reusable debug inspection instead of ad hoc scene-local diagnostics
- tree inspection, structured dumps, and targeted viewers for imported content

Primary source:

- local framework debug-tree facilities
- selected viewer logic from `Legacy.RaySharp`

### 8. Game-facing module layer

Need:

- game-specific modules on top of the cleaned foundation
- likely future homes for gameplay/state systems rather than keeping everything in host scenes

Examples:

- globals
- mission/runtime state
- trim/content-facing logic

Rule:

- these should build on the cleaned local foundation, not arrive before it

## Deferred items

### `ZBatcher`

Status:

- deferred

Rule:

- do not prioritize it ahead of the current framework, scene, mesh, and particle cleanup
- revisit only after real runtime workloads show the need

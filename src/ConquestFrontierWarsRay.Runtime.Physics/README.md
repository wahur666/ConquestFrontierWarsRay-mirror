# ConquestFrontierWarsRay Runtime Physics

This project is the cleaned physics foundation harvested from `ConquestSharp/Physics`.

The old DACOM and engine-plumbing surface has been removed. The runtime is now a direct-use managed library:

- create solvers with `new EulerSolver()`, `new Rk4Solver()`, or `new TrapezoidalSolver()`
- create the physics runtime with `new PhysicsService()` or `new PhysicsService(new PhysicsOptions { Solver = ... })`
- register archetypes and instances directly through `RegisterArchetype` and `RegisterInstance`

## Scope

The current port keeps the useful simulation behavior:

- fixed-step updates with `min_dt` splitting
- force, impulse, momentum, and extent handling
- direct solver selection without factory descriptors
- active forward-only Trap behavior preserved in `TrapezoidalSolver`

The project intentionally drops the old glue:

- DACOM factories and descriptors
- `ConquestSharp.Engine`, `ConquestSharp.System`, and `ConquestSharp.DOSFile` dependencies
- aggregate/component registration boilerplate
- file-system driven archetype creation hooks

## Related code

- [PhysicsService.cs](D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/ConquestFrontierWarsRay.Runtime.Physics/PhysicsService.cs)
- [PhysicsContracts.cs](D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/ConquestFrontierWarsRay.Runtime.Physics/PhysicsContracts.cs)
- [PhysicsSolvers.cs](D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/ConquestFrontierWarsRay.Runtime.Physics/PhysicsSolvers.cs)

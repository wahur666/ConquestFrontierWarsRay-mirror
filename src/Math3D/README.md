# ConquestFrontierWarsRay Math3D

This is the stripped-down managed `Math3D` harvest from `ConquestSharp/Math3D`.

It keeps the math types and numerics-backed engine that the physics runtime needs:

- `Matrix3`
- `Transform3`
- persistence structs for vector, matrix, transform, and quaternion
- static `MathEngine`

It intentionally drops the COM/DACOM compatibility layer:

- no descriptors
- no registration runtime
- no implementation-name indirection
- no engine instance abstraction

The namespace stays `Math3D` so donor code ports with minimal churn.

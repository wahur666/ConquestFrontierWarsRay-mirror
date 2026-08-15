# Plan

This document tracks the next framework steps for the node-based raylib foundation.

Related: [Framework Status](./framework.md)

## Completed Foundation

- [x] Introduce a base `Node` abstraction with lifecycle hooks.
- [x] Support parent/child composition for reusable scene graphs.
- [x] Move app startup into a foundation-oriented runtime loop.
- [x] Add named input handling and per-frame action state.
- [x] Add a first 2D stack with `CanvasItem`, `Node2D`, `Sprite`, `CompressedTexture2D`, and `AtlasTexture`.
- [x] Add a shared `Resource` base type for reusable asset ownership and disposal.
- [x] Add `Node2D` rendering properties such as `Visible`, `ZIndex`, and optional draw ordering controls.
- [x] Fold in reusable menu/UI primitives from the former scene reference layer.

## Next Plan

1. [x] `SceneTree`:
   add a Godot-style owner for the active tree, quit flow, root switching, and global update/draw orchestration instead of keeping all runtime control inside `RaylibApplication`.

2. [ ] `Camera3DNode`:
   move 3D camera state into the node framework so scenes can compose and switch cameras through the tree instead of keeping `Camera3D` instances in scene-local code.

3. [ ] `VisualInstance3D` and `MeshInstance3D`:
   add a reusable 3D renderable base plus a first concrete mesh/model node so the 3D framework can compose visible world objects instead of transform-only placeholders.

4. [ ] `Socket3D`:
   add named attachment nodes for hardpoints, weapon mounts, particle anchors, and compound-part composition so Conquest-style ship assembly fits the scene graph cleanly.

5. [ ] `Light3D`:
   add a reusable lighting node so 3D scenes can own light setup through the framework instead of relying on scene-local rendering glue.

6. [ ] `AnimatedSprite2D`:
   build the next obvious 2D primitive on top of atlas textures and frame metadata, instead of leaving atlas handling to higher-level scenes.

7. [ ] `ResourceCache`:
   add a simple shared loader/cache layer for textures and atlas definitions so scenes stop manually owning duplicate resources and disposal rules become less error-prone.

8. [ ] Framework UI tests:
   add direct coverage for `MenuNavigation`, `MenuList`, and `PauseDialog`, since these were harvested after the original donor framework test suite.

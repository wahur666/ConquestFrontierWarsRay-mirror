# Frontend of Conquest

Date: 2026-08-20

## Purpose

This document defines the practical frontend direction for porting the original
Conquest: Frontier Wars menu/UI into the current raylib-based framework.

The goal is not to redesign the frontend first. The goal is to:

1. reproduce the original authored frontend faithfully
2. render it through the current framework
3. scale the authored `800x600` composition into a centered `1080p`-class
   presentation with black side bars
4. keep the implementation simple enough that later `MiniLayout` experiments
   can happen without blocking the legacy port

## Core Decision

The frontend should start with a **fixed-position legacy composer**, not with a
general dynamic layout engine.

That means:

- `Menu1.xml` and similar data remain the source of truth
- controls are materialized at authored coordinates
- the whole menu subtree is scaled and centered as one composed surface
- component naming may align with original data types where that reduces
  translation friction

`MiniLayout` remains useful later for new framework-native screens. It is not
the first tool for the legacy frontend import.

## Base Presentation Model

### Authored space

The original frontend data is authored in an `800x600` coordinate space.

Legacy records such as:

- `BUTTON_DATA`
- `STATIC_DATA`
- `ANIMATE_DATA`
- `DROPDOWN_DATA`
- `LISTBOX_DATA`
- `SLIDER_DATA`

all encode local positions and dimensions in that base coordinate system.

### Display rule

The runtime should compose the frontend in authored coordinates, then apply one
uniform viewport transform.

For `1920x1080`:

- scale = `min(1920 / 800, 1080 / 600) = 1.8`
- content size = `1440x1080`
- remaining width = `480`
- left bar = `240`
- right bar = `240`

So the rendering model is:

1. place controls in `800x600`
2. scale the whole frontend tree by `1.8`
3. offset the tree by `(240, 0)`
4. clear the outside region to black

This is the correct first implementation because it preserves the authored
screen exactly.

## Framework Shape

## `LegacyMenuRoot`

Introduce a dedicated root for legacy-composed frontend screens.

Responsibilities:

- store base size `800x600`
- compute scale-to-fit for the active window
- compute center offset
- draw black bars outside the content rect
- host the composed subtree for one legacy menu screen

Suggested surface:

```csharp
public sealed class LegacyMenuRoot : Node2D {
    public Vector2 BaseResolution { get; } = new(800f, 600f);
    public Rectangle ContentViewport { get; }
    public float UniformScale { get; }
}
```

The important rule is that child menu controls keep authored coordinates.
`LegacyMenuRoot` owns the viewport transform.

## Composition Model

A legacy menu screen should be materialized as:

- one screen root node
- child controls created from typed GT data
- local positions copied directly from source XML/model values

That is a composition/materialization pass, not a layout pass.

## Data Path

The repo already contains the useful typed boundaries:

- GT shared structs define:
  - `BUTTON_DATA`
  - `STATIC_DATA`
  - `ANIMATE_DATA`
  - `LISTBOX_DATA`
  - `SLIDER_DATA`
  - `DROPDOWN_DATA`
- `Menu1.xml` already maps into those model types

Relevant files:

- [GT_SHARED_STRUCTS.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestSharp/Common/Models/GT/GT_SHARED_STRUCTS.cs)
- [Menu1.xml](/D:/git2/Conquest-Frontier-Wars-Source2/DB/xml/GenData.db/GT_MENU1/Menu1.xml)

That means the frontend port should not start by inventing a new schema.
It should consume the existing typed records directly.

## Naming Policy

The naming policy should optimize for **traceability back to original frontend
data**, not for idealized framework purity.

If a legacy record is a simple 1:1 mapping onto an existing framework control,
it is acceptable to introduce a thin legacy-facing type that inherits the
existing control only to align names.

Example:

- `BUTTON_DATA` can map to a thin `LegacyButton` over `ButtonNode`
- `STATIC_DATA` can map to a thin `LegacyStatic` over `TextNode`, `PanelNode`,
  or image/static surfaces depending on archetype

This wrapper should:

- preserve the legacy-facing name
- avoid adding unrelated behavior
- only adapt construction or data binding if needed

This is explicitly acceptable because it reduces translation overhead when
moving from old data and old code into the new frontend runtime.

## Record Mapping Strategy

The first pass should map legacy records to framework nodes as directly as
possible.

### `BUTTON_DATA`

Source meaning:

- button archetype id
- text id
- origin
- optional button area

First-pass replacement:

- simple framework button node
- positioned directly from `XOrigin` / `YOrigin`
- size taken from archetype or explicit area when present

Recommendation:

- treat this as 1:1 equatable to a simple button
- do not over-generalize on the first pass

### `STATIC_DATA`

Source meaning:

- static archetype id
- text id
- tooltip/hint ids
- alignment
- origin
- width/height

First-pass replacement:

- static text node
- image/static panel node
- or combined image+text static surface depending on archetype

Important note:

In the original codebase, `Static` was broader than a pure label. Some static
records are effectively image panels or framed surfaces with optional text.

So `STATIC_DATA` should not be collapsed too early into "just a label."

### `ANIMATE_DATA`

Source meaning:

- animation archetype id
- origin
- timer
- optional fuzz effect

Typed model:

- [GT_SHARED_STRUCTS.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestSharp/Common/Models/GT/GT_SHARED_STRUCTS.cs)

Legacy evidence:

- [Animate.cpp](/D:/git2/Conquest-Frontier-Wars-Source2/src/Conquest/Animate.cpp) shows the old control:
  - derives from a rectangular UI base
  - resolves an animation archetype
  - computes screen rect from origin + frame size
  - advances cells over time
  - supports looping/pause
  - optionally uses a fuzz effect

Archetype mapping:

- [Animate!!Multi.xml](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/assets/DB/xml/GenData.db/GT_ANIMATE/Animate!!Multi.xml)
  maps `Animate!!Multi` to `VFXShape!!AnimateMulti`

Replacement asset evidence:

- [animMulti_atlas.json](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/assets/interface/animMulti_atlas.json)
- [animMulti_atlas.png](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/assets/interface/animMulti_atlas.png)

The atlas metadata shows:

- source file `animMulti.SHP`
- 30 frames
- frame rectangles already extracted

Framework seam already available:

- [AtlasDefinitionResource.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/AtlasDefinitionResource.cs)
- [AtlasTexture.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/AtlasTexture.cs)
- [Sprite.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/Sprite.cs)

Recommended replacement:

- add a lightweight `LegacyAnimate` / `AnimatedImageControl`
- it owns:
  - atlas texture
  - atlas definition
  - current frame index
  - per-frame timing from `ANIMATE_DATA.Timer`
  - looping flag
- it draws one atlas frame through `Sprite` or the same underlying
  `Texture2D`/`AtlasTexture` path

Do not port:

- archetype factory plumbing
- DirectDraw/GDI assumptions
- talking-head fuzz behavior into framework core

Fuzz can stay app-specific and optional.

### `LISTBOX_DATA`

First-pass replacement:

- list surface with fixed authored bounds
- explicit scroll/selection behavior later

The important thing now is preserving authored placement and visible area.

### `DROPDOWN_DATA`

The source data already models dropdowns as a composite:

- outer screen rect
- button data
- listbox data

So the first port should keep that composition model.

Recommendation:

- `LegacyDropdown` owns one button node and one listbox node
- preserve source-local offsets exactly

This is a case where the data shape already tells us the right runtime shape.

### `SLIDER_DATA`

First-pass replacement:

- fixed-position slider node
- preserve authored bounds/origin

No layout abstraction is needed to get this working.

## Archetype Resolution

A legacy frontend control is not defined by the data record alone. It is also
defined by its referenced archetype:

- `Button!!...`
- `Static!!...`
- `Dropdown!!...`
- `ListBox!!...`
- `Slider!!...`
- `Animate!!...`

The composer should therefore have two distinct responsibilities:

1. materialize control placement from the GT record
2. resolve visual/behavioral archetype data from the referenced type id

That is a better split than mixing XML parsing, resource lookup, and control
construction into one step.

## Recommended Runtime Layers

### Layer 1: Legacy Data Reader

Consumes:

- typed GT model data already parsed from XML/binary sources

Produces:

- plain in-memory record objects for one menu screen

### Layer 2: Legacy Archetype Resolver

Consumes:

- type ids like `Animate!!Multi` or `Button!!Back2D`

Produces:

- resolved visual metadata
- atlas/image/frame data
- default dimensions when source data omits them

### Layer 3: Legacy Menu Composer

Consumes:

- parsed screen records
- resolved archetype metadata

Produces:

- framework node subtree under `LegacyMenuRoot`

This is the key new runtime layer.

## What Should Reuse Existing Framework Nodes

Reuse existing framework nodes where behavior already matches:

- `ButtonNode`
- `TextNode`
- `PanelNode`
- `DropdownNode`
- `SliderNode`
- `Sprite`

But do not force reuse when the semantic gap is large.

It is acceptable to add:

- `LegacyButton`
- `LegacyStatic`
- `LegacyAnimate`
- `LegacyListBox`
- `LegacyDropdown`

as thin frontend-specific types if that keeps the mapping explicit.

The standard component remains the real implementation. The legacy type is
there to preserve 1:1 source correspondence and construction semantics.

## What Should Not Be Framework-Core

Keep these out of framework core on the first pass:

- GT/archetype factory plumbing
- legacy hintbox/status/history coupling
- talking-head fuzz/video-border effects
- legacy modal runtime behavior copied literally
- launcher/network/profile workflow logic

Those belong in frontend/app composition layers, not in the base UI nodes.

## Relationship To MiniLayout

`MiniLayout` still has a place, but not as the importer for the legacy
frontend.

Recommended order:

1. get exact legacy menus rendering correctly
2. stabilize component mapping and archetype resolution
3. then experiment with replacing some newly-authored screens using
   `MiniLayout`
4. only use dynamic layout where it clearly improves maintainability without
   breaking fidelity

This keeps frontend reproduction and layout modernization separate.

## Initial Implementation Slice

The smallest credible implementation slice is:

1. add `LegacyMenuRoot`
2. add one legacy screen composer for the `opening` section of `Menu1.xml`
3. support:
   - `STATIC_DATA`
   - `BUTTON_DATA`
   - `ANIMATE_DATA`
4. drive `ANIMATE_DATA` from atlas-based frame playback
5. render at `1920x1080` using centered pillarboxing

That slice proves:

- fixed authored coordinates
- viewport transform
- asset replacement via atlas
- 1:1 record-to-control materialization

## Follow-Up Slice

After the first slice works:

1. add `DROPDOWN_DATA`
2. add `LISTBOX_DATA`
3. add `SLIDER_DATA`
4. formalize archetype-resolver interfaces
5. evaluate whether legacy wrapper names improve code clarity enough to keep
   them permanently

## Final Recommendation

The frontend should begin as a **legacy-authored fixed-position renderer** over
the current framework.

The correct near-term architecture is:

1. `LegacyMenuRoot` for authored-space scaling and centering
2. typed GT-record to framework-node composition
3. archetype resolution for visuals
4. atlas-backed animation replacement for `ANIMATE_DATA`
5. thin legacy-named wrappers where they improve traceability

Then, after the frontend is reliably recreated, `MiniLayout` can be used to
explore dynamic layout for new screens or selective refactors.

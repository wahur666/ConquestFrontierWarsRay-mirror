# Basic Layout Containers Proposal

## Problem

The framework has reusable controls, but it does not yet have a reusable layout
system. Screens still hard-code `Position`, `Size`, and text coordinates in app
code, so composition remains manual and fragile.

This makes even simple UI maintenance expensive:

- window size changes require manual coordinate edits
- adding a sidebar, footer, or modal body requires hand-tuned offsets
- text length and localization can break layouts
- reusable controls do not compose into reusable screens

## Existing Evidence

The current pain point is visible in the framework test app, where scenes place
panels, labels, buttons, and sliders with fixed coordinates instead of using
parent-driven layout.

The framework `Control` base currently exposes only:

- `Size`
- `GlobalBounds`
- hit testing

That is enough for manual placement, but not enough for parent-owned layout.

## Goal

Add a minimal framework layout system that lets controls participate in
container-driven placement without forcing a full redesign of the existing node
model.

The first goal is not a complete UI toolkit. The first goal is to make common
screen composition practical:

- vertical stacks
- horizontal rows
- padded panels
- consistent spacing and margins
- basic alignment inside a parent area

## MiniLayout Exploration

The `MiniLayout` project is already useful as problem-space exploration.

It proves a small box layout engine can arrange children from:

- `LayoutDirection` (`Row`, `Column`)
- `JustifyContent`
- `AlignItems`
- `Padding`
- `Gap`
- child `Margin`
- child fixed `LayoutSize`

Relevant files:

- [BoxLayoutEngine.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/MiniLayout/BoxLayoutEngine.cs)
- [BoxLayoutStyle.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/MiniLayout/BoxLayoutStyle.cs)
- [LayoutNode.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/MiniLayout/LayoutNode.cs)
- [LayoutPrimitives.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/MiniLayout/LayoutPrimitives.cs)
- [Program.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/MiniLayout.Demo/Program.cs)

### What MiniLayout Already Proves

1. Box layout primitives fit the framework problem well.
2. Padding, gap, margin, and axis alignment are enough to remove a large amount
   of manual positioning.
3. A separate arrange pass is a good mental model for container-owned placement.
4. Responsive container bounds can be recalculated every frame without complex
   state.

### What MiniLayout Does Not Yet Solve

MiniLayout is still a standalone geometry engine, not a framework integration.
It does not currently address:

- layout participation through `Control`
- parent/child node traversal
- minimum or preferred size measurement
- stretch or fill behavior
- text measurement integration
- nested container invalidation
- anchors for non-container placement cases
- interaction with drawing, hit testing, or future modal UI

That means MiniLayout should inform the framework design, but not be copied
blindly as the final public API.

## Proposed Solution

Add a minimal container-based layout layer to the framework, centered on
`Control` participation and a parent-owned layout pass.

## Core Design

### 1. Extend `Control` With Layout Properties

Suggested additions:

- `Margin`
- `MinimumSize`
- `PreferredSize`
- `HorizontalSizePolicy`
- `VerticalSizePolicy`
- `AnchorPreset` or basic anchors for non-container placement

Suggested size policies for the first pass:

- `Fixed`
- `Auto`
- `Fill`

The intent is:

- `Fixed`: use explicit size
- `Auto`: size from content or preferred size
- `Fill`: take remaining container space along an axis

### 2. Add Layout-Capable Container Controls

First-pass containers should stay small and concrete:

- `HBoxContainer`
- `VBoxContainer`
- `PanelContainer`

Responsibilities:

- `HBoxContainer`: arrange visible children in a row
- `VBoxContainer`: arrange visible children in a column
- `PanelContainer`: apply padding and host one primary content child or a small
  group of children

These cover a large percentage of menu, dialog, inspector, and HUD scaffolding
without introducing a large API surface.

### 3. Add A Layout Pass

Containers should own child placement during update or during a dedicated
layout phase.

High-level flow:

1. Parent computes its content rect.
2. Parent measures or reads child desired sizes.
3. Parent arranges children according to axis, gap, margin, and alignment.
4. Parent writes child `Position` and possibly `Size`.

This should be framework-owned, not scene-owned.

### 4. Keep Positioning Escape Hatches

Not every node should require containers. The framework still needs:

- manual absolute positioning for custom scenes
- anchors for HUD-like placement
- direct `Position` and `Size` for low-level control

The layout system should reduce manual placement, not ban it.

## Recommended Public Surface

### Layout Primitives

Reuse the conceptual shape already explored in MiniLayout:

- `Thickness`
- `LayoutDirection`
- `LayoutAlignment`

These are already good framework-level concepts and should likely move into the
framework library rather than stay demo-only.

### Container Properties

For box containers:

- `Direction` (`HBoxContainer` and `VBoxContainer` can hard-code this)
- `Gap`
- `Padding`
- `AlignItems`
- `JustifyContent`

For child participation:

- `Margin`
- `MinimumSize`
- `PreferredSize`
- size policy per axis

## Measurement Strategy

The first version should avoid a complex retained layout engine.

Recommended approach:

1. Each `Control` exposes a lightweight desired-size query.
2. Controls with explicit `Size` can return that as preferred size.
3. Text-based controls can use measured text width/height.
4. Containers use preferred sizes first and support fill behavior second.

Suggested hook:

- `virtual Vector2 GetPreferredSize()`

For the first pass, this can be simple and still useful.

Examples:

- `TextNode` returns measured text bounds
- `ButtonNode` returns text size plus internal padding
- `PanelNode` returns explicit size or child-wrapped size if promoted into a
  container

## Relationship To MiniLayout

MiniLayout should be treated as the reference prototype for first-pass box
arrangement logic.

Recommended reuse pattern:

1. Reuse its primitives and alignment vocabulary.
2. Reuse or adapt its axis math for row/column placement.
3. Do not expose `LayoutNode` as the framework UI contract.
4. Replace standalone name-based child dictionaries with direct `Control`
   references and parent-owned traversal.

In other words, reuse the layout math, but integrate it through `Control` and
container nodes.

## Important Constraints

### Do Not Start With A General Constraint Solver

That would overshoot the current framework maturity. The problem is basic menu
and panel composition, not arbitrary document layout.

### Do Not Couple Layout To Rendering Types

Layout should compute geometry and assign bounds. Drawing should continue to
live in controls.

### Do Not Require Every Node To Become Layout-Aware At Once

Only `Control` and new container types need to participate initially. Legacy or
custom nodes can keep manual positioning until migrated.

## Recommended Implementation Slice

The smallest useful slice is:

1. Move `Thickness`, alignment, and axis primitives into the framework.
2. Add `Margin`, `MinimumSize`, and `GetPreferredSize()` to `Control`.
3. Add `VBoxContainer`.
4. Add `HBoxContainer`.
5. Add `PanelContainer` for padding.
6. Teach `TextNode` and `ButtonNode` to report preferred size.
7. Convert one test app scene to container-based layout as proof.

This is enough to validate:

- parent-owned child placement
- reduced hard-coded coordinates
- simple responsive resizing
- measured text participation

## Suggested Proof Scene

Convert `ControlsScene` or a similar menu-like test scene first.

That gives a good proof because it contains:

- a framed panel
- stacked labels
- a row of buttons
- explanatory text

If that scene can drop most of its manual coordinates, the layout system is
earning its keep.

## Migration Plan

1. Introduce layout primitives into the framework assembly.
2. Add `Control` layout participation properties.
3. Build `VBoxContainer` and `HBoxContainer` using adapted MiniLayout math.
4. Add `PanelContainer` for padding/content wrapping.
5. Add preferred-size support to text and button controls.
6. Convert one showcase scene.
7. Leave direct positioning available for scenes that are not migrated yet.

## Expected Outcome

After this change:

- common screens can be composed from containers instead of coordinates
- control reuse improves because controls can participate in parent layout
- window resizing becomes manageable
- text-driven sizing has a framework path
- the test app can prove reuse by deleting scene-side placement code

This keeps the scope aligned with the current pain point: basic layout
containers first, not a full UI toolkit.

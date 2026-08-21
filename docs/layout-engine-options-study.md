# Layout Direction: Legacy Menu Composer, MiniLayout, And Yoga

Date: 2026-08-20

## Purpose

This document now centers the discussion on the actual requirement implied by
the legacy assets: the framework will likely need to reproduce `Menu1.xml`
faithfully before it tries to reinterpret those screens into a new responsive
layout language.

That leaves three layers of concern:

1. a custom legacy menu composer for exact `Menu1.xml`-style composition
2. `MiniLayout` for framework-native container layout
3. `Yoga` as an external layout engine if later scope justifies it

The real decision is no longer just "MiniLayout vs Yoga." It is:

- how to load and present the authored 800x600 legacy menu data
- when to introduce higher-level framework layout
- whether Yoga earns its integration cost after that

Relevant local references:

- [framework.md](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/docs/framework.md)
- [XamlSceneCompositionPlan.md](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/docs/XamlSceneCompositionPlan.md)
- [basic-layout-containers-proposal.md](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/docs/basic-layout-containers-proposal.md)
- [Control.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/Control.cs)
- [ControlsScene.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework.TestApp/src/Scenes/ControlsScene.cs)
- [BoxLayoutEngine.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/MiniLayout/src/BoxLayoutEngine.cs)

## Framework Reality

The framework is currently:

- raylib-only at runtime
- C# and `Raylib-cs`
- a retained node tree
- built around framework-owned `Node`, `Node2D`, and `Control`

The UI problem is now split in two:

- screens are still manually placing controls with `Position` and `Size`
- `Control` does not yet participate in parent-owned layout
- `MiniLayout` already proves that row/column box placement is useful
- the original project already contains dense authored menu XML with fixed
  coordinates and composite control records

So the requirement is not "find a raylib GUI." The requirements are:

1. keep the retained framework model
2. render legacy menu definitions faithfully
3. scale an authored 800x600 composition to modern output sizes
4. give `Control` a real layout contract for new framework-native screens

That matters because Yoga is not a widget toolkit. It is only a layout engine.
That is a strength in general, but it does not answer the legacy-composer part
by itself.

## Short Recommendation

The default recommendation is now:

1. build a custom legacy menu composer for exact `Menu1.xml`-style screens
2. base that composer on authored local coordinates, not Flexbox reinterpretation
3. scale the authored 800x600 composition uniformly into a centered viewport
4. use `MiniLayout` for new framework-native composition where container layout
   is actually the right abstraction
5. keep `Yoga` as an optional later import, not as the first answer

Put more directly:

- exact legacy menu reproduction points toward a custom composer
- `MiniLayout` is still the best local layout direction for new screens
- `Yoga` is still the strongest outside engine if later responsive layout scope
  grows far beyond the current prototype

## Legacy Composer Requirement

If the target is "place everything from `Menu1.xml` and scale it in one go,"
then the framework needs a legacy composition mode.

That mode is not the same thing as a general layout engine.

### Base coordinate system

`Menu1.xml` is authored for an `800x600` coordinate space.

For a `1920x1080` output with black side bars:

- uniform scale = `min(1920 / 800, 1080 / 600) = 1.8`
- scaled content size = `1440x1080`
- horizontal remainder = `1920 - 1440 = 480`
- pillarbox width = `240` on the left and `240` on the right

So the exact presentation rule is:

- compose in authored `800x600` space
- scale every authored coordinate and size by `1.8`
- offset the final scene by `(240, 0)`
- fill the remaining side areas with black

That is simple, deterministic, and faithful to the source asset.

### What the composer should do

The composer should:

- load a legacy menu section
- create framework controls matching the authored record types
- place them in local 800x600 coordinates
- apply one uniform viewport transform to the whole subtree
- preserve original alignments and composite widgets

This is much closer to a scene/materialization problem than to a Flexbox
problem.

### Why this points away from Yoga as the first move

Yoga is useful when the question is:

- how should a parent distribute children?
- how should children grow, shrink, wrap, align, and measure?

But the legacy menu requirement is first:

- how do we reproduce this authored arrangement exactly?

For that job, Yoga mostly adds a second abstraction layer that the source data
does not ask for.

## What We Know About Yoga

Sources checked on 2026-08-20:

- Yoga docs: <https://www.yogalayout.dev/>
- About Yoga: <https://www.yogalayout.dev/docs/about-yoga>
- Styling: <https://www.yogalayout.dev/docs/styling/>
- External layout systems / measure functions:
  <https://www.yogalayout.dev/docs/advanced/external-layout-systems>
- GitHub: <https://github.com/react/yoga>

### Core facts

Yoga is:

- an embeddable layout system
- layout-only, not a UI framework
- focused on a familiar subset of CSS, mostly Flexbox
- written in C++, with a public C API

That is important for this repo because Yoga does not try to replace rendering,
input, or widgets. It only computes box sizes and positions.

### What it already gives

From the official docs, Yoga supports:

- flex direction
- justify content
- align items / align self
- gap
- margin / padding / border
- width / height
- min / max width and height
- grow / shrink / basis
- wrapping
- relative / absolute / static positioning
- aspect ratio

That is a significantly broader layout surface than the current `MiniLayout`
prototype.

### Defaults that matter

Yoga defaults are not exactly browser defaults.

Per the official styling docs:

- `flex-direction` defaults to `column`
- `align-content` defaults to `flex-start`
- `flex-shrink` defaults to `0`
- `position` defaults to `relative`

Yoga has a `UseWebDefaults` option for some defaults, but not all of them.

That means if Yoga is integrated, the framework should pick and document one
explicit default configuration instead of relying on implicit engine behavior.

### Units and box model

Yoga works in abstract "points" and percentages, not CSS units like `px` or
`em`. It also behaves like `border-box`.

For this framework that is fine. The framework can treat Yoga points as its own
2D layout units and map them directly to raylib-space UI coordinates.

### Measurement hooks

Yoga has measure functions for leaf nodes that need external sizing.

This is one of the strongest reasons it is a real candidate here.

It means framework controls such as:

- `TextNode`
- `ButtonNode`
- `DropdownNode`
- future wrapped text controls

can be measured by framework code while Yoga handles container layout.

The official docs also require that measurement changes mark the node dirty.
That aligns with a retained framework model, but it means we would need proper
invalidation discipline.

### What Yoga does not solve

Yoga does not give us:

- framework controls
- drawing
- hit testing
- input routing
- scroll behavior integration with our controls
- scene-tree lifecycle integration
- C#-native node ownership

So even with Yoga, we still must design the framework-facing layout layer.

## MiniLayout

`MiniLayout` is the current local prototype. It already proves:

- row layout
- column layout
- padding
- gap
- child margin
- axis alignment

That is enough to solve a large part of the framework's current pain.

### Why MiniLayout fits well

- it is already local and readable
- it matches the current retained architecture
- it avoids native interop
- it can be adapted directly into `Control`-based containers
- it matches the proposed `VBoxContainer` / `HBoxContainer` direction already
  described in local docs

### What MiniLayout still lacks

- measurement
- fill and stretch policy
- wrapping
- min/max size rules
- absolute-position participation rules
- dirty/invalidation rules
- deeper nested layout behavior under stress

So `MiniLayout` is a strong first step, but it is not yet a complete framework
layout system.

## Decision Comparison

| Question | Legacy Composer | MiniLayout | Yoga |
| --- | --- | --- |
| Best fit for exact `Menu1.xml` reproduction | Yes | Partial | Poor |
| Keeps current architecture cleanly | Yes | Yes | Yes, with a bridge layer |
| Needs native interop | No | No | Yes |
| Gives basic row/column layout fast | N/A | Yes | Yes |
| Gives broader flexbox behavior | No | Limited today | Yes |
| Gives official measurement hook model | We design it locally | Not yet, we design it | Yes |
| Build/distribution cost | Low-Medium | Low | High |
| Debuggability in this repo | High | High | Medium |
| First useful version speed for legacy menus | Faster | Medium | Slower |

## Evidence From Legacy `Menu1.xml`

Sampled on 2026-08-20 from:

- [Menu1.xml](/D:/git2/Conquest-Frontier-Wars-Source2/DB/xml/GenData.db/GT_MENU1/Menu1.xml)

I did not read the entire file line by line. I sampled the opening section, a
middle multiplayer/options section, and the later help/device sections, then
counted common layout-bearing record types.

### What the file appears to contain

The sampled file is a dense menu definition built from many individually placed
records inside named menu sections.

Observed counts from quick pattern scans:

- `BUTTON_DATA`: 109
- `STATIC_DATA`: 100
- `DROPDOWN_DATA`: 45
- `LISTBOX_DATA`: 47
- `SLIDER_DATA`: 2
- `ANIMATE_DATA`: 6
- `screenRect`: 116
- `alignmenttype`: 98

### Structural patterns seen in the sample

The important patterns are:

- each major menu section declares a `screenRect`
- many child elements are then positioned with `xOrigin` / `yOrigin`
- labels often carry explicit `width`, `height`, and local alignment
- dropdowns are composite records built from a button plus an embedded listbox
- many controls appear in repeated aligned pairs:
  - right-aligned label
  - control placed at a fixed x-position to the right
- some screens include repeated indexed button grids or lists
- the file is largely authored for a fixed `800x600` era screen model

### What that means for layout design

This file increases confidence in some parts of the current direction and
decreases confidence in others.

What it supports:

- the framework needs a legacy composer that can instantiate fixed authored
  coordinates directly
- the framework needs composition for repeated rows, columns, label/control
  pairs, grouped panels, and dropdown/list composites
- the framework still needs an absolute-position escape hatch
- the framework should support nested local coordinate spaces inside panels or
  screen sections

What it does not strongly prove:

- a need for full web-style flex behavior everywhere
- a need for complex relational or constraint-based layout as the first step

The file is dense, but most of the density is content and repetition, not
layout sophistication.

### Effect on Legacy Composer, MiniLayout, and Yoga

This legacy evidence helps all three ideas, but not equally.

#### It helps the legacy composer most because

- the source format is already authored as a fixed composition
- section-level `screenRect` plus local origins map naturally to a composed
  subtree
- one uniform scale-and-offset transform preserves the original intent better
  than re-laying everything through Flexbox

#### It helps MiniLayout because

- much of `Menu1.xml` looks like rows, columns, grouped panels, and repeated
  form-like structures
- a lot of current absolute positioning could be reduced by container layout
  plus local alignment
- the framework does not need a browser-grade layout engine to improve on the
  current state substantially

#### It helps Yoga because

- repeated dropdown/list compositions and option-heavy menus suggest that
  deeper nesting will matter
- a future responsive reinterpretation of these screens may want grow/shrink,
  min/max size, wrapping, and richer alignment behavior
- Yoga's measurement hooks are relevant once labels and variable text lengths
  matter more than the original fixed 800x600 assumptions

### Bottom-line reading of `Menu1.xml`

`Menu1.xml` pushes me first toward a custom composer, second toward local
framework layout, and only distantly toward Yoga.

It pushes me toward this sequencing:

1. build a legacy menu composer with uniform 800x600 scaling and pillarboxing
2. keep authored absolute placement as the default for imported legacy menus
3. introduce `MiniLayout` where new framework-owned screens benefit from real
   container layout
4. revisit Yoga only if the framework starts hitting real limits around nested
   responsive layout, wrapping, and flex behavior

## Integration Shape If We Choose A Legacy Composer

If the framework chooses the legacy-composer-first path, the likely shape is:

1. parse a menu section from the existing XML schema
2. map records like `STATIC_DATA`, `BUTTON_DATA`, `DROPDOWN_DATA`,
   `LISTBOX_DATA`, and `SLIDER_DATA` into framework nodes
3. attach all authored positions and sizes in unscaled 800x600 local space
4. mount the whole menu subtree under a `LegacyMenuRoot` that applies:
   - uniform scale
   - viewport centering offset
   - black-bar presentation outside the content rect
5. preserve composite controls such as dropdown button + listbox ownership

This yields exact reproduction without forcing a new layout language onto old
data.

## Integration Shape If We Choose MiniLayout

If the framework chooses `MiniLayout`, the likely shape is:

1. move layout primitives into `Framework`
2. add layout participation fields to `Control`
3. add preferred-size measurement hooks
4. build `VBoxContainer`, `HBoxContainer`, and likely a padded container
5. adapt `MiniLayout` math into framework-owned measure/arrange passes

This is the shortest path to deleting a large amount of manual coordinate code
for new screens. It is not the primary import path for exact legacy menus.

## Integration Shape If We Choose Yoga

If the framework chooses `Yoga`, the likely shape is:

1. add a Yoga native dependency
2. add a managed wrapper or binding strategy for C#
3. create a framework `LayoutNode` or `YogaLayoutHandle` layer that mirrors
   `Control` state into Yoga nodes
4. use Yoga measure callbacks for content-sized controls
5. run Yoga layout, then write results back into framework `Control.Position`
   and `Control.Size`
6. add dirty propagation when text, font, children, or size policy changes

That is viable, but the bridge layer is not optional. Yoga would not replace
the framework's layout API, and it would not replace the need for a legacy
composer either. It would sit underneath a newer responsive layout layer.

## Cost

### Cost of MiniLayout path

This is mostly framework work:

- API design on `Control`
- container controls
- measurement
- invalidation
- scene conversion
- tests

That is real work, but it is local work, and it directly improves framework
coherence.

### Cost of Yoga path

This includes all of the framework work above, plus:

- native dependency management
- C API binding or wrapper maintenance
- build pipeline changes
- ownership/lifetime bridging
- more difficult debugging

So Yoga is not a way to avoid framework design. It is a way to buy a stronger
layout engine at the cost of more integration.

## Recommendation

Right now the best practical path is:

1. build a custom legacy menu composer for `Menu1.xml`-style authored screens
2. use one uniform 800x600-to-viewport scale with centered pillarboxing for
   1080p-class presentation
3. build `MiniLayout` as the framework-native layout layer for newly authored
   screens
4. keep Yoga as the stronger third option if the feature target grows into
   richer flexbox behavior, wrapping, and more demanding nested responsive cases

That recommendation is mainly about timing and cost:

- the legacy composer solves the unavoidable import problem directly
- `MiniLayout` improves the framework without adding native dependency cost
- Yoga is credible, but expensive enough that it should be adopted for clear
  feature reasons, not just because it provides elegant premade math

## Suggested Next Step

The next proof should be implementation-oriented, not another survey:

1. implement a `LegacyMenuRoot` viewport transform with:
   - base size `800x600`
   - uniform scale-to-fit
   - centered offset
   - black-bar fill
2. materialize one small `Menu1.xml` section exactly from authored coordinates
3. then add layout primitives and size policy to `Control`
4. build `VBoxContainer`
5. teach `TextNode` and `ButtonNode` to report preferred size

After that proof, it will be much easier to judge whether the framework still
needs Yoga at all.

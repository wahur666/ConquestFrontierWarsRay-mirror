# Frontend of Conquest

Date: 2026-08-20

## Purpose

This document defines the first frontend architecture for porting the original
Conquest: Frontier Wars menu/UI into the current raylib-based framework.

Goals:

1. reproduce the authored frontend faithfully
2. render it through the current framework
3. scale the original `800x600` screen into a centered modern viewport with
   black side bars
4. keep the port simple enough that later `MiniLayout` work stays optional

## Current Delivery Scope

The current migration target is intentionally narrow:

1. render the `Menu1.xml` `opening` screen
2. render it as one legacy-authored `800x600` surface inside the current
   raylib framework
3. support only the controls needed by that opening screen
4. stop before modal flow, submenu flow, launcher flow, profile flow, and
   network flow

Out of scope for this slice:

- `DoMenu_*` transitions
- modal dialogs
- submenus such as single-player, options, help, campaign, or network screens
- lobby/bootstrap behavior from `Menu1.cpp`
- player-name validation, registry defaults, or render-device probing

This document therefore describes the long-term frontend shape, while the
first implementation milestone is only the visible `opening` screen.

## Core Decision

Start with a **fixed-position legacy composer**, not a dynamic layout engine.

Implications:

- `Menu1.xml` and similar data remain the source of truth
- controls are materialized at authored coordinates
- the whole menu subtree is scaled and centered as one composed surface
- legacy-facing names are acceptable where they improve traceability

`MiniLayout` stays relevant for later framework-native screens. It is not the
import path for the legacy menus.

## Base Presentation Model

The original frontend is authored in `800x600`. Records such as
`BUTTON_DATA`, `STATIC_DATA`, `ANIMATE_DATA`, `DROPDOWN_DATA`,
`LISTBOX_DATA`, and `SLIDER_DATA` already store positions and sizes in that
space.

The runtime should compose the menu in authored coordinates, then apply one
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

## Framework Shape

### `LegacyMenuRoot`

Use a dedicated root for legacy-composed screens.

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

Children keep authored coordinates. `LegacyMenuRoot` owns the viewport
transform.

## Composition Model

A legacy screen should be materialized as:

- one screen root node
- child controls created from typed GT data
- local positions copied directly from source XML/model values

This is a composition pass, not a layout pass.

## Data Path

The repo already has the right typed boundaries:

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

Do not invent a new schema for the port. Consume these records directly.

## Naming Policy

Optimize naming for **traceability back to the original frontend data**.

If a legacy record maps cleanly onto an existing framework control, a thin
legacy-facing wrapper is acceptable.

Example:

- `BUTTON_DATA` can map to a thin `LegacyButton` over `ButtonNode`
- `STATIC_DATA` can map to a thin `LegacyStatic` over `TextNode`, `PanelNode`,
  or image/static surfaces depending on archetype

Wrapper rules:

- preserve the legacy-facing name
- avoid adding unrelated behavior
- only adapt construction or data binding if needed

## Record Mapping Strategy

Map legacy records to framework nodes as directly as possible.

### `BUTTON_DATA`

- simple framework button node
- positioned directly from `XOrigin` / `YOrigin`
- size taken from archetype or explicit area when present

### `STATIC_DATA`

- static text node
- image/static panel node
- or combined image+text static surface depending on archetype

Do not reduce `STATIC_DATA` to "just a label." Some records are effectively
image panels with optional text.

### `ANIMATE_DATA`

Relevant references:

- [GT_SHARED_STRUCTS.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestSharp/Common/Models/GT/GT_SHARED_STRUCTS.cs)
- [Animate.cpp](/D:/git2/Conquest-Frontier-Wars-Source2/src/Conquest/Animate.cpp) shows the old control:
  - derives from a rectangular UI base
  - resolves an animation archetype
  - computes screen rect from origin + frame size
  - advances cells over time
  - supports looping/pause
  - optionally uses a fuzz effect

- [Animate!!Multi.xml](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/assets/DB/xml/GenData.db/GT_ANIMATE/Animate!!Multi.xml)
  maps `Animate!!Multi` to `VFXShape!!AnimateMulti`
- [animMulti_atlas.json](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/assets/interface/animMulti_atlas.json)
- [animMulti_atlas.png](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/assets/interface/animMulti_atlas.png)

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
- it draws one atlas frame through `Sprite` or the same `Texture2D` /
  `AtlasTexture` path

Do not port:

- archetype factory plumbing
- DirectDraw/GDI assumptions
- talking-head fuzz behavior into framework core

Fuzz can stay app-specific and optional.

### `LISTBOX_DATA`

- list surface with fixed authored bounds
- preserve authored placement and visible area
- add scroll/selection behavior as needed

### `DROPDOWN_DATA`

The source data already models dropdowns as a composite:

- outer screen rect
- button data
- listbox data

- `LegacyDropdown` owns one button node and one listbox node
- preserve source-local offsets exactly

### `SLIDER_DATA`

- fixed-position slider node
- preserve authored bounds/origin

## Archetype Resolution

A legacy control is defined by both its record and its referenced archetype:

- `Button!!...`
- `Static!!...`
- `Dropdown!!...`
- `ListBox!!...`
- `Slider!!...`
- `Animate!!...`

The composer should therefore:

1. materialize control placement from the GT record
2. resolve visual/behavioral archetype data from the referenced type id

## Recommended Runtime Layers

1. `LegacyDataReader`: consumes parsed GT data and produces in-memory records
   for one menu screen
2. `LegacyArchetypeResolver`: consumes type ids like `Animate!!Multi` or
   `Button!!Back2D` and produces visual metadata, atlas/image/frame data, and
   default dimensions
3. `LegacyMenuComposer`: consumes records plus archetype metadata and produces
   a framework node subtree under `LegacyMenuRoot`

## What Should Reuse Existing Framework Nodes

Reuse framework nodes where behavior already matches:

- `ButtonNode`
- `TextNode`
- `PanelNode`
- `DropdownNode`
- `SliderNode`
- `Sprite`

Add thin legacy types when the semantic gap is otherwise awkward:

- `LegacyButton`
- `LegacyStatic`
- `LegacyAnimate`
- `LegacyListBox`
- `LegacyDropdown`

## What Should Not Be Framework-Core

Keep these out of framework core on the first pass:

- GT/archetype factory plumbing
- legacy hintbox/status/history coupling
- talking-head fuzz/video-border effects
- legacy modal runtime behavior copied literally
- launcher/network/profile workflow logic

## Relationship To MiniLayout

`MiniLayout` still has a place, but not as the legacy importer.

Recommended order:

1. get exact legacy menus rendering correctly
2. stabilize component mapping and archetype resolution
3. then experiment with replacing some newly-authored screens using
   `MiniLayout`
4. only use dynamic layout where it clearly improves maintainability without
   breaking fidelity

## Initial Implementation Slice

Smallest credible slice:

1. add `LegacyMenuRoot`
2. add one legacy screen composer for the `opening` section of `Menu1.xml`
3. support:
   - `STATIC_DATA`
   - `BUTTON_DATA`
   - `ANIMATE_DATA`
4. drive `ANIMATE_DATA` from atlas-based frame playback
5. render at `1920x1080` using centered pillarboxing
6. do not yet open modals or submenus when buttons are pressed

This proves:

- fixed authored coordinates
- viewport transform
- asset replacement via atlas
- 1:1 record-to-control materialization
- a clean separation between display migration and later frontend behavior

## Follow-Up Slice

After the opening screen renders reliably:

1. add button interaction wiring for screen-local focus and hover state
2. choose the next visible `Menu1` section to compose
3. add `DROPDOWN_DATA`
4. add `LISTBOX_DATA`
5. add `SLIDER_DATA`
6. formalize archetype-resolver interfaces
7. evaluate whether legacy wrapper names improve code clarity enough to keep
   them permanently

## Trim UI Node Inventory

The following sections copy the Trim tracker wording verbatim for the
UI-facing controls, overlays, and screen shells. This is the working
"what is what" inventory for frontend porting.

### Primitive And Reusable Controls

- [x] `Animate.cpp`: archetype-backed animated UI/hud control that derives from `BaseHotRect`, loads frame shapes through `IShapeLoader`, advances cells on `CQE_UPDATE`, supports indexed sequences, looping, pause, deferred destruction, and optional talking-head fuzz/frame effects. Main types: `ANIMATETYPE`, `Animate`, `AnimateFactory`. Key deps: `BaseHotRect`, `DAnimate`, `GenData`, `DrawAgent`, `IShapeLoader`, `TManager`. Replacement: framework/app `AnimatedImageControl` or `SpriteAnimationPlayer` with frame-list resources and optional overlay effects; keep archetype/factory plumbing out of framework core. Port Priority: Medium. Notes: this is a real reusable control, but the talking-head fuzz path is app-specific. Confidence: `Observed`.
  - Implementation: [HotRectNode.cs](../src/Core.UI/src/HotRectNode.cs) [AnimatedSprite2D.cs](../src/Framework/src/AnimatedSprite2D.cs)
- [x] `Button2.cpp`: core general-purpose button control with keyboard focus (`IKeyboardFocus`), mouse/keyboard press handling, repeater-button mode, optional dropdown-arrow rendering, text or string overrides, and two render paths: shape-file skinned or primitive-drawn. It posts `CQE_BUTTON`, including a high-bit variant for toggle/release cases, and repeats while held for repeater buttons. Main types: `BUTTONTYPE`, `Button2`, `ButtonFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `HOTKEY`, archetype data in `DButton`. Replacement: framework `ButtonControl` with focus/pressed/disabled/toggle/repeater policies and skin/style separation; dropdown-arrow behavior should be a style flag, not a separate control family. Port Priority: High. Notes: this is the primary reusable button baseline for many menu screens. Confidence: `Observed`.
  - Implementation: [LegacyButtonNode.cs](../src/Core.UI/src/LegacyButtonNode.cs)
- [ ] `HotButton.cpp`: lightweight hot-rect button used heavily by gameplay UI; loads image states via `IShapeLoader`, supports left/right/double-click dispatch, optional hotkey posting, push/highlight/context-menu behavior, and hover-owned cursor/status/hint resources. Main types: `HotButton`, `HotButtonFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `IShapeLoader`, `HOTKEY`, `EVENTSYS`, `STATUS`, `SFXMANAGER`, `IInterfaceManager`. Replacement: framework `ImageButtonControl` plus shared hover-resource/status-help plumbing; left/right/double-click command routing should come from generic pointer events. Port Priority: High. Notes: this is a second major reusable button base alongside `Button2.cpp`, biased toward image buttons and gameplay HUD interactions. Confidence: `Observed`.
- [ ] `Icon.cpp`: simple image/icon display with optional tooltip ownership; draws one `IDrawAgent`, updates status text on hover, and otherwise has no command behavior. Main types: `ICONTYPE`, `Icon`. Key deps: `BaseHotRect`, `GenData`, `IShapeLoader`, `DrawAgent`, `STATUS`. Replacement: framework `IconControl` / `ImageControl` with optional tooltip text and hit-target support. Port Priority: Medium. Confidence: `Observed`.
- [x] `Static.cpp`: general static text/image display control with alignment modes, multiline measurement, optional background fill/hash, optional numeric roll-up animation, tooltip/hint ownership, and a "buddy control" mode that forwards hover/press state into an attached `IButton2`. Main types: `STATICTYPE`, `Static`, `StaticFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `IFontDrawAgent`, `IButton2`, `STATUS`, `SFXMANAGER`, archetype data in `DStatic`. Replacement: framework `LabelControl` / `PanelLabel` plus optional tooltip support; buddy-button forwarding should become explicit composition instead of embedded cross-control coupling. Port Priority: High. Notes: this is the main generic display primitive, not just a text label. The attached `GT_STATIC` sample (`Static!!Background.xml`) confirms a real asset can be shape-only (`mainscreen.shp` -> single 800x600 atlas frame, no font, `backgroundDraw=nodraw`, `backdraw=false`), so the replacement should allow pure image-panel usage without forcing text semantics. Confidence: `Observed`.
  - Implementation: [LegacyStaticNode.cs](../src/Core.UI/src/LegacyStaticNode.cs)
- [x] `ProgressStatic.cpp`: progress-text display that combines optional background fill/hash drawing, text alignment, animated numeric roll-up, and a true progress-meter fill based on `current/max`. It also changes text colors on focus. Main types: `PROGRESS_STATICTYPE`, `ProgressStatic`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `IFontDrawAgent`, `SFXMANAGER`, archetype data in `DProgressStatic`. Replacement: framework `ProgressBarControl` plus optional overlaid label/counter behavior; numeric roll-up should be a reusable text animation policy, not baked into every progress bar. Port Priority: Medium. Notes: more capable than a plain label, but still largely reusable framework UI. Confidence: `Observed`.
  - Implementation: [LegacyProgressStaticNode.cs](../src/Core.UI/src/LegacyProgressStaticNode.cs)
- [x] `Edit2.cpp`: single-line text input control with selection, caret blink, mouse drag selection, double-click word selection, insert/overwrite mode, simple copy/paste scratch buffer via Shift/Ctrl+Insert/Delete, optional toolbar/chat/locked-text behaviors, IME composition placement, and per-frame draw/update handling. Main types: `EDITTYPE`, `Edit2`, `EditFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, font resources via `IFontDrawAgent`, `HKEvent`/`HOTKEY`, toolbar/chat focus handoff from Batch 03 services. Replacement: framework `TextInputControl` with caret/selection model, key-text separation, IME support, optional behavior flags split into clearer policy/config hooks, and style resources separate from control logic. Port Priority: High. Notes: this is richer than a basic text field and carries legacy toolbar/chat-specific behavior that should not live in the core widget API unchanged. Confidence: `Observed`.
  - Implementation: [LegacyEditNode.cs](../src/Core.UI/src/LegacyEditNode.cs)
- [x] `Listbox.cpp`: scrollable selectable text list built on a linked-list item store; supports add/remove/update, per-item user data and color, keyboard caret movement, mouse hover selection, optional single-click activation, word-wrap break calculation, and optional owned scrollbar integration through `IScrollBarOwner`. Main types: `LISTBOXTYPE`, `LISTITEM`, `Listbox`, `ListboxFactory`. Key deps: `ScrollBar.cpp`, `BaseHotRect`, `GenData`, `DrawAgent`, font resources via `IFontDrawAgent`. Replacement: framework `ListView`/`SelectionListControl` with item model, selection state, optional activation-on-single-click, scroll viewport, and a separate scrollbar/scroll model instead of embedding linked-list storage in the widget. Port Priority: High. Notes: this is a core dependency for comboboxes, dropdowns, and several menu screens. Confidence: `Observed`.
  - Implementation: [LegacyListBoxNode.cs](../src/Core.UI/src/LegacyListBoxNode.cs) fine tuning needed
- [x] `ScrollBar.cpp`: owner-driven scrollbar with two arrow buttons, proportional thumb sizing from `scrollRange`/`viewRange`, thumb dragging with cancel-on-breakoff behavior, repeated page scrolling while held, optional horizontal mode, and draw paths for either art-driven or primitive skins. Main types: `SCROLLBARTYPE`, `ScrollBar`, `ScrollBarFactory`. Key deps: primitive button control via `IButton2`, `BaseHotRect`, `GenData`, `DrawAgent`, owner callbacks from `Listbox.cpp` through `IScrollBarOwner`. Replacement: framework `ScrollBarControl` backed by a shared scroll model, with arrow buttons and track/thumb input split cleanly from view ownership. Port Priority: High. Notes: the key contract is the owner callback model and thumb math, not the legacy connection-point plumbing. Confidence: `Observed`.
  - Implementation:  [LegacyScrollBarNode.cs](../src/Core.UI/src/LegacyScrollBarNode.cs)
- [x] `Slider.cpp`: discrete slider control with keyboard arrow support, drag-to-step behavior, optional deferred event emission until mouse release, vertical or horizontal orientation, and art-driven or primitive rendering for track and thumb. Main types: `SLIDERTYPE`, `Slider`, `SliderFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `HKEvent`. Replacement: framework `SliderControl` with value range, orientation, immediate-vs-commit change policy, and styleable thumb/track visuals. Port Priority: Medium. Notes: current tracker summary was broadly correct; the important detail is that this slider snaps by integer step rather than tracking a continuous float. Confidence: `Observed`.
  - Implementation: [LegacySliderNode.cs](../src/Core.UI/src/LegacySliderNode.cs)
- [x] `TabButton.cpp`: tab header control plus per-tab focus router; draws tab states, forwards child control events upward, owns selected/highlight state, optionally cycles tabs on `Tab`, moves focus among registered `IKeyboardFocus` children with arrow keys, and hides child interaction when the tab is inactive. Main types: `TABBUTTONTYPE`, `TabButton`, `TabButtonFactory`. Key deps: `TabControl.cpp`, `BaseHotRect`, `GenData`, `DrawAgent`, `SFX`, `Frame`, child controls implementing `IKeyboardFocus`. Replacement: tab-header item plus tab-page focus scope in framework `TabContainer`, with page-local focus traversal handled by generic focus navigation instead of button-owned child lists. Port Priority: High. Notes: this file does more than paint a tab button; it partly owns tab-page focus behavior. Confidence: `Observed`.
  - Implementation: [LegacyTabButtonNode.cs](../src/Core.UI/src/LegacyTabButtonNode.cs)
- [x] `TabControl.cpp`: tab-strip container that instantiates `ITabButton` children from image resources, tracks selected tab, toggles each tab button's selected state, exposes per-tab child menu surfaces through `GetTabMenu`, forwards child control events upward, and supports per-tab default focus targets. Main types: `TABTYPE`, `TabControl`, `TabControlFactory`. Key deps: `TabButton.cpp`, `BaseHotRect`, `GenData`, `IShapeLoader`, `IImageReader`. Replacement: framework `TabContainer` with tab headers, selected page state, page content nodes, and default-focus-per-page support. Port Priority: High. Notes: this is the actual tab-page coordinator; `TabButton.cpp` is only half of the behavior. Confidence: `Observed`.
  - Implementation:[LegacyTabControlNode.cs](../src/Core.UI/src/LegacyTabControlNode.cs)
- [x] `Combobox.cpp`: composite edit-plus-list selector; creates `IButton2`, `IListbox`, and `IEdit2` children from archetype data, forwards most list/edit APIs, auto-completes typed prefixes, toggles drop state, and swaps keyboard focus between edit field and dropped list. Main types: `Combobox`, `ComboboxFactory`. Key deps: `Edit2.cpp`, `Listbox.cpp`, primitive button control via `IButton2`, `BaseHotRect`, `GenData`, `DrawAgent`. Replacement: framework `ComboBoxControl` composed from `TextInput`, popup `ListView`, and trigger button, with explicit popup ownership, focus transfer, and selection/change events. Port Priority: High. Notes: current tracker summary was correct but understated the editable/autocomplete behavior. Confidence: `Observed`.
  - Implementation: [LegacyComboboxNode.cs](../src/Core.UI/src/LegacyComboboxNode.cs)
- [x] `Dropdown.cpp`: non-editable dropdown selector; creates button and list child controls, mirrors the listbox API, owns open/close state, updates button text from selected list entry, and manually treats either button or list hover as alert state while dropped. Main types: `Dropdown`, `DropdownFactory`. Key deps: `Listbox.cpp`, primitive button control via `IButton2`, `BaseHotRect`, `GenData`, `DrawAgent`. Replacement: framework `DropdownControl` with read-only display button plus popup `ListView`, sharing most popup/focus mechanics with combobox but without text entry. Port Priority: High. Notes: sibling of `Combobox.cpp`, not a separate screen shell. Confidence: `Observed`.
  - Implementation:  [LegacyDropdownNode.cs](../src/Core.UI/src/LegacyDropdownNode.cs)

### App-Level And HUD Widgets

- [ ] `BuildButton.cpp`: specialized production/build command button implementing both `IHotButton` and `IActiveButton`; tracks tech gating, queue count, build cost, build-mode highlight, no-money overlay, and percent/stall progress, then posts `CQE_LHOTBUTTON` / `CQE_RHOTBUTTON` to its parent and writes detailed cost/status text into `STATUS`. Main types: `BuildButton`, `BBUTTONSTATE::STATE`. Key deps: `BaseHotRect`, `IShapeLoader`, `DrawAgent`, `IActiveButton`, `DSpaceship` / `DPlatform` mission data, `ObjList`, `HOTKEY`, `STATUS`, `SFXMANAGER`. Replacement: app-level `BuildCommandButton` on top of a reusable framework button/image primitive plus explicit command/state view model for availability, queue, costs, and progress. Port Priority: High. Notes: not just a skinned button; it already mixes command semantics, tooltip composition, and progress rendering. Confidence: `Observed`.
- [ ] `ResearchButton.cpp`: specialized research/upgrade command button implementing `IHotButton` and `IActiveButton`; resolves research/admiral/upgrade costs from `DResearch` data, tracks current tech/upgrade eligibility, queue count, and progress/no-money overlays, and writes detailed status/cost text on hover. Main types: `ResearchButton`. Key deps: `BaseHotRect`, `IShapeLoader`, `DrawAgent`, `IActiveButton`, `DResearch`, `HOTKEY`, `STATUS`, `SFXMANAGER`. Replacement: app-level `ResearchCommandButton` over framework button/image primitives and explicit research-state view models. Port Priority: High. Notes: sibling to `BuildButton.cpp`; similar framework needs, different domain state rules. Confidence: `Observed`.
- [ ] `DiplomacyButton.cpp`: two-state diplomacy relation button with keyboard focus, specialized shape-state rendering, and a dedicated `CQE_DIPLOMACYBUTTON` click message. Its visual state depends on enabled/pressed/hover/focus plus two stored diplomacy flags. Main types: `DIPBUTTONTYPE`, `DiplomacyButton`, `DiplomacyButtonFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `HOTKEY`, archetype data in `DDiplomacyButton`. Replacement: app-level `DiplomacyRelationButton` or segmented/toggle control with explicit two-party relation state, built atop framework button/focus primitives. Port Priority: Medium. Notes: more domain-specific than `Button2.cpp`; probably not framework-core. Confidence: `Observed`.
- [ ] `HotStatic.cpp`: specialized read-only tech meter display that draws repeated full/empty bar icons plus optional caption text; exposes `SetImageLevel` and `SetTextString` rather than click behavior. Main types: `HotStaticArchetype`, `HotStatic`. Key deps: `BaseHotRect`, `GenData`, `IShapeLoader`, `DrawAgent`, `IFontDrawAgent`, archetype data in `DHotStatic`. Replacement: framework/app `TechLevelIndicator` or `IconMeter + Label` composite control, not a generic static text primitive. Port Priority: Low. Notes: prior summary overstated interactivity; this file is mostly a specialized display widget. Confidence: `Observed`.
- [ ] `ShipSilButton.cpp`: gameplay selection widget that renders a ship silhouette in green/yellow/red based on the bound ship state and uses click/shift-click to mutate object selection rather than posting a generic UI command. It also drives status text from the bound ship or fallback tooltip text. Main types: `ShipSilButton`. Key deps: `BaseHotRect`, `IShapeLoader`, `DrawAgent`, `MPart`, `ObjList`, `IBaseObject`, `HOTKEY`, `STATUS`. Replacement: app-level HUD selection item built from an image hit-target plus explicit selection presenter/service; not a framework-core button. Port Priority: Medium. Notes: prior summary was directionally right but understated that this is really gameplay-selection UI. Confidence: `Observed`.
- [ ] `QueueControl.cpp`: specialized build-queue strip that draws queued unit/build icons, overlays progress on the first slot, tracks stall state, highlights hovered entries, and posts removal messages for clicked queue slots to the toolbar. Main types: `QUEUECONTROLTYPE`, `QueueControl`, `QueueControlFactory`. Key deps: `BaseHotRect`, `DrawAgent`, `IActiveButton`, toolbar messaging, defaults/editor pause state. Replacement: app-level `BuildQueueControl` composed from icon cells plus progress/stall overlays, likely outside the core framework library except for reusable icon-button/progress primitives. Port Priority: Medium. Notes: prior summary was directionally right but too generic; this is production-queue UI, not a reusable list control. Confidence: `Observed`.

### Overlays, Input, And Runtime Frontend Services

- [ ] `Cursor.cpp`: owns cursor resource loading, software/hardware cursor switching, DirectInput mouse capture, synthetic mouse event dispatch, busy cursor state, and screenshot hotkey handling; main types: `Cursor`, `group_cursor_entry`, `cursor_entry`, `_cursor`; key deps: `Hotkeys`, `DBHotkeys`, `DrawAgent`, `VideoSurface`, `WindowManager`, `EventSys2`, `BaseHotRect`, `UserDefaults`; replacement: framework/app cursor service plus input backend mouse capture and optional software-cursor renderer, not a control. Port Priority: High. Notes: this is a core input bridge, not just cursor art. Confidence: `Observed`.
- [ ] `HKManager.cpp`: installs normal/debug hotkey tables from `DAHOTKEY` resources, forwards window messages into `HOTKEY`/`DBHOTKEY`, and attaches the debug hotkey filter; main types: `HKManager`, `_hkmanager`; key deps: `EventSys2`, `WindowManager`, `HKEvent`, global `HOTKEY`/`DBHOTKEY`; replacement: framework-level action map/input router with layered contexts for gameplay vs debug bindings. Port Priority: High. Confidence: `Observed`.
- [ ] `Hintbox.cpp`: runtime hint overlay resource that tracks current hint text, lead/trail timing, font reload across 2D/3D mode changes, and bottom-of-screen drawing; main types: `HintResource`, `HistoryNode`, `_hintbox`; key deps: `DrawAgent`, `VideoSurface`, `UserDefaults`, `BaseHotRect`, `EventSys2`; replacement: tooltip/status-help overlay control or HUD service driven by hover ownership and frame UI rendering. Port Priority: Medium. Notes: more like delayed status help than a generic tooltip popup. Confidence: `Observed`.
- [ ] `StatusBar.cpp`: global status-bar resource/service, not a child control; stores current text/name/mode, reloads fonts when switching 2D/3D, draws directly at the bottom of the screen during `CQE_ENDFRAME`, and supports tooltip/build/default/name display modes. Main types: `StatusBarResource`, global `_status`. Key deps: `TResource`, `BaseHotRect`, `DrawAgent`, `UserDefaults`, `VideoSurface`, `EventSys2`, `FULLSCREEN`. Replacement: framework/app status-overlay service or HUD layer with a small model (`text`, `secondary name`, `mode`, `toolbar offset`) rather than a Win32-era global resource singleton. Port Priority: High. Notes: many Batch 03/04 controls depend on this hover-status channel. Confidence: `Observed`.
- [ ] `Teletype.cpp`: global scrolling/typewriter text system that splits strings into multiple `TeletypeLine`/`TeletypeObj` instances, reveals characters over time with caret blink and per-character SFX, supports lifetime and pause-ignoring behavior, and renders/updates from fullscreen event callbacks. Main types: `TeletypeLine`, `TeletypeObj`, `Teletype`, global `_teletype`. Key deps: `ITeletype`, `DrawAgent`, `Frame`, `Hotkeys`, `SFX`, `FULLSCREEN`, `EventPriority`. Replacement: framework/app transient text-overlay service or `TypewriterTextControl` plus manager for queued/lifetime-based overlays. Port Priority: Medium. Notes: broader than a single control; it behaves like a global overlay text channel. Confidence: `Observed`.
- [ ] `InProgressAnim.cpp`: threaded loading/progress presenter, not just a spinner; builds a background static plus foreground animation via `GENDATA`, owns a worker thread, draws a progress bar shape and status string, and manually drives `CQE_UPDATE` / `CQE_ENDFRAME` on child components while coordinating with `VideoSurface`/pipeline critical sections. Main types: `IPAnim`. Key deps: `IAnimate`, `IStatic`, `GenData`, `VideoSurface`, `DrawAgent`, `IDDBackDoor`, `EventSys2`, global pipeline/window critical-section helpers. Replacement: app/framework loading-overlay service or modal loading screen with main-thread update/render, explicit progress model, and no ad hoc worker-owned UI rendering. Port Priority: High. Notes: this should not be ported literally into the control tree. Confidence: `Observed`.
- [ ] `menu.cpp`: Win32 menu-bar/resource service for the running app: toggles menu visibility from Alt timing, handles menu commands and debug dialogs, manages pause state, quick load/save, part-selection dialog, and resolves data/search paths for interface/media/content folders; main types: `MenuResource`, `partDlgSaveStruct`, `_menu`; key deps: `StatusBar`, `Hotkeys`, `DBHotkeys`, `NetBuffer`, `NetPacket`, `Mission`, `Frame`, `Camera`, `ScrollingText`, `WindowManager`; replacement: split across app shell services: command menu/debug menu, content-path/config bootstrap, pause controller, and optional dev dialogs. Port Priority: High. Notes: not the shared `Menu_*` screen base; mostly in-game shell/runtime integration. Confidence: `Observed`.
- [x] `Modal.cpp`: owns the modal loop: frame timing, `CQE_UPDATE`/render/flush dispatch, end-of-frame 2D composition, scene begin/end, swap/clear, and modal result capture via `CQE_DLG_RESULT`; main types: `ModalEventCallback`, `_modalfactory`; key deps: `ObjList`, `DrawAgent`, `VideoSurface`, `Hotkeys`, `SoundManager`, `IBackground`, `NetPacket`, `Sector`, `System`, `EventSys2`, `WindowManager`, `RendPipeline`; replacement: framework modal runner integrated with main loop, screen stack, per-frame update/render phases, and result futures/callbacks. Port Priority: High. Notes: current runtime now has the first reusable authored modal substrate through `LegacyModalNode`, but not the full original modal runtime contract yet. Confidence: `Observed`.
  - Implementation: [LegacyMessageBoxModal.cs](../src/Conquest/src/Frontend/LegacyMessageBoxModal.cs)
- [x] `MovieScreen.cpp`: fullscreen modal movie player screen; loads a video through `VIDEOSYS`, updates frames on `CQE_UPDATE`, draws a letterboxed textured video quad on `CQE_ENDFRAME`, suppresses frame limiting, hides the cursor, and temporarily mutes music during modal playback. Main types: `MenuMovie`, `MovieScreen()` entrypoint. Key deps: `Frame`, `VideoSystem`, `SoundManager`, `Mission`, `Cursor`, modal runtime. Replacement: app-level `VideoModalScreen` built on a framework screen/modal stack plus a video playback service. Port Priority: Medium. Notes: more screen flow than framework media primitive. Confidence: `Observed`.
  - Implementation: [MovieScene.cs](../src/Core.UI/src/MovieScene.cs)

### Screen Shells And Frontend Flows

- [ ] `Menu1.cpp`: concrete front-end main menu screen built from generated archetype data; creates button/static/animation controls, manages focus/hover animation, player-name/profile setup, render-device defaults, lobby startup, and transitions into SP/MP/options/help flows; main types: `dummy_menu1`, `Menu1`; key deps: `Frame`, `IButton2`, `IStatic`, `IAnimate`, `GENDATA`, `Mission`, `SoundManager`, `MusicManager`, `IGameProgress`, `ZoneLobby`, downstream `DoMenu_*` screens, `Modal`; replacement: framework screen node composed from buttons/images/animations plus a launcher/profile/network presenter. Port Priority: High. Confidence: `Observed`.
- [ ] `Menu_Briefing.cpp`: scripted mission-briefing modal implementing `IBriefing`; loads briefing data from `Mission`, plays teletype text, animated comm portraits, streamed audio, and optional replay/start/cancel flow while ticking mission state in the modal. Main types: `MenuBriefing`, `IBriefing`. Key deps: `Mission`, `MScript`, `ITeletype`, `SoundManager`, `MusicManager`, `Streamer`, `IAnimate`, `IButton2`, `IStatic`. Replacement: app-level briefing screen presenter with timeline/script playback services for audio, subtitles/teletype, portrait/video slots, and mission-start actions. Port Priority: High. Notes: substantially richer than a text briefing page; it is a small scripted media runtime. Confidence: `Observed`.
- [x] `Menu_campaign.cpp`: single-player campaign entry screen; shows player name from defaults, selects race/training branch, optionally skips straight into Terran mission flow, checks `GAMEPROGRESS`, and hands off to `DoMenu_mission` or `DoMenu_Briefing`. Main types: `Menu_campaign`. Key deps: `Mission`, `IGameProgress`, `MusicManager`, `UserDefaults`, `Menu_mission.cpp`, `Menu_Briefing.cpp`. Replacement: single-player campaign chooser screen with explicit campaign/training actions and profile/progression service injection. Port Priority: Medium. Notes: despite the name, this is mostly a front-door router into campaign/training flows, not a full progression UI by itself. Confidence: `Observed`.
- [x] `Menu_Confirm.cpp`: generic in-game/custom message box wrapper over archetyped Trim controls; centers the modal, supports `MB_OK`, `MB_OKCANCEL`, and `MB_YESNO`, pauses single-player while open, disables edge-scroll, forces cursor ownership, and falls back to Win32 `MessageBoxW` if Trim archetype data is unavailable. Main types: `Menu_CQMessageBox`, overloaded `CQMessageBox` helpers. Key deps: `GENDATA`, `Cursor`, `MScroll`, `Mission`, `EventSys2`, `IButton2`, `IStatic`. Replacement: framework/app modal dialog service with typed button sets and async result handling, plus app-owned pause/input suppression policy. Port Priority: High. Notes: current runtime now has the first authored replacement path through `LegacyMessageBoxModal` for the `Menu1` quit confirm flow; broader reuse is still pending. Confidence: `Observed`.
- [x] `Menu_Credits.cpp`: scrolling credits modal that reads `credits.txt`, parses lightweight line markers into title/name rows, duplicates font draw agents for foreground/shadow text, scrolls rows upward over a static background, and exits on click or Esc. Main types: `CreditText`, `Menu_Credits`. Key deps: `IStatic`, `IFontDrawAgent`, `DrawAgent`, `MusicManager`. Replacement: lightweight credits/splash screen with streamed text model and text-style resources; keep parsing/data outside framework-core. Port Priority: Low. Notes: current runtime now has a dedicated `MenuCreditsScene` with `Credits.txt` parsing, authored background rendering, click/Esc exit, and mystery-track playback. Confidence: `Observed`.
- [x] `Menu_final.cpp`: final multiplayer/skirmish staging pane layered over child map/slot screens; coordinates accept/start/cancel flow, ready-state countdown, host validation rules, latency resync, exit/drop confirmation, and focus handoff between sibling groups before game start. Main types: `Menu_final`. Key deps: `ICQGame`, `NetBuffer`, `NetPacket`, `Menu_map.cpp`, `Menu_slots.cpp`, `Menu_Confirm.cpp`, `IButton2`, `IStatic`. Replacement: multiplayer final-check screen/presenter with validation rules and countdown state in a session service, plus explicit child-panel composition instead of cross-frame focus wiring. Port Priority: High. Notes: prior summary understated that this file owns readiness validation and launch gating, not just a passive confirmation page. Confidence: `Observed`.
- [x] `Menu_help.cpp`: about/help modal, not a document browser; shows version and product ID/legal strings, plays main-menu music on focus, and opens `DoMenu_Credits`. Main types: `Menu_help`. Key deps: `MusicManager`, `UserDefaults`, `Menu_Credits.cpp`, `IButton2`, `IStatic`. Replacement: simple about/help modal with version/build/license fields and a credits action. Port Priority: Low. Notes: current runtime now has an authored `helpMenu` modal on top of the opening screen, including credits handoff and return-to-about behavior. Confidence: `Observed`.
- [ ] `Menu_igoptions.cpp`: in-game pause/options modal with save, load, options, restart, resign, abdicate, and return actions; launches nested save/load or options modals, pauses single-player while open, and gates buttons by single-player vs multiplayer state. Main types: `dummy_igoptions`, `Menu_igoptions`. Key deps: `Mission`, `Hotkeys`, `CreateOptionsMenu`, `CreateMenuLoadSaveSpecial`, `EventSys2`, `IButton2`, `IStatic`. Replacement: pause/options overlay composed from reusable buttons with app-level commands for save/load/settings/restart/resign. Port Priority: Medium. Notes: this is a command hub, not the main settings screen itself. Confidence: `Observed`.
- [ ] `Menu_LoadSave.cpp`: save/load modal for mission files; enumerates `*.mission` entries from `SAVEDIR`, filters SP vs MP saves by filename prefix, shows descriptions via `Mission`, edits save descriptions, handles overwrite/delete confirmation, and calls `MISSION->Load` / `SaveByDescription`. Main types: `MenuLoadSave`. Key deps: `Mission`, `IFileSystem`, `Menu_Confirm.cpp`, `IEdit2`, `IListbox`, `IButton2`, `IStatic`. Replacement: save/load dialog backed by a savegame service that exposes slots/metadata and command methods, with file naming conventions kept outside widget code. Port Priority: High. Notes: reusable shell behavior, but the file-system and naming policy should move behind a service boundary. Confidence: `Observed`.
- [x] `Menu_map.cpp`: multiplayer/skirmish map-and-rules settings panel over `ICQGame`; owns dropdowns/sliders/toggles for map type, template, money, terrain, units, visibility, speed, command points, difficulty, spectator/diplomacy/lock-settings flags, derives descriptions/max players from selected files, and opens `DoMenu_MapSelect` plus `DoMenu_slots`. Main types: `Menu_map`. Key deps: `ICQGame`, `Mission`, `MapGen`, `NetBuffer`, `Menu_MapSelect.cpp`, `Menu_slots.cpp`, `Dropdown.cpp`, `Slider.cpp`. Replacement: lobby rules/map settings panel with typed view models and validation, backed by session/map services rather than direct mission-file probing inside the screen. Port Priority: High. Notes: this is one of the main multiplayer composition panels and a strong consumer of framework dropdown/slider primitives. Confidence: `Observed`.
- [ ] `Menu_MapSelect.cpp`: modal map picker for three sources: random templates, supplied multiplayer maps, and saved multiplayer missions; enumerates files from `MPMAPDIR` and `SAVEDIR`, keeps a side list mapping supplied-map display rows to file names, updates `ICQGame` map type/name on selection, and returns the selected template string ID for random maps. Main types: `Menu_MapSelect`, nested `SuppliedFile`. Key deps: `ICQGame`, `Mission`, `IFileSystem`, `IListbox`, `IButton2`, `IStatic`. Replacement: map-selection modal with grouped sources and preview/metadata model, returning a typed selection object instead of string-ID/file-name side effects. Port Priority: Medium. Notes: more of a specialized chooser than a general-purpose browser; current summary was directionally right but too generic. Confidence: `Observed`.
- [ ] `Menu_mission.cpp`: single-player mission/movie progression screen; draws mission graph lines, unlocks nodes from `GAMEPROGRESS`, shows hover descriptions, lists mission files from disk, opens `DoMenu_Briefing` for the chosen mission, and can also expose unlock/debug shortcuts and movie buttons. Main types: `Menu_mission`, `LineVar`. Key deps: `Mission`, `IGameProgress`, `SFX`, `MusicManager`, `Menu_Briefing.cpp`, `IAnimate`, `IButton2`, `IListbox`, `IStatic`. Replacement: campaign progression screen with explicit node graph/presenter, mission metadata service, and separate movie unlock/playback handling. Port Priority: Medium. Notes: this is a bespoke campaign graph screen, not a generic mission list/detail layout. Confidence: `Observed`.
- [x] `Menu_mshell.cpp`: multiplayer staging shell implementing `ICQGame`; owns lobby state, chat entry/history, host/client setting replication, ready checks, player add/remove handling, Zone score upload, and transition into `DoMenu_nl` net loading or `DoMenu_final`. Main types: `Menu_mshell`, `CQGAME_PACKET`, `MAP_PACKET`, `CLIENTSETTING_PACKET`, `GRCHAT_PACKET`. Key deps: `Menu_netloading.cpp`, `NetBuffer`, `NetPacket`, `Mission`, `MusicManager`, `ZoneLobby`, `WindowManager`, chat controls via `IEdit2`/`IListbox`, and child slot/options surfaces through shared `ICQGame` state. Replacement: multiplayer lobby screen presenter plus child panels for slots/options/chat, backed by an async networking/session service rather than frame-owned packet code. Port Priority: High. Notes: this is the central multiplayer shell, not just a menu skin. Confidence: `Observed`.
- [x] `Menu_netconn.cpp`: transport/provider chooser for multiplayer entry; enumerates saved DirectPlay connections, describes each transport, launches `DoMenu_sess` for normal session flow, optionally launches `DoMenu_zone`, and can shell out to a web URL. Main types: `Menu_nc`. Key deps: `ZoneLobby`, `Menu_netsess.cpp`, `Menu_zone.cpp`, saved connection enumeration from networking globals. Replacement: network entry screen with explicit provider cards/actions and a service-backed provider/session bootstrap. Port Priority: Medium. Notes: includes obsolete Zone/web launch branching that should stay outside core framework widgets. Confidence: `Observed`.
- [x] `Menu_netloading.cpp`: non-rendering multiplayer load/download modal; coordinates checksum/random-seed handshake, map generation or mission load, file transfer progress, mission initialization, and failure/cancel signaling between host and clients. Main types: `Menu_nl`, nested `FTENUMERATOR`. Key deps: `Mission`, `NetPacket`, `NetBuffer`, `NetFileTransfer` channel APIs, `MusicManager`, `IPAnim`, and `ICQGame` data from `Menu_mshell.cpp`. Replacement: async loading screen/service boundary with progress model, host/client load state machine, and transfer callbacks separate from the screen node. Port Priority: High. Notes: broader than a progress screen; this is multiplayer load orchestration. Confidence: `Observed`.
- [x] `Menu_netsess.cpp`: first-stage multiplayer session setup screen; toggles join vs create, captures player name and optional TCP IP address via combobox, persists defaults, starts session enumeration, and branches to `DoMenu_sess2`. Main types: `Menu_sess`. Key deps: `Combobox.cpp`, `Session_Buffer` enumeration, DirectPlay connection setup, `Menu_netsess2.cpp`, `Menu_mshell.cpp`, `MusicManager`. Replacement: multiplayer setup screen with validated name/address form, join/create mode state, and a session-discovery service. Port Priority: High. Notes: not a session browser yet; this is the pre-browser setup step. Confidence: `Observed`.
- [x] `Menu_netsess2.cpp`: second-stage session browser/create screen; refreshes enumerated sessions, renders per-session description strings from embedded options, validates version/name conflicts, then joins or creates and enters `DoMenu_mshell`. Main types: `Menu_sess2`. Key deps: session enumeration buffers, DirectPlay open/create-player flow, `Menu_mshell.cpp`, options packed into session descriptors. Replacement: session list screen with polling refresh, session metadata view model, join/create actions, and validation surfaced by the networking service. Port Priority: High. Notes: the split between `Menu_netsess.cpp` and this file is real and should remain explicit in a modern flow. Confidence: `Observed`.
- [x] `Menu_newplayer.cpp`: modal player-profile name dialog; edits a name, validates reserved/duplicate cases against the `SavedGame\\` folder layout, and returns the accepted name to callers like options. Main types: `dummy_newplayer`, `Menu_newplayer`. Key deps: `Edit2.cpp`, file-system/profile directories, callers in `Menu_options.cpp`. Replacement: reusable modal text-entry dialog with validation callback supplied by the owning screen. Port Priority: Medium. Confidence: `Observed`.
- [x] `Menu_options.cpp`: large tabbed options screen covering player profile folders, audio volumes, gameplay/input toggles, render device/resolution/gamma, and VFX flags; previews some changes live, writes defaults/registry values, and invokes `DoMenu_newplayer` for profile add/rename. Main types: `NameList`, `ResEnum`, `PackedRes`, `dummy_options`, `Menu_options`. Key deps: `TabControl.cpp`, `Slider.cpp`, `Dropdown.cpp`, `Listbox.cpp`, `IGammaControl`, `IProfileParser`, `UserDefaults`, `Effects`, `Menu_newplayer.cpp`. Replacement: settings screen composed from reusable framework tabs, lists, sliders, toggles, and dropdowns, backed by typed settings services instead of direct registry/file mutations in widget code. Port Priority: High. Notes: mixes framework-worthy controls with legacy render-device plumbing; split those concerns during port. Confidence: `Observed`.
- [ ] `Menu_Pause.cpp`: pause/congestion overlay dialog; displays host/player pause status, boot countdowns, and resignation/quit outcomes, and in multiplayer polls `NetPacket` each frame for pause ownership and timers. Main types: `MenuPause`. Key deps: `NetPacket`, `Mission`, player enumeration callbacks, global pause/hotkey events. Replacement: pause modal overlay with a multiplayer pause-status presenter and result actions routed through app/game services. Port Priority: Medium. Notes: combines local pause menu behavior with network-congestion messaging. Confidence: `Observed`.
- [ ] `Menu_SlideShow.cpp`: shape-driven slideshow/splash modal; steps through frames on `CQE_UPDATE`, draws a single loaded shape each frame, optionally allows early exit, and also backs splash playback that can trigger process close on completion. Main types: `Menu_SlideShow`. Key deps: `IShapeLoader`, `IDrawAgent`, `MScript` `SPLASHINFO`, modal runtime. Replacement: lightweight slideshow/splash screen node or app bootstrap presenter for scripted image sequences. Port Priority: Low. Notes: preserve behavior, not the VFX-shape loading path. Confidence: `Observed`.
- [x] `Menu_slots.cpp`: multiplayer slot editor subpanel over `ICQGame`; manages per-slot state/open/AI difficulty, race, color/player identity, team assignment, ping/name display, host-only editability, boot-player flow, and AI auto-fill naming. Main types: `Menu_slots`. Key deps: `Dropdown.cpp`, `Menu_mshell.cpp` via `ICQGame`, `NetBuffer`, `Mission` color/name helpers. Replacement: reusable lobby slot-grid panel with per-slot view models, validation rules, and host-authority gating supplied by the multiplayer presenter. Port Priority: High. Notes: likely one of the child panels used inside the multiplayer shell; keep it app-level, not framework-core. Confidence: `Observed`.
- [x] `Menu_SPGame.cpp`: single-player entry screen for campaign, skirmish, saved-game load, and quick-battle load; manages save directory existence, counts available saves to enable buttons, and enters campaign, briefing, or local `DoMenu_mshell` skirmish flow. Main types: `Menu_SPGame`. Key deps: `Mission`, `MusicManager`, `Menu_campaign.cpp`, `Menu_mshell.cpp`, `Menu_Briefing.cpp`, load/save flows. Replacement: single-player hub screen with buttons/actions backed by savegame and game-start services. Port Priority: Medium. Notes: skirmish reuses the multiplayer shell path with no remote connection. Confidence: `Observed`.
- [ ] `Menu_SysKitSaveLoad.cpp`: modal save/load UI for per-system lighting kit data; lists files in `\\GT_SYSTEM_KIT`, edits a file name, reads or writes `GT_SYSTEM_KIT` records for the current sector, and pauses the game around the modal. Main types: `MenuSystemKitSaveLoad`. Key deps: `Sector`, `Mission`, `Edit2.cpp`, `Listbox.cpp`, file-system/document APIs. Replacement: specialized editor/debug asset dialog outside the general game UI framework. Port Priority: Low. Notes: narrow tool workflow, not a general save/load screen. Confidence: `Observed`.
- [ ] `Menu_Toolbar.cpp`: in-game toolbar runtime and context-menu loader; parses toolbar data records into concrete controls, owns multiple context toolbars/tabs, dispatches left/right/double-click events to a toolbar client, toggles visibility/focus, and integrates hint/status/debug behaviors with gameplay UI. Main types: `CONTROL_NODE`, `Menu_context`, `Menu_tb`, nested `createCallback`, `FullScreen`. Key deps: primitive toolbar controls (`HotButton`, `ProgressStatic`, `Edit2`, `HotStatic`, `TabControl`, `ShipSilButton`, `Icon`, `QueueControl`), `StatusBar`, `Hintbox`, `ObjList`, `SysMap`, `ScrollingText`, parser/view-constructor infrastructure. Replacement: app-level gameplay HUD/toolbar composition layer built from framework controls plus explicit command routing, not a parser-driven one-off inside the framework core. Port Priority: High. Notes: this is a major integration surface and should probably split into HUD shell, context panels, and control factories during port. Confidence: `Observed`.

## Final Recommendation

Begin with a **legacy-authored fixed-position renderer** over the current
framework.

Near-term architecture:

1. `LegacyMenuRoot` for authored-space scaling and centering
2. typed GT-record to framework-node composition
3. archetype resolution for visuals
4. atlas-backed animation replacement for `ANIMATE_DATA`
5. thin legacy-named wrappers where they improve traceability

After the frontend is reproduced reliably, use `MiniLayout` only for new
screens or selective refactors.

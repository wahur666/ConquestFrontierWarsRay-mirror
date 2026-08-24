# Menu1 Opening Migration Checklist

Date: 2026-08-23

## Purpose

This checklist tracks the first frontend migration milestone:

- render the `Menu1.xml` `opening` screen
- in authored `800x600` space
- through the current raylib framework
- without modal flow or submenu flow

This page is a progress tracker, not a design document. Architectural detail
belongs in [Frontend-of-Conquest.md](Frontend-of-Conquest.md).

## Scope

In scope for this milestone:

- one visible screen: `GT_MENU1.Opening`
- authored coordinate placement
- centered scale-to-fit presentation
- black pillarbox bars outside the composed content
- static background/title/legal/help text surfaces from the opening data
- opening buttons
- opening animated elements
- screen-local hover/focus visuals only if needed for correct presentation

Explicitly out of scope:

- modal dialogs
- submenus
- `DoMenu_*` screen transitions
- player profile/name validation
- registry/defaults migration
- render-device probing
- DirectPlay/lobby/network startup
- original-app-shell music/movie/bootstrap parity decisions

## Target Data Surface

The `opening` section in `Menu1.xml` contains:

- `screenRect`
- `background`
- `single`
- `multi`
- `options`
- `help`
- `intro`
- `quit`
- `staticSingle`
- `staticMulti`
- `staticOptions`
- `staticHelp`
- `staticIntro`
- `staticLegal`
- `animSingle`
- `animMulti`
- `animOptions`
- `animQuestion`
- `animMedia`

These records are sufficient for the first visible menu composition pass.

## Current Status

Completed foundation already present in the repo:

- [x] Typed `GT_MENU1` model exists
- [x] Typed `GT_MENU1` parser exists
- [x] `LegacyButtonNode` exists
- [x] `LegacyStaticNode` exists
- [x] `LegacyListBoxNode` exists
- [x] `LegacyDropdownNode` exists
- [x] `LegacySliderNode` exists
- [x] `LegacyScrollBarNode` exists
- [x] `AnimatedSprite2D` exists
- [x] VFX atlas-backed widget rendering exists in showcase scenes

Missing pieces for the opening-screen milestone:

- [x] `LegacyMenuRoot`
- [x] `Menu1` opening-screen composer
- [x] `ANIMATE_DATA` to runtime animation adapter
- [x] menu-opening scene boot path in the app
- [ ] opening-screen verification pass

Post-slice work now implemented beyond the original narrow opening-only scope:

- [x] `helpMenu` authored about modal is wired from the opening screen
- [x] `GT_CREDITS` full-screen credits scene is wired from the about modal
- [x] `Esc` on the opening screen opens an authored exit-confirm modal
- [x] `Quit` on the opening screen exits directly

## Work Checklist

### 1. Viewport Root

- [x] Add `LegacyMenuRoot`
- [x] Hardcode base resolution to `800x600`
- [x] Compute uniform scale from current window size
- [x] Compute centered viewport offset
- [x] Draw black bars outside the content rect
- [x] Host one authored child subtree without re-layout

Notes:

- `LegacyMenuRoot` now lives in `src/Core.UI/src/LegacyMenuRoot.cs`.
- Focused coverage is in `tests/Framework.Tests/src/LegacyMenuRootTests.cs`.

### 2. Data Read Path

- [x] Read `GT_MENU1` from the typed data path already present in `Data`
- [x] Extract only the `Opening` section for the first pass
- [x] Keep field names traceable to `Menu1.xml`

Notes:

- Current preview path reads `assets/DB/xml/GenData.db/GT_MENU1/Menu1.xml` directly.
- `Menu1OpeningData` keeps XML-traceable field names like `Single`, `StaticSingle`, and `AnimMedia`.
- The current pass still uses `Menu1OpeningDataReader` as the XML boundary for the opening slice rather than the binary `GT_MENU1` parser output.

### 3. Archetype Resolution

- [x] Resolve opening `STATIC_DATA.StaticType` references
- [x] Resolve opening `BUTTON_DATA.ButtonType` references
- [x] Resolve opening `ANIMATE_DATA` type references
- [x] Reuse existing atlas/image lookup paths where possible
- [x] Keep legacy archetype loading out of framework core

Notes:

- `STATIC_DATA` resolves through `GT_STATIC` into `LegacyStaticNode`.
- `BUTTON_DATA` resolves through `GT_BUTTON` into `LegacyButtonNode`.
- `ANIMATE_DATA` resolves through `GT_ANIMATE.VfxType` into atlas-backed `AnimatedSprite2D`.
- The scene owns the resolved atlas resources and disposes them with the preview surface.

### 4. Opening Composer

- [x] Create one screen node/composer for `GT_MENU1.Opening`
- [x] Materialize the background static
- [x] Materialize all opening buttons
- [x] Materialize all opening static labels
- [x] Materialize all opening animations
- [x] Preserve authored positions directly from GT data
- [x] Preserve authored draw order intentionally

Notes:

- Buttons, statics, and animations are now composed as real runtime nodes instead of placeholder markers.
- Button labels and static text ids resolve through the generated RC string data.
- Button hover is routed through `UiEventSource` and now drives the paired opening animations.

### 5. Animation Adapter

- [x] Add a lightweight legacy animation wrapper for `ANIMATE_DATA`
- [x] Resolve atlas/image frames from the referenced animate archetype
- [ ] Drive playback from legacy timer data
- [x] Support looping where the opening data requires it
- [x] Keep fuzz/talking-head behavior out of this milestone

Notes:

- The current adapter uses `AnimatedSprite2D` directly rather than introducing a separate `LegacyAnimate` wrapper yet.
- Opening animations are atlas-backed and hover-driven.
- Playback speed is currently hardcoded in the preview scene rather than derived from the authored `dwTimer`; this remains one of the visible fidelity gaps.

### 6. Interaction Limits

- [x] Allow hover/focus/pressed visuals only as needed for display correctness
- [x] Do not open submenus when buttons activate
- [x] Do not open modal dialogs
- [x] Keep button actions stubbed, logged, or no-op for now

Notes:

- `UiEventSource` is now connected to the opening scene.
- `Single`, `Multi`, `Intro`, `Options`, and `Help` currently use hover-only behavior to drive associated preview animations.
- `Help` now opens a real authored `helpMenu` modal instead of a stub.
- `helpMenu` `Credits` now opens a dedicated credits scene and returns back into `Menu1` with the about modal restored.
- `Esc` now opens an authored `GT_MESSAGEBOX`-driven quit confirm modal.
- `Quit` currently exits through the app quit path without confirmation.

### 7. App Wiring

- [x] Add a simple Conquest frontend scene entrypoint
- [x] Boot directly into the `Menu1` opening composition
- [x] Keep the startup path isolated from later screen-routing work

Notes:

- `src/Conquest/Program.cs` now bootstraps `RaylibApplication`, constructs a `SceneTree`, and enters `Menu1OpeningPreviewScene` directly.
- The opening scene is no longer a pure marker surface; it is a real preview composition pass over legacy-authored data.
- Audio is now connected in the preview scene as a background resource.
- Current intent is to ignore any question of whether the original shell would have loaded this exact music here, because a different background OST approach is planned.

### 8. Verification

- [x] Verify the screen is centered and scaled correctly
- [x] Verify black side bars are present outside content
- [ ] Verify all opening controls appear in authored relative positions
- [x] Verify static text/image surfaces render
- [x] Verify opening animations advance visibly
- [x] Verify button hit areas align with visuals closely enough for this slice
- [x] Capture at least one screenshot/reference image for comparison

Current practical state:

- legacy viewport composition is working
- static surfaces are rendering from `GT_STATIC`
- buttons are rendering from `GT_BUTTON`
- animated elements are rendering from `GT_ANIMATE`
- routed pointer events are connected
- hover-driven animation playback is connected
- background audio playback is connected
- authored about/help modal flow is connected
- authored credits background plus `Credits.txt` scroller flow is connected
- authored quit-confirm message box flow is connected for `Esc`
- remaining work is fidelity validation and any final authored-behavior adjustments

## Exit Criteria

This milestone is done when all of the following are true:

- the app can boot into a visible `Menu1` opening screen
- the screen is composed from typed `GT_MENU1.Opening` data
- the result is presented as a scaled, centered `800x600` legacy surface
- buttons, statics, and animations render in the correct places
- routed interaction is connected closely enough to validate hover behavior
- no modal or submenu flow is required to demonstrate the result

## Deferred Work

Do not mix these into the opening milestone:

- `singlePlayerMenu`
- `selectCampaign`
- `selectMission`
- `netConnections`
- `ipAddress`
- `mshell`
- `map`
- `slots`
- `final`
- `deviceMenu`
- profile and launcher behavior from `Menu1.cpp`
- network/lobby behavior from `Menu1.cpp`

Notes:

- `helpMenu` is no longer deferred in the current runtime; it is implemented as an authored modal overlay.
- `Menu_Credits` is also no longer deferred in practice; it is implemented as a dedicated full-screen scene sourced from `Credits.txt`.

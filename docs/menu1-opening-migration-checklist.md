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
- music/movie/bootstrap parity from the original app shell

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
- [ ] `Menu1` opening-screen composer
- [ ] `ANIMATE_DATA` to runtime animation adapter
- [ ] menu-opening scene boot path in the app
- [ ] opening-screen verification pass

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

### 3. Archetype Resolution

- [ ] Resolve opening `STATIC_DATA.StaticType` references
- [ ] Resolve opening `BUTTON_DATA.ButtonType` references
- [ ] Resolve opening `ANIMATE_DATA` type references
- [ ] Reuse existing atlas/image lookup paths where possible
- [ ] Keep legacy archetype loading out of framework core

### 4. Opening Composer

- [ ] Create one screen node/composer for `GT_MENU1.Opening`
- [ ] Materialize the background static
- [ ] Materialize all opening buttons
- [ ] Materialize all opening static labels
- [ ] Materialize all opening animations
- [ ] Preserve authored positions directly from GT data
- [ ] Preserve authored draw order intentionally

### 5. Animation Adapter

- [ ] Add a lightweight legacy animation wrapper for `ANIMATE_DATA`
- [ ] Resolve atlas/image frames from the referenced animate archetype
- [ ] Drive playback from legacy timer data
- [ ] Support looping where the opening data requires it
- [ ] Keep fuzz/talking-head behavior out of this milestone

### 6. Interaction Limits

- [ ] Allow hover/focus/pressed visuals only as needed for display correctness
- [ ] Do not open submenus when buttons activate
- [ ] Do not open modal dialogs
- [ ] Keep button actions stubbed, logged, or no-op for now

### 7. App Wiring

- [ ] Add a simple Conquest frontend scene entrypoint
- [ ] Boot directly into the `Menu1` opening composition
- [ ] Keep the startup path isolated from later screen-routing work

Notes:

- `src/Conquest/Program.cs` now boots a dedicated `Menu1OpeningPreviewScene` for manual verification.
- Leave these items unchecked until the real opening composer replaces the current data-preview surface.

### 8. Verification

- [ ] Verify the screen is centered and scaled correctly
- [ ] Verify black side bars are present outside content
- [ ] Verify all opening controls appear in authored relative positions
- [ ] Verify static text/image surfaces render
- [ ] Verify opening animations advance visibly
- [ ] Verify button hit areas align with visuals closely enough for this slice
- [ ] Capture at least one screenshot/reference image for comparison

## Exit Criteria

This milestone is done when all of the following are true:

- the app can boot into a visible `Menu1` opening screen
- the screen is composed from typed `GT_MENU1.Opening` data
- the result is presented as a scaled, centered `800x600` legacy surface
- buttons, statics, and animations render in the correct places
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
- `helpMenu`
- `deviceMenu`
- profile and launcher behavior from `Menu1.cpp`
- network/lobby behavior from `Menu1.cpp`

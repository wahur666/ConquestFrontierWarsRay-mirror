# Trim To Framework Port Tracker

This document turns the legacy `Trim` DLL source surface into a staged framework-port plan for `ConquestFrontierWarsRay`.

It is designed for chunked analysis across multiple agent turns. The goal is to avoid context blow-up while still building a concrete, cumulative record of:

- what each Trim source file appears to do
- what the likely framework replacement should be
- which files have already been read closely
- which batch should be analyzed next
- how to phrase the next agent prompt so it extends this document instead of starting over

## Scope

Trim is defined by `D:\git2\Conquest-Frontier-Wars-Source2\src\Conquest\Trim\CMakeLists.txt`.

That target is not just "UI widgets". It spans:

- widget controls
- shell/menu composition
- modal flow
- focus and keyboard routing
- chat/network UI
- window and system integration
- media/audio/video presentation
- rendering helpers used by UI and shell screens

## Confidence Labels

- `Observed`: file was directly inspected in this analysis pass.
- `Inferred`: summary is based on filename, Trim target role, and nearby conventions. It still needs confirmation by reading the source.
- `Pending`: no useful summary should be trusted yet.

## Framework Replacement Themes

These replacement ideas line up with the current framework docs and pain points:

- `Runtime glue`: belongs in `Windowing`, `SceneTree`, or app bootstrap services.
- `Control primitive`: belongs in `Framework.Control` descendants.
- `Composite control`: belongs in framework-owned controls composed from smaller controls.
- `Layout container`: belongs in `MiniLayout` plus future `HBoxContainer`, `VBoxContainer`, `PanelContainer`, and container-aware `Control`.
- `Input/focus system`: belongs in framework-level event dispatch, focus ownership, and action routing.
- `Screen shell`: belongs in reusable screen/menu/modal nodes, not app-scattered scene code.
- `Media/resource service`: belongs in framework resource wrappers or future shared services.
- `Do not port literally`: preserve behavior, not the legacy component/container architecture.

## Batch Plan

Use these batches to keep each analysis turn bounded.

- `Batch 01`: bootstrap/runtime/container/windowing
- `Batch 02`: core drawing/resources/media plumbing
- `Batch 03`: input, focus, menu shell base, modal flow
- `Batch 04`: primitive display controls
- `Batch 05`: text entry, list, selection, scroll, tab controls
- `Batch 06`: shell/menu screens A
- `Batch 07`: shell/menu screens B and multiplayer shell
- `Batch 08`: network/chat support used by Trim
- `Batch 09`: leftovers, cross-file dependency cleanup, doc normalization

## Agent Prompt Template

Use this prompt shape for the next agent:

```text
Read docs/trim-framework-port-tracker.md first.

Analyze Batch NN from the Trim tracker.
Read the listed source files directly from D:\git2\Conquest-Frontier-Wars-Source2\src\Conquest\...

For each file in that batch:
- confirm or correct the current summary
- state what the file actually does
- identify the main classes/structs/interfaces involved
- note important cross-file dependencies
- propose the likely replacement shape in ConquestFrontierWarsRay Framework
- update the tracker doc in place
- change confidence from Inferred to Observed where justified
- add "Port Priority" and "Notes" where helpful

Do not analyze unrelated files except for narrow dependency confirmation.
Keep the update cumulative and concise.
```

## Current Port Priorities

1. Input/focus dispatch
2. Layout containers and child measurement
3. Modal/screen shell patterns
4. Composite controls (`Dropdown`, `Combobox`, `ListView`, `Slider`)
5. Resource/media helpers only where needed by real screens
6. Legacy app/window/bootstrap code only where behavior still matters

## Tracker

### Batch 01 - Bootstrap, Runtime, Container, Windowing

- `Pch.cpp`: translation unit for precompiled headers; no framework port value. Replacement: none. Confidence: `Inferred`.
- `cqpipeline.cpp`: likely legacy render/system pipeline bootstrap wrapper inserted by `SysContainer`. Replacement: framework-owned runtime bootstrap service, not a user-facing node. Confidence: `Inferred`.
- `GenData.cpp`: likely archetype/resource-driven control factory and generated data loader. Replacement: structured resource registry or UI definition loader, if still needed. Confidence: `Inferred`.
- `IniConfig.cpp`: likely legacy INI/config persistence used by shell and defaults. Replacement: explicit configuration service or typed settings store. Confidence: `Inferred`.
- `LogFile.cpp`: likely diagnostic logging sink. Replacement: plain logging utility, not a framework node. Confidence: `Inferred`.
- `Objwatch.cpp`: likely debug/inspection watcher utilities. Replacement: debug overlay or diagnostics helpers. Confidence: `Inferred`.
- `PrintHeap.cpp`: likely heap/debug memory diagnostics. Replacement: none for first port; keep only if debugging value remains. Confidence: `Inferred`.
- `SysContainer.cpp`: dynamic aggregate container that loads system components, initializes them, updates them, and exposes connection points. Replacement: do not port literally; use explicit startup wiring in app/framework services. Confidence: `Observed`.
- `System.cpp`: likely shared system registration/startup/shutdown layer around Trim services. Replacement: explicit app bootstrap and service registration. Confidence: `Inferred`.
- `TManager.cpp`: likely top-level manager for Trim subsystems or UI/theme resources. Replacement: split responsibilities into typed services. Confidence: `Inferred`.
- `TestScript.cpp`: likely developer test hook or scripting harness. Replacement: test app scenes or debug tooling. Confidence: `Inferred`.
- `Trim.cpp`: likely DLL entry/registration root for Trim target. Replacement: none as-is; convert only meaningful registrations into explicit framework setup. Confidence: `Inferred`.
- `UserDefaults.cpp`: likely persistent user preference/default store used by menus and game setup. Replacement: typed settings service plus serialization. Confidence: `Inferred`.
- `WindowManager.cpp`: Win32 window ownership, message pump, fullscreen/windowed transitions, cursor position tracking, activation handling, and exit dispatch. Replacement: mostly `Windowing` layer; keep framework APIs above it Raylib-first. Confidence: `Observed`.

### Batch 02 - Drawing, Resources, Media, Visual Plumbing

- `Animate.cpp`: likely sprite/UI animation playback helper. Replacement: lightweight animation node or utility on top of `Sprite`/`Control`. Confidence: `Inferred`.
- `BmpRead.cpp`: bitmap loading helper. Replacement: asset/resource loader if still needed; likely avoid direct port. Confidence: `Inferred`.
- `Camera.cpp`: likely camera helper for shell/render contexts. Replacement: `Camera2D`/future `Camera3DNode` wrapper depending on usage. Confidence: `Inferred`.
- `CQImage.cpp`: likely image/surface wrapper used by UI and shell rendering. Replacement: texture/image resource wrapper around Raylib concepts. Confidence: `Inferred`.
- `DrawAgent.cpp`: likely draw abstraction for UI/shell sprites, text, and primitives. Replacement: structured drawing helpers inside framework controls and resources. Confidence: `Inferred`.
- `DrawAgent16.cpp`: likely variant draw path for 16-bit or legacy surface format. Replacement: probably none; preserve only visible behavior. Confidence: `Inferred`.
- `GameProgress.cpp`: likely progress/loading presentation widget or screen helper. Replacement: dedicated progress control or modal loading screen node. Confidence: `Inferred`.
- `GridVector.cpp`: likely geometry/math helper used by UI or shell coordinate handling. Replacement: move to math helper only if still actively useful. Confidence: `Inferred`.
- `LineManager.cpp`: likely managed line drawing/text underline/border support. Replacement: simple draw helpers or canvas primitives. Confidence: `Inferred`.
- `lines.cpp`: likely primitive line rendering helpers. Replacement: draw helpers internal to framework. Confidence: `Inferred`.
- `LoadFont.cpp`: likely font loading and caching. Replacement: framework font resource service and measured text helpers. Confidence: `Inferred`.
- `MovieScreen.cpp`: likely video playback screen/control wrapper. Replacement: `VideoPlayer` plus screen/modal composition. Confidence: `Inferred`.
- `MultiLineFont.cpp`: likely wrapped/measured multiline text layout and rendering. Replacement: text measurement, wrapping, and label sizing in framework text stack. Confidence: `Inferred`.
- `MusicManager.cpp`: likely shell music playback/state manager. Replacement: app-level audio/music service using framework audio resources. Confidence: `Inferred`.
- `SFX.cpp`: likely sound effect trigger utilities. Replacement: app/service layer over `AudioPlayer`. Confidence: `Inferred`.
- `ShapeLoader.cpp`: likely shape/primitive asset loader or vector-ish UI asset helper. Replacement: typed resource wrapper if still needed. Confidence: `Inferred`.
- `SoundManager.cpp`: likely broader sound routing/state than `SFX`. Replacement: explicit sound service; keep out of core node tree unless needed. Confidence: `Inferred`.
- `SpaceEnv.cpp`: likely space backdrop/environment rendering used by menus or shell scenes. Replacement: specialized visual node or scene helper. Confidence: `Inferred`.
- `Streamer.cpp`: likely streaming media/file loader used by audio/video. Replacement: resource/media service. Confidence: `Inferred`.
- `StringData.cpp`: likely localized text/string table access. Replacement: typed string-pack/localization service. Confidence: `Inferred`.
- `Subtitle.cpp`: likely subtitle rendering/timing helper for cinematics/video. Replacement: subtitle overlay control or video companion node. Confidence: `Inferred`.
- `Tgaread.cpp`: TGA loader utility. Replacement: asset conversion or resource loader only if still needed. Confidence: `Inferred`.
- `VertexBuffer.cpp`: likely D3D vertex buffer wrapper used by UI/media rendering. Replacement: do not port literally into Raylib framework. Confidence: `Inferred`.
- `VfxRead.cpp`: likely VFX asset reader. Replacement: resource loader if those assets survive migration. Confidence: `Inferred`.
- `VideoSurface.cpp`: likely video frame surface/render target management. Replacement: `VideoPlayer` backend plumbing, not public framework API. Confidence: `Inferred`.
- `VoxCompress.cpp`: likely voice/audio compression helper. Replacement: probably none for first UI port. Confidence: `Inferred`.

### Batch 03 - Input, Focus, Shell Base, Modal Flow

- `Cursor.cpp`: owns cursor resource loading, software/hardware cursor switching, DirectInput mouse capture, synthetic mouse event dispatch, busy cursor state, and screenshot hotkey handling; main types: `Cursor`, `group_cursor_entry`, `cursor_entry`, `_cursor`; key deps: `Hotkeys`, `DBHotkeys`, `DrawAgent`, `VideoSurface`, `WindowManager`, `EventSys2`, `BaseHotRect`, `UserDefaults`; replacement: framework/app cursor service plus input backend mouse capture and optional software-cursor renderer, not a control. Port Priority: High. Notes: this is a core input bridge, not just cursor art. Confidence: `Observed`.
- `HKManager.cpp`: installs normal/debug hotkey tables from `DAHOTKEY` resources, forwards window messages into `HOTKEY`/`DBHOTKEY`, and attaches the debug hotkey filter; main types: `HKManager`, `_hkmanager`; key deps: `EventSys2`, `WindowManager`, `HKEvent`, global `HOTKEY`/`DBHOTKEY`; replacement: framework-level action map/input router with layered contexts for gameplay vs debug bindings. Port Priority: High. Confidence: `Observed`.
- `Hintbox.cpp`: runtime hint overlay resource that tracks current hint text, lead/trail timing, font reload across 2D/3D mode changes, and bottom-of-screen drawing; main types: `HintResource`, `HistoryNode`, `_hintbox`; key deps: `DrawAgent`, `VideoSurface`, `UserDefaults`, `BaseHotRect`, `EventSys2`; replacement: tooltip/status-help overlay control or HUD service driven by hover ownership and frame UI rendering. Port Priority: Medium. Notes: more like delayed status help than a generic tooltip popup. Confidence: `Observed`.
- `Macrohelp.cpp`: compile-disabled stub signatures used only to document common macros/functions for IDE assistance; no runtime behavior; main types: none beyond dummy function declarations; key deps: none at runtime; replacement: none. Port Priority: Low. Notes: this batch entry was misclassified; it is not help-system logic. Confidence: `Observed`.
- `menu.cpp`: Win32 menu-bar/resource service for the running app: toggles menu visibility from Alt timing, handles menu commands and debug dialogs, manages pause state, quick load/save, part-selection dialog, and resolves data/search paths for interface/media/content folders; main types: `MenuResource`, `partDlgSaveStruct`, `_menu`; key deps: `StatusBar`, `Hotkeys`, `DBHotkeys`, `NetBuffer`, `NetPacket`, `Mission`, `Frame`, `Camera`, `ScrollingText`, `WindowManager`; replacement: split across app shell services: command menu/debug menu, content-path/config bootstrap, pause controller, and optional dev dialogs. Port Priority: High. Notes: not the shared `Menu_*` screen base; mostly in-game shell/runtime integration. Confidence: `Observed`.
- `Menu1.cpp`: concrete front-end main menu screen built from generated archetype data; creates button/static/animation controls, manages focus/hover animation, player-name/profile setup, render-device defaults, lobby startup, and transitions into SP/MP/options/help flows; main types: `dummy_menu1`, `Menu1`; key deps: `Frame`, `IButton2`, `IStatic`, `IAnimate`, `GENDATA`, `Mission`, `SoundManager`, `MusicManager`, `IGameProgress`, `ZoneLobby`, downstream `DoMenu_*` screens, `Modal`; replacement: framework screen node composed from buttons/images/animations plus a launcher/profile/network presenter. Port Priority: High. Confidence: `Observed`.
- `Modal.cpp`: owns the modal loop: frame timing, `CQE_UPDATE`/render/flush dispatch, end-of-frame 2D composition, scene begin/end, swap/clear, and modal result capture via `CQE_DLG_RESULT`; main types: `ModalEventCallback`, `_modalfactory`; key deps: `ObjList`, `DrawAgent`, `VideoSurface`, `Hotkeys`, `SoundManager`, `IBackground`, `NetPacket`, `Sector`, `System`, `EventSys2`, `WindowManager`, `RendPipeline`; replacement: framework modal runner integrated with main loop, screen stack, per-frame update/render phases, and result futures/callbacks. Port Priority: High. Notes: broader than focus trapping; it is effectively Trim's UI modal runtime. Confidence: `Observed`.
- `PosTool.cpp`: debug hot-rect tool that grabs cursor/status ownership, shows live mouse or world coordinates in the status bar, and exits on right-click; main types: `PosTool`, `_postool`; key deps: `BaseHotRect`, `DBHotKeys`, `UserDefaults`, `Camera`, `StatusBar`; replacement: debug overlay/tool mode on top of framework input and status text, not layout infrastructure. Port Priority: Low. Notes: prior summary was incorrect; this is inspection tooling, not positioning logic. Confidence: `Observed`.
- `SuperTrans.cpp`: math implementation for `TRANSFORM` rotations, yaw/pitch/roll extraction, 2D distance, and canonical world transform initialization; main types: `TRANSFORM`, `DummyInitializer`; key deps: `Search.h`, `SuperTrans.h`; replacement: shared math/transform utility, likely outside the UI framework or absorbed by existing engine math. Port Priority: Low. Notes: this batch entry was misclassified; it is not screen-transition code. Confidence: `Observed`.

### Batch 04 - Primitive Display Controls

- `BuildButton.cpp`: specialized production/build command button implementing both `IHotButton` and `IActiveButton`; tracks tech gating, queue count, build cost, build-mode highlight, no-money overlay, and percent/stall progress, then posts `CQE_LHOTBUTTON` / `CQE_RHOTBUTTON` to its parent and writes detailed cost/status text into `STATUS`. Main types: `BuildButton`, `BBUTTONSTATE::STATE`. Key deps: `BaseHotRect`, `IShapeLoader`, `DrawAgent`, `IActiveButton`, `DSpaceship` / `DPlatform` mission data, `ObjList`, `HOTKEY`, `STATUS`, `SFXMANAGER`. Replacement: app-level `BuildCommandButton` on top of a reusable framework button/image primitive plus explicit command/state view model for availability, queue, costs, and progress. Port Priority: High. Notes: not just a skinned button; it already mixes command semantics, tooltip composition, and progress rendering. Confidence: `Observed`.
- `Button2.cpp`: core general-purpose button control with keyboard focus (`IKeyboardFocus`), mouse/keyboard press handling, repeater-button mode, optional dropdown-arrow rendering, text or string overrides, and two render paths: shape-file skinned or primitive-drawn. It posts `CQE_BUTTON`, including a high-bit variant for toggle/release cases, and repeats while held for repeater buttons. Main types: `BUTTONTYPE`, `Button2`, `ButtonFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `HOTKEY`, archetype data in `DButton`. Replacement: framework `ButtonControl` with focus/pressed/disabled/toggle/repeater policies and skin/style separation; dropdown-arrow behavior should be a style flag, not a separate control family. Port Priority: High. Notes: this is the primary reusable button baseline for many menu screens. Confidence: `Observed`.
- `DiplomacyButton.cpp`: two-state diplomacy relation button with keyboard focus, specialized shape-state rendering, and a dedicated `CQE_DIPLOMACYBUTTON` click message. Its visual state depends on enabled/pressed/hover/focus plus two stored diplomacy flags. Main types: `DIPBUTTONTYPE`, `DiplomacyButton`, `DiplomacyButtonFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `HOTKEY`, archetype data in `DDiplomacyButton`. Replacement: app-level `DiplomacyRelationButton` or segmented/toggle control with explicit two-party relation state, built atop framework button/focus primitives. Port Priority: Medium. Notes: more domain-specific than `Button2.cpp`; probably not framework-core. Confidence: `Observed`.
- `HotButton.cpp`: lightweight hot-rect button used heavily by gameplay UI; loads image states via `IShapeLoader`, supports left/right/double-click dispatch, optional hotkey posting, push/highlight/context-menu behavior, and hover-owned cursor/status/hint resources. Main types: `HotButton`, `HotButtonFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `IShapeLoader`, `HOTKEY`, `EVENTSYS`, `STATUS`, `SFXMANAGER`, `IInterfaceManager`. Replacement: framework `ImageButtonControl` plus shared hover-resource/status-help plumbing; left/right/double-click command routing should come from generic pointer events. Port Priority: High. Notes: this is a second major reusable button base alongside `Button2.cpp`, biased toward image buttons and gameplay HUD interactions. Confidence: `Observed`.
- `HotStatic.cpp`: specialized read-only tech meter display that draws repeated full/empty bar icons plus optional caption text; exposes `SetImageLevel` and `SetTextString` rather than click behavior. Main types: `HotStaticArchetype`, `HotStatic`. Key deps: `BaseHotRect`, `GenData`, `IShapeLoader`, `DrawAgent`, `IFontDrawAgent`, archetype data in `DHotStatic`. Replacement: framework/app `TechLevelIndicator` or `IconMeter + Label` composite control, not a generic static text primitive. Port Priority: Low. Notes: prior summary overstated interactivity; this file is mostly a specialized display widget. Confidence: `Observed`.
- `Icon.cpp`: simple image/icon display with optional tooltip ownership; draws one `IDrawAgent`, updates status text on hover, and otherwise has no command behavior. Main types: `ICONTYPE`, `Icon`. Key deps: `BaseHotRect`, `GenData`, `IShapeLoader`, `DrawAgent`, `STATUS`. Replacement: framework `IconControl` / `ImageControl` with optional tooltip text and hit-target support. Port Priority: Medium. Confidence: `Observed`.
- `InProgressAnim.cpp`: threaded loading/progress presenter, not just a spinner; builds a background static plus foreground animation via `GENDATA`, owns a worker thread, draws a progress bar shape and status string, and manually drives `CQE_UPDATE` / `CQE_ENDFRAME` on child components while coordinating with `VideoSurface`/pipeline critical sections. Main types: `IPAnim`. Key deps: `IAnimate`, `IStatic`, `GenData`, `VideoSurface`, `DrawAgent`, `IDDBackDoor`, `EventSys2`, global pipeline/window critical-section helpers. Replacement: app/framework loading-overlay service or modal loading screen with main-thread update/render, explicit progress model, and no ad hoc worker-owned UI rendering. Port Priority: High. Notes: this should not be ported literally into the control tree. Confidence: `Observed`.
- `ProgressStatic.cpp`: progress-text display that combines optional background fill/hash drawing, text alignment, animated numeric roll-up, and a true progress-meter fill based on `current/max`. It also changes text colors on focus. Main types: `PROGRESS_STATICTYPE`, `ProgressStatic`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `IFontDrawAgent`, `SFXMANAGER`, archetype data in `DProgressStatic`. Replacement: framework `ProgressBarControl` plus optional overlaid label/counter behavior; numeric roll-up should be a reusable text animation policy, not baked into every progress bar. Port Priority: Medium. Notes: more capable than a plain label, but still largely reusable framework UI. Confidence: `Observed`.
- `ResearchButton.cpp`: specialized research/upgrade command button implementing `IHotButton` and `IActiveButton`; resolves research/admiral/upgrade costs from `DResearch` data, tracks current tech/upgrade eligibility, queue count, and progress/no-money overlays, and writes detailed status/cost text on hover. Main types: `ResearchButton`. Key deps: `BaseHotRect`, `IShapeLoader`, `DrawAgent`, `IActiveButton`, `DResearch`, `HOTKEY`, `STATUS`, `SFXMANAGER`. Replacement: app-level `ResearchCommandButton` over framework button/image primitives and explicit research-state view models. Port Priority: High. Notes: sibling to `BuildButton.cpp`; similar framework needs, different domain state rules. Confidence: `Observed`.
- `ShipSilButton.cpp`: gameplay selection widget that renders a ship silhouette in green/yellow/red based on the bound ship state and uses click/shift-click to mutate object selection rather than posting a generic UI command. It also drives status text from the bound ship or fallback tooltip text. Main types: `ShipSilButton`. Key deps: `BaseHotRect`, `IShapeLoader`, `DrawAgent`, `MPart`, `ObjList`, `IBaseObject`, `HOTKEY`, `STATUS`. Replacement: app-level HUD selection item built from an image hit-target plus explicit selection presenter/service; not a framework-core button. Port Priority: Medium. Notes: prior summary was directionally right but understated that this is really gameplay-selection UI. Confidence: `Observed`.
- `Static.cpp`: general static text/image display control with alignment modes, multiline measurement, optional background fill/hash, optional numeric roll-up animation, tooltip/hint ownership, and a "buddy control" mode that forwards hover/press state into an attached `IButton2`. Main types: `STATICTYPE`, `Static`, `StaticFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `IFontDrawAgent`, `IButton2`, `STATUS`, `SFXMANAGER`, archetype data in `DStatic`. Replacement: framework `LabelControl` / `PanelLabel` plus optional tooltip support; buddy-button forwarding should become explicit composition instead of embedded cross-control coupling. Port Priority: High. Notes: this is the main generic display primitive, not just a text label. The attached `GT_STATIC` sample (`Static!!Background.xml`) confirms a real asset can be shape-only (`mainscreen.shp` -> single 800x600 atlas frame, no font, `backgroundDraw=nodraw`, `backdraw=false`), so the replacement should allow pure image-panel usage without forcing text semantics. Confidence: `Observed`.
- `StatusBar.cpp`: global status-bar resource/service, not a child control; stores current text/name/mode, reloads fonts when switching 2D/3D, draws directly at the bottom of the screen during `CQE_ENDFRAME`, and supports tooltip/build/default/name display modes. Main types: `StatusBarResource`, global `_status`. Key deps: `TResource`, `BaseHotRect`, `DrawAgent`, `UserDefaults`, `VideoSurface`, `EventSys2`, `FULLSCREEN`. Replacement: framework/app status-overlay service or HUD layer with a small model (`text`, `secondary name`, `mode`, `toolbar offset`) rather than a Win32-era global resource singleton. Port Priority: High. Notes: many Batch 03/04 controls depend on this hover-status channel. Confidence: `Observed`.
- `Teletype.cpp`: global scrolling/typewriter text system that splits strings into multiple `TeletypeLine`/`TeletypeObj` instances, reveals characters over time with caret blink and per-character SFX, supports lifetime and pause-ignoring behavior, and renders/updates from fullscreen event callbacks. Main types: `TeletypeLine`, `TeletypeObj`, `Teletype`, global `_teletype`. Key deps: `ITeletype`, `DrawAgent`, `Frame`, `Hotkeys`, `SFX`, `FULLSCREEN`, `EventPriority`. Replacement: framework/app transient text-overlay service or `TypewriterTextControl` plus manager for queued/lifetime-based overlays. Port Priority: Medium. Notes: broader than a single control; it behaves like a global overlay text channel. Confidence: `Observed`.

### Batch 05 - Text Entry, List, Selection, Scroll, Tabs

- `Combobox.cpp`: composite edit-plus-list selector; creates `IButton2`, `IListbox`, and `IEdit2` children from archetype data, forwards most list/edit APIs, auto-completes typed prefixes, toggles drop state, and swaps keyboard focus between edit field and dropped list. Main types: `Combobox`, `ComboboxFactory`. Key deps: `Edit2.cpp`, `Listbox.cpp`, primitive button control via `IButton2`, `BaseHotRect`, `GenData`, `DrawAgent`. Replacement: framework `ComboBoxControl` composed from `TextInput`, popup `ListView`, and trigger button, with explicit popup ownership, focus transfer, and selection/change events. Port Priority: High. Notes: current tracker summary was correct but understated the editable/autocomplete behavior. Confidence: `Observed`.
- `Dropdown.cpp`: non-editable dropdown selector; creates button and list child controls, mirrors the listbox API, owns open/close state, updates button text from selected list entry, and manually treats either button or list hover as alert state while dropped. Main types: `Dropdown`, `DropdownFactory`. Key deps: `Listbox.cpp`, primitive button control via `IButton2`, `BaseHotRect`, `GenData`, `DrawAgent`. Replacement: framework `DropdownControl` with read-only display button plus popup `ListView`, sharing most popup/focus mechanics with combobox but without text entry. Port Priority: High. Notes: sibling of `Combobox.cpp`, not a separate screen shell. Confidence: `Observed`.
- `Edit2.cpp`: single-line text input control with selection, caret blink, mouse drag selection, double-click word selection, insert/overwrite mode, simple copy/paste scratch buffer via Shift/Ctrl+Insert/Delete, optional toolbar/chat/locked-text behaviors, IME composition placement, and per-frame draw/update handling. Main types: `EDITTYPE`, `Edit2`, `EditFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, font resources via `IFontDrawAgent`, `HKEvent`/`HOTKEY`, toolbar/chat focus handoff from Batch 03 services. Replacement: framework `TextInputControl` with caret/selection model, key-text separation, IME support, optional behavior flags split into clearer policy/config hooks, and style resources separate from control logic. Port Priority: High. Notes: this is richer than a basic text field and carries legacy toolbar/chat-specific behavior that should not live in the core widget API unchanged. Confidence: `Observed`.
- `Listbox.cpp`: scrollable selectable text list built on a linked-list item store; supports add/remove/update, per-item user data and color, keyboard caret movement, mouse hover selection, optional single-click activation, word-wrap break calculation, and optional owned scrollbar integration through `IScrollBarOwner`. Main types: `LISTBOXTYPE`, `LISTITEM`, `Listbox`, `ListboxFactory`. Key deps: `ScrollBar.cpp`, `BaseHotRect`, `GenData`, `DrawAgent`, font resources via `IFontDrawAgent`. Replacement: framework `ListView`/`SelectionListControl` with item model, selection state, optional activation-on-single-click, scroll viewport, and a separate scrollbar/scroll model instead of embedding linked-list storage in the widget. Port Priority: High. Notes: this is a core dependency for comboboxes, dropdowns, and several menu screens. Confidence: `Observed`.
- `MScroll.cpp`: global edge-scroll service for the game view, not a UI scroll widget; watches cursor position and hotkeys each update, grabs cursor ownership when active, swaps directional cursor icons, and calls `SYSMAP` scroll commands for edge or keyboard scrolling. Main types: `MScroll`, `_mscroll`. Key deps: `Cursor.cpp`, `StatusBar.cpp`, `SysMap.h`, `Hotkeys`, `DBHotkeys`, `WindowManager`, `EventSys2`. Replacement: app/world input service for camera or map scrolling, outside framework controls; expose it as gameplay viewport behavior, not as `ScrollContainer` infrastructure. Port Priority: Low. Notes: prior summary was incorrect; this batch entry is a gameplay/system input helper, not a list or scrollbar control. Confidence: `Observed`.
- `QueueControl.cpp`: specialized build-queue strip that draws queued unit/build icons, overlays progress on the first slot, tracks stall state, highlights hovered entries, and posts removal messages for clicked queue slots to the toolbar. Main types: `QUEUECONTROLTYPE`, `QueueControl`, `QueueControlFactory`. Key deps: `BaseHotRect`, `DrawAgent`, `IActiveButton`, toolbar messaging, defaults/editor pause state. Replacement: app-level `BuildQueueControl` composed from icon cells plus progress/stall overlays, likely outside the core framework library except for reusable icon-button/progress primitives. Port Priority: Medium. Notes: prior summary was directionally right but too generic; this is production-queue UI, not a reusable list control. Confidence: `Observed`.
- `ScrollBar.cpp`: owner-driven scrollbar with two arrow buttons, proportional thumb sizing from `scrollRange`/`viewRange`, thumb dragging with cancel-on-breakoff behavior, repeated page scrolling while held, optional horizontal mode, and draw paths for either art-driven or primitive skins. Main types: `SCROLLBARTYPE`, `ScrollBar`, `ScrollBarFactory`. Key deps: primitive button control via `IButton2`, `BaseHotRect`, `GenData`, `DrawAgent`, owner callbacks from `Listbox.cpp` through `IScrollBarOwner`. Replacement: framework `ScrollBarControl` backed by a shared scroll model, with arrow buttons and track/thumb input split cleanly from view ownership. Port Priority: High. Notes: the key contract is the owner callback model and thumb math, not the legacy connection-point plumbing. Confidence: `Observed`.
- `Slider.cpp`: discrete slider control with keyboard arrow support, drag-to-step behavior, optional deferred event emission until mouse release, vertical or horizontal orientation, and art-driven or primitive rendering for track and thumb. Main types: `SLIDERTYPE`, `Slider`, `SliderFactory`. Key deps: `BaseHotRect`, `GenData`, `DrawAgent`, `HKEvent`. Replacement: framework `SliderControl` with value range, orientation, immediate-vs-commit change policy, and styleable thumb/track visuals. Port Priority: Medium. Notes: current tracker summary was broadly correct; the important detail is that this slider snaps by integer step rather than tracking a continuous float. Confidence: `Observed`.
- `TabButton.cpp`: tab header control plus per-tab focus router; draws tab states, forwards child control events upward, owns selected/highlight state, optionally cycles tabs on `Tab`, moves focus among registered `IKeyboardFocus` children with arrow keys, and hides child interaction when the tab is inactive. Main types: `TABBUTTONTYPE`, `TabButton`, `TabButtonFactory`. Key deps: `TabControl.cpp`, `BaseHotRect`, `GenData`, `DrawAgent`, `SFX`, `Frame`, child controls implementing `IKeyboardFocus`. Replacement: tab-header item plus tab-page focus scope in framework `TabContainer`, with page-local focus traversal handled by generic focus navigation instead of button-owned child lists. Port Priority: High. Notes: this file does more than paint a tab button; it partly owns tab-page focus behavior. Confidence: `Observed`.
- `TabControl.cpp`: tab-strip container that instantiates `ITabButton` children from image resources, tracks selected tab, toggles each tab button's selected state, exposes per-tab child menu surfaces through `GetTabMenu`, forwards child control events upward, and supports per-tab default focus targets. Main types: `TABTYPE`, `TabControl`, `TabControlFactory`. Key deps: `TabButton.cpp`, `BaseHotRect`, `GenData`, `IShapeLoader`, `IImageReader`. Replacement: framework `TabContainer` with tab headers, selected page state, page content nodes, and default-focus-per-page support. Port Priority: High. Notes: this is the actual tab-page coordinator; `TabButton.cpp` is only half of the behavior. Confidence: `Observed`.

### Batch 06 - Shell And Menu Screens A

- `EulaWin.cpp`: EULA screen/dialog. Replacement: modal screen node with scrollable text and accept/decline actions. Confidence: `Inferred`.
- `Menu_Briefing.cpp`: mission briefing screen. Replacement: dedicated screen node with text/media/layout containers. Confidence: `Inferred`.
- `Menu_campaign.cpp`: campaign selection/progression screen. Replacement: dedicated screen node plus list/detail layout. Confidence: `Inferred`.
- `Menu_Confirm.cpp`: generic confirmation dialog screen. Replacement: reusable modal dialog node. Confidence: `Inferred`.
- `Menu_Credits.cpp`: credits screen. Replacement: scrolling text/media screen node. Confidence: `Inferred`.
- `Menu_final.cpp`: likely final game-setup or launch confirmation screen. Replacement: dedicated screen node and shared shell flow. Confidence: `Inferred`.
- `Menu_help.cpp`: help screen. Replacement: scrollable document/help panel screen. Confidence: `Inferred`.
- `Menu_igoptions.cpp`: in-game options menu. Replacement: modal options dialog built from reusable controls. Confidence: `Inferred`.
- `Menu_LoadSave.cpp`: load/save dialog. Replacement: file-slot management screen or modal with list/detail controls. Confidence: `Inferred`.
- `Menu_map.cpp`: map settings/details screen. Replacement: screen section or child panel in larger shell. Confidence: `Inferred`.
- `Menu_MapSelect.cpp`: map selection screen. Replacement: list/grid selector screen with preview panel. Confidence: `Inferred`.
- `Menu_mission.cpp`: mission selection or mission shell. Replacement: screen node with list/detail composition. Confidence: `Inferred`.

### Batch 07 - Shell And Menu Screens B

- `Menu_mshell.cpp`: multiplayer staging shell implementing `ICQGame`; owns lobby state, chat entry/history, host/client setting replication, ready checks, player add/remove handling, Zone score upload, and transition into `DoMenu_nl` net loading or `DoMenu_final`. Main types: `Menu_mshell`, `CQGAME_PACKET`, `MAP_PACKET`, `CLIENTSETTING_PACKET`, `GRCHAT_PACKET`. Key deps: `Menu_netloading.cpp`, `NetBuffer`, `NetPacket`, `Mission`, `MusicManager`, `ZoneLobby`, `WindowManager`, chat controls via `IEdit2`/`IListbox`, and child slot/options surfaces through shared `ICQGame` state. Replacement: multiplayer lobby screen presenter plus child panels for slots/options/chat, backed by an async networking/session service rather than frame-owned packet code. Port Priority: High. Notes: this is the central multiplayer shell, not just a menu skin. Confidence: `Observed`.
- `Menu_netconn.cpp`: transport/provider chooser for multiplayer entry; enumerates saved DirectPlay connections, describes each transport, launches `DoMenu_sess` for normal session flow, optionally launches `DoMenu_zone`, and can shell out to a web URL. Main types: `Menu_nc`. Key deps: `ZoneLobby`, `Menu_netsess.cpp`, `Menu_zone.cpp`, saved connection enumeration from networking globals. Replacement: network entry screen with explicit provider cards/actions and a service-backed provider/session bootstrap. Port Priority: Medium. Notes: includes obsolete Zone/web launch branching that should stay outside core framework widgets. Confidence: `Observed`.
- `Menu_netloading.cpp`: non-rendering multiplayer load/download modal; coordinates checksum/random-seed handshake, map generation or mission load, file transfer progress, mission initialization, and failure/cancel signaling between host and clients. Main types: `Menu_nl`, nested `FTENUMERATOR`. Key deps: `Mission`, `NetPacket`, `NetBuffer`, `NetFileTransfer` channel APIs, `MusicManager`, `IPAnim`, and `ICQGame` data from `Menu_mshell.cpp`. Replacement: async loading screen/service boundary with progress model, host/client load state machine, and transfer callbacks separate from the screen node. Port Priority: High. Notes: broader than a progress screen; this is multiplayer load orchestration. Confidence: `Observed`.
- `Menu_netsess.cpp`: first-stage multiplayer session setup screen; toggles join vs create, captures player name and optional TCP IP address via combobox, persists defaults, starts session enumeration, and branches to `DoMenu_sess2`. Main types: `Menu_sess`. Key deps: `Combobox.cpp`, `Session_Buffer` enumeration, DirectPlay connection setup, `Menu_netsess2.cpp`, `Menu_mshell.cpp`, `MusicManager`. Replacement: multiplayer setup screen with validated name/address form, join/create mode state, and a session-discovery service. Port Priority: High. Notes: not a session browser yet; this is the pre-browser setup step. Confidence: `Observed`.
- `Menu_netsess2.cpp`: second-stage session browser/create screen; refreshes enumerated sessions, renders per-session description strings from embedded options, validates version/name conflicts, then joins or creates and enters `DoMenu_mshell`. Main types: `Menu_sess2`. Key deps: session enumeration buffers, DirectPlay open/create-player flow, `Menu_mshell.cpp`, options packed into session descriptors. Replacement: session list screen with polling refresh, session metadata view model, join/create actions, and validation surfaced by the networking service. Port Priority: High. Notes: the split between `Menu_netsess.cpp` and this file is real and should remain explicit in a modern flow. Confidence: `Observed`.
- `Menu_newplayer.cpp`: modal player-profile name dialog; edits a name, validates reserved/duplicate cases against the `SavedGame\\` folder layout, and returns the accepted name to callers like options. Main types: `dummy_newplayer`, `Menu_newplayer`. Key deps: `Edit2.cpp`, file-system/profile directories, callers in `Menu_options.cpp`. Replacement: reusable modal text-entry dialog with validation callback supplied by the owning screen. Port Priority: Medium. Confidence: `Observed`.
- `Menu_options.cpp`: large tabbed options screen covering player profile folders, audio volumes, gameplay/input toggles, render device/resolution/gamma, and VFX flags; previews some changes live, writes defaults/registry values, and invokes `DoMenu_newplayer` for profile add/rename. Main types: `NameList`, `ResEnum`, `PackedRes`, `dummy_options`, `Menu_options`. Key deps: `TabControl.cpp`, `Slider.cpp`, `Dropdown.cpp`, `Listbox.cpp`, `IGammaControl`, `IProfileParser`, `UserDefaults`, `Effects`, `Menu_newplayer.cpp`. Replacement: settings screen composed from reusable framework tabs, lists, sliders, toggles, and dropdowns, backed by typed settings services instead of direct registry/file mutations in widget code. Port Priority: High. Notes: mixes framework-worthy controls with legacy render-device plumbing; split those concerns during port. Confidence: `Observed`.
- `Menu_Pause.cpp`: pause/congestion overlay dialog; displays host/player pause status, boot countdowns, and resignation/quit outcomes, and in multiplayer polls `NetPacket` each frame for pause ownership and timers. Main types: `MenuPause`. Key deps: `NetPacket`, `Mission`, player enumeration callbacks, global pause/hotkey events. Replacement: pause modal overlay with a multiplayer pause-status presenter and result actions routed through app/game services. Port Priority: Medium. Notes: combines local pause menu behavior with network-congestion messaging. Confidence: `Observed`.
- `Menu_SlideShow.cpp`: shape-driven slideshow/splash modal; steps through frames on `CQE_UPDATE`, draws a single loaded shape each frame, optionally allows early exit, and also backs splash playback that can trigger process close on completion. Main types: `Menu_SlideShow`. Key deps: `IShapeLoader`, `IDrawAgent`, `MScript` `SPLASHINFO`, modal runtime. Replacement: lightweight slideshow/splash screen node or app bootstrap presenter for scripted image sequences. Port Priority: Low. Notes: preserve behavior, not the VFX-shape loading path. Confidence: `Observed`.
- `Menu_slots.cpp`: multiplayer slot editor subpanel over `ICQGame`; manages per-slot state/open/AI difficulty, race, color/player identity, team assignment, ping/name display, host-only editability, boot-player flow, and AI auto-fill naming. Main types: `Menu_slots`. Key deps: `Dropdown.cpp`, `Menu_mshell.cpp` via `ICQGame`, `NetBuffer`, `Mission` color/name helpers. Replacement: reusable lobby slot-grid panel with per-slot view models, validation rules, and host-authority gating supplied by the multiplayer presenter. Port Priority: High. Notes: likely one of the child panels used inside the multiplayer shell; keep it app-level, not framework-core. Confidence: `Observed`.
- `Menu_SPGame.cpp`: single-player entry screen for campaign, skirmish, saved-game load, and quick-battle load; manages save directory existence, counts available saves to enable buttons, and enters campaign, briefing, or local `DoMenu_mshell` skirmish flow. Main types: `Menu_SPGame`. Key deps: `Mission`, `MusicManager`, `Menu_campaign.cpp`, `Menu_mshell.cpp`, `Menu_Briefing.cpp`, load/save flows. Replacement: single-player hub screen with buttons/actions backed by savegame and game-start services. Port Priority: Medium. Notes: skirmish reuses the multiplayer shell path with no remote connection. Confidence: `Observed`.
- `Menu_SysKitSaveLoad.cpp`: modal save/load UI for per-system lighting kit data; lists files in `\\GT_SYSTEM_KIT`, edits a file name, reads or writes `GT_SYSTEM_KIT` records for the current sector, and pauses the game around the modal. Main types: `MenuSystemKitSaveLoad`. Key deps: `Sector`, `Mission`, `Edit2.cpp`, `Listbox.cpp`, file-system/document APIs. Replacement: specialized editor/debug asset dialog outside the general game UI framework. Port Priority: Low. Notes: narrow tool workflow, not a general save/load screen. Confidence: `Observed`.
- `Menu_Toolbar.cpp`: in-game toolbar runtime and context-menu loader; parses toolbar data records into concrete controls, owns multiple context toolbars/tabs, dispatches left/right/double-click events to a toolbar client, toggles visibility/focus, and integrates hint/status/debug behaviors with gameplay UI. Main types: `CONTROL_NODE`, `Menu_context`, `Menu_tb`, nested `createCallback`, `FullScreen`. Key deps: primitive toolbar controls (`HotButton`, `ProgressStatic`, `Edit2`, `HotStatic`, `TabControl`, `ShipSilButton`, `Icon`, `QueueControl`), `StatusBar`, `Hintbox`, `ObjList`, `SysMap`, `ScrollingText`, parser/view-constructor infrastructure. Replacement: app-level gameplay HUD/toolbar composition layer built from framework controls plus explicit command routing, not a parser-driven one-off inside the framework core. Port Priority: High. Notes: this is a major integration surface and should probably split into HUD shell, context panels, and control factories during port. Confidence: `Observed`.
- `Menu_zone.cpp`: Microsoft Zone / lobby launcher bridge; launches the external Zone app from registry, waits on lobby connection settings in a worker thread, initializes DirectPlay lobby/session state, then enters `DoMenu_mshell` or returns a special quit code. Main types: `Menu_zone`. Key deps: `ZoneLobby`, `DPLOBBY`, `DPLAY`, `Menu_mshell.cpp`, process launching via `ShellExecuteEx`, worker-thread polling. Replacement: omit or isolate behind an optional legacy online-service adapter; no direct framework equivalent unless an external lobby integration is revived. Port Priority: Low. Notes: clearly service-integration code, not reusable menu infrastructure. Confidence: `Observed`.

### Batch 08 - Network And Chat Support Used By Trim

- `NetBuffer.cpp`: likely network buffering/bandwidth pacing service used by multiplayer shell. Replacement: app/service layer, not framework control. Confidence: `Inferred`.
- `NetConnect.cpp`: likely network transport/session connection setup. Replacement: dedicated networking service outside UI framework. Confidence: `Inferred`.
- `NetConnectBuffers.cpp`: likely connection buffer structures/helpers. Replacement: networking service internals. Confidence: `Inferred`.
- `NetFileTransfer.cpp`: likely multiplayer file/map transfer support. Replacement: async transfer service with UI progress reporting. Confidence: `Inferred`.
- `NetPacket.cpp`: likely packet send/receive wrapper. Replacement: networking service API. Confidence: `Inferred`.

### Batch 09 - Misc Legacy Helpers Bound Into Trim

- `DumpView.cpp`: likely debug or dump visualization screen/helper. Replacement: debug-only tooling if still valuable. Confidence: `Inferred`.
- `LFParser.cpp`: likely parser for legacy layout/form/text definitions. Replacement: only port if data-driven UI parsing remains required. Confidence: `Inferred`.

## Cross-Cutting Conclusions So Far

### 1. Layout Alone Is Not The Main Port Risk

`MiniLayout` covers only the placement problem. It is the correct first step for:

- `PosTool`-style coordinate cleanup
- dialog centering
- row/column arrangement
- padding/margin/gap behavior

But Trim also depends heavily on:

- focus ownership
- modal flow
- keyboard routing
- control composition
- screen shell composition

### 2. Combobox Confirms The Needed Runtime Shape

From `Combobox.cpp`, the framework needs:

- parent-owned child control composition
- focus transfer between child controls
- visible/hidden popup ownership
- control IDs or a cleaner event source model
- event dispatch for click, selection, edit changed, and escape
- draw-order control for active popups

### 3. Menu Screens Confirm The Screen-Shell Problem

From `Menu_mshell.cpp`, Trim screens are not just layout files. They also own:

- control creation
- screen state
- focus group behavior
- child screen composition
- network state transitions
- modal entry/exit
- async/polling behavior

Framework replacement therefore needs reusable screen-shell conventions, not just controls.

### 4. Batch 03 Confirms Three Separate Port Surfaces

Batch 03 is not one subsystem. It breaks into:

- input backends and action routing: `Cursor.cpp`, `HKManager.cpp`
- modal/frame runtime: `Modal.cpp`
- screen/shell and overlay behaviors: `Menu1.cpp`, `menu.cpp`, `Hintbox.cpp`

It also included two non-framework strays:

- `Macrohelp.cpp`: IDE-only stub file
- `SuperTrans.cpp`: engine math helper

### 5. Batch 05 Confirms The Framework Control Boundaries

Batch 05 separates three different kinds of work that should not be ported the same way:

- reusable composite controls: `Combobox.cpp`, `Dropdown.cpp`, `Listbox.cpp`, `ScrollBar.cpp`, `Slider.cpp`, `TabButton.cpp`, `TabControl.cpp`
- reusable but policy-heavy text entry: `Edit2.cpp`
- non-framework or app-specific behavior: `MScroll.cpp`, `QueueControl.cpp`

The main framework conclusions are:

- popup ownership and focus transfer are first-class control concerns, especially for `Combobox.cpp` and `Dropdown.cpp`
- selection controls depend on a shared scroll model, not ad hoc owner callbacks scattered through each widget
- tab containers need page-local focus scope and default-focus behavior, not just painted tab headers
- text input needs a proper editing model with IME, selection, caret, and behavior policies separated from toolbar/chat special cases

Batch 05 also confirmed two misclassified files:

- `MScroll.cpp`: gameplay viewport edge-scroll service
- `QueueControl.cpp`: specialized build-queue widget

That means remaining analysis should favor real screen files next, because the reusable control surface is now much clearer.

### 6. Batch 07 Confirms The Multiplayer Shell Split

Batch 07 showed that the legacy multiplayer front end is already split into distinct stages:

- provider/transport entry: `Menu_netconn.cpp`
- player-name and join/create setup: `Menu_netsess.cpp`
- session discovery and join/create: `Menu_netsess2.cpp`
- live lobby staging and slot/chat/options state: `Menu_mshell.cpp`, `Menu_slots.cpp`
- host/client load handshake and transfer orchestration: `Menu_netloading.cpp`

That split is useful and should survive the port, but the implementation shape should change:

- networking/session state moves into presenter/services instead of `Frame` subclasses
- screen nodes own only UI state and transitions
- app-specific screens like `Menu_options.cpp`, `Menu_Toolbar.cpp`, and `Menu_SysKitSaveLoad.cpp` stay out of framework-core boundaries

### 7. Batch 04 Clarifies The Primitive-Control Split

Batch 04 showed that Trim's "primitive display controls" are actually four different groups:

- reusable framework primitives: `Button2.cpp`, `HotButton.cpp`, `Icon.cpp`, parts of `Static.cpp`, parts of `ProgressStatic.cpp`
- app-level command widgets built on those primitives: `BuildButton.cpp`, `ResearchButton.cpp`, `DiplomacyButton.cpp`
- app-level gameplay HUD widgets: `ShipSilButton.cpp`, `HotStatic.cpp`
- global overlay/services rather than child widgets: `StatusBar.cpp`, `Teletype.cpp`, `InProgressAnim.cpp`

The main framework conclusions are:

- the framework needs both a text-capable general button and an image-button path; Trim uses both heavily
- hover ownership of status/help/cursor is part of the runtime contract, not incidental decoration
- `Static.cpp` is a generic panel/image/text primitive and should not be collapsed into a text-only label API
- status/help text, typewriter overlays, and loading overlays should become explicit overlay services or top-layer nodes, not ordinary children
- command widgets with domain state should live above framework-core and consume view models/services rather than embedding mission logic in the control

The attached `Static!!Background.xml` and `mainscreen_atlas` export confirm that at least one real `GT_STATIC` asset is just a fullscreen image background with no font data, which strengthens the case for a framework `Panel/Image/Label` split instead of a single catch-all static control.

## Suggested Next Analysis Order

1. `Batch 06`
2. `Batch 02`
3. `Batch 08`
4. `Batch 01`
5. `Batch 09`

Reason:

- Batch 03, Batch 04, Batch 05, and Batch 07 now cover the main input/control/runtime surfaces that the remaining screens sit on top of
- Batch 06 should come next because those screen files can now be interpreted with much better control-level context
- Batch 02 should follow so media/drawing helpers can be judged against actual screen usage instead of in isolation
- Batch 08 can then narrow the networking internals behind the multiplayer shell already mapped in Batch 07
- leave bootstrap and leftovers until the screen/control/media boundaries are stable enough to avoid rework in the tracker

## Tracker Maintenance Rules

When updating this document in future passes:

- keep prior summaries unless the source disproves them
- replace `Inferred` with `Observed` only after reading the file directly
- keep replacement ideas framework-oriented, not legacy-architecture-oriented
- record when a file should be split across multiple framework concepts
- avoid porting DACOM/container patterns unless behavior absolutely depends on them

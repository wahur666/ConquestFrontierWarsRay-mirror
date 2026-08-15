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

- `BuildButton.cpp`: specialized build-button control. Replacement: `ButtonNode` subclass or style/skin variant, depending on unique behavior. Confidence: `Inferred`.
- `Button2.cpp`: primary button control implementation. Replacement: `ButtonNode` plus event hooks and focus states. Confidence: `Inferred`.
- `DiplomacyButton.cpp`: specialized button or toggle for diplomacy states. Replacement: button subclass or composite control with explicit state model. Confidence: `Inferred`.
- `HotButton.cpp`: hover/click-sensitive button variant, likely tied to hot-rect model. Replacement: framework event-driven button behavior. Confidence: `Inferred`.
- `HotStatic.cpp`: static display element with hover/interaction behavior. Replacement: label/image control with optional hit target. Confidence: `Inferred`.
- `Icon.cpp`: icon/image display control. Replacement: image/icon control using texture resources. Confidence: `Inferred`.
- `InProgressAnim.cpp`: likely spinner/animated "busy" control. Replacement: activity indicator control. Confidence: `Inferred`.
- `ProgressStatic.cpp`: progress display control. Replacement: progress bar or status meter control. Confidence: `Inferred`.
- `ResearchButton.cpp`: specialized button with research state/skin. Replacement: styled button subclass or data-driven skinning. Confidence: `Inferred`.
- `ShipSilButton.cpp`: likely button rendering a ship silhouette preview. Replacement: composite image button control. Confidence: `Inferred`.
- `Static.cpp`: plain text/image static display control. Replacement: `TextNode`, `PanelNode`, and image-label primitives. Confidence: `Inferred`.
- `StatusBar.cpp`: status bar control. Replacement: dedicated status bar or simple horizontal container with labels/icons. Confidence: `Inferred`.
- `Teletype.cpp`: likely animated typewriter text control. Replacement: text reveal animation node or subtitle/text effect control. Confidence: `Inferred`.

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

- `Menu_mshell.cpp`: multiplayer shell screen; composes chat UI, nested child frames, focus-group behavior, network packet handling, ready state logic, and modal transitions into loading. Replacement: screen-shell node with child panels, event-driven controls, async/network presenter service, and modal navigation. Confidence: `Observed`.
- `Menu_netconn.cpp`: network connection setup screen. Replacement: dedicated network setup screen and service bridge. Confidence: `Inferred`.
- `Menu_netloading.cpp`: multiplayer loading/progress screen. Replacement: loading modal/screen with async state updates. Confidence: `Inferred`.
- `Menu_netsess.cpp`: multiplayer session browser/host screen. Replacement: session list screen with join/host actions. Confidence: `Inferred`.
- `Menu_netsess2.cpp`: likely second-stage session details/options screen. Replacement: session detail/config screen. Confidence: `Inferred`.
- `Menu_newplayer.cpp`: new player/profile creation screen. Replacement: profile creation dialog/screen. Confidence: `Inferred`.
- `Menu_options.cpp`: main options/settings screen. Replacement: settings screen built from reusable control panels. Confidence: `Inferred`.
- `Menu_Pause.cpp`: pause menu screen. Replacement: node-based pause modal/menu. Confidence: `Inferred`.
- `Menu_SlideShow.cpp`: slideshow/cinematic or static image sequence screen. Replacement: slideshow screen node with transitions. Confidence: `Inferred`.
- `Menu_slots.cpp`: player slot/team setup screen or subpanel. Replacement: slot editor panel in game-setup flow. Confidence: `Inferred`.
- `Menu_SPGame.cpp`: single-player game setup screen. Replacement: single-player setup screen built from reusable panels. Confidence: `Inferred`.
- `Menu_SysKitSaveLoad.cpp`: likely system-kit save/load or special profile/save UI. Replacement: specialized save/load screen only if still needed. Confidence: `Inferred`.
- `Menu_Toolbar.cpp`: in-game toolbar shell or toolbar screen section. Replacement: toolbar container plus reusable action controls. Confidence: `Inferred`.
- `Menu_zone.cpp`: Zone/online service integration shell. Replacement: separate service-specific screen if feature still exists. Confidence: `Inferred`.

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

## Suggested Next Analysis Order

1. `Batch 07`
2. `Batch 04`
3. `Batch 06`
4. `Batch 02`
5. `Batch 08`
6. `Batch 01`
7. `Batch 09`

Reason:

- Batch 03 and Batch 05 now cover the critical runtime and composite-control mechanics
- next inspect real multiplayer/menu screens that exercise those controls and modal/shell behavior together
- then backfill primitive controls and remaining single-player shell screens
- leave peripheral helpers, networking internals, bootstrap, and leftovers until the screen/control surface is mapped

## Tracker Maintenance Rules

When updating this document in future passes:

- keep prior summaries unless the source disproves them
- replace `Inferred` with `Observed` only after reading the file directly
- keep replacement ideas framework-oriented, not legacy-architecture-oriented
- record when a file should be split across multiple framework concepts
- avoid porting DACOM/container patterns unless behavior absolutely depends on them

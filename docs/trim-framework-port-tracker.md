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

- `Pch.cpp`: precompiled-header translation unit only; no runtime behavior and no port value. Main types: none. Key deps: `pch.h`. Replacement: none. Port Priority: Low. Confidence: `Observed`.
- `cqpipeline.cpp`: thread-safe `IRenderPipeline` adapter inserted by the system container; forwards almost the entire render-pipeline surface through an optional critical section and resets some global texture/vertex-buffer accounting on startup. Main types: `CQPipeline`, `IRenderPipeline`. Key deps: `SysContainer.cpp`, `rendpipeline.h`, `TComponent2.h`, global `TEXMEMORYUSED` / `VBMEMORYUSED`. Replacement: internal renderer-backend facade or synchronization wrapper, not a framework control or app-level node. Port Priority: Medium. Notes: this is an adapter around the real pipeline, not the renderer implementation itself. Confidence: `Observed`.
- `GenData.cpp`: global archetype database and factory service; loads packed generated archetype/type data, caches loaded archetypes with refcounts, creates component instances through `ICQFactory`, exposes raw archetype data, and can copy/open the backing data file. Main types: `ARCHDATATYPE`, `ARCHDATA`, `GENNODE`, `GenData`, `_genlist`. Key deps: `GenData.h`, `Document`, `MemFile`, `FileSys`, `EventSys2`, `UserDefaults`, `Cursor`. Replacement: typed asset/archetype registry plus explicit factories only where legacy generated data is still required; avoid porting DACOM factory plumbing into framework core. Port Priority: High. Notes: broader than UI definitions; this is one of the old data-driven construction seams. Confidence: `Observed`.
- `IniConfig.cpp`: legacy render-device INI helper, not general settings persistence; patches `RendN` sections with enumerated device GUIDs, contains mostly disabled/stubbed DirectDraw/Direct3D capability tests, and leaves video-mode enumeration commented out. Main types: `FileData`. Key deps: `UserDefaults`, `DEffectOpts`, Win32 file mapping APIs, old Direct3D headers. Replacement: omit for the Raylib-first port or keep only as a one-off config migration/import helper. Port Priority: Low. Notes: much of the original render-device probing path is already dead code in this snapshot. Confidence: `Observed`.
- `LogFile.cpp`: `IFileSystem` decorator for asset-usage tracing; forwards file operations to an underlying file system, intercepts `CreateInstance`/`OpenChild` to record accessed file names, merges prior log contents, and writes a deduplicated `%ACTION% ... %DEST%` list on shutdown. Main types: `LFNODE`, `LogFile`. Key deps: `IFileSystem`, `FileSys`, `THashList`, `TComponent2.h`. Replacement: dev-only file-access tracing wrapper around the new asset/file service. Port Priority: Low. Notes: this is instrumentation, not the main logging system. Confidence: `Observed`.
- `Objwatch.cpp`: intrusive object-pointer watcher maintenance, not a generic debug watcher; registers `OBJPTR<IBaseObject>` links on target objects and clears/invalidate-watcher lists by player, volatility class, or full object teardown. Main types: `OBJPTR<IBaseObject>` helpers via free functions. Key deps: `objwatch.h`, `IBaseObject` ownership/watch lists. Replacement: engine-side weak-handle or observer invalidation helper for gameplay objects, only if the port keeps this lifetime model. Port Priority: Low. Notes: prior summary was wrong; this is object lifetime bookkeeping, not diagnostics UI. Confidence: `Observed`.
- `PrintHeap.cpp`: debug memory-dump helper with localized string/message-box shims; walks heap allocations, prints block summaries, and can mark allocated blocks for later diagnostics. Main types: free functions `_localprintf`, `_localMessageBox`, `PrintHeap`, `MarkAllocatedBlocks`. Key deps: `HeapObj.h`, `CQTrace`, Win32 string/message APIs. Replacement: optional dev-only memory diagnostics utility. Port Priority: Low. Confidence: `Observed`.
- `SysContainer.cpp`: dynamic aggregate runtime container; reads the `[System]` profile section, instantiates listed DACOM components, tracks `ISystemComponent` / `IAggregateComponent` members, updates and shuts them down, exposes connection-point enumeration, and prepends the `CQPipeline` wrapper. Main types: `SysConInner`, `SystemContainer`, nested `ELEMENT`. Key deps: `system.h`, `IProfileParser`, `IConnection`, `cqpipeline.cpp`. Replacement: do not port literally; replace with explicit bootstrap wiring and typed service registration. Port Priority: High. Notes: useful as a dependency map for old startup ordering, but not as a target architecture. Confidence: `Observed`.
- `System.cpp`: large runtime glue layer spanning much more than startup; includes posted-message dispatch for `BaseHotRect`, primitive-builder helpers, global startup/cleanup registries, hotkey-event peeking, render/video option parsing, gamma and window/fullscreen mode control, 3D pipeline startup/shutdown, hardpoint enumeration helpers, profile-parser creation, and some file-system utilities. Main types: `CLEANUP_NODE`, `STARTUP_NODE`, global `PrimitiveBuilder2 PB`, plus several free runtime functions (`CreateGlobalComponents`, `ReadRenderOptions`, `ParseVideoINI`, `Start3DMode`, `Shutdown3DMode`, `Enable3DMode`). Key deps: `WindowManager`, `UserDefaults`, `VideoSurface`, `DrawAgent`, `TManager`, `CQBatch`, `EventSys2`, render-pipeline/texture/vertex-buffer interfaces. Replacement: split across app bootstrap, renderer lifecycle management, utility helpers, and framework/runtime services rather than keeping one catch-all module. Port Priority: High. Notes: this file is a major boundary problem and should be decomposed during port. Confidence: `Observed`.
- `TManager.cpp`: texture/resource manager, not a general subsystem manager; caches textures by name with refcounts, builds textures from BMP/TGA/VFX readers, generates mipmaps and bumpmap conversions, manages reusable draw-agent textures, and responds to update events to recycle temporary texture slots. Main types: `TMNODE`, `DANODE`, `TManager`, `_tmanager`. Key deps: `IImageReader`, `WindowManager`, `EventSys2`, `FileSys`, `HKEvent`, render pipeline texture APIs, legacy readers created in Batch 02. Replacement: renderer texture cache/resource service with atlas/texture uploads and explicit lifetime management. Port Priority: High. Notes: this is one of the concrete render-resource services the new framework/runtime needs. Confidence: `Observed`.
- `TestScript.cpp`: developer network-test harness; writes or reuses `cqnet.log`, seeds a `FULLCQGAME` setup, toggles host/client test behavior from the command line, and can bootstrap mission/network test state. Main types: free functions `EnableLogging`, `RunTestScript`. Key deps: `CQGame.h`, `Mission`, `UserDefaults`, `FileSys`, `EventSys2`, `MScroll`, `InProgressAnim`. Replacement: omit from framework port; recreate only as debug/test tooling if automated multiplayer repro still matters. Port Priority: Low. Notes: app-debug support only. Confidence: `Observed`.
- `Trim.cpp`: DLL entry stub, not a registration root; seeds RNG on attach, acquires the heap, and sets up a placeholder heap-message path. Main types: `DllMain`. Key deps: `HeapObj.h`, `Time.h`. Replacement: none. Port Priority: Low. Notes: the earlier assumption about meaningful registration work here was incorrect. Confidence: `Observed`.
- `UserDefaults.cpp`: registry-backed settings/profile service with extra tooling hooks; loads/stores `USER_DEFAULTS`, supports per-player defaults, registry strings/binary values, MRU lists, input/output filenames, window placement, install-path lookups, user/script data viewers, and EULA launch. Main types: `UserDefaults`, `USER_DEFAULTS`, global `IUserDefaults::pUserDefaults`. Key deps: `WindowManager`, `Viewer`, `Document`, parser/viewer creation, Win32 registry/dialog APIs. Replacement: split into typed settings/profile persistence, file-picker/path helpers, and any editor/viewer tooling kept out of framework core. Port Priority: High. Notes: many menus depend on this, but a sizable portion is Windows/editor baggage that should not survive intact. Confidence: `Observed`.
- `WindowManager.cpp`: Win32 window and message-pump service; creates the singleton main window, owns the top-level `WndProc`, tracks app activation and cursor position, switches between fullscreen/windowed styles, serves the message queue, stores window/client rectangles, and dispatches exit handling. Main types: `WMInner`, `WindowManager`. Key deps: `System.h`, `TConnPoint` / `TConnContainer`, Win32 window APIs, global `hMainWindow`. Replacement: mostly `Windowing` backend/service; expose a smaller framework-facing surface above the Raylib-first platform layer. Port Priority: High. Notes: still a useful reference for behavior such as activation, window flags, and message pumping, but not a shape to port directly. Confidence: `Observed`.

### Batch 02 - Drawing, Resources, Media, Visual Plumbing

- `Animate.cpp`: archetype-backed animated UI/hud control that derives from `BaseHotRect`, loads frame shapes through `IShapeLoader`, advances cells on `CQE_UPDATE`, supports indexed sequences, looping, pause, deferred destruction, and optional talking-head fuzz/frame effects. Main types: `ANIMATETYPE`, `Animate`, `AnimateFactory`. Key deps: `BaseHotRect`, `DAnimate`, `GenData`, `DrawAgent`, `IShapeLoader`, `TManager`. Replacement: framework/app `AnimatedImageControl` or `SpriteAnimationPlayer` with frame-list resources and optional overlay effects; keep archetype/factory plumbing out of framework core. Port Priority: Medium. Notes: this is a real reusable control, but the talking-head fuzz path is app-specific. Confidence: `Observed`.
- `BmpRead.cpp`: in-memory BMP decoder implementing `IImageReader`; handles indexed and truecolor BMP variants, row/mask stride, palette depalettizing, region extraction, and output conversion to indexed/RGB/RGBA buffers. Main types: `BMP_READER`. Key deps: `IImageReader`. Replacement: avoid a literal framework port; prefer offline conversion or a generic image decoder layer that outputs `Image`/`Texture` data. Port Priority: Low. Notes: keep only if legacy BMP assets remain in active use at runtime. Confidence: `Observed`.
- `Camera.cpp`: full gameplay/world camera service, not a shell helper; wraps `BaseCamera`/viewer state, panes, transforms, FOV/orbit/zoom/rotation/shake/movie mode, screen/world projection, frustum tests, mouse wheel zoom, and hotkey-driven movement. Main types: `Camera`, global `_camera`. Key deps: `Camera.h`, `SuperTrans`, `Sector`, `VideoSurface`, `WindowManager`, `EventSys2`, `BaseCam`/`Viewer`/`Engine`, `Hotkeys`, `UserDefaults`. Replacement: engine/app `WorldCameraService` or `Camera3DNode` plus input adapter, not a framework UI camera primitive. Port Priority: High. Notes: prior summary was too UI-oriented; this is a core gameplay render/input bridge. Confidence: `Observed`.
- `CQImage.cpp`: crash/assert/error reporting and dump infrastructure, not an image wrapper; records stack traces, symbol tables, memory reports, modal error dialogs, and exception handling around `ICQImage::Assert/Bomb/Error/Exception`. Main types: `CQImage`, `ICQImage`, `TEXT_BUFFER`, `CQERROR_TYPE`. Key deps: `WindowManager`, `dbghelp`, `EventSys2`, `ObjMapIterator`, Win32 dialog APIs. Replacement: framework/app diagnostics service with logging, asserts, crash dumps, and optional dev-only UI. Port Priority: Medium. Notes: this batch entry was misclassified. Confidence: `Observed`.
- `DrawAgent.cpp`: primary 2D draw-resource implementation; converts image readers or VFX shapes into drawable textured quads, supports block-based texture uploads, debug-font rendering, JSON atlas/frame replacement for some VFX assets, and shared primitive helpers like line/point/rectangle/texture draw. Main types: `DrawAgent`, `FontDrawAgent`, `VFX_SHAPETABLE_EX`, `BLOCKRECT`. Key deps: `IImageReader`, `VideoSurface`, `CQBatch`, `TManager`, `Camera`, `RendPipeline`, `MyVertex`, `VFX_shapes.hpp`. Replacement: framework render-resource layer split into `TextureRegion`, `SpriteFrame`, bitmap-font/debug-font helpers, and low-level canvas draw primitives. Port Priority: High. Notes: this is one of the main rendering seams that many controls depend on. Confidence: `Observed`.
- `DrawAgent16.cpp`: GDI-backed font rasterizer that also exposes itself as an `IImageReader`; renders wide strings into temporary bitmaps, converts them to RGBA/index data, duplicates agents, and includes a `NumberFont` specialization plus a factory for font archetypes. Main types: `FontDrawAgent16`, `NumberFont`, `FontFactory`. Key deps: `DrawAgent.h`, `IImageReader`, `DFonts`, Win32 `HFONT`/GDI, `GenData`. Replacement: do not port literally; replace with a font raster/cache service or bitmap font path built on the new text system. Port Priority: Medium. Notes: prior summary was too generic; this is about text rasterization, not a second general image draw path. Confidence: `Observed`.
- `GameProgress.cpp`: persistent single-player progression bitfield service stored per profile under `SavedGame\\<player>\\player.gpf`; tracks missions completed/seen and movies seen, plus a temporary mission bitmask and forced-intro flag. Main types: `PROGRESS_DATA`, `GameProgress`. Key deps: `IGameProgress`, `UserDefaults`, `FileSys`. Replacement: app-level campaign/profile progression service with typed save data, not a framework progress widget. Port Priority: Medium. Notes: this batch entry was misclassified. Confidence: `Observed`.
- `GridVector.cpp`: tiny quantized world-grid math helper; `GRIDVECTOR`/`NETGRIDVECTOR` encode coarse map positions in quarter-cell units and use a precomputed distance table for cheap approximate distance. Main types: `GRIDVECTOR`, `NETGRIDVECTOR`, `__gridvector_setup`. Key deps: `GridVector.h`, `Vector`. Replacement: shared engine math/helper type only if network/gameplay code still benefits from this packed representation. Port Priority: Low. Notes: not UI-related. Confidence: `Observed`.
- `LineManager.cpp`: transient world/screen debug-visual line service; tracks timed line objects between screen points and/or game objects, redraws them each frame via `DA::LineDraw`, and removes expired entries. Main types: `LineBaseObj`, `Line2DTo2D`, `Line2DToObj`, `LineObjToObj`, `LineManager`. Key deps: `ILineManager`, `ObjList`, `IObject`, `Camera`, `Frame`, `SFX`, `DrawAgent` line primitives. Replacement: debug overlay/canvas service outside framework core, or a lightweight diagnostics draw list. Port Priority: Low. Notes: more instrumentation than general UI line styling. Confidence: `Observed`.
- `lines.cpp`: standalone software antialiased line rasterizer that writes directly into a locked 16-bit `VFX_WINDOW` buffer using `PixelToColorRef`/`ColorRefToPixel`. Main types: free functions `AALine`, `IntensifyPixel`. Key deps: `PANE`, legacy VFX pixel conversion helpers. Replacement: none in the Raylib-first framework; use GPU line drawing or a simple software fallback internal to rendering if ever required. Port Priority: Low. Confidence: `Observed`.
- `LoadFont.cpp`: startup/shutdown helper that reads the `FONTS` profile section and registers/removes Win32 font resources with `AddFontResource` / `RemoveFontResource`. Main types: `_loadfont`. Key deps: `IProfileParser`, Win32 font APIs. Replacement: app bootstrap font registration or asset preload hook, not a framework text-measurement service. Port Priority: Low. Notes: this batch entry was misclassified. Confidence: `Observed`.
- `MovieScreen.cpp`: fullscreen modal movie player screen; loads a video through `VIDEOSYS`, updates frames on `CQE_UPDATE`, draws a letterboxed textured video quad on `CQE_ENDFRAME`, suppresses frame limiting, hides the cursor, and temporarily mutes music during modal playback. Main types: `MenuMovie`, `MovieScreen()` entrypoint. Key deps: `Frame`, `VideoSystem`, `SoundManager`, `Mission`, `Cursor`, modal runtime. Replacement: app-level `VideoModalScreen` built on a framework screen/modal stack plus a video playback service. Port Priority: Medium. Notes: more screen flow than framework media primitive. Confidence: `Observed`.
- `MultiLineFont.cpp`: multiline/wrapping font draw agent built with GDI; rasterizes text into a bitmap, measures per-character widths, computes line breaks against pane width, and exposes the same `IFontDrawAgent` + `IImageReader` contract as the single-line font path. Main types: `MLFont`. Key deps: `DrawAgent.h`, `IImageReader`, `GenData`, Win32 `HFONT`/GDI. Replacement: framework text layout engine with wrapping and measurement separated from raster backend. Port Priority: High. Notes: the wrapping/measurement behavior matters more than the GDI implementation. Confidence: `Observed`.
- `MusicManager.cpp`: high-level background music service; streams WAVs with `STREAMER`, persists state, initializes race-based playlists, handles smooth transitions, updates playback on document events, and reacts to streamer completion notifications. Main types: `MManager`, global `_music`. Key deps: `MusicManager.h`, `Streamer`, `SoundManager`, `Mission`, `DocumentClient`, `EventSys2`, `UserDefaults`. Replacement: app-level music service with playlist/state logic over the new audio backend. Port Priority: Medium. Confidence: `Observed`.
- `SFX.cpp`: low-level sound-effect bank/player; loads effect metadata and chunks, preloads and reference-counts DirectSound buffers, computes pan/volume from object positions and camera state, updates live instances, and exposes global sound IDs/settings. Main types: `Sfx`, `SFX_ENTRY`, `SoundEffect`, `SoundInstance`. Key deps: `SFX.h`, `UserDefaults`, `Sector`, `FogOfWar`, `Camera`, `GenData`, `EventSys2`, DirectSound/ACM. Replacement: app/engine sound-effect service with positional playback and caching over a modern audio API. Port Priority: Medium. Notes: keep this out of framework-core UI boundaries. Confidence: `Observed`.
- `ShapeLoader.cpp`: small asset adapter around generated `GT_VFXSHAPE` archetypes; loads raw BMP/TGA/VFX files from `INTERFACEDIR`, infers file type by extension, then creates either `IImageReader` or `IDrawAgent` instances for specific subimages. Main types: `SHPTYPE`, `ShapeLoader`, `ShapeLoaderFactory`. Key deps: `IShapeLoader`, `GenData`, `IImageReader`, `DrawAgent`, `FileSys`. Replacement: framework/app resource loader that resolves a sprite sheet or image asset into `TextureRegion` objects without DACOM factories. Port Priority: High. Confidence: `Observed`.
- `SoundManager.cpp`: high-level speech/chat/movie audio orchestrator; owns stream lists and pending playback, coordinates `STREAMER`, `SFX`, and `MusicManager`, can attach sounds to world objects or paired `IAnimate` talking-head animations, blits movie textures, exposes volume UI/state, and handles pause/mute rules. Main types: `SOUNDSTATE`, `STREAM_NODE`, `PENDING_NODE`, `SoundMan`. Key deps: `Streamer`, `SFX`, `MusicManager`, `IAnimate`, `LFParser`, `ObjList`, `IBriefing`, `EventSys2`, `FileSys`. Replacement: app-level media playback coordinator split into voice/chat/movie channels plus optional talking-head presenter. Port Priority: High. Notes: broader than sound settings; this is a central audio/video presentation service. Confidence: `Observed`.
- `SpaceEnv.cpp`: 3D background environment renderer for space scenes; loads per-system nebula meshes/shapes, owns vertex-buffer-backed background geometry, restores lost buffers, sorts/renders meshes, and integrates with the active camera/sector as an `IBackground`. Main types: `SpaceEnvironment`, `ColorRGB`. Key deps: `IBackground`, `Camera`, `Sector`, `TManager`, `IVertexBuffer`, `renderer`/`mesh`, `CQBatch`. Replacement: engine/app scene-background renderer, likely outside the UI framework entirely. Port Priority: Low. Notes: prior summary was directionally right but this is gameplay/world rendering, not shell-only backdrop logic. Confidence: `Observed`.
- `Streamer.cpp`: DirectSound/streamer bootstrap; reads sound device config, creates/initializes `DSOUND`, sets cooperative level and primary buffer format, and initializes the global `STREAMER` service with buffer timing and window callbacks. Main types: `_streamer`. Key deps: `Streamer.h`, `WindowManager`, `IProfileParser`, DirectSound. Replacement: audio backend initialization during app startup, not a framework media-resource loader. Port Priority: Medium. Confidence: `Observed`.
- `StringData.cpp`: bulk binary string-pack/archetype-data loader, not simple localization lookup; recursively reads every file under `StringPack.db`, stores each file blob under its filename, and serves raw `M_STRING*`/size pairs by name through `IStringData`. Main types: `ARCHDATATYPE`, `ARCHDATA`, `StringData`. Key deps: `StringData.h`, `FileSys`, `MemFile`, `EventSys2`, `Cursor`, `UserDefaults`. Replacement: typed resource/string-pack service with explicit parsing and lookup APIs instead of raw file blobs by filename. Port Priority: Medium. Notes: this batch entry was only partially right; it is a generic packed-data registry, not just UI text localization. Confidence: `Observed`.
- `Subtitle.cpp`: global subtitle overlay service for briefings and general playback; stores current wide-text line keyed to a sound handle, redraws it each frame near the bottom of the screen, and reloads fonts when display mode changes. Main types: `Subtitle`, global `_subtitle`. Key deps: `ISubtitle`, `DrawAgent`, `SoundManager`, `Frame`, `BaseHotRect`, `EventSys2`. Replacement: top-layer subtitle/status-caption overlay driven by media playback state. Port Priority: Medium. Confidence: `Observed`.
- `Tgaread.cpp`: in-memory TGA decoder implementing `IImageReader`; parses headers, supports paletted/truecolor and RLE-compressed data, and converts whole-image or rect subsets into indexed/RGB/RGBA buffers. Main types: `TGAHEADER`, `TGAREADER`. Key deps: `IImageReader`, `CQTrace`. Replacement: prefer generic image decoding or offline conversion; keep only if runtime TGA loading remains necessary. Port Priority: Low. Confidence: `Observed`.
- `VertexBuffer.cpp`: tiny lost-device restore registry for objects that own vertex buffers; tracks `IVertexBufferOwner` instances, calls `RestoreVertexBuffers()` on each, then restores batch and light resources. Main types: `CQ_VB_Manager`, global `vb_mgr`, `RestoreAllSurfaces()`. Key deps: `IVertexBuffer`, `CQBatch`, `CQLight`, `IDDBackDoor`. Replacement: none as-is in Raylib; fold any needed device-loss recovery into the renderer backend. Port Priority: Low. Notes: this batch entry was overstated. Confidence: `Observed`.
- `VfxRead.cpp`: decoder for legacy `.shp`/VFX shape tables implementing `IImageReader`; exposes frame dimensions, palette extraction, and indexed/RGB/RGBA conversion for a chosen subimage. Main types: `VFXREADER`. Key deps: `IImageReader`, `VFX_shapes.hpp`. Replacement: legacy-shape import/decoder layer or offline conversion to atlas/image assets. Port Priority: Medium. Confidence: `Observed`.
- `VideoSurface.cpp`: display-surface access wrapper, not video playback; exposes the current render/back buffer as `VFX_WINDOW`/`PANE`, locks/unlocks the pipeline buffer, can lock the DirectDraw primary/front buffer, and records pixel-format masks/shifts for software drawing paths. Main types: `IVideoSurface`, `VideoSurface`. Key deps: `RendPipeline`, `IDDBackDoor`, `WindowManager`, DirectDraw surface APIs. Replacement: internal renderer framebuffer access abstraction only if software-readback/draw is still needed; otherwise omit from framework API. Port Priority: Medium. Notes: this batch entry was misclassified. Confidence: `Observed`.
- `VoxCompress.cpp`: ACM-based voice compression/decompression service; negotiates wave formats, opens codec streams, checks working buffer sizes, and compresses or decompresses VOX/voice payloads. Main types: `VoxCompression`, `VOXACM_WAVEFORMATEX`. Key deps: `VoxCompress.h`, `IProfileParser`, WinMM/ACM. Replacement: omit for first framework port unless legacy voice chat is revived; if needed, isolate behind a codec service. Port Priority: Low. Confidence: `Observed`.

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

- `EulaWin.cpp`: Win32 EULA dialog outside the Trim `Frame` system; streams an RTF file into a RichEdit control, draws localized Accept/Decline button labels with custom fonts for some locales, and persists acceptance in registry-backed `UserDefaults`. Main types: global dialog state, `eulaDialogCallback`, `buttonCallback`. Key deps: `UserDefaults`, Win32 dialog APIs, RichEdit `EM_STREAMIN`, localized string helpers. Replacement: none for framework port unless startup/legal compliance explicitly requires it; if ever needed, keep it as app/bootstrap UI, not a framework `Control`. Port Priority: Very Low. Notes: effectively ignorable for framework work; this file sits in `src\\Conquest\\`, not `src\\Conquest\\Trim\\`; do not spend port effort on the custom button-paint path. Confidence: `Observed`.
- `Menu_Briefing.cpp`: scripted mission-briefing modal implementing `IBriefing`; loads briefing data from `Mission`, plays teletype text, animated comm portraits, streamed audio, and optional replay/start/cancel flow while ticking mission state in the modal. Main types: `MenuBriefing`, `IBriefing`. Key deps: `Mission`, `MScript`, `ITeletype`, `SoundManager`, `MusicManager`, `Streamer`, `IAnimate`, `IButton2`, `IStatic`. Replacement: app-level briefing screen presenter with timeline/script playback services for audio, subtitles/teletype, portrait/video slots, and mission-start actions. Port Priority: High. Notes: substantially richer than a text briefing page; it is a small scripted media runtime. Confidence: `Observed`.
- `Menu_campaign.cpp`: single-player campaign entry screen; shows player name from defaults, selects race/training branch, optionally skips straight into Terran mission flow, checks `GAMEPROGRESS`, and hands off to `DoMenu_mission` or `DoMenu_Briefing`. Main types: `Menu_campaign`. Key deps: `Mission`, `IGameProgress`, `MusicManager`, `UserDefaults`, `Menu_mission.cpp`, `Menu_Briefing.cpp`. Replacement: single-player campaign chooser screen with explicit campaign/training actions and profile/progression service injection. Port Priority: Medium. Notes: despite the name, this is mostly a front-door router into campaign/training flows, not a full progression UI by itself. Confidence: `Observed`.
- `Menu_Confirm.cpp`: generic in-game/custom message box wrapper over archetyped Trim controls; centers the modal, supports `MB_OK`, `MB_OKCANCEL`, and `MB_YESNO`, pauses single-player while open, disables edge-scroll, forces cursor ownership, and falls back to Win32 `MessageBoxW` if Trim archetype data is unavailable. Main types: `Menu_CQMessageBox`, overloaded `CQMessageBox` helpers. Key deps: `GENDATA`, `Cursor`, `MScroll`, `Mission`, `EventSys2`, `IButton2`, `IStatic`. Replacement: framework/app modal dialog service with typed button sets and async result handling, plus app-owned pause/input suppression policy. Port Priority: High. Notes: this is the reusable confirmation primitive for many other screens, not just one menu. Confidence: `Observed`.
- `Menu_Credits.cpp`: scrolling credits modal that reads `credits.txt`, parses lightweight line markers into title/name rows, duplicates font draw agents for foreground/shadow text, scrolls rows upward over a static background, and exits on click or Esc. Main types: `CreditText`, `Menu_Credits`. Key deps: `IStatic`, `IFontDrawAgent`, `DrawAgent`, `MusicManager`. Replacement: lightweight credits/splash screen with streamed text model and text-style resources; keep parsing/data outside framework-core. Port Priority: Low. Notes: functionally a bespoke credits scroller, not a reusable list control. Confidence: `Observed`.
- `Menu_final.cpp`: final multiplayer/skirmish staging pane layered over child map/slot screens; coordinates accept/start/cancel flow, ready-state countdown, host validation rules, latency resync, exit/drop confirmation, and focus handoff between sibling groups before game start. Main types: `Menu_final`. Key deps: `ICQGame`, `NetBuffer`, `NetPacket`, `Menu_map.cpp`, `Menu_slots.cpp`, `Menu_Confirm.cpp`, `IButton2`, `IStatic`. Replacement: multiplayer final-check screen/presenter with validation rules and countdown state in a session service, plus explicit child-panel composition instead of cross-frame focus wiring. Port Priority: High. Notes: prior summary understated that this file owns readiness validation and launch gating, not just a passive confirmation page. Confidence: `Observed`.
- `Menu_help.cpp`: about/help modal, not a document browser; shows version and product ID/legal strings, plays main-menu music on focus, and opens `DoMenu_Credits`. Main types: `Menu_help`. Key deps: `MusicManager`, `UserDefaults`, `Menu_Credits.cpp`, `IButton2`, `IStatic`. Replacement: simple about/help modal with version/build/license fields and a credits action. Port Priority: Low. Notes: current summary was too broad; there is no scrollable help system here. Confidence: `Observed`.
- `Menu_igoptions.cpp`: in-game pause/options modal with save, load, options, restart, resign, abdicate, and return actions; launches nested save/load or options modals, pauses single-player while open, and gates buttons by single-player vs multiplayer state. Main types: `dummy_igoptions`, `Menu_igoptions`. Key deps: `Mission`, `Hotkeys`, `CreateOptionsMenu`, `CreateMenuLoadSaveSpecial`, `EventSys2`, `IButton2`, `IStatic`. Replacement: pause/options overlay composed from reusable buttons with app-level commands for save/load/settings/restart/resign. Port Priority: Medium. Notes: this is a command hub, not the main settings screen itself. Confidence: `Observed`.
- `Menu_LoadSave.cpp`: save/load modal for mission files; enumerates `*.mission` entries from `SAVEDIR`, filters SP vs MP saves by filename prefix, shows descriptions via `Mission`, edits save descriptions, handles overwrite/delete confirmation, and calls `MISSION->Load` / `SaveByDescription`. Main types: `MenuLoadSave`. Key deps: `Mission`, `IFileSystem`, `Menu_Confirm.cpp`, `IEdit2`, `IListbox`, `IButton2`, `IStatic`. Replacement: save/load dialog backed by a savegame service that exposes slots/metadata and command methods, with file naming conventions kept outside widget code. Port Priority: High. Notes: reusable shell behavior, but the file-system and naming policy should move behind a service boundary. Confidence: `Observed`.
- `Menu_map.cpp`: multiplayer/skirmish map-and-rules settings panel over `ICQGame`; owns dropdowns/sliders/toggles for map type, template, money, terrain, units, visibility, speed, command points, difficulty, spectator/diplomacy/lock-settings flags, derives descriptions/max players from selected files, and opens `DoMenu_MapSelect` plus `DoMenu_slots`. Main types: `Menu_map`. Key deps: `ICQGame`, `Mission`, `MapGen`, `NetBuffer`, `Menu_MapSelect.cpp`, `Menu_slots.cpp`, `Dropdown.cpp`, `Slider.cpp`. Replacement: lobby rules/map settings panel with typed view models and validation, backed by session/map services rather than direct mission-file probing inside the screen. Port Priority: High. Notes: this is one of the main multiplayer composition panels and a strong consumer of framework dropdown/slider primitives. Confidence: `Observed`.
- `Menu_MapSelect.cpp`: modal map picker for three sources: random templates, supplied multiplayer maps, and saved multiplayer missions; enumerates files from `MPMAPDIR` and `SAVEDIR`, keeps a side list mapping supplied-map display rows to file names, updates `ICQGame` map type/name on selection, and returns the selected template string ID for random maps. Main types: `Menu_MapSelect`, nested `SuppliedFile`. Key deps: `ICQGame`, `Mission`, `IFileSystem`, `IListbox`, `IButton2`, `IStatic`. Replacement: map-selection modal with grouped sources and preview/metadata model, returning a typed selection object instead of string-ID/file-name side effects. Port Priority: Medium. Notes: more of a specialized chooser than a general-purpose browser; current summary was directionally right but too generic. Confidence: `Observed`.
- `Menu_mission.cpp`: single-player mission/movie progression screen; draws mission graph lines, unlocks nodes from `GAMEPROGRESS`, shows hover descriptions, lists mission files from disk, opens `DoMenu_Briefing` for the chosen mission, and can also expose unlock/debug shortcuts and movie buttons. Main types: `Menu_mission`, `LineVar`. Key deps: `Mission`, `IGameProgress`, `SFX`, `MusicManager`, `Menu_Briefing.cpp`, `IAnimate`, `IButton2`, `IListbox`, `IStatic`. Replacement: campaign progression screen with explicit node graph/presenter, mission metadata service, and separate movie unlock/playback handling. Port Priority: Medium. Notes: this is a bespoke campaign graph screen, not a generic mission list/detail layout. Confidence: `Observed`.

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

- `NetBuffer.cpp`: low-level DirectPlay buffer/throttle layer under the multiplayer shell; timestamps outgoing packets, simulates latency and packet loss for testing, tracks rolling throughput, rate-limits ordinary vs file-transfer traffic, performs player clock-sync/checksum exchange with `PT_PLAYER_SYNC`, and turns DirectPlay system messages into `CQE_NETADDPLAYER` / `CQE_NETDELETEPLAYER`. Main types: `SUPERBASE_PACKET`, `PLAYER_NODE`, `SYNC_PACKET`, `NetBuffer`. Key deps: `NetPacket` (`OnGuaranteedDeliveryFailure`), `UserDefaults`, `EventSys2`, DirectPlay globals, `Menu_mshell.cpp` (`SetMaxBandwidth`), `Menu_netloading.cpp` (`EnableThroughputLimiting`). Replacement: app-level multiplayer transport shim with packet queueing, per-peer timing stats, bandwidth policy, and diagnostics hooks; do not port the DirectPlay-specific buffer shape into framework core. Port Priority: High. Notes: actual file lives in `src\\Conquest\\`, not `src\\Conquest\\Trim\\`; this is a networking substrate, not a Trim control. Confidence: `Observed`.
- `NetConnect.cpp`: session bootstrap/teardown glue around DirectPlay, DirectPlay Lobby, and Zone score integration; exposes shutdown, optional Zone host-change notification, and host IP extraction from the active connection. Main functions/types: `StopNetConnection`, `SendZoneHostChange`, `StartNetConnection`, `GetHostIPAddress`, `AddressEnumStruct`. Key deps: `NetBuffer`, `NetFileTransfer`, `ZoneLobby`, DirectPlay/DirectPlayLobby globals, `Menu_netsess.cpp`, `Menu_mshell.cpp`. Replacement: app-level connection bootstrap service with explicit provider/lobby/session lifecycle APIs, plus optional host-address inspection for diagnostics or lobby UI. Port Priority: Medium. Notes: the checked-in `StartNetConnection()` currently does `StopNetConnection(); goto Done;`, so its connection-start path is effectively disabled in this source snapshot and should not be treated as a healthy reference implementation. Confidence: `Observed`.
- `NetConnectBuffers.cpp`: packed temporary result buffers for enumerating connection providers, available sessions, and current players through DirectPlay; validates providers by initializing temporary DP instances, drives async session enumeration start/continue/stop, and adds a small amount of UI coupling through busy-cursor and join-failure message calls. Main types: `CONN_BUFFER`, `SESSION_BUFFER`, `PLAYER_BUFFER`, `SAVED_CONNECTION`, `SAVED_SESSION`, `SAVED_PLAYER`. Key deps: `Cursor`, `Resource`, `NetConnectBuffers.h`, DirectPlay globals, `Menu_netsess.cpp`, `Menu_netsess2.cpp`. Replacement: session-discovery DTO lists returned by a multiplayer discovery service, with async polling/cancellation separated from UI and no packed in-place memory format exposed above the service boundary. Port Priority: Medium. Notes: this is mostly adapter/storage code for provider/session browser screens, not framework widget logic. Confidence: `Observed`.
- `NetFileTransfer.cpp`: custom unreliable file-transfer protocol layered over `PT_FILE_TRANSFER` packets; opens per-transfer channels, requests files by name, serves them either from callback-supplied content or DOS-backed files, streams 400-byte chunks with retry/timeout logic, and exposes per-channel progress enumeration for the loading screen. Main types: `FTCHANNEL`, `CREQUEST_PACKET`, `CREPLY_PACKET`, `DSEND_PACKET`, `DREPLY_PACKET`, `FileTransfer`. Key deps: `NetBuffer` (`TestFTPSend`, `Send`), `NetPacket` event routing, `FileSys`, `MemFile`, `EventSys2`, `Menu_netloading.cpp`. Replacement: async asset/map transfer service with per-transfer state objects, progress callbacks, retry policy, and pluggable file providers, kept entirely outside framework-core UI types. Port Priority: High. Notes: this is a real dependency of the multiplayer load flow, so the replacement needs a clear service boundary even if the first Ray port avoids peer file transfer. Confidence: `Observed`.
- `NetPacket.cpp`: the real multiplayer session transport manager above `NetBuffer`; adds reliable ordered delivery with per-peer send/receive queues, ACK/NACK/resend handling, host migration (`PT_HOST`, `PT_HOSTPEND`, `PT_HOSTPENDACK`), pause/turtle/boot detection, keepalive/update processing, player enumeration, and dispatch of received gameplay packets into `CQE_NETPACKET`. Main types: `PAUSE_PACKET`, `TURTLE_PACKET`, `PAUSEWARNING_PACKET`, `NEWHOST_PACKET`, `HOSTPEND_PACKET`, `HOSTPENDACK_PACKET`, `SUPERBASE_PACKET`, `PACKET_NODE`, `NETPLAYER`, `NetPacket`. Key deps: `NetBuffer`, `WindowManager`, `DrawAgent`, `EventSys2`, `GRPackets.h`, `Menu_mshell.cpp` (`Send`, `TestLowPrioritySend`), `Menu_Pause.cpp` (`EnumeratePlayers`, `GetPauseTimeForPlayer`, `GetTimeUntilBooting`), `NetFileTransfer.cpp` via `CQE_NETPACKET`. Replacement: central multiplayer session service that owns reliability, host-authority state, pause/disconnect policy, and packet dispatch behind typed messages or commands; keep UI screens as observers/command senders only. Port Priority: High. Notes: the earlier placeholder was far too small; this file is the main networking state machine for the multiplayer shell. Confidence: `Observed`.

### Batch 09 - Misc Legacy Helpers Bound Into Trim

- `DumpView.cpp`: global debug/error dump sink and optional live dump window; registers the standard heap/error handler, can open a RichEdit-backed top-level dialog, streams trace/assert/error text into the dialog and/or `CQDump.txt`, persists dialog placement, and logs the build version during startup. Main types: `DumpView`, global `view`. Key deps: `Startup`, `UserDefaults`, `WindowManager`, `FileSys`, `DBHotkeys`, heap error handling, Win32 dialog/RichEdit APIs. Replacement: dev-only diagnostics console/log sink with optional in-app debug window; keep it outside framework-core runtime. Port Priority: Low. Notes: this is instrumentation infrastructure, not a gameplay or UI screen. Confidence: `Observed`.
- `LFParser.cpp`: asynchronous lip-flap timing parser for speech assets, not a general layout/form parser; reads a file from `MSPEECHDIR`, handles overlapped I/O and CD-ROM retry prompts, parses per-line mouth-frame values into a read-only `U32` frame array, and repeats the last frame when a line contains alphabetic phoneme text instead of a numeric frame. Main types: `LFParser`, `ILFParser`. Key deps: `LFParser.h`, `FileSys`, `MSPEECHDIR`, `Resource` message prompts, `CQTrace`. Replacement: app/media service helper for talking-head or subtitle/voice playback timing, or omit entirely if lip-flap presentation is dropped. Port Priority: Low. Notes: the earlier placeholder was wrong; this is a specialized media parser, not generic UI markup/data parsing. Confidence: `Observed`.

## Cross-Cutting Conclusions So Far

### 1. Batch 01 Confirms The Runtime Split

Batch 01 showed that the old Trim/bootstrap surface is not one subsystem. It breaks into at least seven separate replacement surfaces:

- platform windowing and message pump: `WindowManager.cpp`
- render backend synchronization/wrapping: `cqpipeline.cpp`
- renderer lifecycle and mode switching: large parts of `System.cpp`
- texture/resource cache and uploads: `TManager.cpp`
- archetype/resource factory data: `GenData.cpp`
- settings/profile persistence and tooling hooks: `UserDefaults.cpp`
- dev/debug instrumentation and test harnesses: `LogFile.cpp`, `PrintHeap.cpp`, `TestScript.cpp`, `Trim.cpp`, `Objwatch.cpp`, `IniConfig.cpp`

The main framework conclusions are:

- the port needs a hard split between framework runtime, renderer backend, app bootstrap, and app services
- `System.cpp` is too mixed to port as a unit; its responsibilities should be decomposed early
- `SysContainer.cpp` is useful for discovering startup dependencies, but its aggregate/DACOM container shape should not survive
- `WindowManager.cpp` and `cqpipeline.cpp` confirm that platform and renderer synchronization belong below framework controls
- `GenData.cpp` and `UserDefaults.cpp` are major service dependencies for many screens, but both should be split into narrower typed services in the new codebase
- several Batch 01 files are dev-only or obsolete enough that they should not influence framework architecture at all

### 2. Layout Alone Is Not The Main Port Risk

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

Batch 01 and Batch 02 add two more constraints:

- runtime/bootstrap decomposition matters as much as widget layout
- render/resource ownership is also a first-class port surface, especially for images, fonts, animation frames, subtitles, and media overlays

### 3. Combobox Confirms The Needed Runtime Shape

From `Combobox.cpp`, the framework needs:

- parent-owned child control composition
- focus transfer between child controls
- visible/hidden popup ownership
- control IDs or a cleaner event source model
- event dispatch for click, selection, edit changed, and escape
- draw-order control for active popups

### 4. Menu Screens Confirm The Screen-Shell Problem

From `Menu_mshell.cpp`, Trim screens are not just layout files. They also own:

- control creation
- screen state
- focus group behavior
- child screen composition
- network state transitions
- modal entry/exit
- async/polling behavior

Framework replacement therefore needs reusable screen-shell conventions, not just controls.

### 5. Batch 02 Confirms The Render And Media Split

Batch 02 showed that "drawing/resources/media" is not one subsystem. It breaks into at least six separate replacement surfaces:

- render resources and sprite/frame drawing: `DrawAgent.cpp`, `ShapeLoader.cpp`, `VfxRead.cpp`, `BmpRead.cpp`, `Tgaread.cpp`
- text raster/layout: `DrawAgent16.cpp`, `MultiLineFont.cpp`, `LoadFont.cpp`
- framebuffer and lost-device plumbing: `VideoSurface.cpp`, `VertexBuffer.cpp`, parts of `lines.cpp`
- audio/media playback services: `Streamer.cpp`, `MusicManager.cpp`, `SFX.cpp`, `SoundManager.cpp`, `MovieScreen.cpp`, `Subtitle.cpp`, `VoxCompress.cpp`
- world/render-engine systems rather than UI framework: `Camera.cpp`, `SpaceEnv.cpp`, `GridVector.cpp`, `LineManager.cpp`
- app data/services rather than visual plumbing: `GameProgress.cpp`, `StringData.cpp`, `CQImage.cpp`

The main framework conclusions are:

- the framework needs an explicit render-resource layer, not just controls calling ad hoc draw helpers
- the old shape-file pipeline should not be the target architecture; prefer `_atlas.json` + `.png` sprite-atlas assets and typed frame metadata
- text measurement/wrapping is a real framework concern, but Win32 GDI raster paths are not
- video, speech, subtitles, and talking-head presentation belong in app/media services layered over the framework, not inside generic controls
- DirectDraw/DirectSound device-loss and surface-lock patterns should disappear behind the new renderer/audio backends

### 6. Batch 03 Confirms Three Separate Port Surfaces

Batch 03 is not one subsystem. It breaks into:

- input backends and action routing: `Cursor.cpp`, `HKManager.cpp`
- modal/frame runtime: `Modal.cpp`
- screen/shell and overlay behaviors: `Menu1.cpp`, `menu.cpp`, `Hintbox.cpp`

It also included two non-framework strays:

- `Macrohelp.cpp`: IDE-only stub file
- `SuperTrans.cpp`: engine math helper

### 7. Batch 05 Confirms The Framework Control Boundaries

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

### 8. Batch 07 Confirms The Multiplayer Shell Split

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

### 9. Batch 08 Confirms The Networking Stack Split

Batch 08 showed that the multiplayer/network layer under Trim is not one service. It already breaks into at least five distinct replacement surfaces:

- connection bootstrap/lobby integration: `NetConnect.cpp`
- provider/session/player enumeration adapters: `NetConnectBuffers.cpp`
- low-level transport timing/bandwidth/sync buffering: `NetBuffer.cpp`
- reliable ordered session transport and host migration: `NetPacket.cpp`
- ad hoc file/map transfer during load: `NetFileTransfer.cpp`

The main framework conclusions are:

- none of this belongs in framework-core controls or screen classes
- the port needs a dedicated multiplayer service boundary between UI screens and the transport/session stack
- reliability, ACK/NACK, host migration, pause/turtle detection, and boot policy should live together in a session service, not leak into screen code
- session discovery and provider enumeration should become typed async APIs, not packed in-place buffers owned by UI callers
- file transfer should be isolated behind its own async transfer service, even if the first Ray port stubs or defers peer map download
- `NetBuffer.cpp` and `NetPacket.cpp` confirm that packet timing, pacing, and reliability are separate concerns and should stay separated in the replacement shape

Batch 08 also corrected one earlier framing issue:

- these files live in `src\\Conquest\\`, not `src\\Conquest\\Trim\\`
- they are Trim dependencies, not Trim widgets
- `NetConnect.cpp` in this source snapshot contains an effectively disabled `StartNetConnection()` path, so it should not be treated as a trustworthy behavioral baseline

### 10. Batch 04 Clarifies The Primitive-Control Split

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

The attached `Static!!Background.xml` and `mainscreen_atlas` export confirm that at least one real `GT_STATIC` asset is just a fullscreen image background with no font data, which strengthens two conclusions:

- the framework should split `Panel`, `Image`, and `Label` concerns instead of keeping a single catch-all static control
- atlas-based image metadata is the right replacement direction; the legacy shape-file container should be treated as a source format, not a runtime target

### 11. Batch 06 Confirms The Shell-Screen Split

Batch 06 showed that "menu screens" still break into several distinct replacement shapes:

- app/bootstrap dialogs outside the framework screen tree: `EulaWin.cpp` (defer unless explicitly required)
- reusable modal shell primitives: `Menu_Confirm.cpp`, parts of `Menu_LoadSave.cpp`
- simple informational modals: `Menu_help.cpp`, `Menu_Credits.cpp`
- scripted media/presentation screens: `Menu_Briefing.cpp`
- progression/launcher screens: `Menu_campaign.cpp`, `Menu_mission.cpp`
- multiplayer composition panels and launch gating: `Menu_map.cpp`, `Menu_MapSelect.cpp`, `Menu_final.cpp`
- in-game pause command hub: `Menu_igoptions.cpp`

The main framework conclusions are:

- not every legacy "menu" should become a framework-owned screen type; several are plainly app-level flows
- the framework still needs a strong modal service, because confirmation, save/load, and picker flows recur across unrelated screens
- file-system, registry, mission metadata, and progression lookups should move behind services instead of living in widget code
- media/timeline playback for briefing-like screens is a separate surface from ordinary controls and layout
- sibling-panel focus handoff in multiplayer setup (`Menu_map.cpp`, `Menu_final.cpp`, `Menu_slots.cpp`) should become explicit screen composition with scoped focus navigation, not ad hoc cross-frame calls

Batch 06 also confirmed two naming mismatches from the earlier tracker assumptions:

- `EulaWin.cpp` is not a Trim `Frame` screen at all
- `Menu_help.cpp` is an about/legal modal, not a document-style help browser

### 12. Atlas Assets Should Replace Shape Files

The current framework port should assume the legacy VFX/BMP/TGA shape files are being cut over to `_atlas.json` + `.png` outputs everywhere.

That changes the desired replacement shape in a few important ways:

- `ShapeLoader.cpp` should map to atlas/frame lookup, not a new runtime parser for legacy shape containers
- `DrawAgent.cpp` should center on `TextureRegion` or sprite-frame metadata backed by atlas exports
- control/resource APIs should ask for named frames, subrects, and animation sequences, not raw shape subimage indices
- legacy readers like `BmpRead.cpp`, `Tgaread.cpp`, and `VfxRead.cpp` become import/compatibility paths, not preferred framework runtime dependencies

### 13. Batch 09 Confirms The Remaining Strays Are Dev Or Media Helpers

Batch 09 confirmed that the last unclassified files do not reopen a new framework surface:

- `DumpView.cpp` is dev-only diagnostics infrastructure
- `LFParser.cpp` is a narrow lip-sync/media timing helper

The main framework conclusions are:

- neither file should shape framework-core control or layout architecture
- `DumpView.cpp` belongs with debug tooling and crash/assert reporting, alongside earlier findings from `CQImage.cpp`, `LogFile.cpp`, and `PrintHeap.cpp`
- `LFParser.cpp` belongs, if anywhere, with app-level media presentation helpers near `SoundManager.cpp` and talking-head playback rather than with generic parsers or screen loading
- the planned tracker discovery pass is now complete; remaining work is synthesis, prioritization, or implementation planning rather than more broad source classification

## Suggested Next Analysis Order

1. No remaining planned analysis batches

Reason:

- Batches 01 through 09 are now observed
- the tracker now covers the full planned Trim surface at the batch level
- the next useful step is implementation planning or framework work ordered by the existing port priorities, not another open-ended classification pass

## Tracker Maintenance Rules

When updating this document in future passes:

- keep prior summaries unless the source disproves them
- replace `Inferred` with `Observed` only after reading the file directly
- keep replacement ideas framework-oriented, not legacy-architecture-oriented
- record when a file should be split across multiple framework concepts
- avoid porting DACOM/container patterns unless behavior absolutely depends on them

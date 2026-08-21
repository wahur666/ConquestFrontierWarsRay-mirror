## Current Windows-Only Dependency Inventory

This section is a repo-level inventory for the current `ConquestFrontierWarsRay` codebase, separate from the legacy Trim source audit above.

The current state is narrower than "the project is Windows-only everywhere". Most libraries and runtime code are already portable. The real Windows-only surface is concentrated in a small set of media, shell, and tooling paths.

### Summary

- `Vortice.MediaFoundation`: real Windows dependency in the shared `Framework` project. This is the strongest current blocker for cross-platform video playback and embedded-media audio.
- `NAudio`: Windows-only, but limited to `Framework.TestApp`. This is not a product-wide blocker.
- Win32 window hooks (`user32.dll`): small Windows-only shim in `Windowing`, plus one legacy mouse-confinement path in `Legacy.RaySharp`.
- Windows Forms / `System.Drawing`: only in `Legacy.RaySharp`, mostly for splash and editor dialogs.
- Windows system-font lookup: soft dependency only; the framework already falls back when not on Windows.
- `net10.0-windows` target frameworks: some projects are marked Windows-only even when the code dependency is local and likely removable after small refactors.

### 1. Vortice.MediaFoundation

Where it is used now:

- `src/Framework/Framework.csproj`: package reference to `Vortice.MediaFoundation`
- `src/Framework/src/MediaFoundationRuntime.cs`: global MF startup/shutdown lease
- `src/Framework/src/VideoPlayer.cs`: video decode path and source reader setup
- `src/Framework/src/MediaFoundationAudioStreamResource.cs`: embedded audio decode/streaming for media playback
- `src/Legacy.RaySharp/Legacy.RaySharp.csproj`: package reference
- `src/Legacy.RaySharp/src/MediaFoundationRuntime.cs`
- `src/Legacy.RaySharp/src/VideoPlayer.cs`
- `src/Legacy.RaySharp/Program.cs`: explicit MF startup

Dependency strength:

- Strong in the current shared framework because `VideoPlayer` directly depends on Media Foundation types and behavior.
- Moderate at the app level because the dependency is already localized to video playback and media-audio decode, not general UI, layout, scene graph, or resource loading.

Can it be substituted?

- Yes, but not by a one-line package swap.
- The correct replacement shape is a media backend interface, for example:
	- `IVideoDecoder` / `IVideoPlayerBackend`
	- `IAudioDecodeStream` for embedded media audio
- After that split, Windows can keep a Media Foundation backend while other platforms use another decoder path.

Port assessment:

- This is the main cross-platform blocker inside the reusable framework.
- If cross-platform support matters soon, isolate it first. Everything else is smaller.

### 2. NAudio

Where it is used now:

- `src/Framework.TestApp/Framework.TestApp.csproj`: package reference to `NAudio`
- `src/Framework.TestApp/src/NAudioStreamResource.cs`: `AudioFileReader`, `WaveChannel32`, `WaveOutEvent`
- `src/Framework.TestApp/src/Scenes/AudioPlayerScene.cs`: backend dropdown and creation of `NAudioStreamResource`

Dependency strength:

- Weak at repo level.
- It is confined to the test app and is already presented as a selectable playback backend beside a non-NAudio path.

Can it be substituted?

- Easily.
- The test app already has a backend seam in `AudioPlayerScene`; `NAudioStreamResource` can be removed, kept as Windows-only test coverage, or replaced with a cross-platform decoder/player implementation.

Port assessment:

- Not a meaningful blocker for the main framework or game runtime.
- This should be treated as optional tooling/demo code.

### 3. Win32 Window Hooks And Native Message Handling

Where it is used now:

- `src/Windowing/src/Win32Window.cs`: subclasses the native window proc and uses `SetWindowLongPtr`, `CallWindowProc`, `SetTimer`, `KillTimer`
- `src/Windowing/src/RaylibApplication.cs`: installs the workaround through `Win32Window.TryInstall(...)`
- `src/Conquest/Program.cs`: installs the same workaround
- `src/Legacy.RaySharp/src/AppWindow.cs`
- `src/Legacy.RaySharp/src/Win32.cs`: Win32 mouse confinement helpers

Dependency strength:

- Weak to moderate.
- These paths are shell/backend shims, not core gameplay or framework-control architecture.

Can it be substituted?

- Yes.
- `Win32Window` is specifically a workaround for Windows move/resize modal-loop behavior. Other platforms do not need the same hook, and the code already no-ops on non-Windows.
- The legacy mouse-confinement path can be replaced with a platform abstraction or dropped if the target UX changes.

Port assessment:

- This should not block a non-Windows port of the core framework.
- It does mean the current app shell behavior is not yet fully abstracted.

### 4. Windows Forms And System.Drawing

Where it is used now:

- `src/Legacy.RaySharp/Legacy.RaySharp.csproj`: `<UseWindowsForms>true</UseWindowsForms>`
- `src/Legacy.RaySharp/Program.cs`: `[STAThread]`
- `src/Legacy.RaySharp/src/NativeSplash.cs`: `System.Windows.Forms.Form`, `PictureBox`, `System.Drawing.Bitmap`
- `src/Legacy.RaySharp/src/Particle/ParticleEditorScene.cs`: `OpenFileDialog`, `FolderBrowserDialog`, `ColorDialog`

Dependency strength:

- Weak at repo level because this is isolated to `Legacy.RaySharp`.
- Strong only if `Legacy.RaySharp` itself must remain a first-class supported cross-platform app.

Can it be substituted?

- Yes.
- Splash screen: can be dropped or replaced with an in-engine loading screen.
- File/folder/color dialogs: can be moved behind a tiny platform service or replaced with in-app tools for editor workflows.

Port assessment:

- Not a blocker for the current shared framework.
- It is a blocker for making `Legacy.RaySharp` itself cross-platform without cleanup.

### 5. Windows System Font Lookup

Where it is used now:

- `src/Framework/src/UiText.cs`: checks `OperatingSystem.IsWindows()` and probes `%WINDIR%\\Fonts`

Dependency strength:

- Very weak.
- This is a convenience fallback only.

Can it be substituted?

- Already mostly substituted.
- The framework first looks for workspace fonts, then only probes Windows fonts as a fallback, and otherwise falls back again to Raylib default fonts.

Port assessment:

- Not a blocker.
- At most, cross-platform polish would improve if the repo always shipped the intended UI fonts instead of probing OS fonts.

### 6. Windows-Targeted Project Settings

Projects currently marked Windows-only:

- `src/Framework.TestApp/Framework.TestApp.csproj`: `net10.0-windows`
- `src/Legacy.RaySharp/Legacy.RaySharp.csproj`: `net10.0-windows`, `UseWindowsForms`
- `src/Conquest/ConquestFrontierWars.csproj`: `net10.0-windows`, `UseWindowsForms`
- `src/MiniLayout.Demo/MiniLayout.Demo.csproj`: `net10.0-windows`

Notes:

- `Legacy.RaySharp` is genuinely Windows-tied today because of Forms, System.Drawing, and Win32 helpers.
- `Framework.TestApp` is Windows-tied mainly because of `NAudio`.
- `Conquest` appears less strongly tied than its project file suggests; the visible code path is the `Win32Window.TryInstall(...)` workaround, and the project file still carries `UseWindowsForms` even though no matching Forms usage was found under `src/Conquest`.
- `MiniLayout.Demo` being `-windows` likely reflects shell/bootstrap convenience rather than a hard library dependency, but it still needs a direct source pass before changing the target framework.

### Practical Conclusion

- The user's main read is correct: `Vortice.MediaFoundation` is the strongest Windows-only dependency that affects shared runtime code.
- `NAudio` is Windows-only, but it is contained and not architecturally important.
- The rest of the Windows-only usage is mostly shell glue, legacy tooling, or convenience project settings, not deep framework lock-in.
- The shortest path to broader platform support is:
	1. isolate Media Foundation behind a media backend interface
	2. keep or remove `NAudio` as test-app-only code
	3. reduce `-windows` target frameworks where only small Win32 shims remain
	4. move any remaining native shell behavior behind narrow platform adapters

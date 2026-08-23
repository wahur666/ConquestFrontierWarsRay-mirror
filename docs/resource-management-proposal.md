# Resource Management Proposal

## Problem

The framework already has reusable resource wrappers, but resource sharing is
still application-local instead of framework-owned.

Today, `Resource` gives each concrete type:

- lazy loading
- unload-on-dispose
- private child ownership through `Own(...)`

That is a useful base, but it is still a unique-ownership model. Once multiple
scenes, nodes, or helper objects want to use the same asset path, the framework
does not currently define:

- whether identical paths should share one loaded instance
- who owns the lifetime of a passed-in resource
- how asset paths should be resolved consistently
- how preloading should work

This is why the current pain point is not "resources do not exist". The real
problem is that shared asset policy does not exist yet.

## Current State

### Current Implementation Status

The work has started.

Implemented so far:

- `SharedContext` now has an explicit `ResourceLocator` field/service entry.
- `IResourceLocator` exists in the framework layer.
- `AssetRootResourceLocator` exists as the default root-based implementation.
- `RepoResourceLocator` exists as the repository-backed application
  implementation.
- `Framework.TestApp` and `Conquest` now register a shared locator during
  startup.
- First audio/video call sites now resolve paths through the shared locator
  instead of building those paths locally.

This is intentionally the first slice only. It centralizes path resolution, but
it does not yet add shared cached resources or shared handles.

### What Already Exists

The current framework already has a solid low-level resource base:

- `Resource`
- `Texture2D`
- `CompressedTexture2D`
- `AtlasDefinitionResource`
- `SpriteFrames`
- `AtlasFramesResource`
- `AudioStreamResource`
- `MusicAudioResource`
- `NAudioStreamResource`
- `MediaFoundationAudioStreamResource`

This existing work should be preserved. The next layer should build on top of
it rather than replacing it.

### What The Current Design Proves

The current implementation already proves several good ideas:

1. Lazy load on first real use is practical for this framework.
2. File-backed resource wrappers are a reasonable public abstraction.
3. Composite resources can own internal helper resources cleanly.
4. Disposal logic belongs close to the native backend object.

Those are good foundations. The missing piece is shared ownership across
callers.

### What Is Still Missing

The framework still lacks:

- a shared cache keyed by resource identity
- a shared-handle model for callers
- framework-defined ownership rules for passed-in resources
- preload helpers
- tests that cover cache reuse and last-user disposal

## Evidence From The Current Code

### `Resource` Is A Unique-Ownership Primitive

`Resource` handles lazy load and unload cleanly, but it is fundamentally a
single-owner object with optional owned children.

That is visible in:

- [Resource.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/Resource.cs:35)
- [Resource.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/Resource.cs:60)
- [Resource.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/Resource.cs:95)

This is appropriate for a resource instance. It is not enough for a
framework-wide asset-sharing policy.

### Composite Resources Already Rely On Private Ownership

`AtlasFramesResource` uses `Own(...)` to manage its texture, atlas metadata, and
frame collection:

- [AtlasFramesResource.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/AtlasFramesResource.cs:13)

That is valid because those child resources are private implementation details
of one composite resource. It also shows why shared caching should live outside
`Resource` rather than inside it.

### Nodes Already Expose Ownership Ambiguity

`AudioPlayer` currently has to accept ad hoc ownership flags:

- [AudioPlayer.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/AudioPlayer.cs:119)
- [AudioPlayer.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/AudioPlayer.cs:214)

This works, but it is framework code compensating for the lack of a shared
resource-handle model.

`VideoPlayer` does the same when it assigns embedded audio:

- [VideoPlayer.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/VideoPlayer.cs:323)
- [VideoPlayer.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework/src/VideoPlayer.cs:369)

### Scenes Still Own Asset Lifetime Manually

The test app still constructs and disposes resources directly:

- [SpriteScene.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework.TestApp/src/Scenes/SpriteScene.cs:107)
- [SpriteScene.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework.TestApp/src/Scenes/SpriteScene.cs:231)
- [AudioPlayerScene.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework.TestApp/src/Scenes/AudioPlayerScene.cs:243)

That means scene code still decides:

- when a new resource instance is created
- whether a previous instance should be disposed
- whether repeated file paths should reuse a shared loaded asset

This is exactly the framework gap described in the pain point.

### A Current Leak-Like Ownership Gap Exists

`SpriteScene` creates `_test2Texture`, but its `OnDispose()` currently disposes
only `_posterAtlas`:

- [SpriteScene.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework.TestApp/src/Scenes/SpriteScene.cs:118)
- [SpriteScene.cs](/D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Framework.TestApp/src/Scenes/SpriteScene.cs:231)

This is a useful example of why manual scene-owned disposal does not scale.

## Design Goal

Keep `Resource` as the low-level lazy-load/dispose primitive, and add a
separate framework-owned asset management layer for path resolution, reuse, and
shared lifetime.

The new layer should:

1. preserve the current concrete resource types
2. avoid forcing ref-counting into every `Resource`
3. make asset sharing explicit and boring
4. reduce manual ownership flags in higher-level nodes over time

## Proposed Solution

## 1. Add An Asset Path Resolver

Introduce a small resolver abstraction that maps logical asset identifiers to
canonical full paths.

Implemented first-pass interface:

```csharp
public interface IResourceLocator {
    string AssetRootPath { get; }
    string ResolveAssetPath(string relativeAssetPath);
    string ResolveMoviePath(string moviePath);
    string ResolveMusicPath(string musicPath);
    string ResolveSpeechPath(string speechPath);
}
```

Responsibilities:

- normalize separators
- resolve relative asset paths against one configured root
- return canonical full paths for cache keys
- keep scene code out of direct `Path.Combine(...)` chains where practical

Implemented pieces:

- framework abstraction: `IResourceLocator`
- framework default: `AssetRootResourceLocator`
- repo-backed app implementation: `RepoResourceLocator`

The main remaining value here is not inventing more resolver types. It is
moving more path-owning code to use the shared locator consistently.

## 2. Add A Shared Resource Cache

Add a framework-owned cache that stores one underlying `Resource` instance per
resource key and returns disposable shared handles to callers.

Suggested shape:

```csharp
public interface IResourceCache {
    SharedResource<T> Acquire<T>(ResourceKey key, Func<T> factory)
        where T : Resource;

    SharedResource<T> Preload<T>(ResourceKey key, Func<T> factory)
        where T : Resource;
}
```

Suggested key:

```csharp
public readonly record struct ResourceKey(string Kind, string Identity);
```

`Identity` should be based on canonical path plus any options that change the
underlying loaded result.

Examples:

- texture path plus filter mode
- audio path plus playback backend
- atlas texture path plus atlas definition path

The cache should not key only by raw path text, because some resource types have
meaningful creation options.

## 3. Return Shared Handles, Not Raw Ownership

Do not make `Resource` itself ref-counted. Instead, introduce a small shared
lease type.

Suggested shape:

```csharp
public sealed class SharedResource<T> : IDisposable where T : Resource {
    public T Value { get; }
}
```

Rules:

- acquiring a cached resource increments a reference count
- disposing the shared handle decrements the count
- the underlying `Resource` is disposed only when the last handle is released

This keeps `Resource` simple and moves shared lifetime into the cache layer,
where it belongs.

## 4. Define Ownership Rules Explicitly

The framework should document a simple ownership model.

Recommended rules:

1. Raw `Resource` references are borrowed by default.
2. Receivers must not dispose borrowed resources unless explicitly documented.
3. `SharedResource<T>` means the receiver owns a lease and must dispose that
   lease.
4. `Resource.Own(...)` remains valid only for private implementation children,
   not for globally shared cached assets.

This should become the standard interpretation for framework APIs.

## 5. Start With Leaf Resource Types

The first cache pass should target file-backed leaf resources first:

- `CompressedTexture2D`
- `AtlasDefinitionResource`
- `MusicAudioResource`
- `NAudioStreamResource`
- `MediaFoundationAudioStreamResource`

These are the highest-value reuse points and the least controversial cache
targets.

Then evaluate whether to cache higher-level composed resources such as:

- `SpriteFrames`
- `AtlasFramesResource`

My current recommendation is to defer composed-resource caching until the leaf
resource cache is stable.

## Why The Cache Should Live Outside `Resource`

This is the most important design choice.

If ref-counting moves into `Resource` itself:

- every direct resource use becomes more complex
- private composite ownership becomes harder to reason about
- construction and disposal semantics become less obvious
- the base class starts mixing two different concerns:
  - native/backend lifetime
  - shared cache lifetime

If the cache owns shared lifetime instead:

- `Resource` stays small and coherent
- existing concrete resource types stay mostly unchanged
- unique resources and shared resources can coexist
- migration can happen incrementally

This is the better fit for the current codebase.

## Recommended Public Surface

The first pass should stay deliberately small.

Suggested first-pass types:

- `IResourceLocator`
- `AssetRootResourceLocator`
- `ResourceKey`
- `IResourceCache`
- `ResourceCache`
- `SharedResource<T>`

Suggested first-pass helper methods:

- `GetTexture(string assetPath, TextureFilter? filter = null)`
- `GetAtlasDefinition(string assetPath)`
- `GetAudio(string assetPath, AudioBackend backend)`

These can exist as helper methods on top of the generic cache API if that reads
better in scene code.

## Migration Plan

### Phase 0: Document The Problem

This proposal is the tracking document for the resource-management work.

### Phase 1: Add Path Resolution

1. Introduce `IResourceLocator`.
2. Add one root-based default implementation.
3. Add one repo-backed application implementation.
4. Normalize paths before cache-key generation.

### Phase 2: Add Shared Cache Infrastructure

1. Introduce `ResourceKey`.
2. Introduce `SharedResource<T>`.
3. Implement `ResourceCache`.
4. Add unit tests for acquire/release behavior.

### Phase 3: Cache Leaf Resources

1. Add cached acquisition helpers for textures.
2. Add cached acquisition helpers for audio.
3. Add cached acquisition helpers for atlas metadata.
4. Verify that repeated requests reuse the same underlying resource instance.

### Phase 4: Convert One Scene

Convert one test-app scene as the proof slice.

Recommended first target: `SpriteScene` or `HotRectEventScene`.

`SpriteScene` is the better proof because it already exercises:

- atlas texture access
- atlas definition access
- sprite reuse
- a standalone second texture

It also already demonstrates the current ownership gap.

### Phase 5: Reduce Manual Ownership Flags

After cached handles exist:

1. review `AudioPlayer`
2. review `VideoPlayer`
3. replace `takeOwnership`-style flows where shared leases are a better fit

This should be incremental. Do not force a full API rewrite in the first pass.

### Phase 6: Evaluate Preload And Async Extensions

Only after synchronous cache reuse is stable:

1. add explicit preload helpers
2. consider staged loading for large assets
3. consider async loading only if a real caller requires it

Async loading is not the first problem to solve here.

## Tracking Checklist

- [x] Confirm that `Resource` already provides lazy load and disposal.
- [x] Confirm that composite resources already use private child ownership.
- [x] Confirm that scenes still create and dispose resources manually.
- [x] Confirm that nodes still negotiate ownership ad hoc.
- [x] Add explicit `SharedContext.ResourceLocator`.
- [x] Add `IResourceLocator`.
- [x] Add one root-based resolver implementation.
- [x] Add one repo-backed resolver implementation.
- [x] Register the shared locator in application startup.
- [x] Convert current audio/video path resolution to use the locator.
- [ ] Add `ResourceKey`.
- [ ] Add `SharedResource<T>`.
- [ ] Add `ResourceCache`.
- [ ] Add tests for cache reuse.
- [ ] Add tests for last-handle disposal.
- [ ] Convert one sprite-oriented scene to cached resources.
- [ ] Review `AudioPlayer` ownership flags after the cache proof exists.
- [ ] Review `VideoPlayer` embedded-audio ownership after the cache proof exists.
- [ ] Decide whether composed resources should also be cached.

## Open Questions

These points should stay explicit while implementation begins:

1. Should `ResourceCache` be a service passed into scenes, owned by
   `SceneTree`, or owned by a higher application layer?
2. Should composed resources like `AtlasFramesResource` be cached directly, or
   should the cache stop at file-backed leaf resources?
3. Do texture filters belong in the cache key, or should filter changes happen
   only on uncached private instances?
4. Should preload call `EnsureLoaded()` immediately, or should it only
   materialize the cached wrapper and leave actual load lazy?

These are implementation questions, not blockers for the first design slice.

## Immediate Next Step

The next implementation step should be:

1. add `SharedResource<T>` and `ResourceCache`
2. write tests for acquire/release behavior
3. convert `SpriteScene` as the first cache proof
4. then revisit `AudioPlayer` and `VideoPlayer` ownership flags

That is the smallest slice that validates the design without overcommitting to
async loading, global manifests, or large public API churn.

## Expected Outcome

After this work:

- scenes stop constructing duplicate resources casually
- disposal rules become consistent
- path resolution becomes centralized
- framework nodes can consume shared assets without ad hoc ownership policy
- the test app can prove reuse by deleting manual lifetime code

That is the real goal of this proposal: move resource sharing from "every scene
figures it out itself" to "the framework defines one boring, reliable way to do
it."

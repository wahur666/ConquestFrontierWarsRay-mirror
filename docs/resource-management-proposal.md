# Resource Management Proposal

## Problem

The framework has good low-level resource wrappers, but shared asset policy is
still incomplete.

Today the framework does not fully define:

- shared reuse for identical asset requests
- consistent ownership rules across callers
- preload behavior
- cache lifetime

`Resource` is still a unique-ownership primitive. That is fine for one resource
instance, but it is not yet a framework-wide sharing model.

## Current State

Implemented:

- `SharedContext` owns a `ResourceLocator` and a lazily-created default
  `ResourceManager`.
- changing `SharedContext.ResourceLocator` recreates the default framework
  `ResourceManager` when needed
- `IResourceLocator`, `AssetRootResourceLocator`, and `RepoResourceLocator`
- `ResourceManager.Audio` and `ResourceManager.Videos`
- `AudioResourceManager` for music and speech resources with explicit backend
  selection
- `VideoResourceManager` for Media Foundation video-source setup, metadata
  extraction, and embedded-audio probing
- audio/video call sites now use shared locator/resource services instead of
  building paths directly
- `AudioPlayer` now has explicit ownership APIs:
  - `SetAudio(...)` for borrowed resources
  - `SetOwnedAudio(...)` for owned resources
  - `ClearAudio()` for removing a borrowed resource
- `VideoPlayer` now uses the borrowed embedded-audio path explicitly

Not implemented:

- shared cache keyed by resource identity
- shared disposable handles
- preload helpers
- cache reuse tests
- last-handle disposal tests

## What This Proves

The current code already supports:

- lazy load on first use
- backend-specific resource wrappers
- private child ownership with `Own(...)`
- centralized asset path resolution
- managed acquisition services for audio and video

That is enough foundation to add caching without replacing the existing
resource types.

## Remaining Gap

Scenes and helper objects still decide when to create and dispose many
resources themselves. Repeated requests for the same asset path still create
separate resource instances unless a caller manually reuses one.

So the missing layer is still shared reuse and shared lifetime.

## Proposed Model

Keep `Resource` unchanged as the low-level lazy-load/dispose primitive.

Add a separate framework-owned cache layer:

```csharp
public readonly record struct ResourceKey(string Kind, string Identity);

public interface IResourceCache {
    SharedResource<T> Acquire<T>(ResourceKey key, Func<T> factory)
        where T : Resource;
}

public sealed class SharedResource<T> : IDisposable where T : Resource {
    public T Value { get; }
}
```

Rules:

- the cache owns sharing and reference counts
- `Resource` stays non-ref-counted
- raw `Resource` references are borrowed by default
- `SharedResource<T>` is an owned lease
- `Resource.Own(...)` stays for private implementation details only

## First Cache Targets

Start with file-backed leaf resources:

- `CompressedTexture2D`
- `AtlasDefinitionResource`
- `MusicAudioResource`
- `NAudioStreamResource`
- `MediaFoundationAudioStreamResource`

Defer composed-resource caching until the leaf cache is stable.

## Recommended Next Step

1. add `ResourceKey`
2. add `SharedResource<T>`
3. add `ResourceCache`
4. add reuse and last-release tests
5. convert one sprite-oriented scene as the proof slice

`SpriteScene` is still the best first conversion target.

## Tracking Checklist

- [x] Add explicit `SharedContext.ResourceLocator`
- [x] Add locator implementations
- [x] Add shared framework `ResourceManager`
- [x] Add `AudioResourceManager`
- [x] Keep `VideoResourceManager` as the managed video acquisition layer
- [x] Route current audio/video path usage through shared services
- [x] Split `AudioPlayer` borrowed vs owned assignment
- [x] Update `VideoPlayer` embedded-audio assignment to match
- [ ] Add `ResourceKey`
- [ ] Add `SharedResource<T>`
- [ ] Add `ResourceCache`
- [ ] Add cache reuse tests
- [ ] Add last-handle disposal tests
- [ ] Convert one scene to cached resources
- [ ] Decide whether composed resources should also be cached

## Open Questions

1. Where should `ResourceCache` live: `SceneTree`, `SharedContext`, or a higher
   application layer?
2. Should composed resources like `AtlasFramesResource` be cached directly?
3. Do texture filters belong in the cache key?
4. Should preload force `EnsureLoaded()` or stay lazy?

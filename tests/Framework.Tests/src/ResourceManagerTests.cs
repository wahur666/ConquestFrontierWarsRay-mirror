using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class ResourceManagerTests {
	[Fact]
	public void SharedContext_RecreatesDefaultResourceManager_WhenLocatorChanges() {
		var shared = new SharedContext();
		var first = shared.ResourceManager;

		shared.ResourceLocator = new AssetRootResourceLocator(Path.Combine(Path.GetTempPath(), "alt-assets"));
		var second = shared.ResourceManager;

		Assert.NotSame(first, second);
		Assert.IsType<ResourceManager>(second);
	}

	[Fact]
	public void SharedContext_PreservesExplicitResourceManager_WhenLocatorChanges() {
		var shared = new SharedContext();
		var custom = new StubResourceManager();
		shared.ResourceManager = custom;

		shared.ResourceLocator = new AssetRootResourceLocator(Path.Combine(Path.GetTempPath(), "alt-assets"));

		Assert.Same(custom, shared.ResourceManager);
	}

	[Fact]
	public void Preload_EagerLoadsFrameworkResources_AndReusesTheSameInstance() {
		var manager = new ResourceManager(new AssetRootResourceLocator(Path.Combine(Path.GetTempPath(), "assets")));

		var created = 0;
		var first = manager.Preload("resource.test", () => {
			created++;
			return new TrackingResource();
		});
		var found = manager.TryGetPreloaded<TrackingResource>("resource.test", out var second);

		Assert.True(found);
		Assert.Same(first, second);
		Assert.Equal(1, created);
		Assert.True(first.IsLoaded);
	}

	[Fact]
	public void ReleasePreloaded_DisposesStoredResource() {
		var manager = new ResourceManager(new AssetRootResourceLocator(Path.Combine(Path.GetTempPath(), "assets")));
		var resource = manager.Preload("resource.test", () => new TrackingResource());

		var released = manager.ReleasePreloaded("resource.test");
		var found = manager.TryGetPreloaded<TrackingResource>("resource.test", out _);

		Assert.True(released);
		Assert.False(found);
		Assert.True(resource.IsDisposed);
		Assert.False(resource.IsLoaded);
		Assert.Equal(1, resource.LoadCount);
		Assert.Equal(1, resource.UnloadCount);
	}

	[Fact]
	public void Dispose_ReleasesRemainingPreloadedResources() {
		var manager = new ResourceManager(new AssetRootResourceLocator(Path.Combine(Path.GetTempPath(), "assets")));
		var resource = manager.Preload("resource.test", () => new TrackingResource());

		manager.Dispose();

		Assert.True(resource.IsDisposed);
		Assert.Equal(1, resource.UnloadCount);
	}

	private sealed class StubResourceManager : IResourceManager {
		public IAudioResourceManager Audio { get; } = new StubAudioResourceManager();
		public IVideoResourceManager Videos { get; } = new StubVideoResourceManager();
		public bool IsDisposed { get; private set; }

		public T Preload<T>(string name, Func<T> factory) where T : class, IDisposable {
			throw new NotSupportedException();
		}

		public bool TryGetPreloaded<T>(string name, out T? resource) where T : class, IDisposable {
			resource = null;
			return false;
		}

		public bool ReleasePreloaded(string name) {
			throw new NotSupportedException();
		}

		public void Dispose() {
			IsDisposed = true;
		}
	}

	private sealed class StubAudioResourceManager : IAudioResourceManager {
		public AudioStreamResource OpenFile(string path, AudioPlaybackBackend backend = AudioPlaybackBackend.RaylibMusic) {
			throw new NotSupportedException();
		}

		public AudioStreamResource OpenMusic(string musicPath, AudioPlaybackBackend backend = AudioPlaybackBackend.RaylibMusic) {
			throw new NotSupportedException();
		}

		public AudioStreamResource OpenSpeech(string speechPath, AudioPlaybackBackend backend = AudioPlaybackBackend.RaylibMusic) {
			throw new NotSupportedException();
		}
	}

	private sealed class StubVideoResourceManager : IVideoResourceManager {
		public VideoSource OpenFile(string path) {
			throw new NotSupportedException();
		}

		public VideoSource OpenMovie(string moviePath) {
			throw new NotSupportedException();
		}
	}

	private sealed class TrackingResource : Resource {
		public int LoadCount { get; private set; }

		public int UnloadCount { get; private set; }

		protected override void LoadCore() {
			LoadCount++;
		}

		protected override void UnloadCore() {
			UnloadCount++;
		}
	}
}

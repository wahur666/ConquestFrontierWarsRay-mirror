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

	private sealed class StubResourceManager : IResourceManager {
		public IVideoResourceManager Videos { get; } = new StubVideoResourceManager();
	}

	private sealed class StubVideoResourceManager : IVideoResourceManager {
		public VideoSource OpenFile(string path) {
			throw new NotSupportedException();
		}

		public VideoSource OpenMovie(string moviePath) {
			throw new NotSupportedException();
		}
	}
}

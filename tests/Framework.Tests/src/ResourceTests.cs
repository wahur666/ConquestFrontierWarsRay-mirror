using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class ResourceTests {
	[Fact]
	public void Constructor_NormalizesBlankResourcePath() {
		var resource = new TestResource("   ");

		Assert.Null(resource.ResourcePath);
		Assert.False(resource.IsLoaded);
		Assert.False(resource.IsDisposed);
	}

	[Fact]
	public void EnsureLoaded_LoadsOnlyOnce() {
		var resource = new TestResource("asset.bin");

		resource.Load();
		resource.Load();

		Assert.True(resource.IsLoaded);
		Assert.Equal(1, resource.LoadCount);
	}

	[Fact]
	public void Dispose_UnloadsLoadedResourceOnce() {
		var resource = new TestResource("asset.bin");
		resource.Load();

		resource.Dispose();
		resource.Dispose();

		Assert.False(resource.IsLoaded);
		Assert.True(resource.IsDisposed);
		Assert.Equal(1, resource.LoadCount);
		Assert.Equal(1, resource.UnloadCount);
	}

	[Fact]
	public void EnsureLoaded_ThrowsAfterDispose() {
		var resource = new TestResource("asset.bin");
		resource.Dispose();

		Assert.Throws<ObjectDisposedException>(() => resource.Load());
	}
}

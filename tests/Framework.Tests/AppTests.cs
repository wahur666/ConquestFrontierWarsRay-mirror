using ConquestFrontierWarsRay.Framework;
using ConquestFrontierWarsRay.Framework.App;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class AppTests {
	[Fact]
	public void WindowOptions_StoresConstructorValuesAndDefaults() {
		var defaults = new WindowOptions(640, 480, "Demo");
		var custom = new WindowOptions(800, 600, "Game", 120, ConfigFlags.Msaa4xHint);

		Assert.Equal(640, defaults.Width);
		Assert.Equal(480, defaults.Height);
		Assert.Equal("Demo", defaults.Title);
		Assert.Equal(60, defaults.TargetFps);
		Assert.Equal(ConfigFlags.VSyncHint, defaults.Flags);
		Assert.Equal(ConfigFlags.VSyncHint | ConfigFlags.AlwaysRunWindow, defaults.StartupFlags);

		Assert.Equal(800, custom.Width);
		Assert.Equal(600, custom.Height);
		Assert.Equal("Game", custom.Title);
		Assert.Equal(120, custom.TargetFps);
		Assert.Equal(ConfigFlags.Msaa4xHint, custom.Flags);
		Assert.Equal(ConfigFlags.Msaa4xHint | ConfigFlags.AlwaysRunWindow, custom.StartupFlags);
	}

	[Fact]
	public void NodeContext_StoresDependenciesAndRejectsNulls() {
		var input = new InputManager();
		var quitCalled = false;
		var context = new NodeContext(input, () => quitCalled = true);

		Assert.Same(input, context.Input);
		context.RequestQuit();
		Assert.True(quitCalled);

		Assert.Throws<ArgumentNullException>(() => new NodeContext(null!, () => {
		}));
		Assert.Throws<ArgumentNullException>(() => new NodeContext(input, null!));
	}

	[Fact]
	public void RaylibApplication_RejectsNullRootAndDisposesRoot() {
		var options = new WindowOptions(320, 200, "Test");

		Assert.Throws<ArgumentNullException>(() => new RaylibApplication(options, null!));

		var root = new Node("Root");
		var app = new RaylibApplication(options, root);

		app.Dispose();
		app.Dispose();

		Assert.Empty(root.Children);
	}
}

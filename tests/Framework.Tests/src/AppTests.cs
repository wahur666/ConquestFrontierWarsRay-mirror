using ConquestFrontierWarsRay.Framework.App;
using ConquestFrontierWarsRay.Windowing;
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
		var tree = new SceneTree(new Node("Root"), new InputManager());
		var shared = new SharedContext();
		var context = new NodeContext(tree, shared);

		Assert.Same(tree, context.Tree);
		Assert.Same(tree.Input, context.Input);
		Assert.Same(shared, context.Shared);
		context.RequestQuit();
		Assert.True(tree.IsQuitRequested);

		Assert.Throws<ArgumentNullException>(() => new NodeContext(null!));
	}

	[Fact]
	public void SharedContext_StoresAndResolvesTypedValues() {
		var shared = new SharedContext();
		var value = new SampleSharedState("Menu");

		shared.Set(value);

		Assert.True(shared.Contains<SampleSharedState>());
		Assert.True(shared.TryGet<SampleSharedState>(out var resolved));
		Assert.Same(value, resolved);
		Assert.Same(value, shared.GetRequired<SampleSharedState>());
		Assert.True(shared.Remove<SampleSharedState>());
		Assert.False(shared.Contains<SampleSharedState>());
		Assert.False(shared.TryGet<SampleSharedState>(out _));
		Assert.Throws<InvalidOperationException>(() => shared.GetRequired<SampleSharedState>());
		Assert.Throws<ArgumentNullException>(() => shared.Set<SampleSharedState>(null!));
	}

	[Fact]
	public void SharedContext_StoresAndResolvesNamedValues() {
		var shared = new SharedContext();
		shared.Set("network.host", "localhost");
		shared.Set("graphics.quality", 3);

		Assert.True(shared.Contains("network.host"));
		Assert.True(shared.TryGet("network.host", out var raw));
		Assert.Equal("localhost", raw);
		Assert.True(shared.TryGet<string>("network.host", out var host));
		Assert.Equal("localhost", host);
		Assert.True(shared.TryGet<int>("graphics.quality", out var quality));
		Assert.Equal(3, quality);
		Assert.Equal("localhost", shared.GetRequired<string>("network.host"));
		Assert.True(shared.Remove("network.host"));
		Assert.False(shared.Contains("network.host"));
		Assert.False(shared.TryGet<string>("network.host", out _));
		Assert.Throws<InvalidOperationException>(() => shared.GetRequired<string>("network.host"));
		Assert.Throws<ArgumentException>(() => shared.Set("", "value"));
		Assert.Throws<ArgumentException>(() => shared.Contains(" "));
	}

	[Fact]
	public void RaylibApplication_Dispose_IsIdempotent() {
		var options = new WindowOptions(320, 200, "Test");
		var app = new RaylibApplication(options);

		app.Dispose();
		app.Dispose();
	}

	[Fact]
	public void RaylibApplication_ExposesSharedContext_ForWholeAppLifetime() {
		var options = new WindowOptions(320, 200, "Test");
		var app = new RaylibApplication(options);

		app.Shared.Set("session.id", "abc");
		app.Shared.Set(new SampleSharedState("Intro"));

		Assert.Equal("abc", app.Shared.GetRequired<string>("session.id"));
		Assert.Equal("Intro", app.Shared.GetRequired<SampleSharedState>().ScreenName);
	}

	private sealed record SampleSharedState(string ScreenName);
}

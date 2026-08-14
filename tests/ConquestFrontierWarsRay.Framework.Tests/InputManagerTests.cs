using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class InputManagerTests {
	[Fact]
	public void SetupHotkeys_RegistersUiEscapeAction() {
		var input = new InputManager();

		input.SetupHotkeys();

		Assert.True(input.IsActionReleased("UiEscape"));
		Assert.False(input.IsActionPressed("UiEscape"));
		Assert.False(input.IsActionJustPressed("UiEscape"));
		Assert.False(input.IsActionJustReleased("UiEscape"));
	}

	[Fact]
	public void ActionQueries_RejectUnknownAction() {
		var input = new InputManager();

		Assert.Throws<KeyNotFoundException>(() => input.IsActionPressed("Missing"));
		Assert.Throws<KeyNotFoundException>(() => input.IsActionReleased("Missing"));
		Assert.Throws<KeyNotFoundException>(() => input.IsActionJustPressed("Missing"));
		Assert.Throws<KeyNotFoundException>(() => input.IsActionJustReleased("Missing"));
	}

	[Fact]
	public void ActionQueries_RejectBlankActionName() {
		var input = new InputManager();

		Assert.Throws<ArgumentException>(() => input.IsActionPressed(" "));
		Assert.Throws<ArgumentException>(() => input.IsActionReleased(" "));
		Assert.Throws<ArgumentException>(() => input.IsActionJustPressed(" "));
		Assert.Throws<ArgumentException>(() => input.IsActionJustReleased(" "));
	}

	[Fact]
	public void ExitRequest_IsFalseBeforeAnyUpdate() {
		var input = new InputManager();

		Assert.False(input.IsExitRequested());
	}
}

using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class DropdownNodeTests {
	[Fact]
	public void SetItems_SelectsRequestedIndex() {
		var dropdown = new DropdownNode();

		dropdown.SetItems(["NAudio", "Raylib"], selectedIndex: 1);

		Assert.Equal(2, dropdown.Items.Count);
		Assert.Equal(1, dropdown.SelectedIndex);
		Assert.Equal("Raylib", dropdown.SelectedLabel);
		Assert.False(dropdown.IsExpanded);
	}

	[Fact]
	public void SetItems_WithNoItems_ResetsSelection() {
		var dropdown = new DropdownNode();

		dropdown.SetItems(["A", "B"], selectedIndex: 0);
		dropdown.SetItems([]);

		Assert.Empty(dropdown.Items);
		Assert.Equal(-1, dropdown.SelectedIndex);
		Assert.Equal(string.Empty, dropdown.SelectedLabel);
		Assert.False(dropdown.IsExpanded);
	}

	[Fact]
	public void SetSelectedIndex_ClampsToValidRange() {
		var dropdown = new DropdownNode();
		dropdown.SetItems(["NAudio", "Raylib"], selectedIndex: 0);

		dropdown.SetSelectedIndex(10);

		Assert.Equal(1, dropdown.SelectedIndex);
		Assert.Equal("Raylib", dropdown.SelectedLabel);
	}
}

using ConquestFrontierWarsRay.Core.UI;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class LegacyDropdownNodeTests {
	[Fact]
	public void AddStringAndDataValue_RoundTripsLegacySelectionData() {
		var dropdown = new LegacyDropdownNode();

		var index = dropdown.AddString("Medium");
		dropdown.SetDataValue(index, 2);
		dropdown.SetCurrentSelection(index);

		Assert.Equal(1, dropdown.GetNumberOfItems());
		Assert.Equal("Medium", dropdown.GetString(index));
		Assert.Equal<uint>(2, dropdown.GetDataValue(index));
		Assert.Equal(index, dropdown.GetCurrentSelection());
		Assert.Equal("Medium", dropdown.SelectedLabel);
	}

	[Fact]
	public void AddStringToHead_ShiftsCurrentSelection() {
		var dropdown = new LegacyDropdownNode();
		dropdown.AddString("B");
		dropdown.SetCurrentSelection(0);

		dropdown.AddStringToHead("A");

		Assert.Equal(2, dropdown.GetNumberOfItems());
		Assert.Equal(1, dropdown.GetCurrentSelection());
		Assert.Equal("B", dropdown.SelectedLabel);
	}

	[Fact]
	public void ResetContent_ClearsItemsAndSelection() {
		var dropdown = new LegacyDropdownNode();
		dropdown.AddString("One");
		dropdown.SetCurrentSelection(0);

		dropdown.ResetContent();

		Assert.Equal(0, dropdown.GetNumberOfItems());
		Assert.Equal(-1, dropdown.GetCurrentSelection());
		Assert.Equal(string.Empty, dropdown.SelectedLabel);
	}
}

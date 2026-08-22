using ConquestFrontierWarsRay.Core.UI;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class LegacyListBoxNodeTests {
	[Fact]
	public void AddStringAndDataValue_RoundTripsLegacyItemPayload() {
		var listbox = new LegacyListBoxNode();

		var index = listbox.AddString("Medium");
		listbox.SetDataValue(index, 2);
		listbox.SetCurrentSelection(index);

		Assert.Equal(1, listbox.GetNumberOfItems());
		Assert.Equal("Medium", listbox.GetString(index));
		Assert.Equal<uint>(2, listbox.GetDataValue(index));
		Assert.Equal(index, listbox.GetCurrentSelection());
	}

	[Fact]
	public void AddStringToHead_ShiftsCurrentSelection() {
		var listbox = new LegacyListBoxNode();
		listbox.AddString("B");
		listbox.SetCurrentSelection(0);

		listbox.AddStringToHead("A");

		Assert.Equal(2, listbox.GetNumberOfItems());
		Assert.Equal(1, listbox.GetCurrentSelection());
		Assert.Equal("B", listbox.SelectedLabel);
	}

	[Fact]
	public void SetCurrentSelection_RejectsOutOfRangeButAllowsMinusOne() {
		var listbox = new LegacyListBoxNode();
		listbox.AddString("A");
		listbox.AddString("B");
		listbox.SetCurrentSelection(1);

		listbox.SetCurrentSelection(99);
		Assert.Equal(1, listbox.GetCurrentSelection());

		listbox.SetCurrentSelection(-1);
		Assert.Equal(-1, listbox.GetCurrentSelection());
	}

	[Fact]
	public void EnsureVisible_ScrollsTopLineDownToReachSelection() {
		var listbox = new LegacyListBoxNode();
		listbox.FontSize = 12f;
		listbox.ApplyLegacyDefinition(
			new ConquestFrontierWarsRay.Data.Models.GT.GT_LISTBOX(),
			new ConquestFrontierWarsRay.Data.Models.GT.LISTBOX_DATA {
				XOrigin = 0,
				YOrigin = 0,
				TextArea = new ConquestFrontierWarsRay.Data.Models.RECT {
					Left = 0,
					Top = 0,
					Right = 100,
					Bottom = 19
				},
				LeadingHeight = 0
			});

		for (var i = 0; i < 8; i++) {
			listbox.AddString($"Item {i}");
		}

		listbox.SetCurrentSelection(5);

		Assert.True(listbox.TopLine > 0);
		Assert.True(listbox.GetTopVisibleString() <= 5);
		Assert.True(listbox.GetBottomVisibleString() >= 5);
	}
}

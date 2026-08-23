using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;

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

	[Fact]
	public void SetCurrentSelection_RejectsOutOfRangeButAllowsMinusOne() {
		var dropdown = CreateConfiguredDropdown();
		dropdown.AddString("A");
		dropdown.AddString("B");
		dropdown.SetCurrentSelection(1);

		dropdown.SetCurrentSelection(99);
		Assert.Equal(1, dropdown.GetCurrentSelection());

		dropdown.SetCurrentSelection(-1);
		Assert.Equal(-1, dropdown.GetCurrentSelection());
	}

	[Fact]
	public void EnsureVisible_ScrollsVisibleWindowToSelection() {
		var dropdown = CreateConfiguredDropdown();
		for (var i = 0; i < 8; i++) {
			dropdown.AddString($"Item {i}");
		}

		dropdown.SetCurrentSelection(5);

		Assert.True(dropdown.GetTopVisibleString() > 0);
		Assert.True(dropdown.GetTopVisibleString() <= 5);
		Assert.True(dropdown.GetBottomVisibleString() >= 5);
	}

	[Fact]
	public void CaretMethods_ForwardToSelection() {
		var dropdown = CreateConfiguredDropdown();
		dropdown.AddString("A");
		dropdown.AddString("B");
		dropdown.AddString("C");

		dropdown.CaretEnd();
		Assert.Equal(2, dropdown.GetCaretPosition());

		dropdown.CaretLineUp();
		Assert.Equal(1, dropdown.GetCaretPosition());

		dropdown.CaretHome();
		Assert.Equal(0, dropdown.GetCaretPosition());
	}

	[Fact]
	public void PopupOffset_IsConfigurable() {
		var dropdown = CreateConfiguredDropdown();

		dropdown.PopupOffset = new System.Numerics.Vector2(0f, 20f);

		Assert.Equal(new System.Numerics.Vector2(0f, 20f), dropdown.PopupOffset);
	}

	private static LegacyDropdownNode CreateConfiguredDropdown() {
		var dropdown = new LegacyDropdownNode {
			FontSize = 12f
		};
		dropdown.ApplyLegacyDefinition(
			new GT_BUTTON {
				DisabledText = new GT_COLOR(),
				NormalText = new GT_COLOR(),
				HighlightText = new GT_COLOR()
			},
			new GT_LISTBOX {
				SelectedText = new GT_COLOR(),
				SelectedTextGrayed = new GT_COLOR()
			},
			new DROPDOWN_DATA {
				ScreenRect = new RECT {
					Left = 0,
					Top = 0,
					Right = 100,
					Bottom = 20
				},
				ListboxData = new LISTBOX_DATA {
					XOrigin = 0,
					YOrigin = 20,
					TextArea = new RECT {
						Left = 0,
						Top = 0,
						Right = 100,
						Bottom = 39
					},
					LeadingHeight = 0
				}
			});
		return dropdown;
	}
}

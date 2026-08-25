using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class LegacyListBoxScene : ShowcaseScene {
	private readonly UiEventSource _eventSource = new("LegacyListBoxEventSource");
	private readonly PanelNode _panel = new("LegacyListBoxPanel") {
		Position = new Vector2(364f, 20f),
		Size = new Vector2(896f, 680f),
		Fill = new Color(14, 18, 30, 255),
		Outline = new Color(68, 84, 110, 255),
		OutlineThickness = 2f
	};
	private readonly TextNode _statusLabel = new("LegacyListBoxStatus") {
		Position = new Vector2(402f, 88f),
		FontSize = 17f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _selectionLabel = new("LegacyListBoxSelection") {
		Position = new Vector2(402f, 118f),
		FontSize = 17f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _breakLabel = new("LegacyListBoxBreakLabel") {
		Position = new Vector2(402f, 148f),
		FontSize = 17f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly LegacyListBoxNode _artListBox = new("ArtListBox");
	private readonly LegacyListBoxNode _primitiveListBox = new("PrimitiveListBox");
	private readonly LegacyListBoxNode _dropSlotPreview = new("DropSlotPreview");
	private readonly LegacyListBoxNode _dropRacePreview = new("DropRacePreview");
	private readonly LegacyListBoxNode _dropSystemsPreview = new("DropSystemsPreview");
	private readonly ButtonNode _lineUpButton = new("LineUpButton") {
		Position = new Vector2(824f, 228f),
		Size = new Vector2(120f, 34f),
		Text = "Line Up",
		FontSize = 16f
	};
	private readonly ButtonNode _lineDownButton = new("LineDownButton") {
		Position = new Vector2(952f, 228f),
		Size = new Vector2(120f, 34f),
		Text = "Line Down",
		FontSize = 16f
	};
	private readonly ButtonNode _pageUpButton = new("PageUpButton") {
		Position = new Vector2(824f, 270f),
		Size = new Vector2(120f, 34f),
		Text = "Page Up",
		FontSize = 16f
	};
	private readonly ButtonNode _pageDownButton = new("PageDownButton") {
		Position = new Vector2(952f, 270f),
		Size = new Vector2(120f, 34f),
		Text = "Page Down",
		FontSize = 16f
	};
	private readonly ButtonNode _homeButton = new("HomeButton") {
		Position = new Vector2(824f, 312f),
		Size = new Vector2(120f, 34f),
		Text = "Home",
		FontSize = 16f
	};
	private readonly ButtonNode _endButton = new("EndButton") {
		Position = new Vector2(952f, 312f),
		Size = new Vector2(120f, 34f),
		Text = "End",
		FontSize = 16f
	};
	private readonly ButtonNode _caretUpButton = new("CaretUpButton") {
		Position = new Vector2(824f, 372f),
		Size = new Vector2(120f, 34f),
		Text = "Caret Up",
		FontSize = 16f
	};
	private readonly ButtonNode _caretDownButton = new("CaretDownButton") {
		Position = new Vector2(952f, 372f),
		Size = new Vector2(120f, 34f),
		Text = "Caret Down",
		FontSize = 16f
	};
	private readonly ButtonNode _commitButton = new("CommitButton") {
		Position = new Vector2(824f, 414f),
		Size = new Vector2(120f, 34f),
		Text = "Commit",
		FontSize = 16f
	};
	private readonly ButtonNode _resetButton = new("ResetButton") {
		Position = new Vector2(952f, 414f),
		Size = new Vector2(120f, 34f),
		Text = "Rebuild",
		FontSize = 16f
	};
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private string _lastCommitted = "none";

	public LegacyListBoxScene() : base("LegacyListBoxScene", "Legacy ListBox") {
		_xmlDbRepository = new XmlDbRepository(RepoPaths.LocateUtfDbPaths());
		_vfxRepository = VfxAnimationDataRepository.LocateFromRepo();
		_eventSource.ScopeRoot = this;

		_artListBox.CaretMoved += _ => _primitiveListBox.SetCurrentSelection(_artListBox.GetCurrentSelection());
		_artListBox.SelectionCommitted += _ => _lastCommitted = $"{_artListBox.SelectedLabel} / data={_artListBox.GetDataValue(_artListBox.GetCurrentSelection())}";

		_lineUpButton.Clicked += _ => _artListBox.ScrollLineUp();
		_lineDownButton.Clicked += _ => _artListBox.ScrollLineDown();
		_pageUpButton.Clicked += _ => _artListBox.ScrollPageUp();
		_pageDownButton.Clicked += _ => _artListBox.ScrollPageDown();
		_homeButton.Clicked += _ => _artListBox.CaretHome();
		_endButton.Clicked += _ => _artListBox.CaretEnd();
		_caretUpButton.Clicked += _ => _artListBox.CaretLineUp();
		_caretDownButton.Clicked += _ => _artListBox.CaretLineDown();
		_commitButton.Clicked += _ => CommitCurrentSelection();
		_resetButton.Clicked += _ => ConfigureListBoxes();

		ConfigureListBoxes();

		AddChild(_eventSource);
		AddChild(_panel);
		AddChild(_statusLabel);
		AddChild(_selectionLabel);
		AddChild(_breakLabel);
		AddChild(_artListBox);
		AddChild(_primitiveListBox);
		AddChild(_dropSlotPreview);
		AddChild(_dropRacePreview);
		AddChild(_dropSystemsPreview);
		AddChild(_lineUpButton);
		AddChild(_lineDownButton);
		AddChild(_pageUpButton);
		AddChild(_pageDownButton);
		AddChild(_homeButton);
		AddChild(_endButton);
		AddChild(_caretUpButton);
		AddChild(_caretDownButton);
		AddChild(_commitButton);
		AddChild(_resetButton);
	}

	protected override void OnUpdate(float deltaTime) {
		_statusLabel.Text = $"Art-backed listbox    top={_artListBox.GetTopVisibleString()}    bottom={_artListBox.GetBottomVisibleString()}    selected={_artListBox.GetCurrentSelection()}    items={_artListBox.GetNumberOfItems()}";
		_selectionLabel.Text = $"Selected: {_artListBox.SelectedLabel}    data={GetSelectedDataValue()}    last committed: {_lastCommitted}";
		_breakLabel.Text = $"GetBreakIndex(\"A very long label for the legacy box\") = {_artListBox.GetBreakIndex("A very long label for the legacy box")}";
	}

	protected override void OnDraw() {
		UiText.Draw("Legacy ListBox", 402f, 50f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("Left: art-backed ListBox!!DropColor with ScrollBar!!Default composed in. Right: the same authored ListBox!!DropColor geometry through the primitive fallback. Click rows, use the mouse wheel over the left list, and drive caret/scroll with the buttons.", 402f, 178f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("ListBox!!DropColor / atlas", 402f, 208f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("ListBox!!DropColor / primitive", 604f, 208f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("ListBox!!DropSlot", 402f, 430f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("ListBox!!DropRace", 534f, 430f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("ListBox!!DropSystems", 686f, 430f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("This pass composes the default legacy scrollbar into the listbox. The remaining gaps are fidelity details, not the basic listbox/scrollbar contract.", 824f, 470f, 16f, new Color(188, 200, 218, 255));
	}

	private void CommitCurrentSelection() {
		var selection = _artListBox.GetCurrentSelection();
		if (selection < 0) {
			return;
		}

		_lastCommitted = $"{_artListBox.SelectedLabel} / data={_artListBox.GetDataValue(selection)}";
	}

	private void ConfigureListBoxes() {
		_lastCommitted = "none";

		var dropColorArchetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", "ListBox!!DropColor");
		var dropSlotArchetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", "ListBox!!DropSlot");
		var dropRaceArchetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", "ListBox!!DropRace");
		var dropSystemsArchetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", "ListBox!!DropSystems");

		_artListBox.ApplyLegacyDefinition(
			dropColorArchetype,
			CreateListBoxData("ListBox!!DropColor", 402, 238, 6, 6, 36, 120, includeScrollbar: true),
			_vfxRepository,
			_xmlDbRepository);
		_artListBox.SetKeyboardFocus(true);

		_primitiveListBox.ApplyLegacyDefinition(
			dropColorArchetype,
			CreateListBoxData("ListBox!!DropColor", 604, 238, 6, 6, 36, 120, includeScrollbar: true),
			repository: null,
			xmlDbRepository: _xmlDbRepository);
		_primitiveListBox.SetKeyboardFocus(true);

		PopulateList(_artListBox);
		PopulateList(_primitiveListBox);
		ConfigurePreview(_dropSlotPreview, dropSlotArchetype, CreateListBoxData("ListBox!!DropSlot", 402, 458, 6, 6, 74, 96), new[] { "Open", "Closed", "Human", "AI", "Ready" });
		ConfigurePreview(_dropRacePreview, dropRaceArchetype, CreateListBoxData("ListBox!!DropRace", 534, 458, 6, 6, 104, 48), new[] { "Solarian", "Vyrium", "Mantis" });
		ConfigurePreview(_dropSystemsPreview, dropSystemsArchetype, CreateListBoxData("ListBox!!DropSystems", 686, 458, 6, 6, 104, 216), new[] { "Sparse", "Balanced", "Heavy", "Rings", "Stars", "Nebula", "Cluster", "Frontier", "Ancient", "Dead" });
	}

	private void PopulateList(LegacyListBoxNode listBox) {
		listBox.ResetContent();
		var labels = new[] {
			"Yel",
			"Red",
			"Blu",
			"Pnk",
			"Grn",
			"Org",
			"Pur",
			"Aqa",
			"Wht",
			"Blk",
			"Tan",
			"Slv"
		};

		var colors = new[] {
			new Color(255, 224, 94, 255),
			new Color(255, 92, 92, 255),
			new Color(88, 170, 255, 255),
			new Color(255, 130, 220, 255),
			new Color(88, 214, 119, 255),
			new Color(255, 167, 76, 255),
			new Color(180, 104, 255, 255),
			new Color(77, 230, 224, 255),
			new Color(242, 242, 242, 255),
			new Color(90, 90, 98, 255),
			new Color(210, 188, 144, 255),
			new Color(188, 196, 204, 255)
		};

		for (var i = 0; i < labels.Length; i++) {
			var index = listBox.AddString(labels[i]);
			listBox.SetDataValue(index, (uint)i);
			listBox.SetColorValue(index, colors[i]);
		}

		listBox.SetCurrentSelection(0);
	}

	private void ConfigurePreview(LegacyListBoxNode listBox, GT_LISTBOX archetype, LISTBOX_DATA data, IReadOnlyList<string> labels) {
		listBox.ApplyLegacyDefinition(archetype, data, _vfxRepository);
		listBox.ResetContent();
		for (var i = 0; i < labels.Count; i++) {
			var index = listBox.AddString(labels[i]);
			listBox.SetDataValue(index, (uint)i);
		}

		listBox.SetCurrentSelection(0);
	}

	private uint GetSelectedDataValue() {
		var selection = _artListBox.GetCurrentSelection();
		return selection >= 0 ? _artListBox.GetDataValue(selection) : 0u;
	}

	private static LISTBOX_DATA CreateListBoxData(string typeId, int x, int y, int left, int top, int right, int bottom, bool includeScrollbar = false) {
		return new LISTBOX_DATA {
			ListboxType = typeId,
			XOrigin = x,
			YOrigin = y,
			TextArea = new RECT {
				Left = left,
				Top = top,
				Right = right,
				Bottom = bottom
			},
			LeadingHeight = 0,
			Flags = (uint)(LegacyListBoxFlags.SingleClick | LegacyListBoxFlags.SolidBackground | (includeScrollbar ? LegacyListBoxFlags.Scrollbar : 0))
		};
	}

	private T ReadTypedEntry<T>(string typeName, string fileName) where T : class {
		var details = _xmlDbRepository.ReadEntryDetails("GenData.db", typeName, fileName);
		return details.TypedValue as T
			?? throw new InvalidOperationException($"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
	}

	[Flags]
	private enum LegacyListBoxFlags : uint {
		SingleClick = 1 << 1,
		Scrollbar = 1 << 2,
		SolidBackground = 1 << 3
	}
}

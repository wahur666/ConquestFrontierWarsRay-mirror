using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class LegacyComboboxScene : ShowcaseScene {
	private readonly UiEventSource _eventSource = new("LegacyComboboxEventSource");
	private readonly PanelNode _panel = new("LegacyComboboxPanel") {
		Position = new Vector2(364f, 20f),
		Size = new Vector2(896f, 680f),
		Fill = new Color(14, 18, 30, 255),
		Outline = new Color(68, 84, 110, 255),
		OutlineThickness = 2f
	};
	private readonly TextNode _statusLabel = new("LegacyComboboxStatus") {
		Position = new Vector2(402f, 88f),
		FontSize = 17f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _textLabel = new("LegacyComboboxText") {
		Position = new Vector2(402f, 118f),
		FontSize = 17f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _selectionLabel = new("LegacyComboboxSelection") {
		Position = new Vector2(402f, 148f),
		FontSize = 17f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _editStatusLabel = new("LegacyEditStatus") {
		Position = new Vector2(402f, 418f),
		FontSize = 16f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly LegacyComboboxNode _artCombobox = new("ArtCombobox");
	private readonly LegacyComboboxNode _primitiveCombobox = new("PrimitiveCombobox");
	private readonly LegacyEditNode _nameEdit = new("LegacyNameEdit");
	private readonly LegacyEditNode _saveEdit = new("LegacySaveEdit");
	private readonly ButtonNode _resetButton = new("ComboboxResetButton") {
		Position = new Vector2(402f, 332f),
		Size = new Vector2(120f, 34f),
		Text = "Reset",
		FontSize = 16f
	};
	private readonly ButtonNode _expandButton = new("ComboboxExpandButton") {
		Position = new Vector2(534f, 332f),
		Size = new Vector2(120f, 34f),
		Text = "Open",
		FontSize = 16f
	};
	private readonly ButtonNode _fillButton = new("ComboboxFillButton") {
		Position = new Vector2(666f, 332f),
		Size = new Vector2(140f, 34f),
		Text = "Set localhost",
		FontSize = 16f
	};
	private readonly ButtonNode _focusNameEditButton = new("FocusNameEditButton") {
		Position = new Vector2(938f, 452f),
		Size = new Vector2(120f, 32f),
		Text = "Focus name",
		FontSize = 15f
	};
	private readonly ButtonNode _focusSaveEditButton = new("FocusSaveEditButton") {
		Position = new Vector2(938f, 494f),
		Size = new Vector2(120f, 32f),
		Text = "Focus save",
		FontSize = 15f
	};
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;
	private string _lastCommitted = "none";

	public LegacyComboboxScene() : base("LegacyComboboxScene", "Legacy Combobox") {
		_xmlDbRepository = new XmlDbRepository(RepoPaths.LocateUtfDbPaths());
		_vfxRepository = VfxAnimationDataRepository.LocateFromRepo();
		_eventSource.ScopeRoot = this;

		_artCombobox.SelectionCommitted += HandleComboboxSelectionCommitted;
		_primitiveCombobox.SelectionCommitted += _ => _primitiveCombobox.SetText(_primitiveCombobox.SelectedLabel);

		_resetButton.Clicked += _ => ConfigureComboboxes();
		_expandButton.Clicked += _ => _artCombobox.SetKeyboardFocus(true);
		_fillButton.Clicked += _ => _artCombobox.SetText("127.0.0.1");
		_focusNameEditButton.Clicked += _ => _nameEdit.RequestKeyboardFocus();
		_focusSaveEditButton.Clicked += _ => _saveEdit.RequestKeyboardFocus();

		_nameEdit.Activated += edit => _lastCommitted = $"name={edit.Text}";
		_saveEdit.Activated += edit => _lastCommitted = $"save={edit.Text}";

		ConfigureComboboxes();
		ConfigureLegacyEdits();

		AddChild(_eventSource);
		AddChild(_panel);
		AddChild(_statusLabel);
		AddChild(_textLabel);
		AddChild(_selectionLabel);
		AddChild(_editStatusLabel);
		AddChild(_artCombobox);
		AddChild(_primitiveCombobox);
		AddChild(_nameEdit);
		AddChild(_saveEdit);
		AddChild(_resetButton);
		AddChild(_expandButton);
		AddChild(_fillButton);
		AddChild(_focusNameEditButton);
		AddChild(_focusSaveEditButton);
	}

	protected override void OnUpdate(float deltaTime) {
		_statusLabel.Text = $"Art-backed combobox    items={_artCombobox.GetNumberOfItems()}    selected={_artCombobox.GetCurrentSelection()}    controlId=0x{_artCombobox.ControlId:X4}";
		_textLabel.Text = $"Typed text: {_artCombobox.Text}";
		var selectedIndex = _artCombobox.GetCurrentSelection();
		var dataValue = selectedIndex >= 0 ? _artCombobox.GetDataValue(selectedIndex) : 0u;
		_selectionLabel.Text = $"Selected: {_artCombobox.SelectedLabel}    data={dataValue}    last committed: {_lastCommitted}";
		_editStatusLabel.Text = $"Edit2 samples    name=\"{_nameEdit.Text}\"    save=\"{_saveEdit.Text}\"    overwrite={_saveEdit.OverwriteMode}";
	}

	protected override void OnDraw() {
		UiText.Draw("Legacy Combobox", 402f, 50f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("This scene reads the real Menu1 IP-address combobox through XmlDbRepository, composes the edit field, arrow button, and popup list from their authored GT records, and exercises the original editable-prefix behavior.", 402f, 180f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Menu1.ipAddress.comboboxIP / art-backed", 402f, 210f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("Same data / primitive edit fallback", 612f, 210f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("Type digits or dots, backspace through the autocomplete suffix, click the arrow to drop the list, and pick a stored address. The right-hand control disables only the edit background art to prove the core behavior is not tied to one atlas.", 402f, 390f, 16f, new Color(188, 200, 218, 255));
		UiText.Draw("Standalone Edit2 / GT_NEWPLAYER", 402f, 452f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("Primitive Edit2 / GT_LOADSAVE", 402f, 494f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("The edit controls use real XML-authored archetypes and model placements through XmlDbRepository. Double-click selects a word, drag selects ranges, Insert toggles overwrite, Ctrl+Insert copies, Shift+Insert pastes, and Shift+Delete cuts into the scratch buffer.", 402f, 548f, 16f, new Color(188, 200, 218, 255));
	}

	private void HandleComboboxSelectionCommitted(LegacyComboboxNode combobox) {
		var selectedIndex = combobox.GetCurrentSelection();
		var dataValue = selectedIndex >= 0 ? combobox.GetDataValue(selectedIndex) : 0u;
		_lastCommitted = $"{combobox.SelectedLabel} / data={dataValue}";
	}

	private void ConfigureComboboxes() {
		_lastCommitted = "none";

		var menu1 = ReadTypedEntry<GT_MENU1>("GT_MENU1", "Menu1");
		var comboboxData = menu1.IpAddress.ComboboxIp;
		var comboboxArchetype = ReadTypedEntry<GT_COMBOBOX>("GT_COMBOBOX", comboboxData.ComboboxType);
		var editArchetype = ReadTypedEntry<GT_EDIT>("GT_EDIT", comboboxData.EditData.EditType);
		var buttonArchetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", comboboxData.ButtonData.ButtonType);
		var listboxArchetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", comboboxData.ListboxData.ListboxType);

		var artData = Shift(comboboxData, 122, 22);
		var primitiveData = Shift(comboboxData, 332, 22);

		_artCombobox.ApplyLegacyDefinition(
			comboboxArchetype,
			editArchetype,
			buttonArchetype,
			listboxArchetype,
			artData,
			_vfxRepository,
			_xmlDbRepository);
		_artCombobox.ControlId = 0x3101;
		_artCombobox.RequestKeyboardFocus();
		_artCombobox.SetMaxChars(40);

		_primitiveCombobox.ApplyLegacyDefinition(
			comboboxArchetype,
			editArchetype,
			buttonArchetype,
			listboxArchetype,
			primitiveData,
			repository: null,
			xmlDbRepository: _xmlDbRepository);
		_primitiveCombobox.ControlId = 0x3102;
		_primitiveCombobox.SetMaxChars(40);

		PopulateHistory(_artCombobox);
		PopulateHistory(_primitiveCombobox);
		_artCombobox.SetText("192.");
		_primitiveCombobox.SetText("10.");
	}

	private void ConfigureLegacyEdits() {
		var newPlayer = ReadTypedEntry<GT_NEWPLAYER>("GT_NEWPLAYER", "MenuNewPlayer");
		var loadSave = ReadTypedEntry<GT_LOADSAVE>("GT_LOADSAVE", "MenuLoadSave");
		var nameArchetype = ReadTypedEntry<GT_EDIT>("GT_EDIT", newPlayer.Edit.EditType);
		var saveArchetype = ReadTypedEntry<GT_EDIT>("GT_EDIT", loadSave.EditFile.EditType);

		_nameEdit.ApplyLegacyDefinition(nameArchetype, Shift(newPlayer.Edit, 302, 332), _vfxRepository);
		_nameEdit.ControlId = 0x3201;
		_nameEdit.SetMaxChars(24);
		_nameEdit.SetText("Admiral Hayes");

		_saveEdit.ApplyLegacyDefinition(saveArchetype, Shift(loadSave.EditFile, 302, 374), repository: null);
		_saveEdit.ControlId = 0x3202;
		_saveEdit.SetMaxChars(40);
		_saveEdit.SetText("savegame_001");
		_saveEdit.SetKeyboardFocus(false);
	}

	private static void PopulateHistory(LegacyComboboxNode combobox) {
		combobox.ResetContent();
		var addresses = new[] {
			"127.0.0.1",
			"192.168.0.7",
			"192.168.0.21",
			"10.0.0.4",
			"10.0.1.12",
			"172.16.1.55",
			"203.0.113.9"
		};

		for (var i = 0; i < addresses.Length; i++) {
			var index = combobox.AddString(addresses[i]);
			combobox.SetDataValue(index, (uint)(5000 + i));
		}
	}

	private T ReadTypedEntry<T>(string typeName, string fileName) where T : class {
		var details = _xmlDbRepository.ReadEntryDetails("GenData.db", typeName, fileName);
		return details.TypedValue as T
			?? throw new InvalidOperationException($"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
	}

	private static COMBOBOX_DATA Shift(COMBOBOX_DATA data, int offsetX, int offsetY) {
		return new COMBOBOX_DATA {
			ComboboxType = data.ComboboxType,
			ScreenRect = Shift(data.ScreenRect, offsetX, offsetY),
			EditData = new EDIT_DATA {
				EditType = data.EditData.EditType,
				EditText = data.EditData.EditText,
				XOrigin = data.EditData.XOrigin,
				YOrigin = data.EditData.YOrigin
			},
			ButtonData = new BUTTON_DATA {
				ButtonType = data.ButtonData.ButtonType,
				ButtonText = data.ButtonData.ButtonText,
				XOrigin = data.ButtonData.XOrigin,
				YOrigin = data.ButtonData.YOrigin,
				ButtonArea = data.ButtonData.ButtonArea
			},
			ListboxData = new LISTBOX_DATA {
				ListboxType = data.ListboxData.ListboxType,
				XOrigin = data.ListboxData.XOrigin,
				YOrigin = data.ListboxData.YOrigin,
				TextArea = data.ListboxData.TextArea,
				LeadingHeight = data.ListboxData.LeadingHeight,
				Flags = data.ListboxData.Flags
			}
		};
	}

	private static RECT Shift(RECT rect, int offsetX, int offsetY) {
		return new RECT {
			Left = rect.Left + offsetX,
			Top = rect.Top + offsetY,
			Right = rect.Right + offsetX,
			Bottom = rect.Bottom + offsetY
		};
	}

	private static EDIT_DATA Shift(EDIT_DATA data, int offsetX, int offsetY) {
		return new EDIT_DATA {
			EditType = data.EditType,
			EditText = data.EditText,
			XOrigin = data.XOrigin + offsetX,
			YOrigin = data.YOrigin + offsetY
		};
	}
}

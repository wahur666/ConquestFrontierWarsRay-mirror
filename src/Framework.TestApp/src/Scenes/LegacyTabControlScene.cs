using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class LegacyTabControlScene : ShowcaseScene {
	private readonly UiEventSource _eventSource = new("LegacyTabEventSource");
	private readonly PanelNode _panel = new("LegacyTabPanel") {
		Position = new Vector2(364f, 20f),
		Size = new Vector2(896f, 680f),
		Fill = new Color(14, 18, 30, 255),
		Outline = new Color(68, 84, 110, 255),
		OutlineThickness = 2f
	};
	private readonly TextNode _statusLabel = new("LegacyTabStatus") {
		Position = new Vector2(402f, 88f),
		FontSize = 17f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _detailsLabel = new("LegacyTabDetails") {
		Position = new Vector2(402f, 116f),
		FontSize = 17f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly LegacyTabControlNode _tabControl = new("LegacyTabControl");
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;

	private string _lastAction = "none";
	private string _selectedResolution = "1024x768";
	private int _soundValue = 7;
	private int _musicValue = 5;
	private int _commValue = 8;

	public LegacyTabControlScene() : base("LegacyTabControlScene", "Legacy TabControl") {
		_xmlDbRepository = new XmlDbRepository(RepoPaths.LocateUtfDbPaths());
		_vfxRepository = VfxAnimationDataRepository.LocateFromRepo();
		_eventSource.ScopeRoot = this;

		BuildScene();

		AddChild(_eventSource);
		AddChild(_panel);
		AddChild(_statusLabel);
		AddChild(_detailsLabel);
		AddChild(_tabControl);
	}

	protected override void OnUpdate(float deltaTime) {
		_statusLabel.Text = $"selected tab={_tabControl.SelectedTabIndex}    resolution={_selectedResolution}    sound={_soundValue}    music={_musicValue}    comm={_commValue}";
		_detailsLabel.Text = $"last action: {_lastAction}";
	}

	protected override void OnDraw() {
		UiText.Draw("Legacy TabControl", 402f, 50f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("This scene reads the authored MenuOptions tab model through XmlDbRepository, composes the tab strip from the real tab archetypes, and exercises page-local focus across existing legacy controls. Use Tab and Shift+Tab to switch tabs. Click inside a page to hand focus to a child control.", 402f, 156f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Player", 560f, 490f, 16f, new Color(224, 230, 242, 255));
		UiText.Draw("Graphics", 690f, 490f, 16f, new Color(224, 230, 242, 255));
		UiText.Draw("Sounds", 820f, 490f, 16f, new Color(224, 230, 242, 255));
	}

	private void BuildScene() {
		var menuOptions = ReadTypedEntry<GT_OPTIONS>("GT_OPTIONS", "MenuOptions");
		var tabArchetype = ReadTypedEntry<GT_TABCONTROL>("GT_TABCONTROL", menuOptions.Tab.TabControlType);
		var hotButtonArchetype = ReadTypedEntry<GT_HOTBUTTON>("GT_HOTBUTTON", menuOptions.Tab.HotButtonType);
		var tabShape = ReadTypedEntry<GT_VFXSHAPE>("GT_VFXSHAPE", "VFXShape!!TabOptions");

		var tabData = new TABCONTROL_DATA {
			TabControlType = menuOptions.Tab.TabControlType,
			HotButtonType = menuOptions.Tab.HotButtonType,
			BaseImage = menuOptions.Tab.BaseImage,
			NumTabs = menuOptions.Tab.NumTabs,
			TextIds = menuOptions.Tab.TextIds,
			UpperTabs = menuOptions.Tab.UpperTabs,
			XPos = 520,
			YPos = 520
		};

		_tabControl.ApplyLegacyDefinition(
			tabArchetype,
			hotButtonArchetype,
			tabShape,
			tabData,
			["Player", "Graphics", "Sounds"],
			_vfxRepository);
		_tabControl.EnableKeyboardFocusing();
		_tabControl.SetKeyboardFocus(true);
		_tabControl.SelectedTabChanged += (_, index) => _lastAction = $"switched to tab {index}";

		BuildPlayerTab(menuOptions);
		BuildGraphicsTab(menuOptions);
		BuildSoundsTab(menuOptions);
	}

	private void BuildPlayerTab(GT_OPTIONS menuOptions) {
		var listboxArchetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", menuOptions.ListNames.ListboxType);
		var buttonArchetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", menuOptions.ButtonNew.ButtonType);

		var players = _tabControl.AddTabPageChild(0, new LegacyListBoxNode("PlayerList"));
		players.ApplyLegacyDefinition(
			listboxArchetype,
			Offset(menuOptions.ListNames, 24, 0),
			repository: null,
			xmlDbRepository: _xmlDbRepository);
		players.AddString("Admiral Hayes");
		players.AddString("Mordred");
		players.AddString("Takei");
		players.SetCurrentSelection(0);
		players.SelectionCommitted += list => _lastAction = $"player={list.SelectedLabel}";

		var buttonNew = CreateSmallButton("ButtonNew", buttonArchetype, Offset(menuOptions.ButtonNew, 24, 0), "New");
		var buttonChange = CreateSmallButton("ButtonChange", buttonArchetype, Offset(menuOptions.ButtonChange, 24, 0), "Rename");
		var buttonDelete = CreateSmallButton("ButtonDelete", buttonArchetype, Offset(menuOptions.ButtonDelete, 24, 0), "Delete");

		buttonNew.Activated += _ => _lastAction = "player action=new";
		buttonChange.Activated += _ => _lastAction = $"player action=rename {players.SelectedLabel}";
		buttonDelete.Activated += _ => _lastAction = $"player action=delete {players.SelectedLabel}";

		_tabControl.AddTabPageChild(0, buttonNew);
		_tabControl.AddTabPageChild(0, buttonChange);
		_tabControl.AddTabPageChild(0, buttonDelete);
		_tabControl.SetDefaultControlForTab(0, players);
	}

	private void BuildGraphicsTab(GT_OPTIONS menuOptions) {
		var dropdownButtonArchetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", menuOptions.DropResolution.ButtonData.ButtonType);
		var dropdownListboxArchetype = ReadTypedEntry<GT_LISTBOX>("GT_LISTBOX", menuOptions.DropResolution.ListboxData.ListboxType);
		var sliderArchetype = ReadTypedEntry<GT_SLIDER>("GT_SLIDER", menuOptions.SliderGamma.SliderType);

		var resolution = _tabControl.AddTabPageChild(1, new LegacyDropdownNode("ResolutionDropdown"));
		resolution.ApplyLegacyDefinition(
			dropdownButtonArchetype,
			dropdownListboxArchetype,
			Offset(menuOptions.DropResolution, 20, 0),
			repository: null,
			xmlDbRepository: _xmlDbRepository);
		resolution.AddString("1024x768");
		resolution.AddString("1280x720");
		resolution.AddString("1600x900");
		resolution.AddString("1920x1080");
		resolution.SetCurrentSelection(0);
		resolution.SelectionCommitted += drop => {
			_selectedResolution = drop.SelectedLabel;
			_lastAction = $"resolution={drop.SelectedLabel}";
		};

		var gamma = _tabControl.AddTabPageChild(1, new LegacySliderNode("GammaSlider"));
		gamma.ApplyLegacyDefinition(sliderArchetype, Offset(menuOptions.SliderGamma, 20, -50), _vfxRepository);
		gamma.SetRange(0, 10);
		gamma.SetSliderPosition(6, emitEvent: false);
		gamma.ValueChanged += slider => _lastAction = $"gamma={slider.SliderPosition}";

		var detail = _tabControl.AddTabPageChild(1, new LegacySliderNode("DetailSlider"));
		detail.ApplyLegacyDefinition(sliderArchetype, Offset(menuOptions.SliderShips3D, 20, -50), _vfxRepository);
		detail.SetRange(0, 10);
		detail.SetSliderPosition(8, emitEvent: false);
		detail.ValueChanged += slider => _lastAction = $"ships3d={slider.SliderPosition}";

		_tabControl.SetDefaultControlForTab(1, resolution);
	}

	private void BuildSoundsTab(GT_OPTIONS menuOptions) {
		var sliderArchetype = ReadTypedEntry<GT_SLIDER>("GT_SLIDER", menuOptions.SliderSound.SliderType);

		var sound = _tabControl.AddTabPageChild(2, new LegacySliderNode("SoundSlider"));
		sound.ApplyLegacyDefinition(sliderArchetype, Offset(menuOptions.SliderSound, 300, 520), _vfxRepository);
		sound.SetRange(0, 10);
		sound.SetSliderPosition(_soundValue, emitEvent: false);
		sound.ValueChanged += slider => {
			_soundValue = slider.SliderPosition;
			_lastAction = $"sound={slider.SliderPosition}";
		};

		var music = _tabControl.AddTabPageChild(2, new LegacySliderNode("MusicSlider"));
		music.ApplyLegacyDefinition(sliderArchetype, Offset(menuOptions.SliderMusic, 300, 520), _vfxRepository);
		music.SetRange(0, 10);
		music.SetSliderPosition(_musicValue, emitEvent: false);
		music.ValueChanged += slider => {
			_musicValue = slider.SliderPosition;
			_lastAction = $"music={slider.SliderPosition}";
		};

		var comm = _tabControl.AddTabPageChild(2, new LegacySliderNode("CommSlider"));
		comm.ApplyLegacyDefinition(sliderArchetype, Offset(menuOptions.SliderComm, 300, 520), _vfxRepository);
		comm.SetRange(0, 10);
		comm.SetSliderPosition(_commValue, emitEvent: false);
		comm.ValueChanged += slider => {
			_commValue = slider.SliderPosition;
			_lastAction = $"comm={slider.SliderPosition}";
		};

		_tabControl.SetDefaultControlForTab(2, sound);
	}

	private LegacyButtonNode CreateSmallButton(string name, GT_BUTTON archetype, BUTTON_DATA data, string text) {
		var button = new LegacyButtonNode(name);
		button.ApplyLegacyDefinition(archetype, data, repository: null);
		button.Text = text;
		return button;
	}

	private T ReadTypedEntry<T>(string typeName, string fileName) where T : class {
		var details = _xmlDbRepository.ReadEntryDetails("GenData.db", typeName, fileName);
		return details.TypedValue as T
			?? throw new InvalidOperationException($"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
	}

	private static BUTTON_DATA Offset(BUTTON_DATA source, int xOffset, int yOffset) {
		return new BUTTON_DATA {
			ButtonType = source.ButtonType,
			ButtonText = source.ButtonText,
			XOrigin = source.XOrigin + xOffset,
			YOrigin = source.YOrigin + yOffset,
			ButtonArea = source.ButtonArea
		};
	}

	private static LISTBOX_DATA Offset(LISTBOX_DATA source, int xOffset, int yOffset) {
		return new LISTBOX_DATA {
			ListboxType = source.ListboxType,
			XOrigin = source.XOrigin + xOffset,
			YOrigin = source.YOrigin + yOffset,
			TextArea = source.TextArea,
			LeadingHeight = source.LeadingHeight,
			Flags = source.Flags
		};
	}

	private static SLIDER_DATA Offset(SLIDER_DATA source, int xOffset, int yOffset) {
		return new SLIDER_DATA {
			SliderType = source.SliderType,
			ScreenRect = source.ScreenRect,
			Origin = source.Origin + new Vector2(xOffset, yOffset)
		};
	}

	private static DROPDOWN_DATA Offset(DROPDOWN_DATA source, int xOffset, int yOffset) {
		return new DROPDOWN_DATA {
			DropdownType = source.DropdownType,
			ScreenRect = new RECT {
				Left = source.ScreenRect.Left + xOffset,
				Top = source.ScreenRect.Top + yOffset,
				Right = source.ScreenRect.Right + xOffset,
				Bottom = source.ScreenRect.Bottom + yOffset
			},
			ButtonData = source.ButtonData,
			ListboxData = source.ListboxData
		};
	}

}

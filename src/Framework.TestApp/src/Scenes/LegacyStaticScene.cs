using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class LegacyStaticScene : ShowcaseScene {
	private readonly UiEventSource _eventSource = new("LegacyStaticEventSource");
	private readonly PanelNode _panel = new("LegacyStaticPanel") {
		Position = new Vector2(364f, 20f),
		Size = new Vector2(896f, 680f),
		Fill = new Color(14, 18, 30, 255),
		Outline = new Color(68, 84, 110, 255),
		OutlineThickness = 2f
	};
	private readonly TextNode _statusLabel = new("LegacyStaticStatus") {
		Position = new Vector2(402f, 88f),
		FontSize = 17f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _detailsLabel = new("LegacyStaticDetails") {
		Position = new Vector2(402f, 116f),
		FontSize = 17f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly LegacyStaticNode _backgroundStatic = new("LegacyBackgroundStatic");
	private readonly LegacyStaticNode _loadSaveStatic = new("LegacyLoadSaveStatic");
	private readonly LegacyStaticNode _titleStatic = new("LegacyTitleStatic");
	private readonly LegacyStaticNode _fillStatic = new("LegacyFillStatic");
	private readonly LegacyStaticNode _hashStatic = new("LegacyHashStatic");
	private readonly LegacyStaticNode _plainStatic = new("LegacyPlainStatic");
	private readonly LegacyStaticNode _buddyStatic = new("LegacyBuddyStatic");
	private readonly LegacyButtonNode _buddyButton = new("LegacyBuddyButton");
	private readonly XmlDbRepository _xmlDbRepository;
	private readonly VfxAnimationDataRepository _vfxRepository;

	private string _lastBuddyAction = "none";

	public LegacyStaticScene() : base("LegacyStaticScene", "Legacy Static") {
		_xmlDbRepository = new XmlDbRepository(RepoPaths.LocateUtfDbPaths());
		_vfxRepository = VfxAnimationDataRepository.LocateFromRepo();
		_eventSource.ScopeRoot = this;

		_buddyButton.Activated += _ => _lastBuddyAction = $"Button activated at {DateTime.Now:HH:mm:ss}";
		ConfigureScene();

		AddChild(_eventSource);
		AddChild(_panel);
		AddChild(_statusLabel);
		AddChild(_detailsLabel);
		AddChild(_backgroundStatic);
		AddChild(_loadSaveStatic);
		AddChild(_titleStatic);
		AddChild(_fillStatic);
		AddChild(_hashStatic);
		AddChild(_plainStatic);
		AddChild(_buddyButton);
		AddChild(_buddyStatic);
	}

	protected override void OnUpdate(float deltaTime) {
		_statusLabel.Text = $"GT_STATIC archetypes: Background, LoadSave, Title, ControlFill, HashBlue, NoBack    buddy: {_lastBuddyAction}";
		_detailsLabel.Text = $"Hash tooltip={_hashStatic.StaticTooltipId} hint={_hashStatic.StaticHintboxId}    title width={_titleStatic.GetStringWidth():0}";
	}

	protected override void OnDraw() {
		UiText.Draw("Legacy Static", 402f, 50f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("This scene renders real GT_STATIC archetypes through LegacyStaticNode: shape-only panels, fill/hash backgrounds, shadowed title text, plain text, and a buddy-control overlay forwarding pointer input into a legacy button.", 402f, 156f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Static!!Background", 402f, 188f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("Static!!LoadSave", 924f, 188f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("Static!!Title", 430f, 226f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("Static!!ControlFill", 430f, 332f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("Static!!HashBlue", 692f, 332f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("Static!!NoBack", 430f, 454f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("Buddy forwarding", 430f, 560f, 18f, new Color(230, 236, 246, 255));
	}

	private void ConfigureScene() {
		var backgroundArchetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", "Static!!Background");
		var loadSaveArchetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", "Static!!LoadSave");
		var titleArchetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", "Static!!Title");
		var fillArchetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", "Static!!ControlFill");
		var hashArchetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", "Static!!HashBlue");
		var plainArchetype = ReadTypedEntry<GT_STATIC>("GT_STATIC", "Static!!NoBack");
		var buddyButtonArchetype = ReadTypedEntry<GT_BUTTON>("GT_BUTTON", "Button!!Back2D");

		_backgroundStatic.ApplyLegacyDefinition(
			backgroundArchetype,
			new STATIC_DATA {
				StaticType = "Static!!Background",
				XOrigin = 402,
				YOrigin = 214,
				Width = 800,
				Height = 600,
				Alignment = (uint)LegacyStaticNode.StaticAlignment.Left
			},
			_vfxRepository);
		_backgroundStatic.Scale = new Vector2(0.62f, 0.62f);

		_loadSaveStatic.ApplyLegacyDefinition(
			loadSaveArchetype,
			new STATIC_DATA {
				StaticType = "Static!!LoadSave",
				XOrigin = 924,
				YOrigin = 214,
				Width = 280,
				Height = 188,
				Alignment = (uint)LegacyStaticNode.StaticAlignment.Left
			},
			_vfxRepository);
		_loadSaveStatic.Scale = new Vector2(0.62f, 0.62f);

		_titleStatic.ApplyLegacyDefinition(
			titleArchetype,
			new STATIC_DATA {
				StaticType = "Static!!Title",
				XOrigin = 430,
				YOrigin = 248,
				Width = 500,
				Height = 34,
				Alignment = (uint)LegacyStaticNode.StaticAlignment.Center
			});
		_titleStatic.SetText("Conquest Frontier Wars");

		_fillStatic.ApplyLegacyDefinition(
			fillArchetype,
			new STATIC_DATA {
				StaticType = "Static!!ControlFill",
				XOrigin = 430,
				YOrigin = 358,
				Width = 220,
				Height = 74,
				Alignment = (uint)LegacyStaticNode.StaticAlignment.Center
			});
		_fillStatic.SetText("FILL");

		_hashStatic.ApplyLegacyDefinition(
			hashArchetype,
			new STATIC_DATA {
				StaticType = "Static!!HashBlue",
				XOrigin = 692,
				YOrigin = 358,
				Width = 220,
				Height = 74,
				Alignment = (uint)LegacyStaticNode.StaticAlignment.Center,
				StaticTooltip = 1,
				StaticHintbox = 1
			});
		_hashStatic.SetText("HASH");

		_plainStatic.ApplyLegacyDefinition(
			plainArchetype,
			new STATIC_DATA {
				StaticType = "Static!!NoBack",
				XOrigin = 430,
				YOrigin = 480,
				Width = 482,
				Height = 48,
				Alignment = (uint)LegacyStaticNode.StaticAlignment.TopLeft
			});
		_plainStatic.SetText("No background, no shadow. This is the plain text case.");

		_buddyButton.ApplyLegacyDefinition(
			buddyButtonArchetype,
			new BUTTON_DATA {
				ButtonType = "Button!!Back2D",
				XOrigin = 430,
				YOrigin = 588
			},
			repository: null);
		_buddyButton.Size = new Vector2(180f, 34f);
		_buddyButton.Text = "Back";

		_buddyStatic.ApplyLegacyDefinition(
			plainArchetype,
			new STATIC_DATA {
				StaticType = "Static!!NoBack",
				XOrigin = 620,
				YOrigin = 588,
				Width = 220,
				Height = 34,
				Alignment = (uint)LegacyStaticNode.StaticAlignment.Left
			});
		_buddyStatic.SetBuddyControl(_buddyButton);
		_buddyStatic.SetText("Click here to drive the buddy button");
	}

	private T ReadTypedEntry<T>(string typeName, string fileName) where T : class {
		var details = _xmlDbRepository.ReadEntryDetails("GenData.db", typeName, fileName);
		return details.TypedValue as T
			?? throw new InvalidOperationException($"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
	}
}

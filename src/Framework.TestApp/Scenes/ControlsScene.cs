using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class ControlsScene : ShowcaseScene {
	private readonly PanelNode _panel = new("ControlsPanel") {
		Position = new Vector2(390f, 76f),
		Size = new Vector2(812f, 568f),
		Fill = new Color(21, 28, 43, 255),
		Outline = new Color(82, 100, 128, 255),
		OutlineThickness = 2f
	};
	private readonly TextNode _title = new("ControlsTitle") {
		Position = new Vector2(426f, 112f),
		Text = "UI controls",
		FontSize = 28f,
		Tint = Color.RayWhite,
		TextStyle = UiTextStyle.Title
	};
	private readonly TextNode _counterLabel = new("CounterLabel") {
		Position = new Vector2(426f, 174f),
		FontSize = 18f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _themeLabel = new("ThemeLabel") {
		Position = new Vector2(426f, 204f),
		FontSize = 18f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly ButtonNode _incrementButton = new("IncrementButton") {
		Position = new Vector2(426f, 260f),
		Size = new Vector2(164f, 44f),
		Text = "Increment",
		FontSize = 18f
	};
	private readonly ButtonNode _resetButton = new("ResetButton") {
		Position = new Vector2(606f, 260f),
		Size = new Vector2(132f, 44f),
		Text = "Reset",
		FontSize = 18f
	};
	private readonly ButtonNode _themeButton = new("ThemeButton") {
		Position = new Vector2(756f, 260f),
		Size = new Vector2(188f, 44f),
		Text = "Cycle accent",
		FontSize = 18f
	};
	private readonly HotZoneNode _hotZone = new("HotZone") {
		Position = new Vector2(840f, 130f),
		Size = new Vector2(290f, 92f)
	};
	private readonly Color[] _accents = [
		new(56, 89, 148, 255),
		new(58, 126, 94, 255),
		new(146, 90, 56, 255)
	];
	private int _count;
	private int _accentIndex;

	public ControlsScene() : base("ControlsScene", "Controls") {
		AddChild(_panel);
		AddChild(_title);
		AddChild(_counterLabel);
		AddChild(_themeLabel);
		AddChild(_incrementButton);
		AddChild(_resetButton);
		AddChild(_themeButton);
		AddChild(_hotZone);
	}

	protected override void OnUpdate(float deltaTime) {
		if (_incrementButton.HandleInput()) {
			_count++;
		}

		if (_resetButton.HandleInput()) {
			_count = 0;
		}

		if (_themeButton.HandleInput()) {
			_accentIndex = (_accentIndex + 1) % _accents.Length;
		}

		_panel.Fill = _accents[_accentIndex];
		_counterLabel.Text = $"Counter value: {_count}";
		_themeLabel.Text = $"Panel accent: {_accentIndex + 1} / {_accents.Length}";
	}

	protected override void OnDraw() {
		UiText.Draw("Control is the hit-test base here. The hot zone below is a plain Control-derived node using ContainsPoint(...) for hover feedback.", 426f, 334f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Buttons and labels are framework nodes; interaction stays inside the framework node tree.", 426f, 358f, 17f, new Color(188, 200, 218, 255));
	}

	private sealed class HotZoneNode : Control {
		public HotZoneNode(string name) : base(name) {
		}

		protected override void Draw() {
			var hovered = ContainsPoint(Raylib.GetMousePosition());
			var bounds = GlobalBounds;
			Raylib.DrawRectangleRec(bounds, hovered ? new Color(73, 101, 158, 220) : new Color(38, 50, 76, 220));
			Raylib.DrawRectangleLinesEx(bounds, 2f, hovered ? Color.Gold : new Color(150, 168, 196, 255));
			UiText.Draw(hovered ? "Pointer inside Control bounds" : "Move the mouse into this Control", bounds.X + 18f, bounds.Y + 22f, 18f, Color.RayWhite);
			UiText.Draw($"Bounds: {bounds.X:0}, {bounds.Y:0}, {bounds.Width:0}, {bounds.Height:0}", bounds.X + 18f, bounds.Y + 52f, 16f, new Color(206, 216, 230, 255));
		}
	}
}

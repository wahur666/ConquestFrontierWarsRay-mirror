using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class UiTextAnimationScene : ShowcaseScene {
	private readonly PanelNode _panel = new("UiTextPanel") {
		Position = new Vector2(364f, 20f),
		Size = new Vector2(896f, 680f),
		Fill = new Color(14, 18, 30, 255),
		Outline = new Color(68, 84, 110, 255),
		OutlineThickness = 2f
	};
	private readonly OrbitGuideNode _orbitGuide = new("UiTextOrbitGuide");
	private readonly UiText _animatedText = new("AnimatedUiText") {
		Text = "Sizing respects transform now",
		FontSize = 32f,
		Tint = Color.Gold,
		TextStyle = UiTextStyle.Title,
		Pivot = new Vector2(0.5f, 0.5f)
	};
	private readonly UiText _statusText = new("UiTextStatus") {
		Position = new Vector2(402f, 572f),
		Text = string.Empty,
		FontSize = 18f,
		Tint = new Color(214, 224, 238, 255)
	};
	private readonly UiText _pivotText = new("UiTextPivot") {
		Text = "Pivot",
		FontSize = 16f,
		Tint = new Color(176, 190, 212, 255),
		Pivot = new Vector2(0.5f, 1f)
	};
	private readonly Vector2 _orbitCenter = new(812f, 332f);
	private float _elapsed;

	public UiTextAnimationScene() : base("UiTextAnimationScene", "UiText Animation") {
		AddChild(_panel);
		AddChild(_orbitGuide);
		AddChild(_animatedText);
		AddChild(_statusText);
		AddChild(_pivotText);
	}

	protected override void OnUpdate(float deltaTime) {
		_elapsed += deltaTime;

		var orbitRadius = new Vector2(220f, 104f);
		var orbitAngle = _elapsed * 0.9f;
		var orbitOffset = new Vector2(
			MathF.Cos(orbitAngle) * orbitRadius.X,
			MathF.Sin(orbitAngle) * orbitRadius.Y);
		var scale = 1f + (MathF.Sin(_elapsed * 2.2f) * 0.35f);

		_animatedText.Position = _orbitCenter + orbitOffset;
		_animatedText.Scale = Vector2.One * scale;
		_animatedText.Rotation = _elapsed * 1.8f;

		_pivotText.Position = _animatedText.Position + new Vector2(0f, -48f);
		_statusText.Text = $"Scale {scale:0.00}    Rotation {_animatedText.Rotation:0.00} rad    Position {_animatedText.Position.X:0}, {_animatedText.Position.Y:0}";
		_orbitGuide.Center = _orbitCenter;
		_orbitGuide.OrbitRadius = orbitRadius;
		_orbitGuide.Target = _animatedText.GlobalPosition;
	}

	protected override void OnDraw() {
		UiText.Draw("UiText control", 402f, 44f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("This scene drives one UiText node through sine-wave scale, circular motion, and pivot rotation. The measured control bounds stay tied to the text instead of treating it like an unscaled screen-space draw call.", 402f, 78f, 17f, new Color(176, 190, 212, 255));
		UiText.Draw("Crosshair marks the orbit center. The gold marker tracks the actual text pivot.", 402f, 122f, 17f, new Color(206, 216, 232, 255));
	}

	private sealed class OrbitGuideNode : Node2D {
		public OrbitGuideNode(string name) : base(name) {
		}

		public Vector2 Center { get; set; }
		public Vector2 OrbitRadius { get; set; }
		public Vector2 Target { get; set; }

		protected override void Draw() {
			Raylib.DrawEllipseLines((int)Center.X, (int)Center.Y, OrbitRadius.X, OrbitRadius.Y, new Color(66, 90, 126, 255));
			Raylib.DrawLineEx(Center + new Vector2(-16f, 0f), Center + new Vector2(16f, 0f), 2f, new Color(110, 132, 174, 255));
			Raylib.DrawLineEx(Center + new Vector2(0f, -16f), Center + new Vector2(0f, 16f), 2f, new Color(110, 132, 174, 255));
			Raylib.DrawCircleV(Target, 5f, Color.Gold);
			Raylib.DrawLineEx(Center, Target, 2f, new Color(92, 120, 170, 180));
		}
	}
}

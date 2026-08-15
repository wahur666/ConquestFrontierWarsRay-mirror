using System.Numerics;
using System.Text;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class HierarchyScene : ShowcaseScene {
	private readonly PanelNode _panel = new("HierarchyPanel") {
		Position = new Vector2(364f, 20f),
		Size = new Vector2(896f, 680f),
		Fill = new Color(14, 18, 30, 255),
		Outline = new Color(68, 84, 110, 255),
		OutlineThickness = 2f
	};
	private readonly CanvasPulseNode _pulseNode = new("CanvasPulse") {
		Position = new Vector2(815f, 330f)
	};
	private readonly TransformMarkerNode _rootMarker = new("RootPivot", "Root", Color.SkyBlue, 18f) {
		Position = new Vector2(815f, 330f)
	};
	private readonly TransformMarkerNode _armMarker = new("ArmPivot", "Arm", Color.Lime, 14f) {
		Position = new Vector2(180f, 0f)
	};
	private readonly TransformMarkerNode _tipMarker = new("TipPivot", "Tip", Color.Orange, 11f) {
		Position = new Vector2(110f, 0f)
	};
	private readonly StringBuilder _treeBuilder = new();
	private float _elapsed;

	public HierarchyScene() : base("HierarchyScene", "Hierarchy / 2D") {
		AddChild(_panel);
		AddChild(_pulseNode);
		AddChild(_rootMarker);
		_rootMarker.AddChild(_armMarker);
		_armMarker.AddChild(_tipMarker);
	}

	protected override void OnUpdate(float deltaTime) {
		_elapsed += deltaTime;
		_rootMarker.Rotation = _elapsed * 0.45f;
		_rootMarker.Scale = Vector2.One * (1f + (MathF.Sin(_elapsed * 0.7f) * 0.06f));
		_armMarker.Rotation = -_elapsed * 1.15f;
		_armMarker.Scale = new Vector2(1.1f + (MathF.Sin(_elapsed * 0.9f) * 0.1f), 1f);
		_tipMarker.Rotation = _elapsed * 2f;
		_pulseNode.Rotation = -_elapsed * 0.25f;
		_pulseNode.Scale = Vector2.One * (1f + (MathF.Sin(_elapsed * 0.6f) * 0.08f));
	}

	protected override void OnDraw() {
		DrawConnections();
		DrawDiagnostics();
	}

	private void DrawConnections() {
		Raylib.DrawLineEx(_rootMarker.GlobalPosition, _armMarker.GlobalPosition, 3f, new Color(70, 120, 200, 255));
		Raylib.DrawLineEx(_armMarker.GlobalPosition, _tipMarker.GlobalPosition, 3f, new Color(96, 186, 96, 255));
		UiText.Draw("CanvasItem helpers", 402f, 44f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("The pulse node uses local-space draw helpers inherited from CanvasItem while the marker chain shows Node2D global transforms.", 402f, 80f, 17f, new Color(176, 190, 212, 255));
	}

	private void DrawDiagnostics() {
		_treeBuilder.Clear();
		using var writer = new StringWriter(_treeBuilder);
		DebugTreeView.Print(_rootMarker, writer, "Transform subtree:");
		var lines = _treeBuilder.ToString()
			.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
			.Take(7)
			.ToArray();

		var y = 470f;
		UiText.Draw("DebugTreeView", 932f, y, 22f, Color.Gold);
		for (var i = 0; i < lines.Length; i++) {
			UiText.Draw(lines[i], 932f, y + 32f + (i * 19f), 15f, new Color(168, 184, 206, 255));
		}
	}

	private sealed class CanvasPulseNode : Node2D {
		private float _elapsed;

		public CanvasPulseNode(string name) : base(name) {
		}

		protected override void OnUpdate(float deltaTime) {
			_elapsed += deltaTime;
		}

		protected override void Draw() {
			var radius = 118f + (MathF.Sin(_elapsed * 1.8f) * 16f);
			DrawCircle(Vector2.Zero, radius, new Color(34, 64, 114, 90));
			DrawCircle(Vector2.Zero, radius * 0.55f, new Color(54, 96, 180, 60));
			DrawPolyline([
				new Vector2(-220f, 0f),
				new Vector2(220f, 0f)
			], new Color(70, 86, 120, 255));
			DrawPolyline([
				new Vector2(0f, -180f),
				new Vector2(0f, 180f)
			], new Color(70, 86, 120, 255));
			DrawPolyline([
				new Vector2(0f, -145f),
				new Vector2(120f, -64f),
				new Vector2(156f, 70f),
				new Vector2(28f, 148f),
				new Vector2(-112f, 98f),
				new Vector2(-146f, -36f)
			], new Color(132, 164, 220, 255), true);
		}
	}

	private sealed class TransformMarkerNode : Node2D {
		private readonly string _label;
		private readonly Color _tint;
		private readonly float _radius;

		public TransformMarkerNode(string name, string label, Color tint, float radius) : base(name) {
			_label = label;
			_tint = tint;
			_radius = radius;
		}

		protected override void Draw() {
			DrawCircle(Vector2.Zero, _radius, _tint);
			Raylib.DrawCircleLinesV(GlobalPosition, _radius, Color.Black);
			var label = $"{_label}  ({GlobalPosition.X:0}, {GlobalPosition.Y:0})";
			var textPosition = new Vector2(GlobalPosition.X + 18f, GlobalPosition.Y - 12f);
			var textSize = UiText.MeasureSize(label, 16f, UiTextStyle.Body);
			Raylib.DrawRectangleRec(
				new Rectangle(
					textPosition.X - 8f,
					textPosition.Y - 4f,
					textSize.X + 16f,
					textSize.Y + 8f),
				new Color(10, 14, 22, 220));
			Raylib.DrawRectangleLinesEx(
				new Rectangle(
					textPosition.X - 8f,
					textPosition.Y - 4f,
					textSize.X + 16f,
					textSize.Y + 8f),
				1f,
				new Color(88, 108, 144, 255));
			UiText.Draw(label, textPosition.X, textPosition.Y, 16f, Color.RayWhite, UiTextStyle.Body);
		}
	}
}

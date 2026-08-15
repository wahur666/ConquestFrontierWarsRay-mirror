using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class ThreeDScene : ShowcaseScene {
	private readonly Camera3D _camera = new(
		new Vector3(9f, 7f, 9f),
		new Vector3(0f, 0.75f, 0f),
		new Vector3(0f, 1f, 0f),
		45f,
		CameraProjection.Perspective);
	private readonly RenderableCubeNode _root = new("WorldRoot", Color.SkyBlue, new Vector3(1.8f, 1.8f, 1.8f));
	private readonly RenderableCubeNode _child = new("ChildNode", Color.Lime, new Vector3(1.1f, 1.1f, 1.1f)) {
		Position = new Vector3(4f, 0f, 0f)
	};
	private readonly RenderableCubeNode _grandChild = new("GrandChildNode", Color.Orange, new Vector3(0.8f, 0.8f, 0.8f)) {
		Position = new Vector3(2.4f, 0f, 0f)
	};
	private float _elapsed;

	public ThreeDScene() : base("ThreeDScene", "3D Nodes") {
		AddChild(_root);
		_root.AddChild(_child);
		_child.AddChild(_grandChild);
	}

	protected override void OnUpdate(float deltaTime) {
		_elapsed += deltaTime;
		_root.Rotation = Quaternion.CreateFromYawPitchRoll(_elapsed * 0.4f, _elapsed * 0.3f, 0f);
		_child.Rotation = Quaternion.CreateFromYawPitchRoll(0f, _elapsed * 1.1f, _elapsed * 0.6f);
		_child.Scale = Vector3.One * (1f + (MathF.Sin(_elapsed * 1.2f) * 0.18f));
		_grandChild.Rotation = Quaternion.CreateFromYawPitchRoll(_elapsed * 2f, 0f, _elapsed * 0.9f);
		_grandChild.Scale = Vector3.One * (0.8f + (MathF.Sin(_elapsed * 1.8f) * 0.12f));
	}

	protected override void OnDraw() {
		Raylib.BeginMode3D(_camera);
		Raylib.DrawGrid(14, 1f);
		DrawNodeRecursive(_root);
		Raylib.EndMode3D();

		UiText.Draw("Node3D hierarchy", 402f, 44f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("The cubes are positioned by Node3D global transforms. Axis lines are derived from the decomposed global rotation to keep transform propagation visible.", 402f, 78f, 17f, new Color(176, 190, 212, 255));
		UiText.Draw($"Root global: {_root.GlobalPosition.X:0.00}, {_root.GlobalPosition.Y:0.00}, {_root.GlobalPosition.Z:0.00}", 402f, 610f, 17f, new Color(188, 200, 218, 255), UiTextStyle.Mono);
		UiText.Draw($"Child global: {_child.GlobalPosition.X:0.00}, {_child.GlobalPosition.Y:0.00}, {_child.GlobalPosition.Z:0.00}", 402f, 634f, 17f, new Color(188, 200, 218, 255), UiTextStyle.Mono);
		UiText.Draw($"Grandchild global: {_grandChild.GlobalPosition.X:0.00}, {_grandChild.GlobalPosition.Y:0.00}, {_grandChild.GlobalPosition.Z:0.00}", 402f, 658f, 17f, new Color(188, 200, 218, 255), UiTextStyle.Mono);
	}

	private static void DrawNodeRecursive(Node node) {
		if (node is RenderableCubeNode cubeNode) {
			cubeNode.DrawWorldGizmo();
		}

		foreach (var child in node.Children) {
			DrawNodeRecursive(child);
		}
	}

	private sealed class RenderableCubeNode : Node3D {
		private readonly Color _tint;
		private readonly Vector3 _boxSize;

		public RenderableCubeNode(string name, Color tint, Vector3 boxSize) : base(name) {
			_tint = tint;
			_boxSize = boxSize;
		}

		public void DrawWorldGizmo() {
			Matrix4x4.Decompose(GlobalTransform, out var scale, out var rotation, out var translation);
			var size = new Vector3(_boxSize.X * scale.X, _boxSize.Y * scale.Y, _boxSize.Z * scale.Z);
			Raylib.DrawCubeV(translation, size, new Color((int)_tint.R, (int)_tint.G, (int)_tint.B, 110));
			Raylib.DrawCubeWiresV(translation, size, _tint);

			var basisX = Vector3.Transform(Vector3.UnitX, rotation) * (1.15f * scale.X);
			var basisY = Vector3.Transform(Vector3.UnitY, rotation) * (1.15f * scale.Y);
			var basisZ = Vector3.Transform(Vector3.UnitZ, rotation) * (1.15f * scale.Z);

			Raylib.DrawLine3D(translation, translation + basisX, Color.Red);
			Raylib.DrawLine3D(translation, translation + basisY, Color.Green);
			Raylib.DrawLine3D(translation, translation + basisZ, Color.Blue);
		}
	}
}

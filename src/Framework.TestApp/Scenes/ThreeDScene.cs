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
		DrawInfoChip($"Root global: {_root.GlobalPosition.X:0.00}, {_root.GlobalPosition.Y:0.00}, {_root.GlobalPosition.Z:0.00}", 402f, 604f);
		DrawInfoChip($"Child global: {_child.GlobalPosition.X:0.00}, {_child.GlobalPosition.Y:0.00}, {_child.GlobalPosition.Z:0.00}", 402f, 632f);
		DrawInfoChip($"Grandchild global: {_grandChild.GlobalPosition.X:0.00}, {_grandChild.GlobalPosition.Y:0.00}, {_grandChild.GlobalPosition.Z:0.00}", 402f, 660f);
	}

	private static void DrawInfoChip(string text, float x, float y) {
		var size = UiText.MeasureSize(text, 17f, UiTextStyle.Body);
		var bounds = new Rectangle(x - 8f, y - 4f, size.X + 16f, size.Y + 8f);
		Raylib.DrawRectangleRec(bounds, new Color(10, 14, 22, 224));
		Raylib.DrawRectangleLinesEx(bounds, 1f, new Color(88, 108, 144, 255));
		UiText.Draw(text, x, y, 17f, new Color(218, 226, 238, 255), UiTextStyle.Body);
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
			var corners = BuildCorners(GlobalTransform, _boxSize);
			DrawSolidCube(corners, new Color((int)_tint.R, (int)_tint.G, (int)_tint.B, 110));
			DrawWireCube(corners, _tint);

			var basisX = Vector3.Transform(Vector3.UnitX, rotation) * (1.15f * scale.X);
			var basisY = Vector3.Transform(Vector3.UnitY, rotation) * (1.15f * scale.Y);
			var basisZ = Vector3.Transform(Vector3.UnitZ, rotation) * (1.15f * scale.Z);

			Raylib.DrawLine3D(translation, translation + basisX, Color.Red);
			Raylib.DrawLine3D(translation, translation + basisY, Color.Green);
			Raylib.DrawLine3D(translation, translation + basisZ, Color.Blue);
		}

		private static Vector3[] BuildCorners(Matrix4x4 world, Vector3 localSize) {
			var half = localSize * 0.5f;
			return [
				Vector3.Transform(new Vector3(-half.X, -half.Y, -half.Z), world),
				Vector3.Transform(new Vector3( half.X, -half.Y, -half.Z), world),
				Vector3.Transform(new Vector3( half.X,  half.Y, -half.Z), world),
				Vector3.Transform(new Vector3(-half.X,  half.Y, -half.Z), world),
				Vector3.Transform(new Vector3(-half.X, -half.Y,  half.Z), world),
				Vector3.Transform(new Vector3( half.X, -half.Y,  half.Z), world),
				Vector3.Transform(new Vector3( half.X,  half.Y,  half.Z), world),
				Vector3.Transform(new Vector3(-half.X,  half.Y,  half.Z), world)
			];
		}

		private static void DrawSolidCube(IReadOnlyList<Vector3> corners, Color fill) {
			DrawQuad(corners, 0, 1, 2, 3, fill);
			DrawQuad(corners, 4, 5, 6, 7, fill);
			DrawQuad(corners, 0, 1, 5, 4, fill);
			DrawQuad(corners, 1, 2, 6, 5, fill);
			DrawQuad(corners, 2, 3, 7, 6, fill);
			DrawQuad(corners, 3, 0, 4, 7, fill);
		}

		private static void DrawWireCube(IReadOnlyList<Vector3> corners, Color color) {
			DrawEdge(corners, 0, 1, color);
			DrawEdge(corners, 1, 2, color);
			DrawEdge(corners, 2, 3, color);
			DrawEdge(corners, 3, 0, color);
			DrawEdge(corners, 4, 5, color);
			DrawEdge(corners, 5, 6, color);
			DrawEdge(corners, 6, 7, color);
			DrawEdge(corners, 7, 4, color);
			DrawEdge(corners, 0, 4, color);
			DrawEdge(corners, 1, 5, color);
			DrawEdge(corners, 2, 6, color);
			DrawEdge(corners, 3, 7, color);
		}

		private static void DrawQuad(IReadOnlyList<Vector3> corners, int a, int b, int c, int d, Color fill) {
			Raylib.DrawTriangle3D(corners[a], corners[b], corners[c], fill);
			Raylib.DrawTriangle3D(corners[a], corners[c], corners[d], fill);
		}

		private static void DrawEdge(IReadOnlyList<Vector3> corners, int startIndex, int endIndex, Color color) {
			Raylib.DrawLine3D(corners[startIndex], corners[endIndex], color);
		}
	}
}

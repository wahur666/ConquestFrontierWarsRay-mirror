using System.Numerics;
using Raylib_cs;

namespace RaySharp;

internal static class LitRenderer {
	private static SceneLighting? sceneLighting;

	internal static void LoadLighting() {
		sceneLighting = SceneLighting.Load();
	}

	internal static void UnloadLighting() {
		sceneLighting?.Unload();
		sceneLighting = null;
	}

	internal static void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color) {
		DrawTriangle(a, b, c, color);
		DrawTriangle(a, c, d, color);
	}

	internal static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color) {
		Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
		if (sceneLighting is null) {
			Raylib.DrawTriangle3D(a, b, c, color);
			return;
		}

		sceneLighting.Begin();
		Rlgl.Begin((int)DrawMode.Triangles);
		EmitLitVertex(a, normal, color);
		EmitLitVertex(b, normal, color);
		EmitLitVertex(c, normal, color);
		Rlgl.End();
		sceneLighting.End();
	}

	private static void EmitLitVertex(Vector3 position, Vector3 normal, Color color) {
		Rlgl.Color4ub(color.R, color.G, color.B, color.A);
		Rlgl.Normal3f(normal.X, normal.Y, normal.Z);
		Rlgl.Vertex3f(position.X, position.Y, position.Z);
	}
}
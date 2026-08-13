using System.Numerics;
using RaySharp.Mesh;
using Raylib_cs;

namespace RaySharp.MeshViewer;

internal sealed class MeshViewerModelRenderer : IDisposable {
	private const int GlOne = 1;
	private const int GlSrcAlpha = 0x0302;
	private const int GlFuncAdd = 0x8006;
	private readonly MeshTextureLighting textureLighting = MeshTextureLighting.Load();

	public bool UseDecodedTextureFlags { get; set; } = true;
	public bool ShowWireframe { get; set; }

	public void Draw(MeshViewerModel model) {
		Color fill = new(146, 155, 166, 255);
		Color wire = new(35, 42, 52, 145);
		foreach ((IReadOnlyList<MeshTriangle> triangles, Matrix4x4 world, Matrix4x4 normal) in RenderBatches(model)) {
			foreach (MeshTriangle triangle in triangles) {
				MeshTextureResource? texture = MeshTextureLoader.Find(model.Textures, triangle.TextureName);
				if (texture is not null && texture.IsValid) {
					DrawTexturedTriangle(world, normal, triangle, texture.Texture, textureLighting, UseDecodedTextureFlags);
				} else {
					if (triangle.UsesAlphaBlend) {
						Raylib.BeginBlendMode(BlendMode.Alpha);
					}

					if (!triangle.WritesDepth) {
						Rlgl.DisableDepthMask();
					}

					LitRenderer.DrawTriangle(
						TransformPosition(world, triangle.A),
						TransformPosition(world, triangle.B),
						TransformPosition(world, triangle.C),
						triangle.TextureName is null ? triangle.Tint : fill);

					if (!triangle.WritesDepth) {
						Rlgl.EnableDepthMask();
					}

					if (triangle.UsesAlphaBlend) {
						Raylib.EndBlendMode();
					}
				}
			}

			foreach (MeshTriangle triangle in triangles) {
				MeshTextureResource? secondDiffuseTexture = MeshTextureLoader.Find(model.Textures, triangle.SecondDiffuseTextureName);
				if (secondDiffuseTexture is not null && secondDiffuseTexture.IsValid) {
					DrawSecondDiffuseTriangle(world, normal, triangle, secondDiffuseTexture.Texture, UseDecodedTextureFlags);
				}
			}

			foreach (MeshTriangle triangle in triangles) {
				MeshTextureResource? emissiveTexture = MeshTextureLoader.Find(model.Textures, triangle.EmissiveTextureName);
				if (emissiveTexture is not null && emissiveTexture.IsValid) {
					DrawEmissiveTriangle(world, normal, triangle, emissiveTexture.Texture, UseDecodedTextureFlags);
				}
			}

			if (ShowWireframe) {
				foreach (MeshTriangle triangle in triangles) {
					Vector3 a = TransformPosition(world, triangle.A);
					Vector3 b = TransformPosition(world, triangle.B);
					Vector3 c = TransformPosition(world, triangle.C);
					Raylib.DrawLine3D(a, b, wire);
					Raylib.DrawLine3D(b, c, wire);
					Raylib.DrawLine3D(c, a, wire);
				}
			}
		}
	}

	public void Dispose() {
		textureLighting.Unload();
	}

	private static void DrawTexturedTriangle(
		Matrix4x4 world,
		Matrix4x4 normalMatrix,
		MeshTriangle triangle,
		Texture2D texture,
		MeshTextureLighting lighting,
		bool useDecodedTextureFlags) {
		if (triangle.UsesAlphaBlend) {
			Raylib.BeginBlendMode(BlendMode.Alpha);
		}

		if (!triangle.WritesDepth) {
			Rlgl.DisableDepthMask();
		}

		lighting.Begin();
		MeshTextureAddress textureAddress = ResolveTextureAddress(triangle.TextureAddress, useDecodedTextureFlags);
		ApplyTextureAddress(texture, textureAddress);
		Rlgl.SetTexture(texture.Id);
		Rlgl.Begin((int)DrawMode.Triangles);
		Rlgl.Color4ub(triangle.Tint.R, triangle.Tint.G, triangle.Tint.B, triangle.Tint.A);
		EmitTexturedVertex(world, normalMatrix, triangle.A, SelectUv(triangle.UvA, triangle.Uv2A, textureAddress), triangle.NormalA);
		EmitTexturedVertex(world, normalMatrix, triangle.B, SelectUv(triangle.UvB, triangle.Uv2B, textureAddress), triangle.NormalB);
		EmitTexturedVertex(world, normalMatrix, triangle.C, SelectUv(triangle.UvC, triangle.Uv2C, textureAddress), triangle.NormalC);
		Rlgl.End();
		Rlgl.SetTexture(0);
		lighting.End();

		if (!triangle.WritesDepth) {
			Rlgl.EnableDepthMask();
		}

		if (triangle.UsesAlphaBlend) {
			Raylib.EndBlendMode();
		}
	}

	private static void DrawSecondDiffuseTriangle(Matrix4x4 world, Matrix4x4 normalMatrix, MeshTriangle triangle, Texture2D texture, bool useDecodedTextureFlags) {
		Raylib.BeginBlendMode(BlendMode.Multiplied);
		Rlgl.DisableDepthMask();
		MeshTextureAddress textureAddress = ResolveTextureAddress(triangle.SecondDiffuseTextureAddress, useDecodedTextureFlags);
		ApplyTextureAddress(texture, textureAddress);
		Rlgl.SetTexture(texture.Id);
		Rlgl.Begin((int)DrawMode.Triangles);
		Rlgl.Color4ub(255, 255, 255, 255);
		EmitTexturedVertex(world, normalMatrix, triangle.A, SelectUv(triangle.UvA, triangle.Uv2A, textureAddress), triangle.NormalA);
		EmitTexturedVertex(world, normalMatrix, triangle.B, SelectUv(triangle.UvB, triangle.Uv2B, textureAddress), triangle.NormalB);
		EmitTexturedVertex(world, normalMatrix, triangle.C, SelectUv(triangle.UvC, triangle.Uv2C, textureAddress), triangle.NormalC);
		Rlgl.End();
		Rlgl.SetTexture(0);
		Rlgl.EnableDepthMask();
		Raylib.EndBlendMode();
	}

	private static void DrawEmissiveTriangle(Matrix4x4 world, Matrix4x4 normalMatrix, MeshTriangle triangle, Texture2D texture, bool useDecodedTextureFlags) {
		Rlgl.SetBlendFactors(GlSrcAlpha, GlOne, GlFuncAdd);
		Raylib.BeginBlendMode(BlendMode.Custom);
		Rlgl.DisableDepthTest();
		Rlgl.DisableDepthMask();
		MeshTextureAddress textureAddress = ResolveTextureAddress(triangle.EmissiveTextureAddress, useDecodedTextureFlags);
		ApplyTextureAddress(texture, textureAddress);
		Rlgl.SetTexture(texture.Id);
		Rlgl.Begin((int)DrawMode.Triangles);
		Rlgl.Color4ub(255, 255, 255, triangle.EmissiveBlend);
		EmitTexturedVertex(world, normalMatrix, triangle.A, SelectUv(triangle.UvA, triangle.Uv2A, textureAddress), triangle.NormalA);
		EmitTexturedVertex(world, normalMatrix, triangle.B, SelectUv(triangle.UvB, triangle.Uv2B, textureAddress), triangle.NormalB);
		EmitTexturedVertex(world, normalMatrix, triangle.C, SelectUv(triangle.UvC, triangle.Uv2C, textureAddress), triangle.NormalC);
		Rlgl.End();
		Rlgl.SetTexture(0);
		Rlgl.EnableDepthMask();
		Rlgl.EnableDepthTest();
		Raylib.EndBlendMode();
	}

	private static void ApplyTextureAddress(Texture2D texture, MeshTextureAddress address) {
		Raylib.SetTextureWrap(texture, ToRaylibTextureWrap(address));
	}

	private static MeshTextureAddress ResolveTextureAddress(MeshTextureAddress address, bool useDecodedTextureFlags) {
		return useDecodedTextureFlags ? address : MeshTextureAddress.Repeat;
	}

	private static Vector2 SelectUv(Vector2 uv0, Vector2 uv1, MeshTextureAddress address) {
		return address.CoordinateSet == 1 ? uv1 : uv0;
	}

	private static TextureWrap ToRaylibTextureWrap(MeshTextureAddress address) {
		if (address.U == 1 || address.V == 1) {
			return TextureWrap.MirrorRepeat;
		}

		if (address.U == 2 || address.V == 2 || address.U == 3 || address.V == 3) {
			return TextureWrap.Clamp;
		}

		return TextureWrap.Repeat;
	}

	private static void EmitTexturedVertex(Matrix4x4 world, Matrix4x4 normalMatrix, Vector3 position, Vector2 uv, Vector3 normal) {
		Vector3 transformedPosition = TransformPosition(world, position);
		Vector3 transformedNormal = TransformNormal(normalMatrix, normal);
		Rlgl.TexCoord2f(uv.X, uv.Y);
		Rlgl.Normal3f(transformedNormal.X, transformedNormal.Y, transformedNormal.Z);
		Rlgl.Vertex3f(transformedPosition.X, transformedPosition.Y, transformedPosition.Z);
	}

	private static IEnumerable<(IReadOnlyList<MeshTriangle> Triangles, Matrix4x4 World, Matrix4x4 Normal)> RenderBatches(MeshViewerModel model) {
		if (model.Geometry.Parts.Count == 0) {
			yield return (model.Geometry.Triangles, model.WorldMatrix, model.NormalMatrix);
			yield break;
		}

		foreach (MeshPartGeometry part in model.Geometry.Parts) {
			Matrix4x4 compoundWorld = model.PartWorldTransforms.GetValueOrDefault(part.Name, part.LocalTransform);
			Matrix4x4 world = compoundWorld * model.WorldMatrix;
			yield return (part.Triangles, world, NormalMatrix(world));
		}
	}

	private static Matrix4x4 NormalMatrix(Matrix4x4 world) {
		return Matrix4x4.Invert(world, out Matrix4x4 inverseWorld)
			? Matrix4x4.Transpose(inverseWorld)
			: Matrix4x4.Identity;
	}

	private static Vector3 TransformPosition(Matrix4x4 world, Vector3 position) {
		return Vector3.Transform(position, world);
	}

	private static Vector3 TransformNormal(Matrix4x4 normalMatrix, Vector3 normal) {
		Vector3 transformed = Vector3.TransformNormal(normal, normalMatrix);
		return transformed == Vector3.Zero ? normal : Vector3.Normalize(transformed);
	}
}

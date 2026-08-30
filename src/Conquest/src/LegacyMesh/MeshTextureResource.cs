using System;
using Raylib_cs;

namespace RaySharp.Mesh;

internal sealed class MeshTextureResource(Texture2D texture, string name) : IDisposable {
	public Texture2D Texture { get; private set; } = texture;
	public string Name { get; } = name;
	public bool IsValid => Texture.Id != 0;

	public void Dispose() {
		if (Texture.Id == 0) {
			return;
		}

		Raylib.UnloadTexture(Texture);
		Texture = default;
	}
}

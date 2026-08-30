using System;
using Raylib_cs;

namespace RaySharp.Particle;

internal sealed class ParticleTextureResource(Texture2D texture, string name, bool isFallback) : IDisposable {
	public Texture2D Texture { get; private set; } = texture;
	public string Name { get; } = name;
	public bool IsFallback { get; } = isFallback;
	public bool IsValid => Texture.Id != 0;

	public void Dispose() {
		if (Texture.Id == 0) {
			return;
		}

		Raylib.UnloadTexture(Texture);
		Texture = default;
	}
}

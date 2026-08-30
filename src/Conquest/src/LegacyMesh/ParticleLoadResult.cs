using System.Collections.Generic;

namespace RaySharp.Particle;

internal sealed class ParticleLoadResult {
	public required ParticleParameters Parameters { get; init; }
	public required IReadOnlyDictionary<string, UtfTextureImage> Textures { get; init; }
}

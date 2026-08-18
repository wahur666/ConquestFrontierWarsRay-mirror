namespace RaySharp.Particle;

internal static class ParticleConstants {
	public const int ColorKeyCount = 32;
	public const int TextureNameLength = 16;
	public const uint RelativeTransform = 1 << 0;
	public const uint RelativeVelocity = 1 << 1;
	public const uint RenderParticleLife = 1 << 3;
	public const uint RenderDither = 1 << 4;
	public const uint RenderFog = 1 << 5;
	public const uint BlendZero = 1;
	public const uint BlendOne = 2;
	public const uint BlendSrcAlpha = 5;
	public const uint BlendInvSrcAlpha = 6;
}

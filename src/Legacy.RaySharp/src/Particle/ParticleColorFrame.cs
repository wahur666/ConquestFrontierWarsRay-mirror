namespace RaySharp.Particle;

internal struct ParticleColorFrame(float r, float g, float b, float a) {
	public float R = r;
	public float G = g;
	public float B = b;
	public float A = a;

	public static ParticleColorFrame White => new(1.0f, 1.0f, 1.0f, 1.0f);
}

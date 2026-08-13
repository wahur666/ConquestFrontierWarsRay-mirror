using System.Numerics;

namespace RaySharp.Particle;

internal sealed class ParticleInstance {
	public Vector3 Position;
	public Vector3 Direction;
	public float Velocity;
	public float Size;
	public float Lifetime;
	public float InitialLifetime;
	public float Age;
}

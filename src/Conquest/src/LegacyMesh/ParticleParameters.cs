using System;
using System.Linq;
using System.Numerics;

namespace RaySharp.Particle;

internal sealed class ParticleParameters {
	public uint PspFlags;
	public ParticleColorFrame[] ColorFrames = CreateWhiteColorFrames();
	public uint ColorKeyFrameBits = 0x80000001;
	public string TextureName = string.Empty;
	public float TextureFps;
	public uint SrcBlend = ParticleConstants.BlendOne;
	public uint DstBlend = ParticleConstants.BlendOne;
	public Vector3 Gravity;
	public Vector3 EmitterDirection = Vector3.One;
	public float EmitterNozzleSize;
	public Vector3 EmitterNozzleDamp;
	public float InitialParticleCount;
	public int MaxParticleCount;
	public float Lifetime;
	public float Frequency;
	public float ParticleLifetime;
	public float ParticlePositionRandomizer;
	public float ParticleVelocity;
	public float ParticleVelocityRandomizer;
	public float ParticleTwistVelocity;
	public float ParticleSize;
	public float ParticleSizeVelocity;
	public float BoundingSphereRadius;

	public static ParticleParameters CreateDefault() {
		return new ParticleParameters();
	}

	public ParticleParameters Clone() {
		return new ParticleParameters {
			PspFlags = PspFlags,
			ColorFrames = ColorFrames.ToArray(),
			ColorKeyFrameBits = ColorKeyFrameBits,
			TextureName = TextureName,
			TextureFps = TextureFps,
			SrcBlend = SrcBlend,
			DstBlend = DstBlend,
			Gravity = Gravity,
			EmitterDirection = EmitterDirection,
			EmitterNozzleSize = EmitterNozzleSize,
			EmitterNozzleDamp = EmitterNozzleDamp,
			InitialParticleCount = InitialParticleCount,
			MaxParticleCount = MaxParticleCount,
			Lifetime = Lifetime,
			Frequency = Frequency,
			ParticleLifetime = ParticleLifetime,
			ParticlePositionRandomizer = ParticlePositionRandomizer,
			ParticleVelocity = ParticleVelocity,
			ParticleVelocityRandomizer = ParticleVelocityRandomizer,
			ParticleTwistVelocity = ParticleTwistVelocity,
			ParticleSize = ParticleSize,
			ParticleSizeVelocity = ParticleSizeVelocity,
			BoundingSphereRadius = BoundingSphereRadius
		};
	}

	private static ParticleColorFrame[] CreateWhiteColorFrames() {
		ParticleColorFrame[] frames = new ParticleColorFrame[ParticleConstants.ColorKeyCount];
		Array.Fill(frames, ParticleColorFrame.White);
		return frames;
	}
}

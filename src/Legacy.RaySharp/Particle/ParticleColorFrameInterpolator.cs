namespace RaySharp.Particle;

internal static class ParticleColorFrameInterpolator
{
	public static int RecalculateInBetweenFrames(ParticleParameters parameters) {
		int[] keyedIndices = GetKeyedIndices(parameters).ToArray();
		if (keyedIndices.Length < 2) {
			return 0;
		}

		int updated = 0;
		for (int keyIndex = 0; keyIndex < keyedIndices.Length - 1; keyIndex++) {
			int start = keyedIndices[keyIndex];
			int end = keyedIndices[keyIndex + 1];
			int span = end - start;
			if (span <= 1) {
				continue;
			}

			ParticleColorFrame startFrame = parameters.ColorFrames[start];
			ParticleColorFrame endFrame = parameters.ColorFrames[end];
			for (int frameIndex = start + 1; frameIndex < end; frameIndex++) {
				float t = (frameIndex - start) / (float)span;
				parameters.ColorFrames[frameIndex] = Lerp(startFrame, endFrame, t);
				updated++;
			}
		}

		return updated;
	}

	public static IEnumerable<int> GetKeyedIndices(ParticleParameters parameters) {
		for (int index = 0; index < ParticleConstants.ColorKeyCount; index++) {
			if ((parameters.ColorKeyFrameBits & (1u << index)) != 0) {
				yield return index;
			}
		}
	}

	private static ParticleColorFrame Lerp(ParticleColorFrame a, ParticleColorFrame b, float t) {
		return new ParticleColorFrame(
			Single.Lerp(a.R, b.R, t),
			Single.Lerp(a.G, b.G, t),
			Single.Lerp(a.B, b.B, t),
			Single.Lerp(a.A, b.A, t));
	}
}

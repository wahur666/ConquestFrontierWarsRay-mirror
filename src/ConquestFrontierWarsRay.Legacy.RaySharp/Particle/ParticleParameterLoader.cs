using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace RaySharp.Particle;

internal static class ParticleParameterLoader {
	public static ParticleLoadResult Load(string path) {
		UtfDocument document = UtfParser.Load(path);
		UtfNode rootNode = FindUtfRootNode(document)
			?? throw new InvalidDataException("This XML does not contain a UTF root directory.");

		if (IsUnifiedParticleRoot(rootNode)) {
			ParticleParameters parameters = ParseUnifiedParticleParameters(rootNode);
			UtfNode? embeddedAssets = FindXmlChild(rootNode, "embeddedAssets");
			IReadOnlyDictionary<string, UtfNode> textureChildren = rootNode.Children.Count > 0
				? rootNode.Children
				: embeddedAssets?.Children ?? new Dictionary<string, UtfNode>();

			return new ParticleLoadResult {
				Parameters = parameters,
				Textures = UtfTextureUtils.LoadTextureImages(textureChildren)
			};
		}

		if (rootNode.Children.Count == 0) {
			throw new InvalidDataException("This XML does not contain a UTF root directory.");
		}

		(ParticleParameters loadedParameters, IReadOnlyDictionary<string, UtfNode> loadedTextureChildren) =
			LoadParticleParameters(rootNode.Children);
		return new ParticleLoadResult {
			Parameters = loadedParameters,
			Textures = UtfTextureUtils.LoadTextureImages(loadedTextureChildren)
		};
	}

	private static (ParticleParameters Parameters, IReadOnlyDictionary<string, UtfNode> TextureChildren) LoadParticleParameters(
		IReadOnlyDictionary<string, UtfNode> children) {
		if (children.TryGetValue("ParticleSystemParameters", out UtfNode? pspNode) && pspNode.Value is not null) {
			return (ParseParticleSystemParameters(pspNode.Value), children);
		}

		if (children.TryGetValue("Particle Event", out UtfNode? eventNode)
			&& eventNode.Children.TryGetValue("particle1.Def", out UtfNode? eventDefNode)
			&& eventDefNode.Value is not null) {
			return (ParseLegacyEventDef(eventDefNode.Value), eventNode.Children);
		}

		throw new InvalidDataException("This XML does not contain ParticleSystemParameters or Particle Event/particle1.Def.");
	}

	private static ParticleParameters ParseParticleSystemParameters(byte[] bytes) {
		int offset = 0;
		ParticleParameters parameters = new();

		parameters.PspFlags = ReadUInt32(bytes, ref offset);
		parameters.ColorFrames = new ParticleColorFrame[ParticleConstants.ColorKeyCount];
		for (int i = 0; i < parameters.ColorFrames.Length; i++) {
			parameters.ColorFrames[i] = new ParticleColorFrame(
				ReadSingle(bytes, offset),
				ReadSingle(bytes, offset + 4),
				ReadSingle(bytes, offset + 8),
				ReadSingle(bytes, offset + 12));
			offset += 16;
		}

		parameters.ColorKeyFrameBits = ReadUInt32(bytes, ref offset);
		parameters.TextureName = ReadCString(bytes, offset, ParticleConstants.TextureNameLength);
		offset += ParticleConstants.TextureNameLength;
		parameters.TextureFps = ReadSingle(bytes, ref offset);
		parameters.SrcBlend = ReadUInt32(bytes, ref offset);
		parameters.DstBlend = ReadUInt32(bytes, ref offset);
		parameters.Gravity = ReadVector3(bytes, ref offset);
		parameters.EmitterDirection = ReadVector3(bytes, ref offset);
		parameters.EmitterNozzleSize = ReadSingle(bytes, ref offset);
		parameters.EmitterNozzleDamp = ReadVector3(bytes, ref offset);
		parameters.InitialParticleCount = ReadInt32(bytes, ref offset);
		parameters.MaxParticleCount = ReadInt32(bytes, ref offset);
		parameters.Lifetime = ReadSingle(bytes, ref offset);
		parameters.Frequency = ReadSingle(bytes, ref offset);
		parameters.ParticleLifetime = ReadSingle(bytes, ref offset);
		parameters.ParticlePositionRandomizer = ReadSingle(bytes, ref offset);
		parameters.ParticleVelocity = ReadSingle(bytes, ref offset);
		parameters.ParticleVelocityRandomizer = ReadSingle(bytes, ref offset);
		parameters.ParticleTwistVelocity = ReadSingle(bytes, ref offset);
		parameters.ParticleSize = ReadSingle(bytes, ref offset);
		parameters.ParticleSizeVelocity = ReadSingle(bytes, ref offset);
		parameters.BoundingSphereRadius = ReadSingle(bytes, ref offset);
		return parameters;
	}

	private static ParticleParameters ParseLegacyEventDef(byte[] bytes) {
		if (bytes.Length < 176) {
			throw new InvalidDataException($"Legacy particle EventDef is too small: {bytes.Length} bytes.");
		}

		ParticleParameters parameters = ParticleParameters.CreateDefault();
		float alpha = ReadSingle(bytes, 0);
		float alphaDecay = ReadSingle(bytes, 4);
		Vector3 color = ReadVector3(bytes, 16);
		Vector3 colorVelocity = ReadVector3(bytes, 40);
		float frequency = ReadSingle(bytes, 52);
		float gravity = ReadSingle(bytes, 56);
		int lifetime = ReadInt32(bytes, 60);
		int maxParticles = ReadInt32(bytes, 64);
		float nozzle = ReadSingle(bytes, 72);
		Vector3 nozzleDamp = ReadVector3(bytes, 76);
		float particleCount = ReadSingle(bytes, 88);
		uint particleLife = ReadUInt32(bytes, 92);
		float randomPosition = ReadSingle(bytes, 100);
		float size = ReadSingle(bytes, 104);
		float sizeVelocity = ReadSingle(bytes, 108);
		string textureName = ReadCString(bytes, 116, ParticleConstants.TextureNameLength);
		float twistSpeed = ReadSingle(bytes, 132);
		float velocity = ReadSingle(bytes, 140);
		float velocityRandom = ReadSingle(bytes, 144);
		byte dither = bytes[148];
		float radius = ReadSingle(bytes, 152);
		Vector3 direction = ReadVector3(bytes, 156);

		if (direction.LengthSquared() <= 0.000001f) {
			direction = Vector3.One;
		}

		parameters.Lifetime = lifetime * 0.001f;
		parameters.Frequency = frequency;
		parameters.InitialParticleCount = particleCount;
		parameters.MaxParticleCount = maxParticles;
		parameters.EmitterDirection = direction;
		parameters.EmitterNozzleSize = nozzle;
		parameters.EmitterNozzleDamp = nozzleDamp;
		parameters.Gravity = new Vector3(0.0f, 0.0f, gravity * 1000.0f);
		parameters.ParticleLifetime = particleLife * 0.001f;
		parameters.ParticlePositionRandomizer = randomPosition;
		parameters.ParticleSize = size;
		parameters.ParticleSizeVelocity = sizeVelocity * 1000.0f;
		parameters.ParticleTwistVelocity = twistSpeed * 1000.0f;
		parameters.ParticleVelocity = velocity * 1000.0f;
		parameters.ParticleVelocityRandomizer = velocityRandom;
		parameters.TextureName = textureName;
		parameters.TextureFps = ReadSingle(bytes, 172);
		parameters.BoundingSphereRadius = radius;

		if (dither != 0) {
			parameters.PspFlags |= ParticleConstants.RenderDither;
		}

		uint legacyFlags = ReadUInt32(bytes, 168);
		if ((legacyFlags & 1) != 0) {
			parameters.PspFlags |= ParticleConstants.RelativeVelocity;
		}

		if ((legacyFlags & 2) != 0) {
			parameters.PspFlags |= ParticleConstants.RelativeTransform;
		}

		if (bytes.Length >= 204) {
			uint srcBlend = ReadUInt32(bytes, 176);
			uint dstBlend = ReadUInt32(bytes, 180);
			Vector3 gravityVector = ReadVector3(bytes, 184);
			int useParticleLifetime = ReadInt32(bytes, 196);
			int fog = ReadInt32(bytes, 200);

			if (srcBlend > 0) {
				parameters.SrcBlend = srcBlend;
			}

			if (dstBlend > 0) {
				parameters.DstBlend = dstBlend;
			}

			parameters.Gravity = new Vector3(
				gravityVector.X * 1000.0f,
				gravityVector.Y * 1000.0f,
				(gravityVector.Z + gravity) * 1000.0f);

			if (useParticleLifetime != 0) {
				parameters.PspFlags |= ParticleConstants.RenderParticleLife;
			}

			if (fog != 0) {
				parameters.PspFlags |= ParticleConstants.RenderFog;
			}
		}

		if (bytes.Length >= 720 && ReadUInt32(bytes, 204) != 0) {
			parameters.ColorKeyFrameBits = ReadUInt32(bytes, 204) | 0x80000001;
			parameters.ColorFrames = new ParticleColorFrame[ParticleConstants.ColorKeyCount];
			for (int i = 0; i < ParticleConstants.ColorKeyCount; i++) {
				int frameOffset = 208 + i * 16;
				parameters.ColorFrames[i] = new ParticleColorFrame(
					ReadSingle(bytes, frameOffset),
					ReadSingle(bytes, frameOffset + 4),
					ReadSingle(bytes, frameOffset + 8),
					ReadSingle(bytes, frameOffset + 12));
			}
		} else if (colorVelocity.LengthSquared() > 0.0f) {
			float durationMs = particleLife != 0 ? particleLife : lifetime != 0 ? lifetime : 1000.0f;
			float frameStepSeconds = durationMs / ParticleConstants.ColorKeyCount * 0.001f;
			parameters.ColorFrames = new ParticleColorFrame[ParticleConstants.ColorKeyCount];
			float r = color.X;
			float g = color.Y;
			float b = color.Z;
			float a = alpha;

			for (int i = 0; i < ParticleConstants.ColorKeyCount; i++) {
				parameters.ColorFrames[i] = new ParticleColorFrame(Clamp01(r), Clamp01(g), Clamp01(b), Clamp01(a));
				r += frameStepSeconds * colorVelocity.X;
				g += frameStepSeconds * colorVelocity.Y;
				b += frameStepSeconds * colorVelocity.Z;
				a += frameStepSeconds * alphaDecay;
			}
		} else {
			parameters.ColorFrames = Enumerable
				.Repeat(new ParticleColorFrame(color.X, color.Y, color.Z, alpha), ParticleConstants.ColorKeyCount)
				.ToArray();
		}

		return parameters;
	}

	private static ParticleParameters ParseUnifiedParticleParameters(UtfNode rootNode) {
		UtfNode parametersNode = FindXmlChild(rootNode, "parameters")
			?? throw new InvalidDataException("Unified particle XML is missing <parameters>.");

		ParticleParameters parameters = ParticleParameters.CreateDefault();
		UtfNode? rendering = FindXmlChild(parametersNode, "rendering");
		UtfNode? emitter = FindXmlChild(parametersNode, "emitter");
		UtfNode? particles = FindXmlChild(parametersNode, "particles");
		UtfNode? colorFrames = FindXmlChild(parametersNode, "colorFrames");

		if (rendering is not null) {
			parameters.TextureName = Attribute(rendering, "textureName", parameters.TextureName);
			parameters.TextureFps = ParseFloatAttribute(rendering, "textureFps", parameters.TextureFps);
			parameters.SrcBlend = (uint)ParseIntAttribute(rendering, "srcBlend", (int)parameters.SrcBlend);
			parameters.DstBlend = (uint)ParseIntAttribute(rendering, "dstBlend", (int)parameters.DstBlend);
			parameters.BoundingSphereRadius = ParseFloatAttribute(rendering, "boundingSphereRadius", parameters.BoundingSphereRadius);
		}

		if (emitter is not null) {
			parameters.InitialParticleCount = ParseFloatAttribute(emitter, "initialParticleCount", parameters.InitialParticleCount);
			parameters.MaxParticleCount = ParseIntAttribute(emitter, "maxParticleCount", parameters.MaxParticleCount);
			parameters.Lifetime = ParseFloatAttribute(emitter, "lifetime", parameters.Lifetime);
			parameters.Frequency = ParseFloatAttribute(emitter, "frequency", parameters.Frequency);
			parameters.EmitterNozzleSize = ParseFloatAttribute(emitter, "nozzleSize", parameters.EmitterNozzleSize);
			parameters.EmitterDirection = ParseUnifiedVector(FindXmlChild(emitter, "direction"), parameters.EmitterDirection);
			parameters.EmitterNozzleDamp = ParseUnifiedVector(FindXmlChild(emitter, "nozzleDamp"), parameters.EmitterNozzleDamp);
		}

		if (particles is not null) {
			parameters.ParticleLifetime = ParseFloatAttribute(particles, "lifetime", parameters.ParticleLifetime);
			parameters.ParticlePositionRandomizer = ParseFloatAttribute(particles, "positionRandomizer", parameters.ParticlePositionRandomizer);
			parameters.ParticleVelocity = ParseFloatAttribute(particles, "velocity", parameters.ParticleVelocity);
			parameters.ParticleVelocityRandomizer = ParseFloatAttribute(particles, "velocityRandomizer", parameters.ParticleVelocityRandomizer);
			parameters.ParticleTwistVelocity = ParseFloatAttribute(particles, "twistVelocity", parameters.ParticleTwistVelocity);
			parameters.ParticleSize = ParseFloatAttribute(particles, "size", parameters.ParticleSize);
			parameters.ParticleSizeVelocity = ParseFloatAttribute(particles, "sizeVelocity", parameters.ParticleSizeVelocity);
			parameters.Gravity = ParseUnifiedVector(FindXmlChild(particles, "gravity"), parameters.Gravity);
		}

		if (colorFrames is not null) {
			parameters.ColorKeyFrameBits = (uint)ParseIntAttribute(colorFrames, "keyFrameBits", unchecked((int)parameters.ColorKeyFrameBits));
			foreach (UtfNode frame in colorFrames.ChildrenList.Where(child => child.TagName == "frame")) {
				int index = ParseIntAttribute(frame, "index", -1);
				if (index < 0 || index >= ParticleConstants.ColorKeyCount) {
					continue;
				}

				ParticleColorFrame fallback = parameters.ColorFrames[index];
				parameters.ColorFrames[index] = new ParticleColorFrame(
					ParseFloatAttribute(frame, "r", fallback.R),
					ParseFloatAttribute(frame, "g", fallback.G),
					ParseFloatAttribute(frame, "b", fallback.B),
					ParseFloatAttribute(frame, "a", fallback.A));
			}
		}

		return parameters;
	}

	private static UtfNode? FindUtfRootNode(UtfDocument document) {
		if (document.RootNode is not null) {
			return document.RootNode;
		}

		return document.Roots.Values.FirstOrDefault(node => node.Children.Count > 0);
	}

	private static bool IsUnifiedParticleRoot(UtfNode rootNode) {
		return rootNode.TagName == "particleEditor"
			&& rootNode.Attributes.TryGetValue("format", out string? format)
			&& format == "cfw-unified-particle";
	}

	private static UtfNode? FindXmlChild(UtfNode? node, string tagName) {
		return node?.ChildrenList.FirstOrDefault(child => child.TagName == tagName);
	}

	private static string Attribute(UtfNode node, string name, string fallback) {
		return node.Attributes.TryGetValue(name, out string? value) ? value : fallback;
	}

	private static float ParseFloatAttribute(UtfNode node, string name, float fallback) {
		return float.TryParse(Attribute(node, name, string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
			? value
			: fallback;
	}

	private static int ParseIntAttribute(UtfNode node, string name, int fallback) {
		string raw = Attribute(node, name, string.Empty);
		if (string.IsNullOrWhiteSpace(raw)) {
			return fallback;
		}

		return raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
			? int.TryParse(raw[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hex) ? hex : fallback
			: int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
	}

	private static Vector3 ParseUnifiedVector(UtfNode? node, Vector3 fallback) {
		return node is null
			? fallback
			: new Vector3(
				ParseFloatAttribute(node, "x", fallback.X),
				ParseFloatAttribute(node, "y", fallback.Y),
				ParseFloatAttribute(node, "z", fallback.Z));
	}

	private static Vector3 ReadVector3(byte[] bytes, ref int offset) {
		Vector3 value = ReadVector3(bytes, offset);
		offset += 12;
		return value;
	}

	private static Vector3 ReadVector3(byte[] bytes, int offset) {
		return new Vector3(ReadSingle(bytes, offset), ReadSingle(bytes, offset + 4), ReadSingle(bytes, offset + 8));
	}

	private static uint ReadUInt32(byte[] bytes, ref int offset) {
		uint value = ReadUInt32(bytes, offset);
		offset += 4;
		return value;
	}

	private static uint ReadUInt32(byte[] bytes, int offset) {
		return BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, sizeof(uint)));
	}

	private static int ReadInt32(byte[] bytes, ref int offset) {
		int value = ReadInt32(bytes, offset);
		offset += 4;
		return value;
	}

	private static int ReadInt32(byte[] bytes, int offset) {
		return BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, sizeof(int)));
	}

	private static float ReadSingle(byte[] bytes, ref int offset) {
		float value = ReadSingle(bytes, offset);
		offset += 4;
		return value;
	}

	private static float ReadSingle(byte[] bytes, int offset) {
		return BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(offset, sizeof(float)));
	}

	private static string ReadCString(byte[] bytes, int offset, int maxLength) {
		int end = offset;
		int limit = Math.Min(bytes.Length, offset + maxLength);
		while (end < limit && bytes[end] != 0) {
			end++;
		}

		return Encoding.ASCII.GetString(bytes, offset, end - offset);
	}

	private static float Clamp01(float value) {
		return Math.Clamp(value, 0.0f, 1.0f);
	}
}

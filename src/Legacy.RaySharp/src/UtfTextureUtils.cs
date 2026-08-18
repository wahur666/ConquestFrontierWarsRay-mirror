using System.Buffers.Binary;

namespace RaySharp;

internal static class UtfTextureUtils {
	public static Dictionary<string, UtfTextureImage> LoadTextureImages(UtfDocument document) {
		return LoadTextureImages(document.RootChildren);
	}

	public static Dictionary<string, UtfTextureImage> LoadTextureImages(IReadOnlyDictionary<string, UtfNode> root) {
		Dictionary<string, UtfTextureImage> textures = LoadEmbeddedImageTextures(root);
		IReadOnlyDictionary<string, UtfNode> textureLibrary =
			GetChild(root, "Texture library")?.Children
			?? GetChild(GetChild(root, "embeddedAssets")?.Children, "Texture library")?.Children
			?? new Dictionary<string, UtfNode>();

		foreach ((string textureName, UtfNode textureNode) in textureLibrary) {
			if (textureNode.Children.Count == 0) {
				continue;
			}

			UtfTextureImage? decoded = DecodeTextureNode(textureName, textureNode);
			if (decoded is not null) {
				textures[textureName] = decoded;
			}
		}

		return textures;
	}

	public static UtfTextureImage? FindTextureImage(
		IReadOnlyDictionary<string, UtfTextureImage> textures,
		string textureName) {
		if (textures.TryGetValue(textureName, out UtfTextureImage? exact)) {
			return exact;
		}

		string baseName = Path.GetFileNameWithoutExtension(textureName).ToLowerInvariant();
		return textures.FirstOrDefault(pair =>
			Path.GetFileNameWithoutExtension(pair.Key).ToLowerInvariant() == baseName).Value;
	}

	public static int? GetInt32(UtfNode? node) {
		return node?.Value is { Length: >= 4 } value
			? BinaryPrimitives.ReadInt32LittleEndian(value.AsSpan(0, sizeof(int)))
			: null;
	}

	public static UtfNode? FindChildCI(UtfNode? node, string name) {
		return GetChild(node?.Children, name);
	}

	public static bool HasNonOpaqueAlpha(byte[] rgba) {
		for (int index = 3; index < rgba.Length; index += 4) {
			if (rgba[index] != 255) {
				return true;
			}
		}

		return false;
	}

	private static Dictionary<string, UtfTextureImage> LoadEmbeddedImageTextures(IReadOnlyDictionary<string, UtfNode> root) {
		UtfNode? imageRoot = GetChild(root, "embeddedImages");
		IEnumerable<UtfNode> imageNodes = imageRoot is null
			? Enumerable.Empty<UtfNode>()
			: imageRoot.ChildrenList.Count > 0
				? imageRoot.ChildrenList
				: imageRoot.Children.Values;
		Dictionary<string, UtfTextureImage> textures = [];

		foreach (UtfNode imageNode in imageNodes) {
			if (imageNode.Attributes.Count == 0) {
				continue;
			}

			UtfTextureImage? decoded = DecodeEmbeddedImageNode(imageNode);
			if (decoded is not null) {
				textures[decoded.Name] = decoded;
			}
		}

		return textures;
	}

	private static UtfTextureImage? DecodeEmbeddedImageNode(UtfNode imageNode) {
		Dictionary<string, string> attributes = new(imageNode.Attributes, StringComparer.OrdinalIgnoreCase);
		string format = attributes.GetValueOrDefault("format", string.Empty).ToLowerInvariant();
		UtfNode? dataNode = FindChildCI(imageNode, "Image BMP")
			?? FindChildCI(imageNode, "BMP")
			?? FindChildCI(imageNode, "Image");

		if (format != "bmp" || dataNode?.Value is null) {
			return null;
		}

		Bmp32Image? decoded = DecodeBmp32(dataNode.Value);
		if (decoded is null) {
			return null;
		}

		int width = ParseInt(attributes.GetValueOrDefault("width")) ?? decoded.Width;
		int height = ParseInt(attributes.GetValueOrDefault("height")) ?? decoded.Height;
		string name = attributes.GetValueOrDefault("name", imageNode.Name);
		bool hasAlpha =
			string.Equals(attributes.GetValueOrDefault("alpha"), "source", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(attributes.GetValueOrDefault("alpha"), "luminance", StringComparison.OrdinalIgnoreCase)
			|| HasNonOpaqueAlpha(decoded.Rgba);

		return new UtfTextureImage(name, width, height, decoded.Rgba, hasAlpha);
	}

	private static Bmp32Image? DecodeBmp32(byte[] bytes) {
		if (bytes.Length < 54 || bytes[0] != 0x42 || bytes[1] != 0x4d) {
			return null;
		}

		uint pixelOffset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(10, sizeof(uint)));
		uint dibSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(14, sizeof(uint)));
		if (dibSize < 40) {
			return null;
		}

		int width = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(18, sizeof(int)));
		int signedHeight = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(22, sizeof(int)));
		int height = Math.Abs(signedHeight);
		ushort bitCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(28, sizeof(ushort)));
		uint compression = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(30, sizeof(uint)));
		if (width <= 0 || height <= 0 || bitCount != 32 || compression != 0) {
			return null;
		}

		bool topDown = signedHeight < 0;
		int rowSize = width * 4;
		byte[] rgba = new byte[width * height * 4];

		for (int y = 0; y < height; y++) {
			int sourceY = topDown ? y : height - 1 - y;
			int rowOffset = checked((int)pixelOffset + sourceY * rowSize);
			for (int x = 0; x < width; x++) {
				int source = rowOffset + x * 4;
				int destination = (y * width + x) * 4;
				if (source + 3 >= bytes.Length) {
					continue;
				}

				rgba[destination] = bytes[source + 2];
				rgba[destination + 1] = bytes[source + 1];
				rgba[destination + 2] = bytes[source];
				rgba[destination + 3] = bytes[source + 3];
			}
		}

		return new Bmp32Image(width, height, rgba);
	}

	private static UtfTextureImage? DecodeTextureNode(string textureName, UtfNode textureNode) {
		UtfNode? mipNode = FindMip0Node(textureNode);
		UtfNode? formatNode = FindTextureFormatNode(textureNode) ?? FindTextureFormatNode(mipNode);
		if (formatNode is null) {
			return null;
		}

		int? width = GetInt32(FindChildCI(textureNode, "Image X size")) ?? GetInt32(FindChildCI(mipNode, "Image X size"));
		int? height = GetInt32(FindChildCI(textureNode, "Image Y size")) ?? GetInt32(FindChildCI(mipNode, "Image Y size"));
		if (width is null or <= 0 || height is null or <= 0) {
			return null;
		}

		UtfNode dataNode = FindChildCI(formatNode, "MIP0") ?? formatNode;
		byte[]? palette = FindChildCI(formatNode, "Palette RGB 888")?.Value ?? FindChildCI(dataNode, "Palette RGB 888")?.Value;
		byte[]? indices = FindChildCI(dataNode, "Image indices")?.Value;
		byte[]? colors = FindChildCI(dataNode, "Image colors")?.Value;
		byte[]? alpha = FindChildCI(dataNode, "Alpha 8 bit")?.Value ?? FindChildCI(dataNode, "Image Alpha 8 bit")?.Value;
		string formatName = formatNode.Name;
		byte[]? rgba = null;

		if (IsIndexedTextureFormat(formatName) && palette is not null && indices is not null) {
			rgba = Palette8ToRgba(indices, palette, width.Value, height.Value, alpha);
		} else if (string.Equals(formatName, "true rgb 565", StringComparison.OrdinalIgnoreCase) && colors is not null) {
			rgba = Rgb565WithAlphaToRgba(colors, alpha, width.Value, height.Value);
		} else if (string.Equals(formatName, "true 8 bit", StringComparison.OrdinalIgnoreCase) && colors is not null) {
			rgba = True8ToRgba(colors, alpha, width.Value, height.Value);
		} else if (formatName.StartsWith("Format_TRUE_", StringComparison.Ordinal) && colors is not null) {
			rgba = FormatTrueToRgba(formatName, colors, alpha, width.Value, height.Value);
		} else if (formatName.StartsWith("Format_PAL8", StringComparison.Ordinal) && palette is not null && indices is not null) {
			rgba = Palette8ToRgba(indices, palette, width.Value, height.Value, alpha);
		}

		return rgba is null
			? null
			: new UtfTextureImage(textureName, width.Value, height.Value, rgba, alpha is not null || HasNonOpaqueAlpha(rgba));
	}

	private static UtfNode? FindMip0Node(UtfNode? node) {
		UtfNode? direct = FindChildCI(node, "MIP0");
		if (direct is not null) {
			return direct;
		}

		if (node is null) {
			return null;
		}

		foreach (UtfNode child in node.Children.Values) {
			UtfNode? nested = FindChildCI(child, "MIP0");
			if (nested is not null) {
				return nested;
			}
		}

		return null;
	}

	private static UtfNode? FindTextureFormatNode(UtfNode? node) {
		return node?.Children.Values.FirstOrDefault(child => {
			string lower = child.Name.ToLowerInvariant();
			return lower is "palette 8 bit" or "true rgb 565" or "true 8 bit"
				|| child.Name.StartsWith("Format_", StringComparison.Ordinal);
		});
	}

	private static byte[] Palette8ToRgba(byte[] indices, byte[] palette, int width, int height, byte[]? alpha = null) {
		int pixelCount = Math.Min(indices.Length, width * height);
		byte[] output = new byte[width * height * 4];

		for (int i = 0; i < pixelCount; i++) {
			int paletteIndex = indices[i] * 3;
			output[i * 4] = paletteIndex < palette.Length ? palette[paletteIndex] : (byte)0;
			output[i * 4 + 1] = paletteIndex + 1 < palette.Length ? palette[paletteIndex + 1] : (byte)0;
			output[i * 4 + 2] = paletteIndex + 2 < palette.Length ? palette[paletteIndex + 2] : (byte)0;
			output[i * 4 + 3] = alpha is not null && i < alpha.Length ? alpha[i] : (byte)255;
		}

		return output;
	}

	private static byte[] Rgb565WithAlphaToRgba(byte[] colors, byte[]? alpha, int width, int height) {
		int pixelCount = Math.Min(colors.Length / 2, width * height);
		byte[] output = new byte[width * height * 4];

		for (int i = 0; i < pixelCount; i++) {
			ushort value = BinaryPrimitives.ReadUInt16LittleEndian(colors.AsSpan(i * 2, sizeof(ushort)));
			output[i * 4] = ExpandBits((value >> 11) & 0x1f, 5);
			output[i * 4 + 1] = ExpandBits((value >> 5) & 0x3f, 6);
			output[i * 4 + 2] = ExpandBits(value & 0x1f, 5);
			output[i * 4 + 3] = alpha is not null && i < alpha.Length ? alpha[i] : (byte)255;
		}

		return output;
	}

	private static byte[] True8ToRgba(byte[] colors, byte[]? alpha, int width, int height) {
		int pixelCount = Math.Min(colors.Length, width * height);
		byte[] output = new byte[width * height * 4];

		for (int i = 0; i < pixelCount; i++) {
			byte value = colors[i];
			output[i * 4] = value;
			output[i * 4 + 1] = value;
			output[i * 4 + 2] = value;
			output[i * 4 + 3] = alpha is not null && i < alpha.Length ? alpha[i] : (byte)255;
		}

		return output;
	}

	private static byte[]? FormatTrueToRgba(string formatName, byte[] colors, byte[]? alpha, int width, int height) {
		int[]? bits = ParseFormatTrueBits(formatName);
		if (bits is null) {
			return null;
		}

		int rBits = bits[0];
		int gBits = bits[1];
		int bBits = bits[2];
		int aBits = bits[3];
		int bitsPerPixel = rBits + gBits + bBits + aBits;
		int bytesPerPixel = (int)Math.Ceiling(bitsPerPixel / 8.0);
		if (bytesPerPixel <= 0 || bytesPerPixel > 8) {
			return null;
		}

		int pixelCount = Math.Min(colors.Length / bytesPerPixel, width * height);
		byte[] output = new byte[width * height * 4];

		for (int i = 0; i < pixelCount; i++) {
			ulong value = 0;
			int offset = i * bytesPerPixel;
			for (int byteIndex = 0; byteIndex < bytesPerPixel; byteIndex++) {
				value |= (ulong)colors[offset + byteIndex] << (byteIndex * 8);
			}

			ulong bMask = Mask(bBits);
			ulong gMask = Mask(gBits);
			ulong rMask = Mask(rBits);
			ulong aMask = Mask(aBits);
			ulong b = bBits > 0 ? value & bMask : 0;
			ulong g = gBits > 0 ? (value >> bBits) & gMask : 0;
			ulong r = rBits > 0 ? (value >> (bBits + gBits)) & rMask : 0;
			ulong embeddedAlpha = aBits > 0 ? (value >> (bBits + gBits + rBits)) & aMask : 0;

			output[i * 4] = ExpandBits(r, rBits);
			output[i * 4 + 1] = ExpandBits(g, gBits);
			output[i * 4 + 2] = ExpandBits(b, bBits);
			output[i * 4 + 3] = alpha is not null && i < alpha.Length
				? alpha[i]
				: aBits == 0
					? (byte)255
					: ExpandBits(embeddedAlpha, aBits);
		}

		return output;
	}

	private static int[]? ParseFormatTrueBits(string formatName) {
		string[] parts = formatName
			.Replace("Format_TRUE_", string.Empty, StringComparison.Ordinal)
			.Split('_', StringSplitOptions.RemoveEmptyEntries);

		if (parts.Length == 0 || !int.TryParse(parts[0], out int componentCount) || componentCount <= 0) {
			return null;
		}

		int[] sizes = parts
			.Skip(1)
			.Take(componentCount)
			.Select(part => int.TryParse(part, out int size) ? size : -1)
			.ToArray();

		if (sizes.Length < componentCount || sizes.Any(size => size < 0)) {
			return null;
		}

		if (componentCount == 2) {
			return [sizes[0], 0, 0, sizes[1]];
		}

		return [
			sizes.ElementAtOrDefault(0),
			sizes.ElementAtOrDefault(1),
			sizes.ElementAtOrDefault(2),
			sizes.ElementAtOrDefault(3)
		];
	}

	private static bool IsIndexedTextureFormat(string formatName) {
		return string.Equals(formatName, "palette 8 bit", StringComparison.OrdinalIgnoreCase)
			|| formatName.StartsWith("Format_PAL8", StringComparison.Ordinal);
	}

	private static byte ExpandBits(int value, int bits) {
		return ExpandBits((ulong)value, bits);
	}

	private static byte ExpandBits(ulong value, int bits) {
		return bits <= 0 ? (byte)0 : (byte)Math.Round(value * 255.0 / ((1UL << bits) - 1));
	}

	private static ulong Mask(int bits) {
		return bits <= 0 ? 0 : (1UL << bits) - 1;
	}

	private static UtfNode? GetChild(IReadOnlyDictionary<string, UtfNode>? children, string name) {
		return children?.Values.FirstOrDefault(child => string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase));
	}

	private static int? ParseInt(string? value) {
		return int.TryParse(value, out int parsed) ? parsed : null;
	}

	private sealed record Bmp32Image(int Width, int Height, byte[] Rgba);
}

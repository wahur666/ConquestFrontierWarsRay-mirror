using System.Buffers.Binary;
using System.Text;
using System.Xml.Linq;

namespace RaySharp;

internal static class UtfParser {
	private const int ExpectedSignature = 0x20465455;
	private const int ExpectedVersion = 0x101;

	public static UtfDocument Load(string path) {
		return Parse(File.ReadAllBytes(path));
	}

	public static UtfDocument Parse(byte[] bytes) {
		string firstText = Encoding.ASCII.GetString(bytes, 0, Math.Min(64, bytes.Length)).TrimStart();
		return firstText.StartsWith('<')
			? ParseXmlString(Encoding.UTF8.GetString(bytes))
			: ParseBinary(bytes);
	}

	public static UtfDocument ParseBinary(byte[] bytes) {
		int position = 0;
		int signature = ReadInt32(bytes, ref position);
		int version = ReadInt32(bytes, ref position);

		if (signature != ExpectedSignature || version != ExpectedVersion) {
			throw new InvalidDataException("Unsupported UTF file. Expected signature 0x20465455 and version 0x101.");
		}

		int nodeBlockOffset = ReadInt32(bytes, ref position);
		position += 4;
		position += 8;

		int stringBlockOffset = ReadInt32(bytes, ref position);
		position += 4;
		position += 4;

		int dataBlockOffset = ReadInt32(bytes, ref position);

		Dictionary<string, UtfNode> root = [];
		ParseNode(bytes, nodeBlockOffset, 0, stringBlockOffset, dataBlockOffset, root);
		return new UtfDocument(root);
	}

	public static UtfDocument ParseXmlString(string xmlText) {
		XDocument document = XDocument.Parse(xmlText, LoadOptions.PreserveWhitespace);
		if (document.Root is null) {
			throw new InvalidDataException("Unable to parse UTF XML.");
		}

		UtfNode rootNode = ElementToNode(document.Root);
		return new UtfDocument(new Dictionary<string, UtfNode> { ["\\"] = rootNode });
	}

	private static void ParseNode(
		byte[] bytes,
		int nodeBlockStart,
		int nodeStart,
		int stringBlockOffset,
		int dataBlockOffset,
		Dictionary<string, UtfNode> parent) {
		int offset = nodeBlockStart + nodeStart;

		while (true) {
			int next = ReadInt32(bytes, ref offset);
			int nameOffset = ReadInt32(bytes, ref offset);
			int attributesFlags = ReadInt32(bytes, ref offset);
			_ = ReadInt32(bytes, ref offset);
			int valueOffset = ReadInt32(bytes, ref offset);
			_ = ReadInt32(bytes, ref offset);
			int spaceUsed = ReadInt32(bytes, ref offset);
			_ = ReadInt32(bytes, ref offset);

			offset += 12;

			string name = ReadCString(bytes, stringBlockOffset + nameOffset);
			bool isLeaf = (attributesFlags & 0x80) != 0;
			byte[]? value = null;

			if (isLeaf && spaceUsed > 0) {
				int start = dataBlockOffset + valueOffset;
				value = bytes[start..Math.Min(start + spaceUsed, bytes.Length)];
			}

			UtfNode node = new() {
				Name = name,
				AttributesFlags = attributesFlags,
				Value = value
			};

			parent[name] = node;

			if (!isLeaf && valueOffset > 0) {
				ParseNode(bytes, nodeBlockStart, valueOffset, stringBlockOffset, dataBlockOffset, node.Children);
				node.ChildrenList.AddRange(node.Children.Values);
			}

			if (next == 0) {
				break;
			}

			offset = nodeBlockStart + next;
		}
	}

	private static UtfNode ElementToNode(XElement element) {
		string name = (string?)element.Attribute("name") ?? element.Name.LocalName;
		bool isFile = string.Equals(element.Name.LocalName, "file", StringComparison.Ordinal);
		Dictionary<string, string> attributes = element
			.Attributes()
			.ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value);

		UtfNode node = new() {
			Name = name,
			TagName = element.Name.LocalName,
			Attributes = attributes,
			AttributesFlags = isFile ? 0x80 : 0,
			Value = isFile ? Convert.FromBase64String(RemoveWhitespace(element.Value)) : null
		};

		if (!isFile) {
			foreach (XElement child in element.Elements()) {
				UtfNode childNode = ElementToNode(child);
				node.Children[childNode.Name] = childNode;
				node.ChildrenList.Add(childNode);
			}
		}

		return node;
	}

	private static int ReadInt32(byte[] bytes, ref int position) {
		int value = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(position, sizeof(int)));
		position += sizeof(int);
		return value;
	}

	private static string ReadCString(byte[] bytes, int start) {
		int end = start;
		while (end < bytes.Length && bytes[end] != 0) {
			end++;
		}

		return Encoding.ASCII.GetString(bytes, start, end - start);
	}

	private static string RemoveWhitespace(string value) {
		StringBuilder builder = new(value.Length);
		foreach (char character in value) {
			if (!char.IsWhiteSpace(character)) {
				builder.Append(character);
			}
		}

		return builder.ToString();
	}
}

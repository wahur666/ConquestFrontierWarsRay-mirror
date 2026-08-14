using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models.MT;

namespace ConquestFrontierWarsRay.Data.StringPack;

public sealed record StringPackField(
	int Index,
	int Offset,
	string Label,
	string Value,
	string XmlValue,
	string Hint);

public sealed record StringPackEntryDetails(
	string TypeName,
	string FileName,
	int ByteLength,
	IReadOnlyList<StringPackField> Fields);

public sealed record StringPackFileEntry(string FileName, int ByteLength) {
	public override string ToString() {
		return $"{FileName} ({ByteLength} bytes)";
	}
}

public abstract class StringPackSchema {
	protected StringPackSchema(string typeName, string xmlElementName, string xmlChildName, int fixedFieldSize,
		int? fixedFieldCount) {
		TypeName = typeName;
		XmlElementName = xmlElementName;
		XmlChildName = xmlChildName;
		FixedFieldSize = fixedFieldSize;
		FixedFieldCount = fixedFieldCount;
	}

	public string TypeName { get; }

	public string XmlElementName { get; }

	public string XmlChildName { get; }

	public int FixedFieldSize { get; }

	public int? FixedFieldCount { get; }

	public StringPackEntryDetails ParseDetails(string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) {
		var fields = ParseValues(fileName, rawData);
		var xmlFields = BuildXmlFieldMap(xmlDocument);
		var details = new List<StringPackField>(fields.Count);

		for (var index = 0; index < fields.Count; index++) {
			xmlFields.TryGetValue(index, out var xmlField);

			details.Add(new StringPackField(
				index,
				index * FixedFieldSize,
				BuildLabel(index),
				fields[index],
				xmlField?.Value ?? string.Empty,
				xmlField?.Hint ?? string.Empty));
		}

		return new StringPackEntryDetails(TypeName, fileName, rawData.Length, details);
	}

	protected IReadOnlyList<string> ParseValues(string fileName, ReadOnlySpan<byte> rawData) {
		if (rawData.Length % FixedFieldSize != 0) {
			throw new InvalidDataException(
				$"Entry '{TypeName}/{fileName}' has {rawData.Length} bytes, which is not divisible by {FixedFieldSize}.");
		}

		var fieldCount = rawData.Length / FixedFieldSize;
		if (FixedFieldCount is int expectedCount && fieldCount != expectedCount) {
			throw new InvalidDataException(
				$"Entry '{TypeName}/{fileName}' expected {expectedCount} fields but found {fieldCount}.");
		}

		var fields = new List<string>(fieldCount);
		for (var index = 0; index < fieldCount; index++) {
			fields.Add(DecodeAscii(rawData.Slice(index * FixedFieldSize, FixedFieldSize)));
		}

		return fields;
	}

	private string BuildLabel(int index) {
		return XmlChildName switch {
			"field" => $"field_{index:00}",
			_ => $"element[{index}]"
		};
	}

	private Dictionary<int, XmlFieldData> BuildXmlFieldMap(XDocument? xmlDocument) {
		var result = new Dictionary<int, XmlFieldData>();
		if (xmlDocument?.Root is null) {
			return result;
		}

		foreach (var element in xmlDocument.Root.Elements(XmlChildName)) {
			var indexAttribute = element.Attribute("index")?.Value;
			if (!int.TryParse(indexAttribute, out var index)) {
				continue;
			}

			var hintParts = element.Attributes()
				.Where(attribute => attribute.Name.LocalName is not "index" and not "type")
				.Select(attribute => $"{attribute.Name.LocalName}={attribute.Value}")
				.ToArray();

			result[index] = new XmlFieldData(
				element.Value.Trim(),
				string.Join("; ", hintParts));
		}

		return result;
	}

	protected static string DecodeAscii(ReadOnlySpan<byte> data) {
		var terminator = data.IndexOf((byte)0);
		if (terminator >= 0) {
			data = data[..terminator];
		}

		return System.Text.Encoding.ASCII.GetString(data).TrimEnd();
	}

	private sealed record XmlFieldData(string Value, string Hint);
}

public sealed class MT_STRINGPACKSchema : StringPackSchema {
	public MT_STRINGPACKSchema()
		: base("MT_STRINGPACK", "MT_STRINGPACK", "element", 32, null) {
	}

	public MT_STRINGPACK Parse(string fileName, ReadOnlySpan<byte> rawData) {
		return new MT_STRINGPACK(ParseValues(fileName, rawData));
	}
}

public sealed class MT_UNITSPEECHSchema : StringPackSchema {
	public MT_UNITSPEECHSchema()
		: base("MT_UNITSPEECH", "MT_UNITSPEECH", "field", 32, 22) {
	}

	public MT_UNITSPEECH Parse(string fileName, ReadOnlySpan<byte> rawData) {
		return new MT_UNITSPEECH(ParseValues(fileName, rawData));
	}
}

public static class StringPackSchemas {
	public static readonly MT_STRINGPACKSchema MT_STRINGPACK = new();
	public static readonly MT_UNITSPEECHSchema MT_UNITSPEECH = new();

	public static readonly IReadOnlyDictionary<string, StringPackSchema> All =
		new Dictionary<string, StringPackSchema>(StringComparer.OrdinalIgnoreCase) {
			[MT_STRINGPACK.TypeName] = MT_STRINGPACK,
			[MT_UNITSPEECH.TypeName] = MT_UNITSPEECH
		};
}

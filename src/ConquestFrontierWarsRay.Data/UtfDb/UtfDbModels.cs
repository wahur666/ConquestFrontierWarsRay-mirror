using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.DosFile;

namespace ConquestFrontierWarsRay.Data.UtfDb;

public sealed record UtfDbDatabaseSpec(string Name, string DatabasePath, string? XmlRootPath);

public sealed record UtfDbFileEntry(string FileName, int ByteLength) {
	public override string ToString() {
		return $"{FileName} ({ByteLength} bytes)";
	}
}

public sealed record UtfDbField(
	int Index,
	int Offset,
	string Label,
	string Value,
	string EnumValue,
	string XmlValue,
	string Hint);

public sealed record UtfDbEntryDetails(
	string DatabaseName,
	string TypeName,
	string FileName,
	int ByteLength,
	IReadOnlyList<UtfDbField> Fields,
	object? TypedValue = null);

public interface IUtfDbTypeParser {
	string TypeName { get; }

	UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument);
}

internal sealed class RepeatedAscii32TypeParser : IUtfDbTypeParser {
	private readonly string _typeName;
	private readonly string _xmlChildName;
	private readonly string _fieldLabelPrefix;

	public RepeatedAscii32TypeParser(string typeName, string xmlChildName, string fieldLabelPrefix) {
		_typeName = typeName;
		_xmlChildName = xmlChildName;
		_fieldLabelPrefix = fieldLabelPrefix;
	}

	public string TypeName => _typeName;

	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData,
		XDocument? xmlDocument) {
		if (rawData.Length % 32 != 0) {
			throw new InvalidDataException(
				$"Entry '{_typeName}/{fileName}' has invalid string-pack size {rawData.Length}.");
		}

		var xmlFields = FixedLayoutTypeParser.BuildXmlFieldMap(xmlDocument, _xmlChildName);
		var fields = new List<UtfDbField>(rawData.Length / 32);

		for (var index = 0; index < rawData.Length / 32; index++) {
			var offset = index * 32;
			var value = FixedLayoutTypeParser.DecodeAscii(rawData.Slice(offset, 32));
			var xmlValue = string.Empty;
			var hint = string.Empty;
			if (xmlFields.TryGetValue(index, out var xml)) {
				xmlValue = xml.Value;
				hint = xml.Hint;
			}

			fields.Add(new UtfDbField(index, offset, $"{_fieldLabelPrefix}_{index:00}", value, string.Empty, xmlValue,
				hint));
		}

		return new UtfDbEntryDetails(databaseName, _typeName, fileName, rawData.Length, fields);
	}
}

internal enum ScalarKind {
	U1,
	S1,
	U2,
	S2,
	U4,
	S4,
	F4,
	Ascii32,
	Ascii64,
	Utf16_64
}

internal sealed record ParsedFieldValue(int Offset, string Label, string Value);

internal abstract record LayoutSpec(string Label);

internal sealed record ScalarSpec(string Name, ScalarKind Kind) : LayoutSpec(Name);

internal sealed record PaddingSpec(string Name, int Size) : LayoutSpec(Name);

internal sealed record GroupSpec(string Name, IReadOnlyList<LayoutSpec> Children) : LayoutSpec(Name);

internal sealed record RepeatSpec(LayoutSpec Item, int Count) : LayoutSpec(Item.Label);

internal class FixedLayoutTypeParser : IUtfDbTypeParser {
	private readonly string _typeName;
	private readonly IReadOnlyList<LayoutSpec> _layout;
	private readonly string _xmlChildName;

	public FixedLayoutTypeParser(string typeName, string xmlChildName, IReadOnlyList<LayoutSpec> layout) {
		_typeName = typeName;
		_xmlChildName = xmlChildName;
		_layout = layout;
	}

	public string TypeName => _typeName;

	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData,
		XDocument? xmlDocument) {
		var values = new List<ParsedFieldValue>();
		var offset = 0;
		ParseLayout(_layout, rawData, values, ref offset, null);

		var xmlFields = BuildXmlFieldMap(xmlDocument, _xmlChildName);
		var structuredXml = BuildStructuredXmlFieldMap(xmlDocument);
		var fields = values
			.Select((value, index) => {
				var xmlValue = string.Empty;
				var hint = string.Empty;
				if (xmlFields.TryGetValue(index, out var xml)) {
					xmlValue = xml.Value;
					hint = xml.Hint;
				} else if (structuredXml.TryResolve(value.Label, out xml)) {
					xmlValue = xml.Value;
					hint = xml.Hint;
				}

				return new UtfDbField(
					index,
					value.Offset,
					value.Label,
					value.Value,
					string.Empty,
					xmlValue,
					hint);
			})
			.ToArray();

		return new UtfDbEntryDetails(databaseName, _typeName, fileName, rawData.Length, fields);
	}

	private static void ParseLayout(
		IReadOnlyList<LayoutSpec> layout,
		ReadOnlySpan<byte> data,
		List<ParsedFieldValue> values,
		ref int offset,
		string? prefix) {
		foreach (var spec in layout) {
			switch (spec) {
				case PaddingSpec padding:
					offset += padding.Size;
					break;
				case ScalarSpec scalar:
					values.Add(new ParsedFieldValue(offset, Join(prefix, scalar.Name),
						ReadScalar(data, ref offset, scalar.Kind)));
					break;
				case GroupSpec group:
					ParseLayout(group.Children, data, values, ref offset, Join(prefix, group.Name));
					break;
				case RepeatSpec repeat:
					for (var index = 0; index < repeat.Count; index++) {
						ParseRepeatedLayout(repeat.Item, data, values, ref offset, prefix, index);
					}
					break;
				default:
					throw new InvalidOperationException($"Unsupported layout spec '{spec.GetType().Name}'.");
			}
		}
	}

	private static void ParseRepeatedLayout(
		LayoutSpec spec,
		ReadOnlySpan<byte> data,
		List<ParsedFieldValue> values,
		ref int offset,
		string? prefix,
		int index) {
		switch (spec) {
			case PaddingSpec padding:
				offset += padding.Size;
				break;
			case ScalarSpec scalar:
				values.Add(new ParsedFieldValue(offset, Join(prefix, $"{scalar.Name}[{index}]"),
					ReadScalar(data, ref offset, scalar.Kind)));
				break;
			case GroupSpec group:
				ParseLayout(group.Children, data, values, ref offset, Join(prefix, $"{group.Name}[{index}]"));
				break;
			case RepeatSpec nestedRepeat:
				for (var nestedIndex = 0; nestedIndex < nestedRepeat.Count; nestedIndex++) {
					ParseRepeatedLayout(nestedRepeat.Item, data, values, ref offset,
						Join(prefix, $"{nestedRepeat.Label}[{index}]"), nestedIndex);
				}
				break;
			default:
				throw new InvalidOperationException($"Unsupported repeated layout spec '{spec.GetType().Name}'.");
		}
	}

	private static string ReadScalar(ReadOnlySpan<byte> data, ref int offset, ScalarKind kind) {
		var result = kind switch {
			ScalarKind.U1 => data[offset].ToString(),
			ScalarKind.S1 => unchecked((sbyte)data[offset]).ToString(),
			ScalarKind.U2 => BitConverter.ToUInt16(data.Slice(offset, 2)).ToString(),
			ScalarKind.S2 => BitConverter.ToInt16(data.Slice(offset, 2)).ToString(),
			ScalarKind.U4 => BitConverter.ToUInt32(data.Slice(offset, 4)).ToString(),
			ScalarKind.S4 => BitConverter.ToInt32(data.Slice(offset, 4)).ToString(),
			ScalarKind.F4 => BitConverter.ToSingle(data.Slice(offset, 4)).ToString("G9"),
			ScalarKind.Ascii32 => DecodeAscii(data.Slice(offset, 32)),
			ScalarKind.Ascii64 => DecodeAscii(data.Slice(offset, 64)),
			ScalarKind.Utf16_64 => DecodeUtf16(data.Slice(offset, 64)),
			_ => throw new InvalidOperationException($"Unsupported scalar kind '{kind}'.")
		};

		offset += kind switch {
			ScalarKind.U1 or ScalarKind.S1 => 1,
			ScalarKind.U2 or ScalarKind.S2 => 2,
			ScalarKind.U4 or ScalarKind.S4 or ScalarKind.F4 => 4,
			ScalarKind.Ascii32 => 32,
			ScalarKind.Ascii64 => 64,
			ScalarKind.Utf16_64 => 64,
			_ => 0
		};
		return result;
	}

	private static string Join(string? prefix, string label) {
		return string.IsNullOrWhiteSpace(prefix) ? label : $"{prefix}.{label}";
	}

	internal static string DecodeAscii(ReadOnlySpan<byte> data) {
		var terminator = data.IndexOf((byte)0);
		if (terminator >= 0) {
			data = data[..terminator];
		}

		return System.Text.Encoding.ASCII.GetString(data).TrimEnd();
	}

	internal static string DecodeUtf16(ReadOnlySpan<byte> data) {
		var terminator = -1;
		for (var index = 0; index < data.Length - 1; index += 2) {
			if (data[index] == 0 && data[index + 1] == 0) {
				terminator = index;
				break;
			}
		}

		if (terminator >= 0) {
			data = data[..terminator];
		}

		return System.Text.Encoding.Unicode.GetString(data).TrimEnd();
	}

	internal static Dictionary<int, (string Value, string Hint)> BuildXmlFieldMap(XDocument? xmlDocument,
		string childName) {
		var result = new Dictionary<int, (string Value, string Hint)>();
		if (xmlDocument?.Root is null) {
			return result;
		}

		foreach (var element in xmlDocument.Root.Elements(childName)) {
			if (!int.TryParse(element.Attribute("index")?.Value, out var index)) {
				continue;
			}

			var hint = string.Join("; ",
				element.Attributes()
					.Where(attribute => attribute.Name.LocalName is not "index" and not "type")
					.Select(attribute => $"{attribute.Name.LocalName}={attribute.Value}"));

			result[index] = (element.Value.Trim(), hint);
		}

		return result;
	}

	private static StructuredXmlFieldMap BuildStructuredXmlFieldMap(XDocument? xmlDocument) {
		var map = new StructuredXmlFieldMap();
		if (xmlDocument?.Root is null) {
			return map;
		}

		foreach (var child in xmlDocument.Root.Elements()) {
			CollectStructuredXmlFields(child, child.Name.LocalName, map);
		}

		return map;
	}

	private static void CollectStructuredXmlFields(XElement element, string path, StructuredXmlFieldMap map) {
		if (!element.Elements().Any()) {
			var hintParts = element.Attributes()
				.Where(attribute => attribute.Name.LocalName is not "type")
				.Select(attribute => $"{attribute.Name.LocalName}={attribute.Value}")
				.ToArray();

			map.Add(path, element.Name.LocalName, element.Value.Trim(), string.Join("; ", hintParts));
			return;
		}

		foreach (var child in element.Elements()) {
			CollectStructuredXmlFields(child, $"{path}.{child.Name.LocalName}", map);
		}
	}

	private sealed class StructuredXmlFieldMap {
		private readonly Dictionary<string, (string Value, string Hint)>
			_byPath = new(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, (string Value, string Hint)>
			_byLeaf = new(StringComparer.OrdinalIgnoreCase);

		public void Add(string path, string leaf, string value, string hint) {
			_byPath[Normalize(path)] = (value, hint);
			_byLeaf.TryAdd(Normalize(leaf), (value, hint));
		}

		public bool TryResolve(string label, out (string Value, string Hint) xml) {
			var normalizedFull = Normalize(label);
			if (_byPath.TryGetValue(normalizedFull, out xml)) {
				return true;
			}

			var leaf = label.Split('.').Last();
			if (_byLeaf.TryGetValue(Normalize(leaf), out xml)) {
				return true;
			}

			foreach (var alias in GetAliases(label)) {
				if (_byPath.TryGetValue(alias, out xml) || _byLeaf.TryGetValue(alias, out xml)) {
					return true;
				}
			}

			xml = default;
			return false;
		}

		private static IEnumerable<string> GetAliases(string label) {
			var normalized = Normalize(label);
			var leaf = Normalize(label.Split('.').Last());

			if (normalized == "basetype") {
				yield return "basetype";
			}

			if (label.Equals("base.type", StringComparison.OrdinalIgnoreCase)) {
				yield return "basetype";
			}

			if (label.Equals("font", StringComparison.OrdinalIgnoreCase)) {
				yield return "fonttype";
			}

			if (label.Equals("button_type", StringComparison.OrdinalIgnoreCase)) {
				yield return "buttontype";
			}

			if (leaf != normalized) {
				yield return leaf;
			}
		}

		private static string Normalize(string text) {
			return new string(text.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
		}
	}
}

public sealed class UtfDbRepository {
	private readonly IReadOnlyDictionary<string, UtfDbDatabaseSpec> _databases;
	private readonly Dictionary<string, DosFileReader> _roots;

	public UtfDbRepository(IEnumerable<UtfDbDatabaseSpec> databases) {
		_databases = databases.ToDictionary(database => database.Name, StringComparer.OrdinalIgnoreCase);
		_roots = _databases.Values.ToDictionary(database => database.Name,
			database => new DosFileReader(database.DatabasePath), StringComparer.OrdinalIgnoreCase);
	}

	public IReadOnlyList<string> GetDatabases() {
		return _databases.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
	}

	public IReadOnlyList<string> GetTypes(string databaseName) {
		var root = GetRoot(databaseName);
		return root.FindFiles()
			.Where(entry => entry.IsDirectory)
			.Select(entry => entry.Name)
			.Where(name => UtfDbSupportedTypes.Parsers.ContainsKey(name))
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	public IReadOnlyList<UtfDbFileEntry> GetFiles(string databaseName, string typeName) {
		return GetRoot(databaseName).FindFiles(typeName)
			.Where(entry => !entry.IsDirectory)
			.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
			.Select(entry => new UtfDbFileEntry(entry.Name, checked((int)entry.Length)))
			.ToArray();
	}

	public UtfDbEntryDetails ReadEntryDetails(string databaseName, string typeName, string fileName) {
		if (!UtfDbSupportedTypes.Parsers.TryGetValue(typeName, out var parser)) {
			throw new InvalidOperationException($"Unsupported type '{typeName}'.");
		}

		var bytes = GetRoot(databaseName).ReadAllBytes(Path.Combine(typeName, fileName));
		var xml = LoadXml(databaseName, typeName, fileName);
		return parser.Parse(databaseName, fileName, bytes, xml);
	}

	private XDocument? LoadXml(string databaseName, string typeName, string fileName) {
		if (!_databases.TryGetValue(databaseName, out var database) ||
		    string.IsNullOrWhiteSpace(database.XmlRootPath)) {
			return null;
		}

		var path = Path.Combine(database.XmlRootPath, typeName, fileName + ".xml");
		return File.Exists(path) ? XDocument.Load(path) : null;
	}

	private DosFileReader GetRoot(string databaseName) {
		if (!_roots.TryGetValue(databaseName, out var root)) {
			throw new InvalidOperationException($"Unknown database '{databaseName}'.");
		}

		return root;
	}
}

using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.BT;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.Models.MT;

namespace ConquestFrontierWarsRay.Data.UtfDb;

public sealed class XmlDbRepository {
	private static readonly Lazy<IReadOnlyDictionary<string, Type>> TypedModelTypes = new(BuildTypedModelTypeMap);
	private readonly IReadOnlyDictionary<string, UtfDbDatabaseSpec> _databases;

	public XmlDbRepository(IEnumerable<UtfDbDatabaseSpec> databases) {
		_databases = databases
			.Where(database => !string.IsNullOrWhiteSpace(database.XmlRootPath))
			.ToDictionary(database => database.Name, StringComparer.OrdinalIgnoreCase);
	}

	public IReadOnlyList<string> GetDatabases() {
		return _databases.Keys
			.Where(name => Directory.Exists(_databases[name].XmlRootPath!))
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	public IReadOnlyList<string> GetTypes(string databaseName) {
		var xmlRootPath = GetXmlRootPath(databaseName);
		return Directory.EnumerateDirectories(xmlRootPath)
			.Select(Path.GetFileName)
			.Where(static name => !string.IsNullOrWhiteSpace(name))
			.Where(name => UtfDbSupportedTypes.Parsers.ContainsKey(name!))
			.Select(name => name!)
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	public IReadOnlyList<UtfDbFileEntry> GetFiles(string databaseName, string typeName) {
		var typePath = GetTypePath(databaseName, typeName);
		return Directory.EnumerateFiles(typePath, "*.xml", SearchOption.TopDirectoryOnly)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.Select(path => new UtfDbFileEntry(
				Path.GetFileNameWithoutExtension(path),
				checked((int)new FileInfo(path).Length)))
			.ToArray();
	}

	public UtfDbEntryDetails ReadEntryDetails(string databaseName, string typeName, string fileName) {
		if (!UtfDbSupportedTypes.Parsers.ContainsKey(typeName)) {
			throw new InvalidOperationException($"Unsupported type '{typeName}'.");
		}

		var xmlPath = Path.Combine(GetTypePath(databaseName, typeName), fileName + ".xml");
		if (!File.Exists(xmlPath)) {
			throw new FileNotFoundException($"Could not locate XML entry '{databaseName}/{typeName}/{fileName}'.", xmlPath);
		}

		var document = XDocument.Load(xmlPath);
		var root = document.Root ?? throw new InvalidDataException($"XML '{xmlPath}' does not have a root element.");
		var fields = XmlDbFieldProjector.Project(root);
		var typedValue = XmlDbTypedValueProjector.TryCreate(typeName, root);
		var byteLength = checked((int)new FileInfo(xmlPath).Length);
		return new UtfDbEntryDetails(databaseName, typeName, fileName, byteLength, fields, typedValue);
	}

	private string GetXmlRootPath(string databaseName) {
		if (!_databases.TryGetValue(databaseName, out var database) || string.IsNullOrWhiteSpace(database.XmlRootPath)) {
			throw new InvalidOperationException($"Unknown database '{databaseName}'.");
		}

		return database.XmlRootPath;
	}

	private string GetTypePath(string databaseName, string typeName) {
		var typePath = Path.Combine(GetXmlRootPath(databaseName), typeName);
		if (!Directory.Exists(typePath)) {
			throw new DirectoryNotFoundException($"Could not locate XML type directory '{typePath}'.");
		}

		return typePath;
	}

	private static IReadOnlyDictionary<string, Type> BuildTypedModelTypeMap() {
		return typeof(XmlDbRepository).Assembly
			.GetTypes()
			.Where(type => type is { IsClass: true, IsAbstract: false })
			.Where(type => type.Namespace is "ConquestFrontierWarsRay.Data.Models" or "ConquestFrontierWarsRay.Data.Models.BT" or "ConquestFrontierWarsRay.Data.Models.GT" or "ConquestFrontierWarsRay.Data.Models.MT")
			.GroupBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
			.Where(group => group.Count() == 1)
			.ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);
	}

	private static class XmlDbTypedValueProjector {
		public static object? TryCreate(string typeName, XElement root) {
			if (typeName.Equals("MT_STRINGPACK", StringComparison.OrdinalIgnoreCase) ||
			    typeName.Equals("MT_UNITSPEECH", StringComparison.OrdinalIgnoreCase)) {
				return null;
			}

			if (!TypedModelTypes.Value.TryGetValue(typeName, out var targetType)) {
				return null;
			}

			return CreateValue(root, targetType, propertyName: null);
		}

		private static object? CreateValue(XElement element, Type targetType, string? propertyName) {
			targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

			if (targetType == typeof(string)) {
				return element.Value.Trim();
			}

			if (targetType == typeof(bool)) {
				return ParseBoolean(element.Value);
			}

			if (targetType.IsEnum) {
				if (TryParseEnum(element.Value, targetType, out var enumValue)) {
					return enumValue;
				}

				return Activator.CreateInstance(targetType);
			}

			if (IsScalar(targetType)) {
				return ConvertScalar(element.Value, targetType);
			}

			if (targetType.IsArray) {
				return CreateArray(element, targetType, propertyName);
			}

			var instance = Activator.CreateInstance(targetType)
			              ?? throw new InvalidOperationException($"Could not instantiate '{targetType.FullName}'.");

			var writableProperties = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.Where(property => property.CanWrite)
				.ToArray();

			foreach (var property in writableProperties) {
				if (property.PropertyType.IsArray) {
					var arrayValue = TryCreateArrayPropertyValue(element, property);
					if (arrayValue is not null) {
						property.SetValue(instance, arrayValue);
					}

					continue;
				}

				var specialValue = TryCreateSpecialPropertyValue(element, property);
				if (specialValue is not null) {
					property.SetValue(instance, specialValue);
					continue;
				}

				var child = FindPropertyElement(element, property);
				if (child is null) {
					continue;
				}

				var value = CreateValue(child, property.PropertyType, property.Name);
				if (value is not null) {
					property.SetValue(instance, value);
				}
			}

			PopulateDerivedEnumProperties(element, targetType, instance, writableProperties);
			return instance;
		}

		private static object? TryCreateArrayPropertyValue(XElement element, PropertyInfo property) {
			var specialElements = GetSpecialArrayPropertyElements(element, property);
			if (specialElements.Length > 0) {
				return CreateArrayFromElements(specialElements, property.PropertyType, property.Name);
			}

			var repeatedSiblings = FindRepeatedPropertyElements(element, property);
			if (repeatedSiblings.Length > 1) {
				return CreateArrayFromElements(repeatedSiblings, property.PropertyType, property.Name);
			}

			var child = FindPropertyElement(element, property);
			if (child is not null) {
				return CreateValue(child, property.PropertyType, property.Name);
			}

			var directChildren = element.Elements()
				.Where(candidate => GetPropertyAliases(property.Name)
					.Any(alias => NormalizeName(candidate.Name.LocalName) == alias))
				.OrderBy(GetIndexOrMax)
				.ToArray();

			return directChildren.Length > 0
				? CreateArrayFromElements(directChildren, property.PropertyType, property.Name)
				: null;
		}

		private static object CreateArray(XElement element, Type arrayType, string? propertyName) {
			var elementType = arrayType.GetElementType()
			                 ?? throw new InvalidOperationException($"Array type '{arrayType.FullName}' does not expose an element type.");

			var items = element.Elements().Any()
				? element.Elements().OrderBy(GetIndexOrMax).ToArray()
				: Array.Empty<XElement>();

			if (items.Length == 0 && propertyName is not null) {
				items = element.Parent?.Elements()
					.Where(candidate => GetPropertyAliases(propertyName)
						.Any(alias => NormalizeName(candidate.Name.LocalName) == alias))
					.OrderBy(GetIndexOrMax)
					.ToArray()
					?? Array.Empty<XElement>();
			}

			var array = Array.CreateInstance(elementType, items.Length);
			for (var index = 0; index < items.Length; index++) {
				array.SetValue(CreateValue(items[index], elementType, propertyName), index);
			}

			return array;
		}

		private static object CreateArrayFromElements(XElement[] elements, Type arrayType, string? propertyName) {
			var elementType = arrayType.GetElementType()
			                 ?? throw new InvalidOperationException($"Array type '{arrayType.FullName}' does not expose an element type.");
			var array = Array.CreateInstance(elementType, elements.Length);
			for (var index = 0; index < elements.Length; index++) {
				array.SetValue(CreateValue(elements[index], elementType, propertyName), index);
			}

			return array;
		}

		private static XElement? FindPropertyElement(XElement element, PropertyInfo property) {
			foreach (var alias in GetPropertyAliases(property.Name)) {
				var child = element.Elements().FirstOrDefault(candidate =>
					NormalizeName(candidate.Name.LocalName) == alias);
				if (child is not null) {
					return child;
				}
			}

			return null;
		}

		private static XElement[] FindRepeatedPropertyElements(XElement element, PropertyInfo property) {
			var aliases = GetPropertyAliases(property.Name).ToArray();
			return element.Elements()
				.Where(candidate => aliases.Contains(NormalizeName(candidate.Name.LocalName), StringComparer.OrdinalIgnoreCase))
				.OrderBy(GetIndexOrMax)
				.ToArray();
		}

		private static IEnumerable<string> GetPropertyAliases(string propertyName) {
			yield return NormalizeName(propertyName);

			if (propertyName.Equals("Type", StringComparison.OrdinalIgnoreCase)) {
				yield return "basetype";
			}

			if (propertyName.Equals("FontName", StringComparison.OrdinalIgnoreCase)) {
				yield return "font";
			}

			if (propertyName.Equals("ShapeFile", StringComparison.OrdinalIgnoreCase)) {
				yield return "shapefile";
			}

			if (propertyName.Equals("ScrollBarType", StringComparison.OrdinalIgnoreCase)) {
				yield return "scrollbartype";
			}

			if (propertyName.Equals("ButtonType", StringComparison.OrdinalIgnoreCase)) {
				yield return "buttontype";
			}

			if (propertyName.Equals("ListboxType", StringComparison.OrdinalIgnoreCase)) {
				yield return "listboxtype";
			}

			if (propertyName.Equals("DropdownType", StringComparison.OrdinalIgnoreCase)) {
				yield return "dropdowntype";
			}

			if (propertyName.Equals("ComboBoxType", StringComparison.OrdinalIgnoreCase)) {
				yield return "comboboxtype";
			}

			if (propertyName.Equals("TextIds", StringComparison.OrdinalIgnoreCase)) {
				yield return "textid";
			}

			if (propertyName.Equals("Timer", StringComparison.OrdinalIgnoreCase)) {
				yield return "dwtimer";
			}

			if (propertyName.Equals("FileMame", StringComparison.OrdinalIgnoreCase)) {
				yield return "filename";
			}

			if (propertyName.Equals("TerraParticle", StringComparison.OrdinalIgnoreCase)) {
				yield return "teraparticle";
			}
		}

		private static object? TryCreateSpecialPropertyValue(XElement element, PropertyInfo property) {
			if (property.PropertyType == typeof(Vector2) && property.Name.Equals("Origin", StringComparison.OrdinalIgnoreCase)) {
				var xOrigin = element.Elements().FirstOrDefault(candidate => NormalizeName(candidate.Name.LocalName) == "xorigin");
				var yOrigin = element.Elements().FirstOrDefault(candidate => NormalizeName(candidate.Name.LocalName) == "yorigin");
				if (xOrigin is null || yOrigin is null) {
					return null;
				}

				return new Vector2(
					int.Parse(xOrigin.Value.Trim(), CultureInfo.InvariantCulture),
					int.Parse(yOrigin.Value.Trim(), CultureInfo.InvariantCulture));
			}

			if (property.PropertyType == typeof(uint) && property.Name.Equals("Flags", StringComparison.OrdinalIgnoreCase)) {
				var children = element.Elements()
					.ToDictionary(candidate => NormalizeName(candidate.Name.LocalName), candidate => candidate, StringComparer.OrdinalIgnoreCase);
				if (!children.ContainsKey("static") &&
				    !children.ContainsKey("singleclick") &&
				    !children.ContainsKey("scrollbar") &&
				    !children.ContainsKey("solidbackground") &&
				    !children.ContainsKey("noborder") &&
				    !children.ContainsKey("disablemouseselect")) {
					return null;
				}

				uint flags = 0;
				if (TryReadBoolean(children, "static")) {
					flags |= 1u << 0;
				}
				if (TryReadBoolean(children, "singleclick")) {
					flags |= 1u << 1;
				}
				if (TryReadBoolean(children, "scrollbar")) {
					flags |= 1u << 2;
				}
				if (TryReadBoolean(children, "solidbackground")) {
					flags |= 1u << 3;
				}
				if (TryReadBoolean(children, "noborder")) {
					flags |= 1u << 4;
				}
				if (TryReadBoolean(children, "disablemouseselect")) {
					flags |= 1u << 5;
				}

				return flags;
			}

			return null;
		}

		private static XElement[] GetSpecialArrayPropertyElements(XElement element, PropertyInfo property) {
			string[]? orderedNames = null;
			var parentName = NormalizeName(element.Name.LocalName);

			switch (parentName) {
				case "opening" when property.Name.Equals("Buttons", StringComparison.OrdinalIgnoreCase):
					orderedNames = ["single", "multi", "intro", "options", "help", "quit"];
					break;
				case "opening" when property.Name.Equals("StaticLabels", StringComparison.OrdinalIgnoreCase):
					orderedNames = ["staticsingle", "staticmulti", "staticintro", "staticoptions", "statichelp"];
					break;
				case "opening" when property.Name.Equals("Animations", StringComparison.OrdinalIgnoreCase):
					orderedNames = ["animmedia", "animsingle", "animmulti", "animoptions", "animquestion"];
					break;
				case "singleplayermenu" when property.Name.Equals("Buttons", StringComparison.OrdinalIgnoreCase):
					orderedNames = ["buttoncampaign", "buttonskirmish", "buttonload", "buttonqbload", "buttonback"];
					break;
				case "selectcampaign" when property.Name.Equals("Buttons", StringComparison.OrdinalIgnoreCase):
					orderedNames = ["buttonterran", "buttonmantis", "buttonsolarian", "buttonback"];
					break;
			}

			if (orderedNames is null) {
				return [];
			}

			var children = element.Elements()
				.GroupBy(candidate => NormalizeName(candidate.Name.LocalName), StringComparer.OrdinalIgnoreCase)
				.ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

			return orderedNames
				.Where(children.ContainsKey)
				.Select(name => children[name])
				.ToArray();
		}

		private static bool TryReadBoolean(IReadOnlyDictionary<string, XElement> elements, string normalizedName) {
			return elements.TryGetValue(normalizedName, out var element) && ParseBoolean(element.Value);
		}

		private static void PopulateDerivedEnumProperties(
			XElement element,
			Type targetType,
			object instance,
			IEnumerable<PropertyInfo> writableProperties) {
			foreach (var property in writableProperties) {
				var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
				if (!propertyType.IsEnum) {
					continue;
				}

				var currentValue = property.GetValue(instance);
				if (!Equals(currentValue, Activator.CreateInstance(propertyType))) {
					continue;
				}

				if (property.Name.Equals("Type", StringComparison.OrdinalIgnoreCase) &&
				    TryInferTypeEnum(targetType, propertyType, out var inferredType)) {
					property.SetValue(instance, inferredType);
					continue;
				}

				if (propertyType.GetCustomAttribute<FlagsAttribute>() is not null &&
				    TryBuildFlagsValue(element, propertyType, out var flagsValue)) {
					property.SetValue(instance, flagsValue);
				}
			}
		}

		private static bool TryInferTypeEnum(Type targetType, Type enumType, out object inferredValue) {
			var candidate = targetType.Name.StartsWith("GT_", StringComparison.OrdinalIgnoreCase)
				? $"GBT_{targetType.Name[3..]}"
				: targetType.Name;

			if (Enum.TryParse(enumType, candidate, ignoreCase: true, out var parsed) && parsed is not null) {
				inferredValue = parsed;
				return true;
			}

			inferredValue = default!;
			return false;
		}

		private static bool TryBuildFlagsValue(XElement element, Type enumType, out object flagsValue) {
			ulong combined = 0;
			var matchedAny = false;

			foreach (var member in Enum.GetNames(enumType)) {
				if (string.Equals(member, "None", StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				var child = element.Elements().FirstOrDefault(xmlChild =>
					NormalizeName(xmlChild.Name.LocalName) == NormalizeName(member));
				if (child is null || !ParseBoolean(child.Value)) {
					continue;
				}

				combined |= Convert.ToUInt64(Enum.Parse(enumType, member));
				matchedAny = true;
			}

			flagsValue = Enum.ToObject(enumType, combined);
			return matchedAny;
		}

		private static bool TryParseEnum(string text, Type enumType, out object? enumValue) {
			var trimmed = text.Trim();
			if (Enum.TryParse(enumType, trimmed, ignoreCase: true, out var named) && named is not null) {
				enumValue = named;
				return true;
			}

			try {
				var underlyingType = Enum.GetUnderlyingType(enumType);
				enumValue = Enum.ToObject(enumType, ConvertScalar(trimmed, underlyingType));
				return true;
			} catch {
				enumValue = null;
				return false;
			}
		}

		private static bool ParseBoolean(string text) {
			var trimmed = text.Trim();
			return trimmed switch {
				"0" => false,
				"1" => true,
				_ => bool.Parse(trimmed)
			};
		}

		private static bool IsScalar(Type type) {
			return type == typeof(byte) ||
			       type == typeof(sbyte) ||
			       type == typeof(short) ||
			       type == typeof(ushort) ||
			       type == typeof(int) ||
			       type == typeof(uint) ||
			       type == typeof(long) ||
			       type == typeof(ulong) ||
			       type == typeof(float) ||
			       type == typeof(double) ||
			       type == typeof(decimal);
		}

		private static object ConvertScalar(string text, Type targetType) {
			var trimmed = text.Trim();
			if (targetType != typeof(bool) && TryConvertBooleanLikeNumber(trimmed, targetType, out var booleanLikeValue)) {
				return booleanLikeValue;
			}

			return targetType switch {
				_ when targetType == typeof(byte) => byte.Parse(trimmed, CultureInfo.InvariantCulture),
				_ when targetType == typeof(sbyte) => sbyte.Parse(trimmed, CultureInfo.InvariantCulture),
				_ when targetType == typeof(short) => short.Parse(trimmed, CultureInfo.InvariantCulture),
				_ when targetType == typeof(ushort) => ushort.Parse(trimmed, CultureInfo.InvariantCulture),
				_ when targetType == typeof(int) => int.Parse(trimmed, CultureInfo.InvariantCulture),
				_ when targetType == typeof(uint) => uint.Parse(trimmed, CultureInfo.InvariantCulture),
				_ when targetType == typeof(long) => long.Parse(trimmed, CultureInfo.InvariantCulture),
				_ when targetType == typeof(ulong) => ulong.Parse(trimmed, CultureInfo.InvariantCulture),
				_ when targetType == typeof(float) => float.Parse(trimmed, CultureInfo.InvariantCulture),
				_ when targetType == typeof(double) => double.Parse(trimmed, CultureInfo.InvariantCulture),
				_ when targetType == typeof(decimal) => decimal.Parse(trimmed, CultureInfo.InvariantCulture),
				_ => throw new InvalidOperationException($"Unsupported scalar target type '{targetType.FullName}'.")
			};
		}

		private static bool TryConvertBooleanLikeNumber(string text, Type targetType, out object value) {
			value = default!;
			if (!text.Equals("true", StringComparison.OrdinalIgnoreCase) &&
			    !text.Equals("false", StringComparison.OrdinalIgnoreCase)) {
				return false;
			}

			var numeric = text.Equals("true", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
			value = targetType switch {
				_ when targetType == typeof(byte) => (byte)numeric,
				_ when targetType == typeof(sbyte) => (sbyte)numeric,
				_ when targetType == typeof(short) => (short)numeric,
				_ when targetType == typeof(ushort) => (ushort)numeric,
				_ when targetType == typeof(int) => numeric,
				_ when targetType == typeof(uint) => (uint)numeric,
				_ when targetType == typeof(long) => (long)numeric,
				_ when targetType == typeof(ulong) => (ulong)numeric,
				_ => default!
			};

			return value is not null;
		}
	}

	private static class XmlDbFieldProjector {
		public static IReadOnlyList<UtfDbField> Project(XElement root) {
			var fields = new List<UtfDbField>();
			var index = 0;
			foreach (var child in root.Elements()) {
				Collect(child, child.Name.LocalName, ref index, fields);
			}

			return fields;
		}

		private static void Collect(XElement element, string path, ref int index, ICollection<UtfDbField> fields) {
			var children = element.Elements().ToArray();
			if (children.Length == 0) {
				var hint = string.Join("; ",
					element.Attributes()
						.Where(attribute => attribute.Name.LocalName is not "type" and not "index")
						.Select(attribute => $"{attribute.Name.LocalName}={attribute.Value}"));

				fields.Add(new UtfDbField(
					index,
					index,
					path,
					element.Value.Trim(),
					string.Empty,
					element.Value.Trim(),
					hint));
				index++;
				return;
			}

			foreach (var child in children) {
				Collect(child, $"{path}.{child.Name.LocalName}", ref index, fields);
			}
		}
	}

	private static int GetIndexOrMax(XElement element) {
		return int.TryParse(element.Attribute("index")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
			? index
			: int.MaxValue;
	}

	private static string NormalizeName(string? value) {
		if (string.IsNullOrWhiteSpace(value)) {
			return string.Empty;
		}

		return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
	}
}

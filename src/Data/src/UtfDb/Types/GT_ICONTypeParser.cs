using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_ICONTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_ICON", "field", [UtfDbTypeLayouts.GenBase()]);

	public string TypeName => "GT_ICON";

	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData,
		XDocument? xmlDocument) {
		return UtfDbTypeParserHelpers.ParseTyped(
			databaseName,
			fileName,
			rawData,
			xmlDocument,
			FieldParser,
			ParseTypedValue,
			AnnotateEnumValues);
	}

	private static UtfDbField AnnotateEnumValues(UtfDbField field) {
		return field.Label == "base.type"
			? field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((GENBASE_TYPE)uint.Parse(field.Value)) }
			: field;
	}

	private static GT_ICON ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 4) {
			throw new InvalidDataException($"Entry 'GT_ICON' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		return new GT_ICON {
			Type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset)
		};
	}
}

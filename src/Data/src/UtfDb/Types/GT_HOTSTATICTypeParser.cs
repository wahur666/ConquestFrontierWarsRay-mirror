using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_HOTSTATICTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_HOTSTATIC", "field", [
		UtfDbTypeLayouts.GenBase(),
		new ScalarSpec("font_type", ScalarKind.Ascii32)
	]);

	public string TypeName => "GT_HOTSTATIC";

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

	private static GT_HOTSTATIC ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 36) {
			throw new InvalidDataException($"Entry 'GT_HOTSTATIC' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var fontType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);

		return new GT_HOTSTATIC {
			Type = type,
			FontType = fontType
		};
	}
}

using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_DIPLOMACYBUTTONTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_DIPLOMACYBUTTON", "field", [
		UtfDbTypeLayouts.GenBase(),
		new ScalarSpec("left_margin", ScalarKind.S1),
		new ScalarSpec("top_margin", ScalarKind.S1),
		new PaddingSpec("padding", 2),
		new ScalarSpec("shape_file", ScalarKind.Ascii32)
	]);

	public string TypeName => "GT_DIPLOMACYBUTTON";

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

	private static GT_DIPLOMACYBUTTON ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 40) {
			throw new InvalidDataException($"Entry 'GT_DIPLOMACYBUTTON' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var leftMargin = UtfDbTypeParserHelpers.ReadSByte(rawData, ref offset);
		var topMargin = UtfDbTypeParserHelpers.ReadSByte(rawData, ref offset);
		offset += 2;
		var shapeFile = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);

		return new GT_DIPLOMACYBUTTON {
			Type = type,
			LeftMargin = leftMargin,
			TopMargin = topMargin,
			ShapeFile = shapeFile
		};
	}
}

using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_SHIPSILBUTTONTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_SHIPSILBUTTON", "field", [
		UtfDbTypeLayouts.GenBase(),
		new ScalarSpec("red_yellow_break", ScalarKind.F4),
		new ScalarSpec("yellow_green_break", ScalarKind.F4)
	]);

	public string TypeName => "GT_SHIPSILBUTTON";

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

	private static GT_SHIPSILBUTTON ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 12) {
			throw new InvalidDataException($"Entry 'GT_SHIPSILBUTTON' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);

		return new GT_SHIPSILBUTTON {
			Type = type,
			RedYellowBreak = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			YellowGreenBreak = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset)
		};
	}
}

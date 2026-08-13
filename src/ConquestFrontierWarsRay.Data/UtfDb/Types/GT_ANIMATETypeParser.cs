using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_ANIMATETypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_ANIMATE", "field", [
		UtfDbTypeLayouts.GenBase(),
		new ScalarSpec("vfx_type", ScalarKind.Ascii32)
	]);

	public string TypeName => "GT_ANIMATE";

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

	private static GT_ANIMATE ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 36) {
			throw new InvalidDataException($"Entry 'GT_ANIMATE' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var vfxType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);

		return new GT_ANIMATE {
			Type = type,
			VfxType = vfxType
		};
	}
}

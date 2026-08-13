using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_FONTTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_FONT", "field", [
		UtfDbTypeLayouts.GenBase(),
		new ScalarSpec("font", ScalarKind.U4),
		new ScalarSpec("flags", ScalarKind.U4)
	]);

	public string TypeName => "GT_FONT";

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
		return field.Label switch {
			"base.type" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((GENBASE_TYPE)uint.Parse(field.Value))
			},
			"font" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((GT_FONT_FACE)uint.Parse(field.Value))
			},
			"flags" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((GT_FONT_FLAGS)uint.Parse(field.Value))
			},
			_ => field
		};
	}

	private static GT_FONT ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 12) {
			throw new InvalidDataException($"Entry 'GT_FONT' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var font = (GT_FONT_FACE)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		var flags = (GT_FONT_FLAGS)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);

		return new GT_FONT {
			Type = type,
			Font = font,
			Flags = flags
		};
	}
}

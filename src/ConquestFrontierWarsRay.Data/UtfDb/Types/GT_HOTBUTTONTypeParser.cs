using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_HOTBUTTONTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_HOTBUTTON", "field", [
		UtfDbTypeLayouts.GenBase(),
		new ScalarSpec("button_type", ScalarKind.U4),
		new ScalarSpec("font_type", ScalarKind.Ascii32),
		UtfDbTypeLayouts.Color("text_color")
	]);

	public string TypeName => "GT_HOTBUTTON";

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
			"button_type" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((GT_HOTBUTTON_TYPE)uint.Parse(field.Value))
			},
			_ => field
		};
	}

	private static GT_HOTBUTTON ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 43) {
			throw new InvalidDataException($"Entry 'GT_HOTBUTTON' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var buttonType = (GT_HOTBUTTON_TYPE)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		var fontType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		var textColor = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);

		return new GT_HOTBUTTON {
			Type = type,
			ButtonType = buttonType,
			FontType = fontType,
			TextColor = textColor
		};
	}
}

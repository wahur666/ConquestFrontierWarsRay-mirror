using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_SCROLLBARTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_SCROLLBAR", "field", [
		UtfDbTypeLayouts.GenBase(),
		new ScalarSpec("up_button_type", ScalarKind.Ascii32),
		new ScalarSpec("down_button_type", ScalarKind.Ascii32),
		UtfDbTypeLayouts.Color("thumb_color"),
		UtfDbTypeLayouts.Color("background_color"),
		UtfDbTypeLayouts.Color("disabled_color"),
		new ScalarSpec("horizontal", ScalarKind.U1),
		new ScalarSpec("shape_file", ScalarKind.Ascii32)
	]);

	public string TypeName => "GT_SCROLLBAR";

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
			"horizontal" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatBoolean(byte.Parse(field.Value) != 0)
			},
			_ => field
		};
	}

	private static GT_SCROLLBAR ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 78) {
			throw new InvalidDataException($"Entry 'GT_SCROLLBAR' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var upButtonType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		var downButtonType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		var thumbColor = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var backgroundColor = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var disabledColor = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var horizontal = UtfDbTypeParserHelpers.ReadBooleanU1(rawData, ref offset);
		var shapeFile = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);

		return new GT_SCROLLBAR {
			Type = type,
			UpButtonType = upButtonType,
			DownButtonType = downButtonType,
			ThumbColor = thumbColor,
			BackgroundColor = backgroundColor,
			DisabledColor = disabledColor,
			Horizontal = horizontal,
			ShapeFile = shapeFile
		};
	}
}

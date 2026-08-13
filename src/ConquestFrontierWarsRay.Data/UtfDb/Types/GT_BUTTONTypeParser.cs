using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_BUTTONTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_BUTTON", "field", [
		new GroupSpec("base", [
			new ScalarSpec("type", ScalarKind.U4)
		]),
		new ScalarSpec("buttonType", ScalarKind.U1),
		new ScalarSpec("leftMargin", ScalarKind.S1),
		new ScalarSpec("rightMargin", ScalarKind.S1),
		new PaddingSpec("padding", 1),
		new ScalarSpec("fontName", ScalarKind.Ascii32),
		new GroupSpec("disabledText", [
			new ScalarSpec("red", ScalarKind.U1),
			new ScalarSpec("green", ScalarKind.U1),
			new ScalarSpec("blue", ScalarKind.U1)
		]),
		new GroupSpec("normalText", [
			new ScalarSpec("red", ScalarKind.U1),
			new ScalarSpec("green", ScalarKind.U1),
			new ScalarSpec("blue", ScalarKind.U1)
		]),
		new GroupSpec("highlightText", [
			new ScalarSpec("red", ScalarKind.U1),
			new ScalarSpec("green", ScalarKind.U1),
			new ScalarSpec("blue", ScalarKind.U1)
		]),
		new ScalarSpec("shapeFile", ScalarKind.Ascii32)
	]);

	public string TypeName => "GT_BUTTON";

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
			"buttonType" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((GT_BUTTON_TYPE)byte.Parse(field.Value))
			},
			_ => field
		};
	}

	private static GT_BUTTON ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 79) {
			throw new InvalidDataException($"Entry 'GT_BUTTON' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var baseType = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);

		var buttonType = (GT_BUTTON_TYPE)rawData[offset++];
		var leftMargin = UtfDbTypeParserHelpers.ReadSByte(rawData, ref offset);
		var rightMargin = UtfDbTypeParserHelpers.ReadSByte(rawData, ref offset);
		offset += 1;

		var fontName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);

		var disabledText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var normalText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var highlightText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);

		var shapeFile = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);

		return new GT_BUTTON {
			Type = baseType,
			ButtonType = buttonType,
			LeftMargin = leftMargin,
			RightMargin = rightMargin,
			FontName = fontName,
			DisabledText = disabledText,
			NormalText = normalText,
			HighlightText = highlightText,
			ShapeFile = shapeFile
		};
	}
}

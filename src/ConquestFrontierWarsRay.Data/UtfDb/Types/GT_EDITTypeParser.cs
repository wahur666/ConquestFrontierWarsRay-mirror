using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_EDITTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_EDIT", "field", [
		UtfDbTypeLayouts.GenBase(),
		new ScalarSpec("font_name", ScalarKind.Ascii32),
		UtfDbTypeLayouts.Color("disabled_text"),
		UtfDbTypeLayouts.Color("normal_text"),
		UtfDbTypeLayouts.Color("highlight_text"),
		UtfDbTypeLayouts.Color("selected_text"),
		UtfDbTypeLayouts.Color("caret"),
		new ScalarSpec("shape_file", ScalarKind.Ascii32),
		new PaddingSpec("padding", 1),
		new ScalarSpec("justify", ScalarKind.S4),
		new ScalarSpec("width", ScalarKind.S4),
		new ScalarSpec("height", ScalarKind.S4)
	]);

	public string TypeName => "GT_EDIT";

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
			_ => field
		};
	}

	private static GT_EDIT ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 92) {
			throw new InvalidDataException($"Entry 'GT_EDIT' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var fontName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		var disabledText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var normalText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var highlightText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var selectedText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var caret = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var shapeFile = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		offset += 1;
		var justify = UtfDbTypeParserHelpers.ReadInt32(rawData, ref offset);
		var width = UtfDbTypeParserHelpers.ReadInt32(rawData, ref offset);
		var height = UtfDbTypeParserHelpers.ReadInt32(rawData, ref offset);

		return new GT_EDIT {
			Type = type,
			FontName = fontName,
			DisabledText = disabledText,
			NormalText = normalText,
			HighlightText = highlightText,
			SelectedText = selectedText,
			Caret = caret,
			ShapeFile = shapeFile,
			Justify = justify,
			Width = width,
			Height = height
		};
	}
}

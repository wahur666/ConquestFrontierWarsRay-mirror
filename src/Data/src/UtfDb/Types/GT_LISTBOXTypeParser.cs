using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_LISTBOXTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_LISTBOX", "field", [
		UtfDbTypeLayouts.GenBase(),
		new ScalarSpec("font_name", ScalarKind.Ascii32),
		UtfDbTypeLayouts.Color("disabled_text"),
		UtfDbTypeLayouts.Color("normal_text"),
		UtfDbTypeLayouts.Color("highlight_text"),
		UtfDbTypeLayouts.Color("selected_text"),
		UtfDbTypeLayouts.Color("selected_text_grayed"),
		new ScalarSpec("shape_file", ScalarKind.Ascii32),
		new ScalarSpec("scroll_bar_type", ScalarKind.Ascii32)
	]);

	public string TypeName => "GT_LISTBOX";

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

	private static GT_LISTBOX ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 85) {
			throw new InvalidDataException($"Entry 'GT_LISTBOX' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var fontName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		var disabledText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var normalText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var highlightText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var selectedText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var selectedTextGrayed = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var shapeFile = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		var scrollBarType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);

		return new GT_LISTBOX {
			Type = type,
			FontName = fontName,
			DisabledText = disabledText,
			NormalText = normalText,
			HighlightText = highlightText,
			SelectedText = selectedText,
			SelectedTextGrayed = selectedTextGrayed,
			ShapeFile = shapeFile,
			ScrollBarType = scrollBarType
		};
	}
}

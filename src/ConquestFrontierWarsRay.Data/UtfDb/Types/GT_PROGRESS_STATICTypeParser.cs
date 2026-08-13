using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_PROGRESS_STATICTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_PROGRESS_STATIC", "field", [
		UtfDbTypeLayouts.GenBase(),
		new ScalarSpec("font_name", ScalarKind.Ascii32),
		UtfDbTypeLayouts.Color("normal_text"),
		UtfDbTypeLayouts.Color("over_text"),
		UtfDbTypeLayouts.Color("background"),
		UtfDbTypeLayouts.Color("background2"),
		new ScalarSpec("shape_file", ScalarKind.Ascii32),
		new ScalarSpec("background_draw", ScalarKind.U4),
		new ScalarSpec("backdraw", ScalarKind.U4)
	]);

	public string TypeName => "GT_PROGRESS_STATIC";

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
			"background_draw" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((GT_DRAWTYPE)uint.Parse(field.Value))
			},
			"backdraw" => field with { EnumValue = UtfDbTypeParserHelpers.FormatBoolean(uint.Parse(field.Value) != 0) },
			_ => field
		};
	}

	private static GT_PROGRESS_STATIC ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 84) {
			throw new InvalidDataException($"Entry 'GT_PROGRESS_STATIC' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var fontName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		var normalText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var overText = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var background = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var background2 = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var shapeFile = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		var backgroundDraw = (GT_DRAWTYPE)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		var backdraw = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset) != 0;

		return new GT_PROGRESS_STATIC {
			Type = type,
			FontName = fontName,
			NormalText = normalText,
			OverText = overText,
			Background = background,
			Background2 = background2,
			ShapeFile = shapeFile,
			BackgroundDraw = backgroundDraw,
			Backdraw = backdraw
		};
	}
}

using System.Drawing;
using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_SLIDERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_SLIDER", "field", [
		UtfDbTypeLayouts.GenBase(),
		UtfDbTypeLayouts.Color("disabled_color"),
		UtfDbTypeLayouts.Color("normal_color"),
		UtfDbTypeLayouts.Color("highlight_color"),
		UtfDbTypeLayouts.Color("alert_color"),
		new ScalarSpec("vertical", ScalarKind.U4),
		new ScalarSpec("indent", ScalarKind.U4),
		new ScalarSpec("shape_file", ScalarKind.Ascii32)
	]);

	public string TypeName => "GT_SLIDER";

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
			"vertical" => field with { EnumValue = UtfDbTypeParserHelpers.FormatBoolean(uint.Parse(field.Value) != 0) },
			_ => field
		};
	}

	private static GT_SLIDER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 56) {
			throw new InvalidDataException($"Entry 'GT_SLIDER' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var disabledColor = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var normalColor = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var highlightColor = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var alertColor = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var vertical = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset) != 0;
		var indent = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		var shapeFile = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);

		return new GT_SLIDER {
			Type = type,
			DisabledColor = Color.FromArgb(255, disabledColor.Red, disabledColor.Green, disabledColor.Blue),
			NormalColor = Color.FromArgb(255, normalColor.Red, normalColor.Green, normalColor.Blue),
			HighlightColor = Color.FromArgb(255, highlightColor.Red, highlightColor.Green, highlightColor.Blue),
			AlertColor = Color.FromArgb(255, alertColor.Red, alertColor.Green, alertColor.Blue),
			Vertical = vertical,
			Indent = indent,
			ShapeFile = shapeFile
		};
	}
}

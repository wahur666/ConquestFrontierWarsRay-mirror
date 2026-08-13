using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_TABCONTROLTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_TABCONTROL", "field", [
		UtfDbTypeLayouts.GenBase(),
		UtfDbTypeLayouts.Color("normal_color"),
		UtfDbTypeLayouts.Color("hilite_color"),
		UtfDbTypeLayouts.Color("selected_color"),
		new PaddingSpec("padding", 3)
	]);

	public string TypeName => "GT_TABCONTROL";

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

	private static GT_TABCONTROL ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 16) {
			throw new InvalidDataException($"Entry 'GT_TABCONTROL' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var normalColor = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var hiliteColor = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		var selectedColor = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);

		return new GT_TABCONTROL {
			Type = type,
			NormalColor = normalColor,
			HiliteColor = hiliteColor,
			SelectedColor = selectedColor
		};
	}
}

using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models.BT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class BT_LIGHTTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_LIGHT", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.Color("color"),
		new PaddingSpec("padding", 1),
		new ScalarSpec("range", ScalarKind.S4),
		UtfDbTypeLayouts.Vector("direction"),
		new ScalarSpec("cutoff", ScalarKind.F4),
		new ScalarSpec("flags", ScalarKind.U4)
	]);

	public string TypeName => "BT_LIGHT";

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
			"base.obj_class" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((OBJCLASS)uint.Parse(field.Value))
			},
			"base.b_edit_dropable" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatBoolean(uint.Parse(field.Value) != 0)
			},
			"flags" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((BT_LIGHT_FLAGS)uint.Parse(field.Value))
			},
			_ => field
		};
	}

	private static BT_LIGHT ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 32) {
			throw new InvalidDataException($"Entry 'BT_LIGHT' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		var color = UtfDbTypeParserHelpers.ParseColor(rawData, ref offset);
		offset += 1;
		var range = UtfDbTypeParserHelpers.ReadInt32(rawData, ref offset);
		var direction = UtfDbTypeParserHelpers.ParseVector(rawData, ref offset);
		var cutoff = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset);
		var flags = (BT_LIGHT_FLAGS)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);

		return new BT_LIGHT {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Color = color,
			Range = range,
			Direction = direction,
			Cutoff = cutoff,
			Flags = flags
		};
	}
}

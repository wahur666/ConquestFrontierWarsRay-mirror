using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models.BT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class BT_GROUPTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser
		FieldParser = new("BT_GROUP", "field", [UtfDbTypeLayouts.BasicData()]);

	public string TypeName => "BT_GROUP";

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
			_ => field
		};
	}

	private static BT_GROUP ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 8) {
			throw new InvalidDataException($"Entry 'BT_GROUP' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_GROUP {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable
		};
	}
}

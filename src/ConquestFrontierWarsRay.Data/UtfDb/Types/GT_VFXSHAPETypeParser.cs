using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_VFXSHAPETypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_VFXSHAPE", "field", [
		UtfDbTypeLayouts.GenBase(),
		new ScalarSpec("filename", ScalarKind.Ascii32),
		new ScalarSpec("b_hi_res", ScalarKind.U1),
		new PaddingSpec("padding", 3)
	]);

	public string TypeName => "GT_VFXSHAPE";

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
			"b_hi_res" => field with { EnumValue = UtfDbTypeParserHelpers.FormatBoolean(byte.Parse(field.Value) != 0) },
			_ => field
		};
	}

	private static GT_VFXSHAPE ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 40) {
			throw new InvalidDataException($"Entry 'GT_VFXSHAPE' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		var type = UtfDbTypeParserHelpers.ParseGenBaseType(rawData, ref offset);
		var filename = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		var bHiRes = UtfDbTypeParserHelpers.ReadBooleanU1(rawData, ref offset);
		offset += 3;

		return new GT_VFXSHAPE {
			Type = type,
			Filename = filename,
			BHiRes = bHiRes
		};
	}
}

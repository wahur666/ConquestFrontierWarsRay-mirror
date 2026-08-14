using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_MOVIESCREENTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_MOVIESCREEN", "field", [
		new GroupSpec("screen_rect", [
			new ScalarSpec("left", ScalarKind.S4),
			new ScalarSpec("top", ScalarKind.S4),
			new ScalarSpec("right", ScalarKind.S4),
			new ScalarSpec("bottom", ScalarKind.S4)
		])
	]);

	public string TypeName => "GT_MOVIESCREEN";

	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData,
		XDocument? xmlDocument) {
		return UtfDbTypeParserHelpers.ParseTyped(
			databaseName,
			fileName,
			rawData,
			xmlDocument,
			FieldParser,
			ParseTypedValue);
	}

	private static GT_MOVIESCREEN ParseTypedValue(ReadOnlySpan<byte> rawData) {
		if (rawData.Length < 16) {
			throw new InvalidDataException($"Entry 'GT_MOVIESCREEN' has invalid size {rawData.Length}.");
		}

		var offset = 0;
		return new GT_MOVIESCREEN {
			ScreenRect = new RECT {
				Left = UtfDbTypeParserHelpers.ReadInt32(rawData, ref offset),
				Top = UtfDbTypeParserHelpers.ReadInt32(rawData, ref offset),
				Right = UtfDbTypeParserHelpers.ReadInt32(rawData, ref offset),
				Bottom = UtfDbTypeParserHelpers.ReadInt32(rawData, ref offset)
			}
		};
	}
}

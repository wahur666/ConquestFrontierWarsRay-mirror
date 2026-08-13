using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_HOTBUTTON : GENBASE_DATA {
	public GT_HOTBUTTON_TYPE ButtonType { get; init; }

	public string FontType { get; init; } = string.Empty;

	public GT_COLOR TextColor { get; init; } = new();
}

using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_FONT : GENBASE_DATA {
	public GT_FONT_FACE Font { get; init; }

	public GT_FONT_FLAGS Flags { get; init; }

	public bool Multiline => Flags.HasFlag(GT_FONT_FLAGS.Multiline);

	public bool NotScaling => Flags.HasFlag(GT_FONT_FLAGS.NotScaling);

	public bool ToolbarMoney => Flags.HasFlag(GT_FONT_FLAGS.ToolbarMoney);
}

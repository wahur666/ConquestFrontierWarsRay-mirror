namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_BUTTON : GENBASE_DATA {
	public GT_BUTTON_TYPE ButtonType { get; init; }

	public sbyte LeftMargin { get; init; }

	public sbyte RightMargin { get; init; }

	public string FontName { get; init; } = string.Empty;

	public GT_COLOR DisabledText { get; init; } = new();

	public GT_COLOR NormalText { get; init; } = new();

	public GT_COLOR HighlightText { get; init; } = new();

	public string ShapeFile { get; init; } = string.Empty;
}

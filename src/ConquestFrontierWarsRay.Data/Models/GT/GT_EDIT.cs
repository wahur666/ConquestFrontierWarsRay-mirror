namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_EDIT : GENBASE_DATA {
	public string FontName { get; init; } = string.Empty;

	public GT_COLOR DisabledText { get; init; } = new();

	public GT_COLOR NormalText { get; init; } = new();

	public GT_COLOR HighlightText { get; init; } = new();

	public GT_COLOR SelectedText { get; init; } = new();

	public GT_COLOR Caret { get; init; } = new();

	public string ShapeFile { get; init; } = string.Empty;

	public int Justify { get; init; }

	public int Width { get; init; }

	public int Height { get; init; }
}

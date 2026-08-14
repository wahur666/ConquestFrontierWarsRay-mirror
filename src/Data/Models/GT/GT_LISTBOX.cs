namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_LISTBOX : GENBASE_DATA {
	public string FontName { get; init; } = string.Empty;

	public GT_COLOR DisabledText { get; init; } = new();

	public GT_COLOR NormalText { get; init; } = new();

	public GT_COLOR HighlightText { get; init; } = new();

	public GT_COLOR SelectedText { get; init; } = new();

	public GT_COLOR SelectedTextGrayed { get; init; } = new();

	public string ShapeFile { get; init; } = string.Empty;

	public string ScrollBarType { get; init; } = string.Empty;
}

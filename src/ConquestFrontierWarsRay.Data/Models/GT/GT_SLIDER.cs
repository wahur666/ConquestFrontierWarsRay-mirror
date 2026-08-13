namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_SLIDER : GENBASE_DATA {
	public GT_COLOR DisabledColor { get; init; } = new();

	public GT_COLOR NormalColor { get; init; } = new();

	public GT_COLOR HighlightColor { get; init; } = new();

	public GT_COLOR AlertColor { get; init; } = new();

	public bool Vertical { get; init; }

	public uint Indent { get; init; }

	public string ShapeFile { get; init; } = string.Empty;
}

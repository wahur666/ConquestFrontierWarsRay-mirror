namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_SCROLLBAR : GENBASE_DATA {
	public string UpButtonType { get; init; } = string.Empty;

	public string DownButtonType { get; init; } = string.Empty;

	public GT_COLOR ThumbColor { get; init; } = new();

	public GT_COLOR BackgroundColor { get; init; } = new();

	public GT_COLOR DisabledColor { get; init; } = new();

	public bool Horizontal { get; init; }

	public string ShapeFile { get; init; } = string.Empty;
}

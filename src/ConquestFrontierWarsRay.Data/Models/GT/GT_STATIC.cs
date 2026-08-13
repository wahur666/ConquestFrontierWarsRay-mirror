namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_STATIC : GENBASE_DATA {
	public string FontName { get; init; } = string.Empty;

	public GT_COLOR NormalText { get; init; } = new();

	public GT_COLOR Background { get; init; } = new();

	public string ShapeFile { get; init; } = string.Empty;

	public GT_DRAWTYPE BackgroundDraw { get; init; }

	public bool Backdraw { get; init; }
}

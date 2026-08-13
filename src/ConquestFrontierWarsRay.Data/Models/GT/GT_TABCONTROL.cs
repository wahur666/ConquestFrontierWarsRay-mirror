namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_TABCONTROL : GENBASE_DATA {
	public GT_COLOR NormalColor { get; init; } = new();

	public GT_COLOR HiliteColor { get; init; } = new();

	public GT_COLOR SelectedColor { get; init; } = new();
}

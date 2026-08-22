using System.Drawing;

namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_SLIDER : GENBASE_DATA {
	public Color DisabledColor { get; init; } = Color.Empty;

	public Color NormalColor { get; init; } = Color.Empty;

	public Color HighlightColor { get; init; } = Color.Empty;

	public Color AlertColor { get; init; } = Color.Empty;

	public bool Vertical { get; init; }

	public uint Indent { get; init; }

	public string ShapeFile { get; init; } = string.Empty;
}

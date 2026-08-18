namespace ConquestFrontierWarsRay.MiniLayout;

public sealed class BoxLayoutStyle {
	public LayoutDirection Direction { get; set; } = LayoutDirection.Row;

	public LayoutAlignment JustifyContent { get; set; } = LayoutAlignment.Start;

	public LayoutAlignment AlignItems { get; set; } = LayoutAlignment.Start;

	public Thickness Padding { get; set; } = Thickness.Zero;

	public float Gap { get; set; }
}

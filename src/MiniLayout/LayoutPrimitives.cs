namespace ConquestFrontierWarsRay.MiniLayout;

public readonly record struct LayoutSize(float Width, float Height) {
	public static LayoutSize Zero => new(0f, 0f);
}

public readonly record struct LayoutRect(float X, float Y, float Width, float Height);

public readonly record struct Thickness(float Left, float Top, float Right, float Bottom) {
	public static Thickness Zero => new(0f, 0f, 0f, 0f);

	public float Horizontal => Left + Right;

	public float Vertical => Top + Bottom;
}

public enum LayoutDirection {
	Row,
	Column
}

public enum LayoutAlignment {
	Start,
	Center,
	End
}

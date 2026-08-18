namespace ConquestFrontierWarsRay.MiniLayout;

public sealed class BoxLayoutResult {
	internal BoxLayoutResult(LayoutRect bounds, IReadOnlyDictionary<string, LayoutRect> children) {
		Bounds = bounds;
		Children = children;
	}

	public LayoutRect Bounds { get; }

	public IReadOnlyDictionary<string, LayoutRect> Children { get; }
}

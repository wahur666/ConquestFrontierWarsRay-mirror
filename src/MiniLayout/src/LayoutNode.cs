namespace ConquestFrontierWarsRay.MiniLayout;

public sealed class LayoutNode {
	public LayoutNode(string name, LayoutSize size, Thickness margin) {
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Size = size;
		Margin = margin;
	}

	public string Name { get; }

	public LayoutSize Size { get; set; }

	public Thickness Margin { get; set; }
}

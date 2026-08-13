namespace RaySharp;

internal sealed class UtfNode {
	public required string Name { get; init; }
	public string? TagName { get; init; }
	public int AttributesFlags { get; init; }
	public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();
	public Dictionary<string, UtfNode> Children { get; } = [];
	public List<UtfNode> ChildrenList { get; } = [];
	public byte[]? Value { get; init; }
	public bool IsLeaf => (AttributesFlags & 0x80) != 0;
}

internal sealed class UtfDocument {
	public UtfDocument(Dictionary<string, UtfNode> roots) {
		Roots = roots;
	}

	public Dictionary<string, UtfNode> Roots { get; }
	public UtfNode? RootNode => Roots.TryGetValue("\\", out UtfNode? root) ? root : null;
	public IReadOnlyDictionary<string, UtfNode> RootChildren => RootNode?.Children ?? Roots;
}

namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_NUGGET_DATA : BASIC_DATA {
	public string NuggetAnim2D { get; set; } = string.Empty;
	public string NuggetMesh { get; set; } = string.Empty;
	public int AnimSizeSmallMax { get; set; }
	public int AnimSizeMax { get; set; }
	public int AnimSizeMin { get; set; }
	public BT_COMPLEX_COLOR Color { get; set; } = new();
	public bool Oriented { get; set; }
	public bool ZRender { get; set; }
	public M_RESOURCE_TYPE ResType { get; set; }
	public M_NUGGET_TYPE NuggetType { get; set; }
	public uint MaxSupplies { get; set; }
}


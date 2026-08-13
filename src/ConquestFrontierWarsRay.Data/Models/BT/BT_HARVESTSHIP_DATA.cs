namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_HARVESTSHIP_DATA : BASIC_DATA {
	public BT_BASE_SPACESHIP_DATA BaseData { get; set; } = new();
	public uint LoadingRate { get; set; }
	public BT_DOCK_TIMING DockTiming { get; set; } = new();
	public BT_NUGGET_TIMING NuggetTiming { get; set; } = new();
	public string GasTankMesh { get; set; } = string.Empty;
	public string MetalTankMesh { get; set; } = string.Empty;
}


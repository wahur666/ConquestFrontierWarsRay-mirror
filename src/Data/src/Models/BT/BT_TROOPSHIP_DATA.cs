namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_TROOPSHIP_DATA : BASIC_DATA {
	public BT_BASE_SPACESHIP_DATA BaseData { get; set; } = new();
	public uint DamagePotential { get; set; }
	public float AssaultRange { get; set; }
	public uint SfxPodRelease { get; set; }
}


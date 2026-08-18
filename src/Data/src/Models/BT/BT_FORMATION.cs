namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_FORMATION : BASIC_DATA {
	public BT_ARTIFACT_BUTTON_INFO ButtonInfo { get; set; } = new();
	public BT_FLEET_GROUP_DEF[] Groups { get; set; } = [];
	public bool FreeForm { get; set; }
	public uint MoveTargetRange { get; set; }
	public uint SpotterRange { get; set; }
	public FORMATION_CLOAK_USAGE CloakUsage { get; set; }
	public bool AdmiralAttackControl { get; set; }
	public BT_ADMIRAL_BONUSES FormationBonuses { get; set; } = new();
	public uint FormationSpecials {
		get => RepairWholeFleet ? 1u : 0u;
		set => RepairWholeFleet = (value & 1) != 0;
	}
	public bool RepairWholeFleet { get; set; }
}


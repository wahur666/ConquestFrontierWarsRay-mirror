namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_GUNBOAT_DATA : BASIC_DATA {
	public BT_BASE_SPACESHIP_DATA BaseData { get; set; } = new();
	public float OuterWeaponRange { get; set; }
	public float OptimalFacingAngle { get; set; }
	public bool NoLineOfSight { get; set; }
	public string[] LauncherType { get; set; } = [];
}


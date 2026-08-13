namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_PLAT_GUN : BASIC_DATA {
	public BT_BASE_PLATFORM_DATA BaseData { get; set; } = new();
	public float OuterWeaponRange { get; set; }
	public bool NoLineOfSight { get; set; }
	public string[] LauncherType { get; set; } = [];
	public string SpecialLauncherType { get; set; } = string.Empty;
}


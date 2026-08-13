namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_TERRAFORM_ARTIFACT : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string ShipName { get; set; } = string.Empty;
	public BT_ARTIFACT_BUTTON_INFO ButtonInfo { get; set; } = new();
	public string TargetPlanetType { get; set; } = string.Empty;
	public float ChangeTime { get; set; }
}


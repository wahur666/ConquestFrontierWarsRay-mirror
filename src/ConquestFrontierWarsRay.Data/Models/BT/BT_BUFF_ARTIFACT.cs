namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_BUFF_ARTIFACT : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string ShipName { get; set; } = string.Empty;
	public uint BuffType { get; set; }
	public BT_ARTIFACT_BUTTON_INFO ButtonInfo { get; set; } = new();
}


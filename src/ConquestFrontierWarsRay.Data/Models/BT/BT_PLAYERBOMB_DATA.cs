namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_PLAYERBOMB_DATA : BASIC_DATA {
	public BT_PLAYER_RACE[] Race { get; set; } = [];
	public string PlayerBombAnim2D { get; set; } = string.Empty;
	public uint AnimSize { get; set; }
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public string Filename { get; set; } = string.Empty;
}


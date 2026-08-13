namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_SYSTEM_BUFF_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public BT_SYSTEM_BUFF_TYPE BuffType { get; set; }
	public BT_SYSTEM_BUFF_TARGET_TYPE TargetType { get; set; }
}


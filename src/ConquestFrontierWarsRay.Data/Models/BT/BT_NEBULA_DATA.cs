namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_NEBULA_DATA : BASIC_DATA {
	public BASE_FIELD_DATA BaseFieldData { get; set; } = new();
	public string CloudEffect { get; set; } = string.Empty;
	public string MapTexName { get; set; } = string.Empty;
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public BT_FIELD_ATTRIBUTES Attributes { get; set; } = new();
	public BT_AMBIENT_NEBULA_LIGHT Ambient { get; set; } = new();
	public uint AmbientSfx { get; set; }
	public BT_NEBTYPE NebType { get; set; }
	public uint NuggetsPerSquare { get; set; }
	public float NuggetZHeight { get; set; }
	public string[] NuggetType { get; set; } = [];
}


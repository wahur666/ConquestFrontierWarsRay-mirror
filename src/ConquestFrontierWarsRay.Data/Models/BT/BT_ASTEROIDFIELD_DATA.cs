namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_ASTEROIDFIELD_DATA : BASIC_DATA {
	public BASE_FIELD_DATA BaseFieldData { get; set; } = new();
	public string[] FileName { get; set; } = [];
	public string DustTexName { get; set; } = string.Empty;
	public string ModTextureName { get; set; } = string.Empty;
	public string MapTexName { get; set; } = string.Empty;
	public string SoftwareTexClearName { get; set; } = string.Empty;
	public string SoftwareTexFogName { get; set; } = string.Empty;
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public BT_FIELD_ATTRIBUTES Attributes { get; set; } = new();
	public int AsteroidsPerSquare { get; set; }
	public int PolyroidsPerSquare { get; set; }
	public int Depth { get; set; }
	public int Range { get; set; }
	public int MaxDriftSpeed { get; set; }
	public int MinDriftSpeed { get; set; }
	public float StationaryPercentage { get; set; }
	public uint AmbientSfx { get; set; }
	public float ModTexSpeedScale { get; set; }
	public int AnimSizeMin { get; set; }
	public int AnimSizeMax { get; set; }
	public uint NuggetsPerSquare { get; set; }
	public float NuggetZHeight { get; set; }
	public string[] NuggetType { get; set; } = [];
	public BT_COLOR AnimColor { get; set; } = new();
}


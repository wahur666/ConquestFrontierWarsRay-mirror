namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_ENGINETRAIL : BASIC_DATA {
	public EFFECTCLASS FxClass { get; set; }
	public float Width { get; set; }
	public uint Segments { get; set; }
	public float TimePerSegment { get; set; }
	public uint TapperType { get; set; }
	public float TapperMod { get; set; }
	public string Hardpoint { get; set; } = string.Empty;
	public string Texture { get; set; } = string.Empty;
	public BT_COLORA ColorMod { get; set; } = new();
	public BT_COLORA EngineGlowColorMod { get; set; } = new();
	public string EngineGlowTexture { get; set; } = string.Empty;
	public float EngineGlowWidth { get; set; }
}


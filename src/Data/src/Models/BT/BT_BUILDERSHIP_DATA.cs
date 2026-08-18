namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_BUILDERSHIP_DATA : BASIC_DATA {
	public SPACESHIPCLASS SpaceshipClass { get; set; }
	public string FileName { get; set; } = string.Empty;
	public BT_DYNAMICS_DATA_JR DynamicsData { get; set; } = new();
	public string WorkAnimation { get; set; } = string.Empty;
	public string SparkAnim { get; set; } = string.Empty;
	public string SparkHardpoint { get; set; } = string.Empty;
	public string ExplosionType { get; set; } = string.Empty;
	public float SparkWidth { get; set; }
	public float SparkDelay { get; set; }
}


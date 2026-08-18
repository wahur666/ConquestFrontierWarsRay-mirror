namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_MOVIE_CAMERA_DATA : BASIC_DATA {
	public string FileName { get; set; } = string.Empty;
	public BT_MISSION_DATA MissionData { get; set; } = new();
}


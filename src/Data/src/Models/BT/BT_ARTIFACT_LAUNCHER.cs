namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_ARTIFACT_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER BaseLaunder { get; set; } = new();
	public string ArtifactName { get; set; } = string.Empty;
}


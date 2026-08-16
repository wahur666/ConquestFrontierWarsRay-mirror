namespace ConquestFrontierWarsRay.Runtime.Trim.Terrain;

public interface ITerrainSegCallback {
    public bool TerrainCallback(FootprintInfo info, Gridvector pos);
}

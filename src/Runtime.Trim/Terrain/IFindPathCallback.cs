using MapGen.Terrain;

namespace ConquestFrontierWarsRay.Runtime.Trim.Terrain;

public interface IFindPathCallback {
    public void SetPath(ITerrainMap map, Gridvector squares, int numSquares);
}

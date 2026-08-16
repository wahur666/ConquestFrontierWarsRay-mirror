namespace ConquestFrontierWarsRay.Runtime.Trim.Terrain;

public interface ITerrainMap {
    public void SetWorldRect(Rect worldRect);

    public void SetFootprint(Gridvector squares, int numSquares, FootprintInfo info);

    public void UndoFootprint(Gridvector squares, int numSquares, FootprintInfo info);

    // enumerates all elements in a square, call with from == to to enumerate all elements of one square
    // returns false if the user's callback function stopped the enum early, else returns true.
    public bool TestSegment(Gridvector from, Gridvector to, ITerrainSegCallback callback);

    public int FindPath(Gridvector from, Gridvector to, uint dwMissionID, uint flags, IFindPathCallback callback);

    public void RenderEdit(); // want the terrain map to draw stuff in editor mode

    public bool IsGridEmpty(Gridvector grid, uint dwIgnoreMissionID, bool bFullSquare = true);

    public bool IsParkedAtGrid(Gridvector grid, uint dwMissionID, bool bFullSquare);

    public bool IsGridValid(Gridvector grid);

    public bool IsGridInSystem(Gridvector grid);

    public uint GetFieldID(Gridvector grid);

    public bool IsOkForBuilding(Gridvector grid, bool checkParkedUnits, bool bFullSquare);
}

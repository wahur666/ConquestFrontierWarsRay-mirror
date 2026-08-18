namespace ConquestFrontierWarsRay.Runtime.Trim.Terrain;

public class PathNode {
    public float G = 0f;
    public float H = 0f;
    public float F = 0f;

    public PathNode? Next = null;
    public PathNode? Prev = null;

    public uint PriorityId = 0;

    public CellRef? CurrentCell = null;
    public PathNode? ParentNode = null;
}

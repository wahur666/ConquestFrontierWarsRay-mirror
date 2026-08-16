namespace ConquestFrontierWarsRay.Runtime.Trim.Terrain;

public class MapMatrix {
    public FootprintList[,] Matrix = new FootprintList[TerrainMap.MAX_MAP_SIZE, TerrainMap.MAX_MAP_SIZE];

    public uint Dimension;

    public static PriorityQueue QOpen = new();
    public static PriorityQueue QClosed = new();
    public static int Maps = 0;
    public static uint OpenId = TerrainMap.OPEN_START;
    public static uint ClosedId = TerrainMap.CLOSED_START;

    public TerrainMap TerrainMap;

    public MapMatrix(uint dimension, TerrainMap terrainMap) {
        if (dimension == 0) {
            throw new ArgumentOutOfRangeException(nameof(dimension));
        }

        Dimension = dimension;
        Maps++;

        float center = Dimension / 2.0f;
        var footprintInfo = new FootprintInfo {
            Flags = TerrainMap.TERRAIN_FULLSQUARE | TerrainMap.TERRAIN_OUTOFSYSTEM | TerrainMap.TERRAIN_IMPASSIBLE |
                    TerrainMap.TERRAIN_FULLYTAKEN
        };

        for (int i = 0; i < Dimension; i++) {
            for (int j = 0; j < Dimension; j++) {
                Matrix[i, j] = new FootprintList {
                    Node = {
                        CurrentCell = new CellRef(i, j)
                    }
                };

                float x = i + 0.5f;
                float y = j + 0.5f;
                float xf = center - x;
                float yf = center - y;
                float distance = MathF.Sqrt(xf * xf + yf * yf);
                if (distance > center) {
                    Matrix[i, j].Add(footprintInfo);
                }
            }
        }

        TerrainMap = terrainMap;
    }

    ~MapMatrix() {
        Maps--;
        if (Maps < 0) {
            throw new InvalidOperationException("Map count went negative.");
        }

        if (Maps == 0) {
            OpenId = TerrainMap.OPEN_START;
            ClosedId = TerrainMap.CLOSED_START;
        }
    }

    public bool AStarPathClense(Gridvector[] gridpath, int numgrids) {
        QOpen.Flush(OpenId);
        QClosed.Flush(ClosedId);

        OpenId += (uint)Maps * 2;
        ClosedId += (uint)Maps * 2;

        var srcCell = new CellRef(gridpath[numgrids - 1].GetIntX(), gridpath[numgrids - 1].GetIntY());
        var dstCell = new CellRef(gridpath[0].GetIntX(), gridpath[0].GetIntY());

        var node = GetNode(srcCell)!;
        node.G = 0;
        node.H = GetDistance(srcCell, dstCell);
        node.F = node.G + node.H;
        node.ParentNode = null;

        QOpen.Push(node);

        while (!QOpen.Empty()) {
            node = QOpen.Pop()!;

            if (node.CurrentCell == dstCell) {
                numgrids = 0;
                PathNode? pnode = node;

                while (pnode is not null) {
                    gridpath[numgrids].Init(pnode.CurrentCell!.X, pnode.CurrentCell.Y);
                    numgrids++;
                    pnode = pnode.ParentNode;
                }

                return true;
            }

            int index = 0;
            CellRef? cell = null;
            for (int j = 0; j < numgrids; j++) {
                var current = new CellRef(gridpath[j].GetIntX(), gridpath[j].GetIntY());
                if (current == node.CurrentCell) {
                    index = j;
                    cell = current;
                    break;
                }
            }

            int next = index + 1;
            int prev = index - 1;

            for (int i = 0; i < numgrids; i++) {
                var newCell = new CellRef(gridpath[i].GetIntX(), gridpath[i].GetIntY());
                if (node.CurrentCell == newCell) {
                    continue;
                }

                var newNode = GetNode(newCell);
                if (newNode is null) {
                    continue;
                }

                if ((i != next && i != prev) &&
                    !TerrainMap.TestSegmentPassiblePrecise(
                        gridpath[index].GetX(),
                        gridpath[index].GetY(),
                        gridpath[i].GetX(),
                        gridpath[i].GetY())) {
                    continue;
                }

                float newCost = node.G + GetDistance(cell!, newCell);
                if ((QOpen.Contains(newNode) || QClosed.Contains(newNode)) && newNode.G <= newCost) {
                    continue;
                }

                newNode.ParentNode = node;
                newNode.G = newCost;
                newNode.H = GetDistance(newNode.CurrentCell!, dstCell);
                newNode.F = newNode.G + newNode.H;

                if (QClosed.Contains(newNode)) {
                    QClosed.Remove(newNode);
                }

                if (!QOpen.Contains(newNode)) {
                    QOpen.Push(newNode);
                }
            }

            QClosed.Push(node);
        }

        return false;
    }

    public int GetFootprintCount(uint x, uint y) {
        if (x < Dimension && y < Dimension) {
            return Matrix[x, y].GetFootprintCount();
        }

        return 0;
    }

    public uint GetFieldId(uint x, uint y) {
        if (x >= Dimension || y >= Dimension) {
            return 0;
        }

        if ((Matrix[x, y].AllFlags & TerrainMap.TERRAIN_FIELD) == 0) {
            return 0;
        }

        foreach (var footprintInfo in Matrix[x, y].FpInfoList) {
            if ((footprintInfo.Flags & TerrainMap.TERRAIN_FIELD) != 0) {
                return footprintInfo.MissionID;
            }
        }

        return 0;
    }

    public static int PLAYERID_MASK = 0x0000000F;

    public bool IsDestinationOpen(uint x, uint y, uint missionId) {
		// if the square is out of bounds, then keep moving
        if ((Matrix[x, y].AllFlags & TerrainMap.TERRAIN_OUTOFSYSTEM) != 0) {
            return false;
        }

        foreach (var footprintInfo in Matrix[x, y].FpInfoList) {
            if (missionId == footprintInfo.MissionID) {
				// ignore footprints that this missionID already set
                continue;
            }

            uint flags = footprintInfo.Flags;
            if (((footprintInfo.MissionID & PLAYERID_MASK) == (missionId & PLAYERID_MASK)) &&
                ((flags & TerrainMap.TERRAIN_DESTINATION) != 0)) {
				// there is a player of the same ID requesting this spot
                return false;
            }

            if ((flags & (TerrainMap.TERRAIN_PARKED | TerrainMap.TERRAIN_IMPASSIBLE |
                          TerrainMap.TERRAIN_OUTOFSYSTEM)) != 0) {
	            // there is someone parked here already
                return false;
            }
        }

        return true;
    }

    public uint GetFlags(int x, int y) {
        if (x < 0 || y < 0 || x >= Dimension || y >= Dimension) {
            throw new ArgumentOutOfRangeException();
        }

        return Matrix[x, y].AllFlags;
    }

    PathNode? GetNode(CellRef cell) {
        if (cell.X >= 0 && cell.X < Dimension && cell.Y >= 0 && cell.Y < Dimension) {
            return Matrix[cell.X, cell.Y].Node;
        }

        return null;
    }

    float GetDistance(CellRef src, CellRef dst) {
        float xdiff = src.X - dst.X;
        float ydiff = src.Y - dst.Y;
        return MathF.Sqrt(xdiff * xdiff + ydiff * ydiff);
    }
    
    
}

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

    public bool IsParkedAtSquare(uint x, uint y, uint missionId) {
        if ((Matrix[x, y].AllFlags & TerrainMap.TERRAIN_OUTOFSYSTEM) != 0) {
            return false;
        }

        foreach (var footprintInfo in Matrix[x, y].FpInfoList) {
            if (missionId != footprintInfo.MissionID) {
                continue;
            }

            if ((footprintInfo.Flags & TerrainMap.TERRAIN_PARKED) != 0) {
                return true;
            }
        }

        return false;
    }

    public bool IsParkedAtCorner(uint x, uint y, uint missionId, uint cornerId) {
        if ((Matrix[x, y].AllFlags & TerrainMap.TERRAIN_FULLYTAKEN) != 0) {
            return false;
        }

        foreach (var footprintInfo in Matrix[x, y].FpInfoList) {
            uint flags = footprintInfo.Flags;
            if ((flags & cornerId) == 0 || missionId != footprintInfo.MissionID) {
                continue;
            }

            if ((flags & TerrainMap.TERRAIN_PARKED) != 0) {
                return true;
            }
        }

        return false;
    }

    public bool IsOkForBuildingAtSquare(uint x, uint y, bool checkParkedUnits) {
        uint testFlags = checkParkedUnits
            ? TerrainMap.TERRAIN_PARKED | TerrainMap.TERRAIN_OUTOFSYSTEM | TerrainMap.TERRAIN_IMPASSIBLE |
              TerrainMap.TERRAIN_BLOCKLOS
            : TerrainMap.TERRAIN_OUTOFSYSTEM | TerrainMap.TERRAIN_IMPASSIBLE | TerrainMap.TERRAIN_BLOCKLOS;

        if ((Matrix[x, y].AllFlags & testFlags) != 0) {
            return false;
        }

        if (checkParkedUnits) {
            foreach (var footprintInfo in Matrix[x, y].FpInfoList) {
                if ((footprintInfo.Flags & testFlags) != 0) {
                    return false;
                }
            }
        }

        return true;
    }

    public bool IsOkForBuildingAtCorner(uint x, uint y, bool checkParkedUnits, uint cornerId) {
        uint testFlags = checkParkedUnits
            ? TerrainMap.TERRAIN_PARKED | TerrainMap.TERRAIN_OUTOFSYSTEM | TerrainMap.TERRAIN_IMPASSIBLE |
              TerrainMap.TERRAIN_BLOCKLOS
            : TerrainMap.TERRAIN_OUTOFSYSTEM | TerrainMap.TERRAIN_IMPASSIBLE | TerrainMap.TERRAIN_BLOCKLOS;

        if ((Matrix[x, y].AllFlags & testFlags) != 0) {
            return false;
        }

        if (checkParkedUnits) {
            foreach (var footprintInfo in Matrix[x, y].FpInfoList) {
                uint flags = footprintInfo.Flags;
                if ((flags & cornerId) == 0) {
                    continue;
                }

                if ((flags & testFlags) != 0) {
                    return false;
                }
            }
        }

        return true;
    }

    public bool IsCornerDestinationOpen(uint x, uint y, uint missionId, uint cornerId) {
        uint allFlags = Matrix[x, y].AllFlags;
        if ((allFlags & TerrainMap.TERRAIN_FULLYTAKEN) != 0) {
            return false;
        }

        foreach (var footprintInfo in Matrix[x, y].FpInfoList) {
            uint thisMissionId = footprintInfo.MissionID;
            if (missionId == thisMissionId) {
                continue;
            }

            uint flags = footprintInfo.Flags;
            bool samePlayer = (thisMissionId & PLAYERID_MASK) == (missionId & PLAYERID_MASK);

            if ((flags & cornerId) != 0) {
                if (samePlayer && (flags & TerrainMap.TERRAIN_DESTINATION) != 0) {
                    return false;
                }

                if ((flags & (TerrainMap.TERRAIN_PARKED | TerrainMap.TERRAIN_IMPASSIBLE |
                              TerrainMap.TERRAIN_OUTOFSYSTEM)) != 0) {
                    return false;
                }
            } else if ((flags & TerrainMap.TERRAIN_FULLSQUARE) != 0 &&
                       (flags & TerrainMap.TERRAIN_DESTINATION) != 0 &&
                       samePlayer) {
                return false;
            }
        }

        return true;
    }

    public bool IsCellValid(int x, int y) {
        return (Matrix[x, y].AllFlags & TerrainMap.TERRAIN_IMPASSIBLE) == 0;
    }

    public bool IsCellInSystem(int x, int y) {
        return (Matrix[x, y].AllFlags & TerrainMap.TERRAIN_OUTOFSYSTEM) == 0;
    }

    public uint GetQuarterFlags(int x, int y, uint quarter) {
        uint flags = 0;

        foreach (var footprintInfo in Matrix[x, y].FpInfoList) {
            if ((footprintInfo.Flags & quarter) != 0) {
                flags |= footprintInfo.Flags;
            }
        }

        if ((Matrix[x, y].AllFlags & TerrainMap.TERRAIN_FULLYTAKEN) != 0) {
            flags |= TerrainMap.TERRAIN_FULLYTAKEN;
        }

        if ((Matrix[x, y].AllFlags & TerrainMap.TERRAIN_IMPASSIBLE) != 0) {
            flags |= TerrainMap.TERRAIN_IMPASSIBLE;
        }

        return flags;
    }

    public void AddFootprint(uint x, uint y, FootprintInfo footprintInfo) {
        if (x >= Dimension || y >= Dimension) {
            throw new ArgumentOutOfRangeException();
        }

        Matrix[x, y].Add(footprintInfo);
    }

    public void UndoFootprint(uint x, uint y, FootprintInfo footprintInfo) {
        if (x >= Dimension || y >= Dimension) {
            throw new ArgumentOutOfRangeException();
        }

        Matrix[x, y].Undo(footprintInfo);
    }

    public uint GetMissionFlags(int x, int y, uint missionId, uint cornerId = 0) {
        return Matrix[x, y].GetMissionFlags(missionId, cornerId);
    }

    PathNode? GetNode(CellRef cell) {
        if (cell.X >= 0 && cell.X < Dimension && cell.Y >= 0 && cell.Y < Dimension) {
            return Matrix[cell.X, cell.Y].Node;
        }

        return null;
    }

    float GetHeuristic(CellRef src, CellRef dst) {
        int deltaX = Math.Abs(src.X - dst.X);
        int deltaY = Math.Abs(src.Y - dst.Y);

        return TerrainMap.DIAGONAL_LENGTH * Math.Min(deltaX, deltaY) +
               TerrainMap.AXIAL_LENGTH * Math.Abs(deltaX - deltaY);
    }

    float GetDistance(CellRef src, CellRef dst) {
        float xdiff = src.X - dst.X;
        float ydiff = src.Y - dst.Y;
        return MathF.Sqrt(xdiff * xdiff + ydiff * ydiff);
    }

    float GetCellCost(CellRef cell, int direction) {
        if ((Matrix[cell.X, cell.Y].AllFlags & TerrainMap.TERRAIN_IMPASSIBLE) != 0) {
            return TerrainMap.INFTY;
        }

        return direction < 4 ? TerrainMap.AXIAL_LENGTH : TerrainMap.DIAGONAL_LENGTH;
    }

    float GetCellCostIgnoreImpassible(int direction) {
        return direction < 4 ? TerrainMap.AXIAL_LENGTH : TerrainMap.DIAGONAL_LENGTH;
    }

    public bool AStarPath(Gridvector src, Gridvector dst, Gridvector[] gridpath, int numgrids) {
        QOpen.Flush(OpenId);
        QClosed.Flush(ClosedId);

        OpenId += (uint)Maps * 2;
        ClosedId += (uint)Maps * 2;

        var srcCell = new CellRef(src.GetIntX(), src.GetIntY());
        var dstCell = new CellRef(dst.GetIntX(), dst.GetIntY());

        var node = GetNode(srcCell)!;
        node.G = 0;
        node.H = GetHeuristic(srcCell, dstCell);
        node.F = node.G + node.H;
        node.ParentNode = null;

        QOpen.Push(node);

        while (!QOpen.Empty()) {
            node = QOpen.Pop()!;
            var currentCell = node.CurrentCell!;

            if (currentCell == dstCell) {
                numgrids = 0;
                PathNode? pathNode = node;

                while (pathNode is not null) {
                    gridpath[numgrids].Init(pathNode.CurrentCell!.X, pathNode.CurrentCell.Y);
                    gridpath[numgrids].Centerpos();
                    numgrids++;
                    pathNode = pathNode.ParentNode;
                }

                gridpath[0] = dst;
                gridpath[numgrids - 1] = src;

                if (numgrids > 4) {
                    AStarPathClense(gridpath, numgrids);
                }

                return true;
            }

            int inftyBits = 0;
            for (int i = 0; i < 8; i++) {
                var newNode = GetNode(currentCell + CellRef.CellDirections[i]);
                if (newNode is null) {
                    inftyBits |= 1 << i;
                    continue;
                }

                float newCost = currentCell == srcCell
                    ? node.G + GetCellCostIgnoreImpassible(i) / 2.0f + GetCellCost(newNode.CurrentCell!, i) / 2.0f
                    : node.G + GetCellCost(currentCell, i) / 2.0f + GetCellCost(newNode.CurrentCell!, i) / 2.0f;

                if (newCost >= TerrainMap.INFTY_LITE) {
                    inftyBits |= 1 << i;
                    continue;
                }

                if (i >= 4) {
                    int testBit1 = 1 << (i - 4);
                    int testBit2 = 1 << ((i - 3) % 4);
                    if ((inftyBits & testBit1) != 0 && (inftyBits & testBit2) != 0) {
                        continue;
                    }
                }

                if ((QOpen.Contains(newNode) || QClosed.Contains(newNode)) && newNode.G <= newCost) {
                    continue;
                }

                newNode.ParentNode = node;
                newNode.G = newCost;
                newNode.H = GetHeuristic(newNode.CurrentCell!, dstCell);
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
}

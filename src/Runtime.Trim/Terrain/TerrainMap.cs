namespace ConquestFrontierWarsRay.Runtime.Trim.Terrain;

public class TerrainMap : ITerrainMap {
    public static int MAX_MAP_SIZE = 64;
    public static int MAX_FOOTPRINT = 16;
    public const float AXIAL_LENGTH = 1.0f;
    public const float DIAGONAL_LENGTH = 1.41421f;
    public const float INFTY = 10000000.0f;
    public const float INFTY_LITE = 4000000.0f;

    public static uint TERRAIN_IMPASSIBLE = 0x00000001;
    public static uint TERRAIN_BLOCKLOS = 0x00000002;
    public static uint TERRAIN_FULLSQUARE = 0x00000040;
    public static uint TERRAIN_HALFSQUARE = 0x00000080;
    public static uint TERRAIN_PARKED = 0x00000100;
    public static uint TERRAIN_MOVING = 0x00000200;
    public static uint TERRAIN_DESTINATION = 0x00000400;
    public static uint TERRAIN_FIELD = 0x00000800;
    public static uint TERRAIN_UNITROTATING = 0x00001000;
    public static uint TERRAIN_OUTOFSYSTEM = 0x00004000;
    public static uint TERRAIN_WILLBEPLAT = 0x00020000;
    public static uint TERRAIN_REGION = 0x00040000;

    public static uint TERRAIN_TOPLEFT = 0x00000004;
    public static uint TERRAIN_TOPRIGHT = 0x00000008;
    public static uint TERRAIN_BOTTOMLEFT = 0x00000010;
    public static uint TERRAIN_BOTTOMRIGHT = 0x00000020;
    public static uint TERRAIN_FULLYTAKEN = 0x00002000;

    public static uint TERRAIN_FP_HALFSQUARE = TERRAIN_HALFSQUARE;
    public static uint TERRAIN_FP_FULLSQUARE = TERRAIN_FULLSQUARE;
    public static uint TERRAIN_FP_FINALPATH = 0x00010000;

    public const int OPEN_START = 1;
    public const int CLOSED_START = 2;

    private Rect _worldRect = new();
    public MapMatrix Map = null!;

    public void SetWorldRect(Rect worldRect) {
        _worldRect = worldRect;
        uint size = (uint)((worldRect.Right - worldRect.Left) / Gridvector.GRIDSIZE);
        Map = new MapMatrix(size, this);
    }

    public void SetFootprint(Gridvector squares, int numSquares, FootprintInfo info) {
        throw new NotImplementedException();
    }

    public void UndoFootprint(Gridvector squares, int numSquares, FootprintInfo info) {
        throw new NotImplementedException();
    }

    public bool TestSegment(Gridvector from, Gridvector to, ITerrainSegCallback callback) {
        throw new NotImplementedException();
    }

    public int FindPath(Gridvector from, Gridvector to, uint dwMissionID, uint flags, IFindPathCallback callback) {
        throw new NotImplementedException();
    }

    public void RenderEdit() {
        throw new NotImplementedException();
    }

    public bool IsGridEmpty(Gridvector grid, uint dwIgnoreMissionID, bool bFullSquare = true) {
        throw new NotImplementedException();
    }

    public bool IsParkedAtGrid(Gridvector grid, uint dwMissionID, bool bFullSquare) {
        throw new NotImplementedException();
    }

    public bool IsGridValid(Gridvector grid) {
        throw new NotImplementedException();
    }

    public bool IsGridInSystem(Gridvector grid) {
        throw new NotImplementedException();
    }

    public uint GetFieldID(Gridvector grid) {
        throw new NotImplementedException();
    }

    public bool IsOkForBuilding(Gridvector grid, bool checkParkedUnits, bool bFullSquare) {
        throw new NotImplementedException();
    }

    public bool TestSquarePassible(int x, int y) {
        return (Map.GetFlags(x, y) & TERRAIN_IMPASSIBLE) == 0;
    }

    public bool TestSegmentPassiblePrecise(float xoInput, float yoInput, float x1Input, float y1Input, Gridvector? backup = null) {
        const float res = 16;

        float xo = xoInput * res;
        float yo = yoInput * res;
        float x1 = x1Input * res;
        float y1 = y1Input * res;

        int oldx = -1;
        int oldy = -1;
        int dx = (int)(x1 - xo);
        int dy = (int)(y1 - yo);
        int xInc;
        int yInc;
        int error = 0;

        float x = xo;
        float y = yo;

        backup?.Init(xo / res, yo / res);

        if (dx >= 0) {
            xInc = 1;
        } else {
            xInc = -1;
            dx = -dx;
        }

        if (dy >= 0) {
            yInc = 1;
        } else {
            yInc = -1;
            dy = -dy;
        }

        if (dx > dy) {
            for (int i = 0; i <= dx; i++) {
				// adjust the error term
                error += dy;

                int newx = (int)(x / res);
                int newy = (int)(y / res);
                if ((newx != oldx || newy != oldy) && !TestSquarePassible(newx, newy)) {
					// cannot pass through
	                return false;
                }

                backup?.Init(x / res, y / res);
                oldx = newx;
                oldy = newy;

				// test if error has overflowed
                if (error > dx) {
                    error -= dx;
                    y += yInc;
                }

                x += xInc;
            }
        } else {
			// draw the line
            for (int i = 0; i <= dy; i++) {
				// adjust the error term
                error += dx;

                int newx = (int)(x / res);
                int newy = (int)(y / res);
                if ((newx != oldx || newy != oldy) && !TestSquarePassible(newx, newy)) {
                    return false;
                }

                backup?.Init(x / res, y / res);
                oldx = newx;
                oldy = newy;

				// test if error overflowed
                if (error > 0) {
                    error -= dy;
                    x += xInc;
                }

                y += yInc;
            }
        }

        return true;
    }
}

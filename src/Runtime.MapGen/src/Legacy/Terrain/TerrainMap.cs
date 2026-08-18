namespace MapGen.Terrain;

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

	private RECT _worldRect;
	public MapMatrix map;

	public void SetWorldRect(RECT worldRect) {
		_worldRect = worldRect;
		uint size = (uint)((worldRect.right - worldRect.left)/GRIDVECTOR.GRIDSIZE);
		map = new MapMatrix(size, this);
	}

	public void SetFootprint(GRIDVECTOR squares, int numSquares, FootprintInfo info) {
		throw new NotImplementedException();
	}

	public void UndoFootprint(GRIDVECTOR squares, int numSquares, FootprintInfo info) {
		throw new NotImplementedException();
	}

	public bool TestSegment(GRIDVECTOR from, GRIDVECTOR to, ITerrainSegCallback callback) {
		throw new NotImplementedException();
	}

	public int FindPath(GRIDVECTOR from, GRIDVECTOR to, uint dwMissionID, uint flags, IFindPathCallback callback) {
		throw new NotImplementedException();
	}

	public void RenderEdit() {
		throw new NotImplementedException();
	}

	public bool IsGridEmpty(GRIDVECTOR grid, uint dwIgnoreMissionID, bool bFullSquare = true) {
		throw new NotImplementedException();
	}

	public bool IsParkedAtGrid(GRIDVECTOR grid, uint dwMissionID, bool bFullSquare) {
		throw new NotImplementedException();
	}

	public bool IsGridValid(GRIDVECTOR grid) {
		throw new NotImplementedException();
	}

	public bool IsGridInSystem(GRIDVECTOR grid) {
		throw new NotImplementedException();
	}

	public uint GetFieldID(GRIDVECTOR grid) {
		throw new NotImplementedException();
	}

	public bool IsOkForBuilding(GRIDVECTOR grid, bool checkParkedUnits, bool bFullSquare) {
		throw new NotImplementedException();
	}

	public bool testSquarePassible(int x, int y) {
		return (map.GetFlags(x, y) & TERRAIN_IMPASSIBLE) == 0;
	}

	public bool testSegmentPassiblePrecise(float _xo, float _yo, float _x1, float _y1, GRIDVECTOR backup = null) {
		// the resolution of the line segment
		const float res = 16;

		float xo = _xo * res;
		float yo = _yo * res;
		float x1 = _x1 * res;
		float y1 = _y1 * res;

		int oldx = -1, oldy = -1;
		int newx, newy;

		int dx, dy;
		int xInc = 0;
		int yInc = 0;
		int error = 0; // the discriminant i.e. error i.e. decision variable
		int i;

		float x = xo, y = yo;

		// compute horizontal and vertical deltas
		dx = (int)(x1 - xo);
		dy = (int)(y1 - yo);

		// initialize the backup
		if (backup is not null) {
			backup.init(xo / res, yo / res);
		}

		// test which direction the line is going in i.e. slope angle
		if (dx >= 0) {
			xInc = 1;
		} else {
			xInc = -1;
			dx = -dx; // need absolute value
		}

		// test y component of slope
		if (dy >= 0) {
			yInc = 1;
		} else {
			yInc = -1;
			dy = -dy; // need absolute value
		}

		// now based on which delta is greater we can draw the line
		if (dx > dy) {
			// test along the line
			for (i = 0; i <= dx; i++) {
				// adjust the error term
				error += dy;

				newx = (int)(x / res);
				newy = (int)(y / res);
				if (newx != oldx || newy != oldy) {
					if (testSquarePassible(newx, newy) == false) {
						// cannot pass through
						return false;
					}
				}

				if (backup is not null) {
					backup.init(x / res, y / res);
				}

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
			for (i = 0; i <= dy; i++) {
				// adjust the error term
				error += dx;

				newx = (int)(x / res);
				newy = (int)(y / res);
				if (newx != oldx || newy != oldy) {
					if (testSquarePassible(newx, newy) == false) {
						return false;
					}
				}

				if (backup is not null) {
					backup.init(x / res, y / res);
				}

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

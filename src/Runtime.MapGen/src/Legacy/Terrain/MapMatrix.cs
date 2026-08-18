namespace MapGen.Terrain;

public class MapMatrix {
	public FootprintList[,]
		matrix = new FootprintList[TerrainMap.MAX_MAP_SIZE,
			TerrainMap.MAX_MAP_SIZE]; // a double array of pointers to FootprintList objects

	public uint nDimension;

	// used for path finding
	public static PriorityQueue QOpen = new();
	public static PriorityQueue QClosed = new();
	public static int nMaps = 0;
	public static uint openID = TerrainMap.OPEN_START;
	public static uint closedID = TerrainMap.CLOSED_START;

	public TerrainMap terrainMap;

	public MapMatrix(uint dimension, TerrainMap pTerrain) {
		nDimension = dimension;
		MapGen.CQASSERT(dimension > 0);

		nMaps++;

		// size the matrix
		float fCntr = nDimension / 2.0f;

		FootprintInfo fpinfo = new FootprintInfo();
		fpinfo.Flags = TerrainMap.TERRAIN_FULLSQUARE | TerrainMap.TERRAIN_OUTOFSYSTEM | TerrainMap.TERRAIN_IMPASSIBLE |
		               TerrainMap.TERRAIN_FULLYTAKEN;

		for (int i = 0; i < nDimension; i++) {
			for (int j = 0; j < nDimension; j++) {
				matrix[i, j] = new FootprintList {
					Node = {
						currentCell = new CellRef(i, j)
					}
				};

				float x = i + 0.5f;
				float y = j + 0.5f;

				// set the out of bounds stuff
				float xf = (fCntr - x);
				float yf = (fCntr - y);
				float fdist = MathF.Sqrt(xf * xf + yf * yf);
				if (fdist > fCntr) {
					// set a footprint for the out of system stuff
					matrix[i, j].Add(fpinfo);
				}
			}
		}

		terrainMap = pTerrain;
	}

	~MapMatrix() {
		nMaps--;

		MapGen.CQASSERT(nMaps >= 0);

		if (nMaps == 0) {
			openID = TerrainMap.OPEN_START;
			closedID = TerrainMap.CLOSED_START;
		}

		// delete everything
		for (int i = 0; i < nDimension; i++) {
			for (int j = 0; j < nDimension; j++) {
				if (matrix[i, j] is not null) {
					matrix[i, j] = null;
				}
			}
		}
	}

	public bool AStarPathClense(GRIDVECTOR[] gridpath, int numgrids) {
		QOpen.flush(openID);
		QClosed.flush(closedID);

		openID += (uint)nMaps * 2;
		closedID += (uint)nMaps * 2;

		// get a pointer to the node given by src
		CellRef srcCell	= new CellRef(gridpath[numgrids - 1].getIntX(), gridpath[numgrids - 1].getIntY());
		CellRef dstCell	= new CellRef(gridpath[0].getIntX(), gridpath[0].getIntY());

		PathNode newnode = null;

		PathNode node = GetNode(srcCell);
		node.g = 0;
		node.h = GetDistance(srcCell, dstCell);
		node.f = node.g + node.h;
		node.parentNode = null;

		QOpen.push(node);

		// this is the body of the search
		while (QOpen.empty() == false) {
			// take the best node off the list 
			node = QOpen.pop();

			if (node.currentCell == dstCell) {
				// hurray!  we've found a cleaner path, we hope so anyway
				numgrids = 0;
				PathNode pnode = node;

				while (pnode is not null) {
					gridpath[numgrids].init(pnode.currentCell.X, pnode.currentCell.Y);
					numgrids++;
					pnode = pnode.parentNode;
				}

				return true;
			}

			// find the index of the current gridvector
			int index = 0;
			CellRef cell = null;
			for (int j = 0; j < numgrids; j++) {
				CellRef cr = new CellRef(gridpath[j].getIntX(), gridpath[j].getIntY());
				if (cr == node.currentCell) {
					index = j;
					cell = cr;
					break;
				}
			}

			int next = index + 1;
			int prev = index - 1;

			// find out what other nodes can be put onto the Open and Closed priority queues
			// each node may be connected to at most numgrid-1 other nodes
			for (int i = 0; i < numgrids; i++) {
				// is the new cell the same as the current?
				CellRef newcell = new CellRef(gridpath[i].getIntX(), gridpath[i].getIntY());

				if (node.currentCell == newcell) {
					continue;
				}

				newnode = GetNode(newcell);

				// can the node and newnode be connected?
				if ((i != next && i != prev) && 
				    terrainMap.testSegmentPassiblePrecise(
					    gridpath[index].getX(),
					    gridpath[index].getY(), 
					    gridpath[i].getX(), 
					    gridpath[i].getY()) == false) {
					continue;
				}

				// the newnode is connected to the node, carry on...
				float newcost = node.g + GetDistance(cell, newcell);

				if ((QOpen.contains(newnode) || QClosed.contains(newnode)) && newnode.g <= newcost)
					continue;

				newnode.parentNode = node;
				newnode.g = newcost;
				newnode.h = GetDistance(newnode.currentCell, dstCell);
				newnode.f = newnode.g + newnode.h;

				// if newnode is on PClosed then remove it
				if (QClosed.contains(newnode)) {
					QClosed.remove(newnode);
				}

				// if newnode is not yet in POpen, then push it on
				if (QOpen.contains(newnode) == false) {
					QOpen.push(newnode);
				}
			}

			// push node onto closed
			QClosed.push(node);
		}

		// a path could not be found
		return false;
	}

	public int GetFootprintCount(uint x, uint y) {
		if ((x >= 0 && x < nDimension) && (y >= 0 && y < nDimension)) {
			return matrix[x, y].GetFootprintCount();
		}

		return 0;
	}

	public uint GetFieldID(uint x, uint y) {
		if (x >= nDimension || y >= nDimension)
			return 0;

		if ((matrix[x, y].AllFlags & TerrainMap.TERRAIN_FIELD) == 0) {
			return 0;
		}

		// find the field ID
		var it = matrix[x, y].FpInfoList;
		foreach (var footprintInfo in it) {
			if ((footprintInfo.Flags & TerrainMap.TERRAIN_FIELD) != 0) {
				return footprintInfo.MissionID;
			}
		}

		// if we've gotten here, than this square is not part of a field
		return 0;
	}

	public static int PLAYERID_MASK = 0x0000000F;

	public bool IsDestinationOpen(uint x, uint y, uint dwMissionID) {
		uint dwFlags;
		var it = matrix[x, y].FpInfoList;

		// if the square is out of bounds, then keep moving
		if ((matrix[x, y].AllFlags & TerrainMap.TERRAIN_OUTOFSYSTEM) != 0) {
			return false;
		}

		foreach (var footprintInfo in it) {
			if (dwMissionID == footprintInfo.MissionID) {
				// ignore footprints that this missionID already set
				continue;
			}

			dwFlags = footprintInfo.Flags;
			if (((footprintInfo.MissionID & PLAYERID_MASK) == (dwMissionID & PLAYERID_MASK)) &&
			    ((dwFlags & TerrainMap.TERRAIN_DESTINATION) != 0)) {
				// there is a player of the same ID requesting this spot
				return false;
			}

			if ((dwFlags & (TerrainMap.TERRAIN_PARKED | TerrainMap.TERRAIN_IMPASSIBLE |
			                TerrainMap.TERRAIN_OUTOFSYSTEM)) != 0) {
				// there is someone parked here already
				return false;
			}
		}

		return true;
	}

	bool IsParkedAtSquare(uint x, uint y, uint dwMissionID) {
		uint dwFlags;
		var it = matrix[x, y].FpInfoList;

		// if the square is out of bounds, then keep moving
		if ((matrix[x, y].AllFlags & TerrainMap.TERRAIN_OUTOFSYSTEM) != 0) {
			return false;
		}

		foreach (var footprintInfo in it) {
			if (dwMissionID != footprintInfo.MissionID) {
				continue;
			}

			dwFlags = footprintInfo.Flags;
			if ((dwFlags & TerrainMap.TERRAIN_PARKED) != 0)
				return true;
		}

		return false;
	}

	bool IsParkedAtCorner(uint x, uint y, uint dwMissionID, uint dwCornerID) {
		// if the square is completely taken, then keep moving
		var allFlags = matrix[x, y].AllFlags;
		if ((matrix[x, y].AllFlags & TerrainMap.TERRAIN_FULLYTAKEN) != 0) {
			return false;
		}

		var it = matrix[x, y].FpInfoList;

		foreach (var footprintInfo in it) {
			var dwFlags = footprintInfo.Flags;

			if ((dwFlags & dwCornerID) == 0 || dwMissionID != footprintInfo.MissionID) {
				continue;
			}

			if ((dwFlags & TerrainMap.TERRAIN_PARKED) != 0)
				return true;
		}

		return false;
	}

	bool IsOkForBuildingAtSquare(uint x, uint y, bool checkParkedUnits) {
		uint dwFlags;
		uint testFlags = (checkParkedUnits)
			? (TerrainMap.TERRAIN_PARKED | TerrainMap.TERRAIN_OUTOFSYSTEM | TerrainMap.TERRAIN_IMPASSIBLE |
			   TerrainMap.TERRAIN_BLOCKLOS)
			: (TerrainMap.TERRAIN_OUTOFSYSTEM | TerrainMap.TERRAIN_IMPASSIBLE | TerrainMap.TERRAIN_BLOCKLOS);

		// no building out of bounds, or in terrain
		if ((matrix[x, y].AllFlags & testFlags) != 0) {
			return false;
		}

		if (checkParkedUnits) {
			var it = matrix[x, y].FpInfoList;
			foreach (var footprintInfo in it) {
				dwFlags = footprintInfo.Flags;
				if ((dwFlags & testFlags) != 0)
					return false;
			}
		}

		return true;
	}

	bool IsOkForBuildingAtCorner(uint x, uint y, bool checkParkedUnits, uint dwCornerID) {
		uint dwFlags;
		uint testFlags = (checkParkedUnits)
			? (TerrainMap.TERRAIN_PARKED | TerrainMap.TERRAIN_OUTOFSYSTEM | TerrainMap.TERRAIN_IMPASSIBLE |
			   TerrainMap.TERRAIN_BLOCKLOS)
			: (TerrainMap.TERRAIN_OUTOFSYSTEM | TerrainMap.TERRAIN_IMPASSIBLE | TerrainMap.TERRAIN_BLOCKLOS);

		// no building out of bounds, or in terrain
		if ((matrix[x, y].AllFlags & testFlags) != 0) {
			return false;
		}

		if (checkParkedUnits) {
			var it = matrix[x, y].FpInfoList;
			foreach (var footprintInfo in it) {
				dwFlags = footprintInfo.Flags;
				if ((dwFlags & dwCornerID) == 0) {
					continue;
				}

				if ((dwFlags & testFlags) != 0)
					return false;
			}
		}

		return true;
	}

	bool IsCornerDestinationOpen(uint x, uint y, uint dwMissionID, uint dwCornerID) {
		// if the square is completely taken, then keep moving
		uint allFlags = matrix[x, y].AllFlags;

		if ((allFlags & TerrainMap.TERRAIN_FULLYTAKEN) != 0) {
			return false;
		}

		uint dwFlags;
		bool bSamePlayer;
		uint thisMissionID;
		var it = matrix[x, y].FpInfoList;

		foreach (var footprintInfo in it) {
			thisMissionID = footprintInfo.MissionID;

			if (dwMissionID == thisMissionID) {
				// ignore footprints that this missionID already set
				continue;
			}

			dwFlags = footprintInfo.Flags;
			bSamePlayer = (thisMissionID & PLAYERID_MASK) == (dwMissionID & PLAYERID_MASK);

			if ((dwFlags & dwCornerID) != 0) {
				if (bSamePlayer && (dwFlags & TerrainMap.TERRAIN_DESTINATION) != 0) {
					// there is a player of the same ID requesting this spot
					return false;
				}

				if ((dwFlags & (TerrainMap.TERRAIN_PARKED | TerrainMap.TERRAIN_IMPASSIBLE |
				                TerrainMap.TERRAIN_OUTOFSYSTEM)) != 0) {
					// there is someone else parked here buddy, keep moving
					return false;
				}
			} else {
				// is there a big freindly ship that has requested this spot
				if ((dwFlags & TerrainMap.TERRAIN_FULLSQUARE) != 0 && (dwFlags & TerrainMap.TERRAIN_DESTINATION) != 0 &&
				    bSamePlayer) {
					return false;
				}
			}
		}

		return true;
	}

	bool IsCellValid(int x, int y) {
		return ((matrix[x, y].AllFlags & TerrainMap.TERRAIN_IMPASSIBLE) != 0) == false;
	}

	bool IsCellInSystem(int x, int y) {
		return (matrix[x, y].AllFlags & TerrainMap.TERRAIN_OUTOFSYSTEM) == 0;
	}

	public uint GetFlags(int x, int y) {
		MapGen.CQASSERT(x < nDimension && y < nDimension);

		return matrix[x, y].AllFlags;
	}

	uint GetQuarterFlags(int x, int y, uint dwQuarter) {
		uint flags = 0;
		var it = matrix[x, y].FpInfoList;


		foreach (var footprintInfo in it) {
			if ((footprintInfo.Flags & dwQuarter) != 0) {
				flags |= footprintInfo.Flags;
			}
		}

		if ((matrix[x, y].AllFlags & TerrainMap.TERRAIN_FULLYTAKEN) != 0) {
			flags |= TerrainMap.TERRAIN_FULLYTAKEN;
		}

		if ((matrix[x, y].AllFlags & TerrainMap.TERRAIN_IMPASSIBLE) != 0) {
			flags |= TerrainMap.TERRAIN_IMPASSIBLE;
		}

		return flags;
	}

	void RenderFootprints() {
		// uint i, j;
		//
		// PIPE.set_render_state(D3DRS_SRCBLEND,D3DBLEND_SRCALPHA);
		// PIPE.set_render_state(D3DRS_DESTBLEND,D3DBLEND_INVSRCALPHA);
		//
		// for (i = 0; i < nDimension; i++)
		// {
		// 	for (j = 0; j < nDimension; j++)
		// 	{
		// 		uint flags = matrix[i][j].allFlags;
		//
		// 		if (matrix[i][j].count > 0)
		// 		{
		// 			float x0 = float(i)*GRIDSIZE;
		// 			float y0 = float(j)*GRIDSIZE;
		// 			float x1 = float(i+1)*GRIDSIZE;
		// 			float y1 = float(j+1)*GRIDSIZE;
		// 			float xM = x0 + HALFGRID;
		// 			float yM = y0 + HALFGRID;
		//
		// 			if (flags & TERRAIN_FULLSQUARE)
		// 			{
		// 				if (flags & TERRAIN_IMPASSIBLE)
		// 				{
		// 					PB.Color4ub(180, 50, 80, 100);
		// 				}
		// 				else
		// 				{
		// 					if (flags & TERRAIN_DESTINATION)
		// 					{
		// 						PB.Color4ub(255, 255, 0, 100);
		// 					}
		// 					else if (flags & TERRAIN_PARKED)
		// 					{
		// 						PB.Color4ub(255, 255, 255, 100);
		// 					}
		// 					else
		// 					{
		// 						PB.Color4ub(80, 50, 180, 100);
		// 					}
		// 				}
		//
		// 				PB.Begin(PB_QUADS);
		// 				PB.Vertex3f(x0, y0, 0);
		// 				PB.Vertex3f(x1, y0, 0);
		// 				PB.Vertex3f(x1, y1, 0);
		// 				PB.Vertex3f(x0, y1, 0);
		// 				PB.End();
		// 			}
		// 			else
		// 			{
		// 				// all small terrain features is blue unless it's a destination
		// 				if (flags & TERRAIN_DESTINATION)
		// 				{
		// 					PB.Color4ub(80, 180, 50, 100);
		// 				}
		// 				else if (flags & TERRAIN_PARKED)
		// 				{
		// 					PB.Color4ub(0, 255, 255, 100);
		// 				}
		// 				else
		// 				{
		// 					PB.Color4ub(80, 50, 180, 100);
		// 				}
		// 				
		// 				PB.Begin(PB_QUADS);
		//
		// 				if (flags & TERRAIN_TOPLEFT)
		// 				{
		// 					PB.Vertex3f(x0, yM, 0);
		// 					PB.Vertex3f(xM, yM, 0);
		// 					PB.Vertex3f(xM, y1, 0);
		// 					PB.Vertex3f(x0, y1, 0);
		// 				}
		// 				if (flags & TERRAIN_TOPRIGHT)
		// 				{
		// 					PB.Vertex3f(xM, yM, 0);
		// 					PB.Vertex3f(x1, yM, 0);
		// 					PB.Vertex3f(x1, y1, 0);
		// 					PB.Vertex3f(xM, y1, 0);
		// 				}
		// 				if (flags & TERRAIN_BOTTOMLEFT)
		// 				{
		// 					PB.Vertex3f(x0, y0, 0);
		// 					PB.Vertex3f(xM, y0, 0);
		// 					PB.Vertex3f(xM, yM, 0);
		// 					PB.Vertex3f(x0, yM, 0);
		// 				}
		// 				if (flags & TERRAIN_BOTTOMRIGHT)
		// 				{
		// 					PB.Vertex3f(xM, y0, 0);
		// 					PB.Vertex3f(x1, y0, 0);
		// 					PB.Vertex3f(x1, yM, 0);
		// 					PB.Vertex3f(xM	, yM, 0);
		// 				}
		// 				PB.End();
		// 			}
		// 		}
		// 	}
		// }
	}

	void AddFootprint(uint i, uint j, FootprintInfo fpi) {
		MapGen.CQASSERT(i < nDimension && j < nDimension, "AddFootprint in MapMatrix");
		matrix[i, j].Add(fpi);
	}

	void UndoFootprint(uint i, uint j, FootprintInfo fpi) {
		MapGen.CQASSERT(i < nDimension && j < nDimension, "Undo Footprint in MapMatrix");
		matrix[i, j].Undo(fpi);
	}

	uint GetMissionFlags(int x, int y, uint missionID, uint cornerID = 0) {
		return matrix[x, y].GetMissionFlags(missionID, cornerID);
	}

	PathNode GetNode(CellRef cell) {
		if ((cell.X < (int)nDimension && cell.X >= 0) && (cell.Y < (int)nDimension && cell.Y >= 0)) {
			return matrix[cell.X, cell.Y].Node;
		}

		return null;
	}

	float GetHeuristic(CellRef src, CellRef dst) {
		int deltaX = Math.Abs(src.X - dst.X);
		int deltaY = Math.Abs(src.Y - dst.Y);

		return TerrainMap.DIAGONAL_LENGTH * (Math.Min(deltaX, deltaY)) +
		       TerrainMap.AXIAL_LENGTH * Math.Abs(deltaX - deltaY);
	}

	float GetDistance(CellRef src, CellRef dst) {
		float xdiff = src.X - dst.X;
		float ydiff = src.Y - dst.Y;
		return MathF.Sqrt(xdiff * xdiff + ydiff * ydiff);
	}

	float GetCellCost(CellRef cell, int dir) {
		// if the cell is impassible, it's cost is INFTY
		// otherwise it's the axial or diagonal length
		uint dwFlags = matrix[cell.X, cell.Y].AllFlags;

		if ((dwFlags & TerrainMap.TERRAIN_IMPASSIBLE) != 0) {
			return TerrainMap.INFTY;
		}

		return dir < 4 ? TerrainMap.AXIAL_LENGTH : TerrainMap.DIAGONAL_LENGTH;
	}

	float GetCellCostIgnoreImpassible(int dir) {
		return dir < 4 ? TerrainMap.AXIAL_LENGTH : TerrainMap.DIAGONAL_LENGTH;
	}

	bool AStarPath(GRIDVECTOR src, GRIDVECTOR dst, GRIDVECTOR[] gridpath, int numgrids) {
		QOpen.flush(openID);
		QClosed.flush(closedID);

		openID += (uint)nMaps * 2;
		closedID += (uint)nMaps * 2;

		// get a pointer to the node given by src
		CellRef srcCell = new CellRef(src.getIntX(), src.getIntY());
		CellRef dstCell = new CellRef(dst.getIntX(), dst.getIntY());

		PathNode newnode = null;

		PathNode node = GetNode(srcCell);
		node.g = 0;
		node.h = GetHeuristic(srcCell, dstCell);
		node.f = node.g + node.h;
		node.parentNode = null;

		QOpen.push(node);

		// this is the body of the search
		while (QOpen.empty() == false) {
			// take the best node off the list
			node = QOpen.pop();

			if (node.currentCell == dstCell) {
				// hurray!  we've found our path, constuct the path by following the parent pointers
				numgrids = 0;
				PathNode pnode = node;

				while (pnode is not null) {
					gridpath[numgrids].init(pnode.currentCell.X, pnode.currentCell.Y);
					gridpath[numgrids].centerpos();
					numgrids++;
					pnode = pnode.parentNode;
				}

				gridpath[0] = dst;
				gridpath[numgrids - 1] = src;

				// clean up the path with another run at a special astar function
				if (numgrids > 4) {
					AStarPathClense(gridpath, numgrids);
				}

				return true;
			}

			// find out what other nodes can be put onto the Open and Closed priority queues
			// each node can be attached to at most 8 other nodes
			int infty_bits = 0;
			for (int i = 0; i < 8; i++) {
				newnode = GetNode(node.currentCell + CellRef.gCellDirections[i]);

				// is this newnode valid?
				if (newnode == null) {
					infty_bits |= 1 << i;
					continue;
				}

				float newcost;
				if (node.currentCell == srcCell) {
					newcost = node.g + GetCellCostIgnoreImpassible(i) / 2.0f +
					          GetCellCost(newnode.currentCell, i) / 2.0f;
				} else {
					newcost = node.g + GetCellCost(node.currentCell, i) / 2.0f +
					          GetCellCost(newnode.currentCell, i) / 2.0f;
				}

				// if we have infinity, then we don't want to include it now do we?
				if (newcost >= TerrainMap.INFTY_LITE) {
					infty_bits |= 1 << i;
					continue;
				}

				// if we are looking at a spuare on the directional, then do a quick check to see if the kitty-corner squares are impassible
				if (i >= 4) {
					int test_bit1 = 1 << (i - 4);
					int test_bit2 = 1 << (i - 3) % 4;

					// if both bits were set then the diagonal is impassible
					if ((infty_bits & test_bit1) != 0 && (infty_bits & test_bit2) != 0) {
						continue;
					}
				}

				if ((QOpen.contains(newnode) || QClosed.contains(newnode)) && newnode.g <= newcost)
					continue;

				newnode.parentNode = node;
				newnode.g = newcost;
				newnode.h = GetHeuristic(newnode.currentCell, dstCell);
				newnode.f = newnode.g + newnode.h;

				// if newnode is on PClosed then remove it
				if (QClosed.contains(newnode)) {
					QClosed.remove(newnode);
				}

				// if newnode is not yet in POpen, then push it on
				if (QOpen.contains(newnode) == false) {
					QOpen.push(newnode);
				}
			}

			// push node onto closed
			QClosed.push(node);
		}

		return false;
	}
}

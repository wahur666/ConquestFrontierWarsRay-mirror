namespace MapGen.Terrain;

public class FootprintList {
	public readonly InfoList<FootprintInfo> FpInfoList;
    
    private int _count;
    public uint AllFlags;
    
    // Needed for path finding
    public PathNode Node { get; set; }
    
    public FootprintList()
    {
        FpInfoList = new InfoList<FootprintInfo>();
        _count = 0;
        AllFlags = 0;
        Node = new PathNode();
    }
    
    public void Add(FootprintInfo fpInfo) {
        FpInfoList.Add(fpInfo);
        _count++;

        AllFlags |= fpInfo.Flags;
    }

    public void Undo(FootprintInfo fpInfo)
    {
        var node = FpInfoList.Items.First;
        while (node is not null)
        {
            var next = node.Next;
            if (node.Value.MissionID == fpInfo.MissionID)
            {
                _count--;
                FpInfoList.Items.Remove(node);
            }
            node = next;
        }
        ResetFlags();
    }

    public int GetFootprintCount()
    {
        return _count;
    }

    public void ResetFlags()
    {
        AllFlags = 0;
        foreach (var info in FpInfoList.Items)
        {
            AllFlags |= info.Flags;
        }
    }

    public uint GetMissionFlags(uint missionID, uint cornerID = 0)
    {
        uint retFlag = 0;
        foreach (var info in FpInfoList.Items)
        {
            // Get all flags associated with the mission ID
            if (cornerID != 0)
            {
                if ((info.MissionID == missionID) &&
                    (info.Flags & cornerID) != 0)
                {
                    retFlag |= info.Flags;
                }
            }
            else
            {
                if (info.MissionID == missionID)
                {
                    retFlag |= info.Flags;
                }
            }
        }
        return retFlag;
    }
    
}

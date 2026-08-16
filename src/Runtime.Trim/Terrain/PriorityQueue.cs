namespace ConquestFrontierWarsRay.Runtime.Trim.Terrain;

public class PriorityQueue {
    private PriorityQueue<PathNode, float> _queue = new();
    private readonly HashSet<PathNode> _members = [];
    private uint _uniqueId;

    public void SetId(uint id)
    {
        _uniqueId = id;
    }

    public bool Empty()
    {
        return _members.Count == 0;
    }

    public void Push(PathNode pNode)
    {
        pNode.PriorityId = _uniqueId;
        _members.Add(pNode);
        _queue.Enqueue(pNode, pNode.F);
    }

    public PathNode? Pop() {
        while (_queue.Count > 0)
        {
            var pNode = _queue.Dequeue();
            if (!_members.Remove(pNode) || pNode.PriorityId != _uniqueId)
            {
                continue;
            }

            pNode.Next = null;
            pNode.Prev = null;
            pNode.PriorityId = 0;
            return pNode;
        }

        return null;
    }

    public void Flush(uint id)
    {
        _queue = new System.Collections.Generic.PriorityQueue<PathNode, float>();
        _members.Clear();
        SetId(id);
    }

    public bool Contains(PathNode pNode)
    {
        return _members.Contains(pNode) && pNode.PriorityId == _uniqueId;
    }

    public void Remove(PathNode pNode) {
	    if (!_members.Remove(pNode)) {
		    return;
	    }

	    pNode.Next = null;
	    pNode.Prev = null;
	    pNode.PriorityId = 0;
    }
}

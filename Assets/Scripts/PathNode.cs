using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode
{
    // No need to keep this data encapsulated since it will be modified by the path finder
    public Vector2Int Position;
    public int Cost = 0;
    public PathNode Parent;

    public PathNode()
    {
        Position = new Vector2Int();
        Cost = 0;
        Parent = null;
    }

    public PathNode(PathNode copy)
    {
        Position = copy.Position;
        Cost = copy.Cost;
        Parent = copy.Parent;
    }

    public PathNode(Vector2Int position, int cost, PathNode parent)
    {
        Position = position;
        Cost = cost;
        Parent = parent;
    }
}

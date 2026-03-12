using System;

public enum NodeAnchor
{
    LEFT,
    RIGHT,
    UP,
    DOWN,
    UP_LEFT,
    UP_RIGHT,
    DOWN_LEFT,
    DOWN_RIGHT
}

[Serializable]
public struct NodeConnection
{
    public int id;
    public int fromGraphId;
    public int toGraphId;

    public NodeAnchor fromAnchor;
    public NodeAnchor toAnchor;
}

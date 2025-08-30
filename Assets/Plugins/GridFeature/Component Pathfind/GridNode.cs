using UnityEngine;

public struct GridNode
{
    public GridNode(Vector2Int index) : this()
    {
        this.index = index;
        indexParent = Vector2Int.one * -1; // Multiply by -1 because there may not be a leading node for the node.
        type = TypeNode.SimplyNode;

        gCost = int.MaxValue;
        hCost = 0;
    }

    public enum TypeNode
    {
        SimplyNode = 1,
        Wall = 2,
    }

    public TypeNode type;

    public Vector2Int index;
    public Vector2Int indexParent;

    public int gCost;
    public int hCost;
    public int FCost => gCost + hCost;
}


using System.Collections.Generic;
using UnityEngine;

public class GridModel<T>
{
    public float CellSize { get; private set; }
    public Vector2 CellOffset { get; private set; }
    public Dictionary<Vector2Int, T> GridDictionary { get; private set; }
    public Dictionary<Vector2Int, GridNode> GridNodes { get; private set; }
    public Vector3 PositionOrigin { get; private set; }
    public Quaternion Rotation { get; private set; }
    public static Vector2Int[] Directions = new Vector2Int[]
    {
        new(1, 1),
        new(-1,-1),
        new(-1,1),
        new(1,-1),
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public GridModel(GridConfig gridConfig)
    {
        CellSize = gridConfig.CellSize;
        CellOffset = gridConfig.CellOffset;
        PositionOrigin = gridConfig.Position;
        Rotation = gridConfig.RotationQuaternion;
        GridDictionary = new Dictionary<Vector2Int, T>();
        GridNodes = new Dictionary<Vector2Int, GridNode>();

        var cells = gridConfig.Cells;
        foreach (var cell in cells)
        {
            GridDictionary.TryAdd(cell, default);
        }
    }
}

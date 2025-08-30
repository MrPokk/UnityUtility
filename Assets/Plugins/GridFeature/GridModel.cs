using UnityEngine;

public class GridModel<T>
{
    public Vector2Int Size { get; private set; }
    public float CellSize { get; private set; }
    public Vector2 CellOffset { get; private set; }
    public T[,] Array { get; private set; }
    public GridNode[,] GridNodes { get; private set; }
    public Vector3 PositionOrigin { get; private set; }

    public GridModel(GridConfig gridConfig)
    {
        Size = gridConfig.size;
        CellSize = gridConfig.cellSize;
        CellOffset = gridConfig.cellOffset;
        PositionOrigin = gridConfig.position;

        Array = new T[Size.x, Size.y];
        GridNodes = new GridNode[Size.x, Size.y];

        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                Array[x, y] = default;
            }
        }
    }
}

using UnityEngine;

public partial class AStar
{
    private static bool IsWithinGrid<T>(Vector2Int indexNode, in T[,] grid)
    {
        return indexNode.x >= 0 && indexNode.x < grid.GetLength(0) && indexNode.y >= 0 && indexNode.y < grid.GetLength(1);
    }

    private static ref T GetNodeByIndex<T>(Vector2Int index, in T[,] grid)
    {
        return ref grid[index.x, index.y];
    }
}


using UnityEngine;

public partial class AStar
{
    private static bool ValidatePath<T>(Vector2Int start, Vector2Int end, T[,] gridNodes)
    {
        if (gridNodes == null || start == end)
        {
            return false;
        }
        if (!IsWithinGrid(start, gridNodes) || !IsWithinGrid(end, gridNodes))
        {
            return false;
        }

        return true;
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class AStar
{
    public static List<Vector2Int> TryGetPathFindNearest<T>(
        Dictionary<Vector2Int, T> gridDict,
        Vector2Int start,
        Vector2Int end,
        in Vector2Int[] allNeighborOffsets,
        out Vector2Int nearestReachableNode,
        Predicate<Vector2Int> nodeCondition = null
    )
    {
        var gridNodeDict = ConvertDictionaryToGridNode(gridDict);
        return TryGetPathFindNearest(gridNodeDict, start, end, allNeighborOffsets, out nearestReachableNode, nodeCondition);
    }

    public static List<Vector2Int> TryGetPathFindNearest(
        Dictionary<Vector2Int, GridNode> gridDict,
        Vector2Int start,
        Vector2Int end,
        in Vector2Int[] allNeighborOffsets,
        out Vector2Int nearestReachableNode,
        Predicate<Vector2Int> nodeCondition = null
    )
    {
        var pathfinder = new AStar();
        pathfinder._gridDict = gridDict;

        var validNeighbors = pathfinder.GetValidNeighbors(
            end,
            allNeighborOffsets,
            nodeCondition);

        if (validNeighbors.Count == 0)
        {
            nearestReachableNode = Vector2Int.one * -1;
            return null;
        }

        var isNearestReachableNode = pathfinder.FindNearestToStart(start, validNeighbors);
        if (isNearestReachableNode != null)
        {
            nearestReachableNode = isNearestReachableNode.Value;
            return TryGetPathFind(gridDict, start, nearestReachableNode, allNeighborOffsets);
        }

        nearestReachableNode = Vector2Int.one * -1;
        return null;
    }

    public static List<Vector2Int> TryGetPathFindNearest<T>(
        T[,] grid,
        Vector2Int start,
        Vector2Int end,
        in Vector2Int[] allNeighborOffsets,
        out Vector2Int nearestReachableNode,
        GridNode[,] gridNode = null,
        Predicate<Vector2Int> nodeCondition = null
    )
    {
        Dictionary<Vector2Int, GridNode> gridDict;
        if (gridNode != null)
        {
            gridDict = ConvertGridNodeArrayToDictionary(gridNode);
        }
        else
        {
            gridDict = ConvertArrayToDictionary(grid);
        }

        return TryGetPathFindNearest(gridDict, start, end, allNeighborOffsets, out nearestReachableNode, nodeCondition);
    }

    private List<Vector2Int> GetValidNeighbors(
        Vector2Int centerPosition,
        in Vector2Int[] neighborOffsets,
        Predicate<Vector2Int> condition
    )
    {
        var validNodes = new List<Vector2Int>();

        if (IsWithinGrid(centerPosition) && IsNodeWalkable(centerPosition) &&
            (condition == null || condition(centerPosition)))
        {
            validNodes.Add(centerPosition);
        }

        foreach (var offset in neighborOffsets)
        {
            var neighborPosition = centerPosition + offset;

            if (!IsWithinGrid(neighborPosition) || !IsNodeWalkable(neighborPosition))
            {
                continue;
            }
            if (condition != null && !condition(neighborPosition))
            {
                continue;
            }

            validNodes.Add(neighborPosition);
        }

        return validNodes;
    }

    private Vector2Int? FindNearestToStart(Vector2Int start, List<Vector2Int> potentialNodes)
    {
        if (potentialNodes.Count == 0)
        {
            return null;
        }
        return potentialNodes
            .OrderBy(node => CalculateHCost(start, node))
            .First();
    }
}

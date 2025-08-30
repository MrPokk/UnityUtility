using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class AStar
{
    private HashSet<Vector2Int> _open;
    private HashSet<Vector2Int> _close;
    private GridNode _current;
    private GridNode _endNode;
    private GridNode _startNode;
    private GridNode[,] _grid;

    public static List<Vector2Int> TryGetPathFind<T>(T[,] grid, Vector2Int start, Vector2Int end, in Vector2Int[] neighborsAll, GridNode[,] gridNode = null)
    {
        if (gridNode == null)
        {
            return ValidatePath(start, end, grid) ? new AStar().Find(start, end,
            new(grid.GetLength(0), grid.GetLength(1)), neighborsAll, gridNode) : null;
        }
        else
        {
            return ValidatePath(start, end, gridNode) ? new AStar().Find(start, end,
            new(gridNode.GetLength(0), gridNode.GetLength(1)), neighborsAll, gridNode) : null;
        }
    }

    private List<Vector2Int> Find(Vector2Int startNode, Vector2Int endNode, Vector2Int gridSize, in Vector2Int[] neighborsAll, GridNode[,] gridNode = null)
    {
        if (gridNode == null)
        {
            SetupGrid(gridSize);
        }
        else
        {
            SetupGrid(gridNode);
        }

        _open = new HashSet<Vector2Int>();
        _close = new HashSet<Vector2Int>();

        _startNode = _grid[startNode.x, startNode.y];
        _startNode.gCost = 0;
        _startNode.hCost = CalculateHCost(startNode, endNode);

        _grid[startNode.x, startNode.y] = _startNode;

        _open.Add(_startNode.index);

        _endNode = _grid[endNode.x, endNode.y];
        _grid[endNode.x, endNode.y] = _endNode;

        while (_open.Count > 0)
        {
            _current = GetLowerFCost(_open);
            _grid[_current.index.x, _current.index.y] = _current;

            _open.Remove(_current.index);
            _close.Add(_current.index);

            if (_current.index == _endNode.index)
            {
                return GetPath(_endNode.index);
            }

            var neighbors = GetIndexNeighbors(_current.index, neighborsAll);
            foreach (var neighbor in neighbors)
            {

                var neighborNode = _grid[neighbor.x, neighbor.y];

                var tentativeGCost = _current.gCost + CalculateHCost(_current.index, neighbor);
                if (tentativeGCost < neighborNode.gCost)
                {
                    neighborNode.indexParent = _current.index;
                    neighborNode.gCost = tentativeGCost;
                    neighborNode.hCost = CalculateHCost(neighborNode.index, _endNode.index);

                    _grid[neighborNode.index.x, neighborNode.index.y] = neighborNode;

                    _open.Add(neighborNode.index);
                }
            }
        }

        return null;
    }

    private int CalculateHCost(Vector2Int startNode, Vector2Int endNode)
    {
        return Mathf.Abs(startNode.x - endNode.x) + Mathf.Abs(startNode.y - endNode.y);
    }

    private List<Vector2Int> GetPath(Vector2Int endNodeIndex)
    {
        var path = new List<Vector2Int>();

        var currentNode = GetNodeByIndex(endNodeIndex, _grid);
        path.Add(currentNode.index);

        while (currentNode.indexParent != Vector2Int.one * -1)
        {
            var cameFromNode = GetNodeByIndex(currentNode.indexParent, _grid);
            path.Add(cameFromNode.index);
            currentNode = cameFromNode;
        }

        path.Reverse();

        return path;
    }

    private List<Vector2Int> GetIndexNeighbors(Vector2Int currentNodeIndex, in Vector2Int[] neighborsAll)
    {
        var neighborAll = new List<Vector2Int>();
        foreach (var neighborsOffset in neighborsAll)
        {
            var neighborIndex = currentNodeIndex + neighborsOffset;

            if (IsWithinGrid(neighborIndex, _grid) && IsNodeWalkable(neighborIndex, _grid))
            {
                neighborAll.Add(neighborIndex);
            }
        }

        return neighborAll;
    }

    private bool IsNodeWalkable(Vector2Int indexNode, GridNode[,] grid)
    {
        var gridNode = GetNodeByIndex(indexNode, grid);
        if (gridNode.type == GridNode.TypeNode.SimplyNode)
        {
            return true;
        }
        else if (gridNode.type == GridNode.TypeNode.Wall)
        {
            return false;
        }

        return false;
    }

    private GridNode GetLowerFCost(HashSet<Vector2Int> nodeArray)
    {
        var indexLowerNode = GetNodeByIndex(nodeArray.First(), _grid);
        foreach (var nodeIndex in nodeArray)
        {
            var nodeElement = GetNodeByIndex(nodeIndex, _grid);

            if (nodeElement.FCost < indexLowerNode.FCost)
            {
                indexLowerNode = nodeElement;
            }
        }

        return indexLowerNode;
    }

    private void SetupGrid(GridNode[,] gridSize)
    {
        _grid = (GridNode[,])gridSize.Clone();
    }

    private void SetupGrid(Vector2Int gridSize)
    {
        _grid = new GridNode[gridSize.x, gridSize.y];
        for (int i = 0; i < gridSize.x; i++)
        {
            for (int j = 0; j < gridSize.y; j++)
            {
                _grid[i, j] = new GridNode(new Vector2Int(i, j));
            }
        }
    }

    private void SetupGrid<T>(T[,] grid)
    {
        var gridSize = new Vector2Int(grid.GetLength(0), grid.GetLength(1));
        _grid = new GridNode[gridSize.x, gridSize.y];
        for (int i = 0; i < gridSize.x; i++)
        {
            for (int j = 0; j < gridSize.y; j++)
            {
                _grid[i, j] = new GridNode(new Vector2Int(i, j));
            }
        }
    }
}


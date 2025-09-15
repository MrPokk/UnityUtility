using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GridNode;

public partial class AStar
{
    private HashSet<Vector2Int> _open;
    private HashSet<Vector2Int> _close;
    private GridNode _current;
    private GridNode _endNode;
    private GridNode _startNode;
    private Dictionary<Vector2Int, GridNode> _gridDict;

    public static List<Vector2Int> TryGetPathFind<T>(Dictionary<Vector2Int, T> gridDict, Vector2Int start, Vector2Int end, in Vector2Int[] neighborsAll)
    {
        var gridDictConvert = ConvertDictionaryToGridNode(gridDict);
        return ValidatePath(start, end, gridDictConvert) ? new AStar().Find(start, end, neighborsAll, gridDictConvert) : null;
    }

    public static List<Vector2Int> TryGetPathFind(Dictionary<Vector2Int, GridNode> gridDict, Vector2Int start, Vector2Int end, in Vector2Int[] neighborsAll)
    {
        return ValidatePath(start, end, gridDict) ? new AStar().Find(start, end, neighborsAll, gridDict) : null;
    }

    public static List<Vector2Int> TryGetPathFind<T>(T[,] grid, Vector2Int start, Vector2Int end, in Vector2Int[] neighborsAll, GridNode[,] gridNode = null)
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

        return TryGetPathFind(gridDict, start, end, neighborsAll);
    }

    private static Dictionary<Vector2Int, GridNode> ConvertDictionaryToGridNode<T>(Dictionary<Vector2Int, T> gridDict)
    {
        var dict = new Dictionary<Vector2Int, GridNode>();
        foreach (var item in gridDict)
        {
            dict[item.Key] = new GridNode(item.Key) { type = TypeNode.SimplyNode };
        }
        return dict;
    }

    private static Dictionary<Vector2Int, GridNode> ConvertArrayToDictionary<T>(T[,] grid)
    {
        var dict = new Dictionary<Vector2Int, GridNode>();
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var index = new Vector2Int(x, y);
                dict[index] = new GridNode(index) { type = TypeNode.SimplyNode };
            }
        }
        return dict;
    }

    private static Dictionary<Vector2Int, GridNode> ConvertGridNodeArrayToDictionary(GridNode[,] gridNode)
    {
        var dict = new Dictionary<Vector2Int, GridNode>();
        int width = gridNode.GetLength(0);
        int height = gridNode.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var index = new Vector2Int(x, y);
                dict[index] = gridNode[x, y];
            }
        }
        return dict;
    }

    private static bool ValidatePath(Vector2Int start, Vector2Int end, Dictionary<Vector2Int, GridNode> gridDict)
    {
        if (gridDict == null || start == end)
        {
            return false;
        }
        if (!gridDict.ContainsKey(start) || !gridDict.ContainsKey(end))
        {
            return false;
        }

        return true;
    }

    private List<Vector2Int> Find(Vector2Int startNode, Vector2Int endNode, in Vector2Int[] neighborsAll, Dictionary<Vector2Int, GridNode> gridDict)
    {
        _gridDict = gridDict;

        _open = new HashSet<Vector2Int>();
        _close = new HashSet<Vector2Int>();

        if (!_gridDict.TryGetValue(startNode, out _startNode))
        {
            return null;
        }
        _startNode.gCost = 0;
        _startNode.hCost = CalculateHCost(startNode, endNode);
        _gridDict[startNode] = _startNode;

        _open.Add(_startNode.index);

        if (!_gridDict.TryGetValue(endNode, out _endNode))
        {
            return null;
        }
        _gridDict[endNode] = _endNode;

        while (_open.Count > 0)
        {
            _current = GetLowerFCost(_open);
            _gridDict[_current.index] = _current;

            _open.Remove(_current.index);
            _close.Add(_current.index);

            if (_current.index == _endNode.index)
            {
                return GetPath(_endNode.index);
            }

            var neighbors = GetIndexNeighbors(_current.index, neighborsAll);
            foreach (var neighbor in neighbors)
            {
                if (!_gridDict.TryGetValue(neighbor, out var neighborNode))
                {
                    continue;
                }

                var tentativeGCost = _current.gCost + CalculateHCost(_current.index, neighbor);
                if (tentativeGCost < neighborNode.gCost)
                {
                    neighborNode.indexParent = _current.index;
                    neighborNode.gCost = tentativeGCost;
                    neighborNode.hCost = CalculateHCost(neighborNode.index, _endNode.index);

                    _gridDict[neighborNode.index] = neighborNode;

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

        if (!_gridDict.TryGetValue(endNodeIndex, out var currentNode))
        {
            return path;
        }
        path.Add(currentNode.index);

        while (currentNode.indexParent != Vector2Int.one * -1)
        {
            if (!_gridDict.TryGetValue(currentNode.indexParent, out var cameFromNode))
            {
                break;
            }
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

            if (IsWithinGrid(neighborIndex) && IsNodeWalkable(neighborIndex))
            {
                neighborAll.Add(neighborIndex);
            }
        }

        return neighborAll;
    }

    private bool IsNodeWalkable(Vector2Int indexNode)
    {
        if (!_gridDict.TryGetValue(indexNode, out var gridNode))
        {
            return false;
        }

        return gridNode.type == TypeNode.SimplyNode;
    }

    private bool IsWithinGrid(Vector2Int indexNode)
    {
        return _gridDict.ContainsKey(indexNode);
    }

    private GridNode GetLowerFCost(HashSet<Vector2Int> nodeArray)
    {
        var indexLowerNode = _gridDict[nodeArray.First()];
        foreach (var nodeIndex in nodeArray)
        {
            var nodeElement = _gridDict[nodeIndex];

            if (nodeElement.FCost < indexLowerNode.FCost)
            {
                indexLowerNode = nodeElement;
            }
        }

        return indexLowerNode;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridPresenter<T>
{
    protected GridModel<T> _grid;
    protected GridView _gridView;

    public GridPresenter(GridConfig gridConfig)
    {
        _grid = new GridModel<T>(gridConfig);
        _gridView = new GameObject("GridView").AddComponent<GridView>();
        _gridView.Instantiate(gridConfig);
    }

    public void CreateSearchNodes()
    {
        var size = _grid.Size;
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                _grid.GridNodes[x, y] = new GridNode();
            }
        }
    }

    public Vector3 GetWorldPosition(Vector2Int index)
    {
        var positionOrigin = _grid.PositionOrigin;
        var totalCellSize = new Vector2(_grid.CellSize, _grid.CellSize) + _grid.CellOffset;

        var localPosition = new Vector3(index.x * totalCellSize.x, index.y * totalCellSize.y, 0);

        return _grid.Rotation * localPosition + positionOrigin;
    }

    public Vector2Int GetGridIndex(Vector3 worldPosition)
    {
        var positionOrigin = _grid.PositionOrigin;
        var totalCellSize = new Vector2(_grid.CellSize, _grid.CellSize) + _grid.CellOffset;

        var localPosition = Quaternion.Inverse(_grid.Rotation) * (worldPosition - positionOrigin);

        var x = Mathf.FloorToInt(localPosition.x / totalCellSize.x);
        var y = Mathf.FloorToInt(localPosition.y / totalCellSize.y);

        return new Vector2Int(x, y);
    }

    public Vector2 GetTotalCellSize()
    {
        return new Vector2(_grid.CellSize, _grid.CellSize) + _grid.CellOffset;
    }

    public T[,] GetArray()
    {
        return _grid.Array;
    }

    public GridNode[,] GetGridNodes()
    {
        return _grid.GridNodes;
    }

    public bool TryGetPositionInGrid(Vector2Int indexNode, out Vector3 positionValue)
    {
        if (IsWithinGrid(indexNode))
        {
            positionValue = GetWorldPosition(indexNode);
            return true;
        }

        positionValue = Vector3.negativeInfinity;
        return false;
    }

    public bool TryGetPositionInGrid(Vector3 objectPosition, out Vector2Int positionValue)
    {
        positionValue = GetGridIndex(objectPosition);
        return IsWithinGrid(positionValue);
    }

    public void SetValueInGrid(Vector2Int index, T value) => _grid.Array[index.x, index.y] = value;

    public bool TrySetValue(Vector3 index, T value)
    {
        return TrySetValue(GetGridIndex(index), value);
    }

    public bool TrySetValue(Vector2Int index, T value)
    {
        if (!IsWithinGrid(index)) return false;

        TrySetValue(index, value);
        return true;
    }

    public T GetByIndex(Vector2Int index)
    {
        return _grid.Array[index.x, index.y];
    }

    public bool TryGetValue(Vector2Int index, out T value)
    {
        value = default;
        if (!IsWithinGrid(index)) return false;

        value = _grid.Array[index.x, index.y];
        return true;
    }

    public IEnumerable<Vector2Int> FindAll(Func<T, bool> predicate)
    {
        for (int x = 0; x < _grid.Size.x; x++)
        {
            for (int y = 0; y < _grid.Size.y; y++)
            {
                if (predicate(_grid.Array[x, y]))
                    yield return new Vector2Int(x, y);
            }
        }
    }

    public Vector2Int? FindFirst(Func<T, bool> predicate)
    {
        return FindAll(predicate).FirstOrDefault();
    }

    public Vector3 ConvertingPosition(Vector2Int index)
    {
        return GetWorldPosition(index) + _grid.Rotation * new Vector3(_grid.CellSize, _grid.CellSize, 0) * 0.5f;
    }

    public Vector2Int ConvertingPosition(Vector3 worldPose)
    {
        return GetGridIndex(worldPose);
    }

    public bool IsWithinGrid(Vector2Int indexNode)
    {
        return indexNode.x >= 0 && indexNode.x < _grid.Size.x && indexNode.y >= 0 && indexNode.y < _grid.Size.y;
    }

    public void FillArea(Vector2Int pointA, Vector2Int pointB, T value)
    {
        var area = GetRectangularArea(pointA, pointB);
        foreach (var index in area.Keys)
        {
            TrySetValue(index, value);
        }
    }

    public void ClearArea(Vector2Int pointA, Vector2Int pointB)
    {
        FillArea(pointA, pointB, default);
    }

    public HashSet<Vector2Int> GetNeighbors(Vector2Int index, Vector2Int[] neighbors, Func<T, bool> filterCondition = null)
    {
        var result = new HashSet<Vector2Int>();
        for (int i = 0; i < neighbors.Length; i++)
        {
            var direction = index + neighbors[i];
            if (TryGetValue(direction, out var value) && (filterCondition == null || filterCondition(value)))
            {
                result.Add(direction);
            }
        }

        return result;
    }

    public HashSet<Vector2Int> GetSquaredNeighbors(Vector2Int index, Func<T, bool> filterCondition = null) => GetNeighbors(index,
    GridModel<T>.Directions, filterCondition);

    public Vector2Int? GetRandomPoint(IList<Vector2Int> includePoints)
    {
        return includePoints.Count > 0 ? includePoints[UnityEngine.Random.Range(0, includePoints.Count)] : null;
    }

    public Dictionary<Vector2Int, T> GetRectangularArea(Vector2Int pointA, Vector2Int pointB, Func<T, bool> filterCondition = null)
    {
        var minX = Mathf.Min(pointA.x, pointB.x);
        var maxX = Mathf.Max(pointA.x, pointB.x);
        var minY = Mathf.Min(pointA.y, pointB.y);
        var maxY = Mathf.Max(pointA.y, pointB.y);

        minX = Mathf.Max(minX, 0);
        maxX = Mathf.Min(maxX, _grid.Size.x - 1);
        minY = Mathf.Max(minY, 0);
        maxY = Mathf.Min(maxY, _grid.Size.y - 1);

        var cells = new Dictionary<Vector2Int, T>(maxX * maxY);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var currentIndex = new Vector2Int(x, y);
                var cellValue = _grid.Array[x, y];

                if (filterCondition == null || filterCondition(cellValue))
                {
                    cells[currentIndex] = cellValue;
                }
            }
        }

        return cells;
    }

    public Dictionary<Vector2Int, T> GetCellsInLine(Vector2Int start, Vector2Int end, Func<T, bool> isEmptyCondition = null)
    {
        var dx = end.x - start.x;
        var dy = end.y - start.y;
        var steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

        var xIncrement = dx / (float)steps;
        var yIncrement = dy / (float)steps;

        float x = start.x;
        float y = start.y;

        var cells = new Dictionary<Vector2Int, T>(dx + dy);

        for (int i = 0; i <= steps; i++)
        {
            var gridPoint = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
            var cellValue = _grid.Array[gridPoint.x, gridPoint.y];

            if (IsWithinGrid(gridPoint) && (isEmptyCondition == null || isEmptyCondition(cellValue)))
            {
                cells[gridPoint] = cellValue;
            }

            x += xIncrement;
            y += yIncrement;
        }

        return cells;
    }

    public Dictionary<Vector2Int, T> GetCellsInRow(int rowIndex, Func<T, bool> filterCondition = null)
    {
        if (rowIndex < 0 || rowIndex >= _grid.Size.y)
        {
            Debug.LogWarning($"Row index {rowIndex} is out of grid bounds (0-{_grid.Size.y - 1})");
            return null;
        }

        var cells = GetRectangularArea(
            new Vector2Int(0, rowIndex),
            new Vector2Int(_grid.Size.x - 1, rowIndex),
            filterCondition
        );

        return cells;
    }

    public Dictionary<Vector2Int, T> GetCellsInColumn(int columnIndex, Func<T, bool> filterCondition = null)
    {
        if (columnIndex < 0 || columnIndex >= _grid.Size.x)
        {
            Debug.LogWarning($"Column index {columnIndex} is out of grid bounds (0-{_grid.Size.x - 1})");
            return null;
        }

        var cells = GetRectangularArea(
            new Vector2Int(columnIndex, 0),
            new Vector2Int(columnIndex, _grid.Size.y - 1),
            filterCondition
        );

        return cells;
    }

    public Vector2Int? FindNearestExpanding(Vector2Int startIndex, Func<T, bool> predicate, int maxRadius = 0)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        if (!IsWithinGrid(startIndex))
            return null;

        if (TryGetValue(startIndex, out var startValue) && predicate(startValue))
            return startIndex;

        for (int radius = 1; radius <= (maxRadius > 0 ? maxRadius : Mathf.Max(_grid.Size.x, _grid.Size.y)); radius++)
        {
            var foundPoint = SearchInRing(startIndex, radius, predicate);
            if (foundPoint.HasValue)
                return foundPoint;

            if (maxRadius > 0 && radius >= maxRadius)
                break;
        }

        return null;
    }

    public Vector2Int? FindNearestExpanding(Vector3 worldPosition, Func<T, bool> predicate, int maxRadius = 0)
    {
        var startIndex = GetGridIndex(worldPosition);
        return FindNearestExpanding(startIndex, predicate, maxRadius);
    }

    public HashSet<Vector2Int> FindAllExpanding(Vector2Int startIndex, Func<T, bool> predicate, int maxRadius = 0)
    {
        var results = new HashSet<Vector2Int>();

        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        if (!IsWithinGrid(startIndex))
            return results;

        var searchRadius = maxRadius > 0 ? maxRadius : Mathf.Max(_grid.Size.x, _grid.Size.y);

        for (int radius = 0; radius <= searchRadius; radius++)
        {
            SearchInRadius(startIndex, radius, predicate, results);

            if (maxRadius > 0 && radius >= maxRadius)
                break;
        }

        return results;
    }

    public Vector2Int? FindNearestWithWorldDistance(Vector3 worldPosition, Func<T, bool> predicate, int maxSearchRadius = 10)
    {
        var startIndex = GetGridIndex(worldPosition);
        var candidates = FindAllExpanding(startIndex, predicate, maxSearchRadius);

        if (candidates.Count == 0)
            return null;

        Vector2Int? nearest = null;
        var minDistance = float.MaxValue;

        foreach (var candidate in candidates)
        {
            var candidateWorldPos = GetWorldPosition(candidate);
            var distance = Vector3.Distance(worldPosition, candidateWorldPos);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private Vector2Int? SearchInRing(Vector2Int center, int radius, Func<T, bool> predicate)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (Mathf.Abs(x) < radius && Mathf.Abs(y) < radius)
                    continue;

                var checkIndex = new Vector2Int(center.x + x, center.y + y);

                if (IsWithinGrid(checkIndex) &&
                    TryGetValue(checkIndex, out var value) &&
                    predicate(value))
                {
                    return checkIndex;
                }
            }
        }

        return null;
    }

    private void SearchInRadius(Vector2Int center, int radius, Func<T, bool> predicate, HashSet<Vector2Int> results)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) > radius)
                    continue;

                var checkIndex = new Vector2Int(center.x + x, center.y + y);

                if (IsWithinGrid(checkIndex) &&
                    !results.Contains(checkIndex) &&
                    TryGetValue(checkIndex, out var value) &&
                    predicate(value))
                {
                    results.Add(checkIndex);
                }
            }
        }
    }

}

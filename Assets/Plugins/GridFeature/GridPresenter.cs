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
        if (gridConfig.NodePrefab != null)
        {
            _gridView = new GameObject("GridView").AddComponent<GridView>();
            _gridView.Instantiate(gridConfig);
        }
    }

    protected Vector3 GetWorldPosition(Vector2Int index)
    {
        var positionOrigin = _grid.PositionOrigin;
        var totalCellSize = new Vector2(_grid.CellSize, _grid.CellSize) + _grid.CellOffset;

        var localPosition = new Vector3(index.x * totalCellSize.x, index.y * totalCellSize.y, 0);

        return _grid.Rotation * localPosition + positionOrigin;
    }

    protected Vector2Int GetGridIndex(Vector3 worldPosition)
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

    public Dictionary<Vector2Int, T> GetGridDictionary()
    {
        return _grid.GridDictionary;
    }

    public Dictionary<Vector2Int, GridNode> GetGridNodes()
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

    public void SetValueInGrid(Vector2Int index, T value) => _grid.GridDictionary[index] = value;

    public bool TrySetValue(Vector3 worldPosition, T value)
    {
        return TrySetValue(GetGridIndex(worldPosition), value);
    }

    public bool TrySetValue(Vector2Int index, T value)
    {
        if (!IsWithinGrid(index)) return false;

        SetValueInGrid(index, value);
        return true;
    }

    public T GetByIndex(Vector2Int index)
    {
        return _grid.GridDictionary.ContainsKey(index) ? _grid.GridDictionary[index] : default;
    }

    public bool TryGetValue(Vector2Int index, out T value)
    {
        return _grid.GridDictionary.TryGetValue(index, out value);
    }

    public IEnumerable<Vector2Int> FindAll(Func<T, bool> predicate)
    {
        foreach (var kvp in _grid.GridDictionary)
        {
            if (predicate(kvp.Value))
                yield return kvp.Key;
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
        return _grid.GridDictionary.ContainsKey(indexNode);
    }

    public void AddGridCell(Vector2Int index, T value = default)
    {
        if (!_grid.GridDictionary.ContainsKey(index))
        {
            _grid.GridDictionary.Add(index, value);
        }
    }

    public bool RemoveGridCell(Vector2Int index)
    {
        return _grid.GridDictionary.Remove(index);
    }

    public void FillArea(Vector2Int pointA, Vector2Int pointB, T value)
    {
        var area = GetRectangularArea(pointA, pointB);
        foreach (var index in area.Keys)
        {
            SetValueInGrid(index, value);
        }
    }

    public void ClearArea(Vector2Int pointA, Vector2Int pointB)
    {
        var area = GetRectangularArea(pointA, pointB);
        foreach (var index in area.Keys)
        {
            _grid.GridDictionary.Remove(index);
        }
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

    public Vector2Int GetRandomPoint(IList<Vector2Int> includePoints)
    {
        return includePoints.Count > 0 ? includePoints[UnityEngine.Random.Range(0, includePoints.Count)] : Vector2Int.one * -1;
    }
    public Vector2Int GetRandomPoint(Func<T, bool> filterCondition = null)
    {
        return GetRandomPoint(GetPointsConditions(filterCondition));
    }

    private List<Vector2Int> GetPointsConditions(Func<T, bool> filterCondition)
    {
        return _grid.GridDictionary.Keys.Where(key => filterCondition == null || filterCondition(_grid.GridDictionary[key])).ToList();
    }

    public Dictionary<Vector2Int, T> GetRectangularArea(Vector2Int pointA, Vector2Int pointB, Func<T, bool> filterCondition = null)
    {
        var minX = Mathf.Min(pointA.x, pointB.x);
        var maxX = Mathf.Max(pointA.x, pointB.x);
        var minY = Mathf.Min(pointA.y, pointB.y);
        var maxY = Mathf.Max(pointA.y, pointB.y);

        var cells = new Dictionary<Vector2Int, T>();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var currentIndex = new Vector2Int(x, y);
                if (_grid.GridDictionary.TryGetValue(currentIndex, out var cellValue))
                {
                    if (filterCondition == null || filterCondition(cellValue))
                    {
                        cells[currentIndex] = cellValue;
                    }
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

        var cells = new Dictionary<Vector2Int, T>();

        for (int i = 0; i <= steps; i++)
        {
            var gridPoint = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
            if (_grid.GridDictionary.TryGetValue(gridPoint, out var cellValue))
            {
                if (isEmptyCondition == null || isEmptyCondition(cellValue))
                {
                    cells[gridPoint] = cellValue;
                }
            }

            x += xIncrement;
            y += yIncrement;
        }

        return cells;
    }

    public Dictionary<Vector2Int, T> GetCellsInRow(int rowIndex, Func<T, bool> filterCondition = null)
    {
        var cells = new Dictionary<Vector2Int, T>();

        foreach (var kvp in _grid.GridDictionary)
        {
            if (kvp.Key.y == rowIndex && (filterCondition == null || filterCondition(kvp.Value)))
            {
                cells[kvp.Key] = kvp.Value;
            }
        }

        return cells;
    }

    public Dictionary<Vector2Int, T> GetCellsInColumn(int columnIndex, Func<T, bool> filterCondition = null)
    {
        var cells = new Dictionary<Vector2Int, T>();

        foreach (var kvp in _grid.GridDictionary)
        {
            if (kvp.Key.x == columnIndex && (filterCondition == null || filterCondition(kvp.Value)))
            {
                cells[kvp.Key] = kvp.Value;
            }
        }

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

        // Calculate max search radius based on grid bounds if not specified
        if (maxRadius <= 0)
        {
            var bounds = GetGridBounds();
            maxRadius = Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.y));
        }

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            var foundPoint = SearchInRing(startIndex, radius, predicate);
            if (foundPoint.HasValue)
                return foundPoint;
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

        // Calculate max search radius based on grid bounds if not specified
        if (maxRadius <= 0)
        {
            var bounds = GetGridBounds();
            maxRadius = Mathf.CeilToInt(Mathf.Max(bounds.size.x, bounds.size.y));
        }

        for (int radius = 0; radius <= maxRadius; radius++)
        {
            SearchInRadius(startIndex, radius, predicate, results);
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

    public Bounds GetGridBounds()
    {
        if (_grid.GridDictionary.Count == 0)
            return new Bounds(Vector3.zero, Vector3.zero);

        var minX = int.MaxValue;
        var maxX = int.MinValue;
        var minY = int.MaxValue;
        var maxY = int.MinValue;

        foreach (var index in _grid.GridDictionary.Keys)
        {
            minX = Mathf.Min(minX, index.x);
            maxX = Mathf.Max(maxX, index.x);
            minY = Mathf.Min(minY, index.y);
            maxY = Mathf.Max(maxY, index.y);
        }

        var cellSize = GetTotalCellSize();
        var width = (maxX - minX + 1) * cellSize.x;
        var height = (maxY - minY + 1) * cellSize.y;

        var center = GetWorldPosition(new Vector2Int((minX + maxX) / 2, (minY + maxY) / 2));

        return new Bounds(center, new Vector3(width, height, 0));
    }

    public void ForEach(Action<Vector2Int, T> action)
    {
        foreach (var kvp in _grid.GridDictionary)
        {
            action(kvp.Key, kvp.Value);
        }
    }

    public void Clear()
    {
        _grid.GridDictionary.Clear();
    }

    public int Count => _grid.GridDictionary.Count;

    public bool IsEmpty => _grid.GridDictionary.Count == 0;
}

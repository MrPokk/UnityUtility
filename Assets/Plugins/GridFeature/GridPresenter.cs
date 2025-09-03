using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridPresenter<T>
{
    private readonly GridModel<T> _grid;
    private readonly GridView _gridView;

    public GridPresenter(GridConfig gridConfig)
    {
        _grid = new GridModel<T>(gridConfig);
        _gridView = new GameObject("GridView").AddComponent<GridView>();
        _gridView.Instantiate(gridConfig);
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
            positionValue = GetWorldPosition(indexNode) + new Vector3(_grid.CellSize, _grid.CellSize, 0) * 0.5f;
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

    public void SetValueInGrid(Vector2Int index, T value)
    {
        if (IsWithinGrid(index))
        {
            _grid.Array[index.x, index.y] = value;
        }
    }

    public T GetNodeByIndex(Vector2Int index)
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

    public bool TrySetValue(Vector2Int index, T value)
    {
        if (!IsWithinGrid(index)) return false;

        SetValueInGrid(index, value);
        return true;
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
            SetValueInGrid(index, value);
        }
    }

    public void ClearArea(Vector2Int pointA, Vector2Int pointB)
    {
        FillArea(pointA, pointB, default);
    }

    public IReadOnlyCollection<Vector2Int> GetNeighbors(Vector2Int index, Vector2Int[] neighbors, Func<T, bool> filterCondition = null)
    {
        var result = new List<Vector2Int>();
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

    public Vector2Int? GetRandomPoint(IList<Vector2Int> excludePoints)
    {
        return excludePoints.Count > 0 ? excludePoints[UnityEngine.Random.Range(0, excludePoints.Count)] : null;
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

        var areaData = new Dictionary<Vector2Int, T>(maxX * maxY);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var currentIndex = new Vector2Int(x, y);
                var value = _grid.Array[x, y];

                if (filterCondition == null || filterCondition(value))
                {
                    areaData[currentIndex] = value;
                }
            }
        }

        return areaData;
    }

    public List<Vector2Int> GetEmptyCellsInLine(Vector2Int start, Vector2Int end, Func<T, bool> isEmptyCondition = null)
    {
        var emptyCells = new List<Vector2Int>();

        var dx = end.x - start.x;
        var dy = end.y - start.y;
        var steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

        var xIncrement = dx / (float)steps;
        var yIncrement = dy / (float)steps;

        float x = start.x;
        float y = start.y;

        for (int i = 0; i <= steps; i++)
        {
            var gridPoint = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));

            if (IsWithinGrid(gridPoint) && (isEmptyCondition == null || isEmptyCondition(_grid.Array[gridPoint.x, gridPoint.y])))
            {
                emptyCells.Add(gridPoint);
            }

            x += xIncrement;
            y += yIncrement;
        }

        return emptyCells;
    }

    public List<Vector2Int> GetEmptyCellsInRow(int rowIndex, Func<T, bool> isEmptyCondition = null)
    {
        var emptyCells = new List<Vector2Int>();

        if (rowIndex < 0 || rowIndex >= _grid.Size.y)
        {
            Debug.LogWarning($"Row index {rowIndex} is out of grid bounds (0-{_grid.Size.y - 1})");
            return emptyCells;
        }

        for (int x = 0; x < _grid.Size.x; x++)
        {
            var cellIndex = new Vector2Int(x, rowIndex);
            var cellValue = _grid.Array[x, rowIndex];

            bool isDefaultValue = EqualityComparer<T>.Default.Equals(cellValue, default(T));

            if ((isEmptyCondition == null && isDefaultValue) ||
                (isEmptyCondition != null && isEmptyCondition(cellValue)))
            {
                emptyCells.Add(cellIndex);
            }
        }

        return emptyCells;
    }

    public List<Vector2Int> GetEmptyCellsInColumn(int columnIndex, Func<T, bool> isEmptyCondition = null)
    {
        var emptyCells = new List<Vector2Int>();

        if (columnIndex < 0 || columnIndex >= _grid.Size.x)
        {
            Debug.LogWarning($"Column index {columnIndex} is out of grid bounds (0-{_grid.Size.x - 1})");
            return emptyCells;
        }

        for (int y = 0; y < _grid.Size.y; y++)
        {
            var cellIndex = new Vector2Int(columnIndex, y);
            var cellValue = _grid.Array[columnIndex, y];

            bool isDefaultValue = EqualityComparer<T>.Default.Equals(cellValue, default(T));

            if ((isEmptyCondition == null && isDefaultValue) ||
                (isEmptyCondition != null && isEmptyCondition(cellValue)))
            {
                emptyCells.Add(cellIndex);
            }
        }

        return emptyCells;
    }

    public List<Vector2Int> GetNonEmptyCellsInRow(int rowIndex, Func<T, bool> isNonEmptyCondition = null)
    {
        var nonEmptyCells = new List<Vector2Int>();

        if (rowIndex < 0 || rowIndex >= _grid.Size.y)
        {
            Debug.LogWarning($"Row index {rowIndex} is out of grid bounds (0-{_grid.Size.y - 1})");
            return nonEmptyCells;
        }

        for (int x = 0; x < _grid.Size.x; x++)
        {
            var cellIndex = new Vector2Int(x, rowIndex);
            var cellValue = _grid.Array[x, rowIndex];

            bool isDefaultValue = EqualityComparer<T>.Default.Equals(cellValue, default);

            if ((isNonEmptyCondition == null && !isDefaultValue) ||
                (isNonEmptyCondition != null && isNonEmptyCondition(cellValue)))
            {
                nonEmptyCells.Add(cellIndex);
            }
        }

        return nonEmptyCells;
    }

    public List<Vector2Int> GetNonEmptyCellsInColumn(int columnIndex, Func<T, bool> isNonEmptyCondition = null)
    {
        var nonEmptyCells = new List<Vector2Int>();

        if (columnIndex < 0 || columnIndex >= _grid.Size.x)
        {
            Debug.LogWarning($"Column index {columnIndex} is out of grid bounds (0-{_grid.Size.x - 1})");
            return nonEmptyCells;
        }

        for (int y = 0; y < _grid.Size.y; y++)
        {
            var cellIndex = new Vector2Int(columnIndex, y);
            var cellValue = _grid.Array[columnIndex, y];

            bool isDefaultValue = EqualityComparer<T>.Default.Equals(cellValue, default);

            if ((isNonEmptyCondition == null && !isDefaultValue) ||
                (isNonEmptyCondition != null && isNonEmptyCondition(cellValue)))
            {
                nonEmptyCells.Add(cellIndex);
            }
        }

        return nonEmptyCells;
    }
}

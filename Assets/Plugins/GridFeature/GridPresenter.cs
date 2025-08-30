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
        return new Vector3(index.x * totalCellSize.x, index.y * totalCellSize.y, 0) + positionOrigin;
    }

    public Vector2Int GetGridIndex(Vector3 worldPosition)
    {
        var positionOrigin = _grid.PositionOrigin;
        var totalCellSize = new Vector2(_grid.CellSize, _grid.CellSize) + _grid.CellOffset;
        var x = Mathf.FloorToInt((worldPosition.x - positionOrigin.x) / totalCellSize.x);
        var y = Mathf.FloorToInt((worldPosition.y - positionOrigin.y) / totalCellSize.y);
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
        return GetWorldPosition(index) + new Vector3(_grid.CellSize, _grid.CellSize, 0) * 0.5f;
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

            if (isEmptyCondition == null || isEmptyCondition(cellValue))
            {
                emptyCells.Add(cellIndex);
            }
        }

        return emptyCells;
    }
}

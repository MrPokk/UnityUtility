#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GridEditorSetting : MonoBehaviour
{
    private GridConfig _gridConfig;
    [SerializeField] private bool _drawAddButtons = true;
    [SerializeField] private Color _addButtonColor = Color.green;
    [SerializeField] private int _buttonFontSize = 12;

    public bool DrawAddButtons => _drawAddButtons;
    public Color AddButtonColor => _addButtonColor;
    public int ButtonFontSize => _buttonFontSize;

    public void Initialized(GridConfig gridConfig)
    {
        _gridConfig = gridConfig;
    }

    public void RefreshGrid()
    {
        SceneView.RepaintAll();
    }

    public IEnumerable<Vector2Int> FindAdjacentPositions()
    {
        if (_gridConfig == null)
            yield break;

        var config = _gridConfig;
        var existingCells = new HashSet<Vector2Int>(config.Cells);
        var processedPositions = new HashSet<Vector2Int>();

        foreach (var cell in config.Cells)
        {
            foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var adjacentPos = cell + direction;

                if (!existingCells.Contains(adjacentPos) &&
                    !processedPositions.Contains(adjacentPos))
                {
                    processedPositions.Add(adjacentPos);
                    yield return adjacentPos;
                }
            }
        }
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        if (_gridConfig == null)
            return Vector3.zero;

        var config = _gridConfig;
        var cellSize = config.CellSize;
        var cellOffset = config.CellOffset;
        var totalCellSize = new Vector2(cellSize, cellSize) + cellOffset;

        var localCellCenter = new Vector2(
            gridPosition.x * totalCellSize.x + cellSize * 0.5f,
            gridPosition.y * totalCellSize.y + cellSize * 0.5f
        );

        return config.RotationQuaternion * localCellCenter + config.Position;
    }
}
#endif

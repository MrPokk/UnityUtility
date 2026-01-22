using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GridEditorSetting : MonoBehaviour
{
    private GridConfig _gridConfig;
    [SerializeField] private bool _drawAddButtons = true;
    [SerializeField] private bool _drawRemoveButtons = true;
    [SerializeField] private Color _addButtonColor = Color.green;
    [SerializeField] private Color _removeButtonColor = Color.red;
    [SerializeField] private int _buttonFontSize = 12;

    private static readonly Vector2Int[] s_directions = new Vector2Int[]
    {
        new(1, 1),
        new(-1,-1),
        new(-1,1),
        new(1,-1),
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public bool DrawAddButtons => _drawAddButtons;
    public bool DrawRemoveButtons => _drawRemoveButtons;
    public Color AddButtonColor => _addButtonColor;
    public Color RemoveButtonColor => _removeButtonColor;
    public int ButtonFontSize => _buttonFontSize;

    public void Initialized(GridConfig gridConfig)
    {
        _gridConfig = gridConfig;
    }

    public void RefreshGrid()
    {
#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif
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
            foreach (var direction in s_directions)
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

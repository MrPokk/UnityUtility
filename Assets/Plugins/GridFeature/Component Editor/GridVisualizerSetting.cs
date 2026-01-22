using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GridVisualizerSetting : MonoBehaviour
{
    private GridConfig _gridConfig;
    [SerializeField] private bool _drawCoordinates = true;
    [SerializeField] private Color _gridColor = Color.white;
    [SerializeField] private Color _fontColor = Color.yellow;
    [SerializeField] private int _fontSize = 12;

    public void Initialized(GridConfig gridConfig)
    {
        _gridConfig = gridConfig;
    }

#if UNITY_EDITOR
    public void DrawGridGizmos()
    {
        if (_gridConfig == null) return;

        Gizmos.color = _gridColor;
        float cellSize = _gridConfig.CellSize;
        Vector2 cellOffset = _gridConfig.CellOffset;
        Vector2 totalCellSize = new Vector2(cellSize, cellSize) + cellOffset;

        Vector3 origin = _gridConfig.Position;
        Quaternion rotation = _gridConfig.RotationQuaternion;

        var style = new GUIStyle();
        style.normal.textColor = _fontColor;
        style.fontSize = _fontSize;
        style.alignment = TextAnchor.MiddleCenter;

        foreach (var cell in _gridConfig.Cells)
        {
            DrawCell(cell, origin, rotation, totalCellSize, cellSize, style);
        }
    }

    private void DrawCell(Vector2Int cell, Vector3 origin, Quaternion rotation,
                         Vector2 totalCellSize, float cellSize, GUIStyle style)
    {
        Vector2 localCellCenter = new Vector2(
            cell.x * totalCellSize.x + cellSize * 0.5f,
            cell.y * totalCellSize.y + cellSize * 0.5f
        );

        Vector2 localCellMin = new Vector2(cell.x * totalCellSize.x, cell.y * totalCellSize.y);
        Vector2 localCellMax = new Vector2(cell.x * totalCellSize.x + cellSize, cell.y * totalCellSize.y + cellSize);

        Vector3 worldCellCenter = rotation * localCellCenter + origin;
        Vector3 worldCellMin = rotation * localCellMin + origin;
        Vector3 worldCellMax = rotation * localCellMax + origin;
        Vector3 worldCellTopLeft = rotation * new Vector2(localCellMin.x, localCellMax.y) + origin;
        Vector3 worldCellBottomRight = rotation * new Vector2(localCellMax.x, localCellMin.y) + origin;

        Gizmos.DrawLine(worldCellMin, worldCellBottomRight);
        Gizmos.DrawLine(worldCellBottomRight, worldCellMax);
        Gizmos.DrawLine(worldCellMax, worldCellTopLeft);
        Gizmos.DrawLine(worldCellTopLeft, worldCellMin);

        if (_drawCoordinates)
        {
            Handles.Label(worldCellCenter, $"[{cell.x},{cell.y}]", style);
        }
    }
#endif
}

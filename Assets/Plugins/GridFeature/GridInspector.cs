using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public partial class GridInspector : MonoBehaviour
{
    [SerializeField] public GridConfig gridConfig;

    private void Start()
    {
#if !UNITY_EDITOR
        Destroy(this);
#endif
    }

#if UNITY_EDITOR
    [SerializeField] public bool drawCoordinates = true;
    [SerializeField] public bool drawAddButtons = true;
    [SerializeField] public Color gridColor = Color.white;
    [SerializeField] public Color fontColor = Color.yellow;
    [SerializeField] public Color addButtonColor = Color.green;
    [SerializeField] public int fontSize = 12;

    private void OnDrawGizmos()
    {
        if (gridConfig == null) return;

        Gizmos.color = gridColor;
        var cellSize = gridConfig.cellSize;
        var cellOffset = gridConfig.cellOffset;
        var totalCellSize = new Vector2(cellSize, cellSize) + cellOffset;

        var origin = gridConfig.position;
        var rotation = gridConfig.RotationQuaternion;

        var style = new GUIStyle();
        style.normal.textColor = fontColor;
        style.fontSize = fontSize;
        style.alignment = TextAnchor.MiddleCenter;

        foreach (var cell in gridConfig.cells)
        {
            var x = cell.x;
            var y = cell.y;

            var localCellCenter = new Vector2(
                x * totalCellSize.x + cellSize * 0.5f,
                y * totalCellSize.y + cellSize * 0.5f
            );

            var localCellMin = new Vector2(x * totalCellSize.x, y * totalCellSize.y);
            var localCellMax = new Vector2(x * totalCellSize.x + cellSize, y * totalCellSize.y + cellSize);

            var worldCellCenter = rotation * localCellCenter + origin;
            var worldCellMin = rotation * localCellMin + origin;
            var worldCellMax = rotation * localCellMax + origin;
            var worldCellTopLeft = rotation * new Vector2(localCellMin.x, localCellMax.y) + origin;
            var worldCellBottomRight = rotation * new Vector2(localCellMax.x, localCellMin.y) + origin;

            Gizmos.DrawLine(worldCellMin, worldCellBottomRight);
            Gizmos.DrawLine(worldCellBottomRight, worldCellMax);
            Gizmos.DrawLine(worldCellMax, worldCellTopLeft);
            Gizmos.DrawLine(worldCellTopLeft, worldCellMin);

            if (drawCoordinates)
            {
                Handles.Label(worldCellCenter, $"[{x},{y}]", style);
            }
        }
    }

    public void RefreshGrid()
    {
#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif
    }
#endif


}

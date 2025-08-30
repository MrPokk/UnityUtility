using UnityEngine;
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
    [SerializeField] private bool _drawCoordinates = true;
    [SerializeField] private Color _gridColor = Color.white;
    [SerializeField] private Color _fontColor = Color.yellow;
    [SerializeField] private int _fontSize = 12;

    private void OnDrawGizmos()
    {
        if (gridConfig == null) return;

        if (transform.hasChanged)
        {
            gridConfig.position = transform.position;
            EditorUtility.SetDirty(gridConfig);
        }

        Gizmos.color = _gridColor;
        var size = gridConfig.size;
        var cellSize = gridConfig.cellSize;
        var cellOffset = gridConfig.cellOffset;
        var totalCellSize = new Vector2(cellSize, cellSize) + cellOffset;

        var origin = gridConfig.position;
        var rotation = transform.rotation;

        var style = new GUIStyle();
        style.normal.textColor = _fontColor;
        style.fontSize = _fontSize;
        style.alignment = TextAnchor.MiddleCenter;

        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
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

                if (_drawCoordinates)
                {
                    Handles.Label(worldCellCenter, $"[{x},{y}]", style);
                }
            }
        }
    }

    private void OnValidate()
    {
        if (gridConfig != null)
        {
            gridConfig.position = transform.position;
            EditorUtility.SetDirty(gridConfig);
        }
    }

#endif
}

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "GridConfig", menuName = "Config/Grid", order = 0)]
public sealed class GridConfig : ScriptableObject
{
    [Header("Grid Dimensions")]
    [Tooltip("World origin position")]
    public Vector3 position;

    [Tooltip("Rotation of the grid in degrees")]
    public Vector3 rotation;

    [Header("Cell Properties")]
    [Tooltip("Size of each individual cell in world units")]
    [Min(0)]
    public float cellSize = 1f;

    [Tooltip("Spacing between cells in world units (X for horizontal, Y for vertical spacing)")]
    public Vector2 cellOffset;

    [Header("Visual Settings")]
    [Tooltip("Prefab to use for grid node visualization")]
    public GameObject nodePrefab;

    [Header("Grid Cells")]
    [Tooltip("List of cell coordinates in grid space")]
    public List<Vector2Int> cells = new();

    public Quaternion RotationQuaternion => Quaternion.Euler(rotation);

    private void OnValidate()
    {
        RemoveDuplicateCells();
    }

    private void RemoveDuplicateCells()
    {
        if (cells == null || cells.Count == 0)
            return;

        int originalCount = cells.Count;

        var uniqueCells = new HashSet<Vector2Int>();
        var newCellsList = new List<Vector2Int>();

        foreach (var cell in cells)
        {
            if (uniqueCells.Add(cell))
            {
                newCellsList.Add(cell);
            }
        }

        if (newCellsList.Count != originalCount)
        {
            cells = newCellsList;
            Debug.LogWarning($"Removed {originalCount - newCellsList.Count} duplicate cells from {name}");
        }
    }
}

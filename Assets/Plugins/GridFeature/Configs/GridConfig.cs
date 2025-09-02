using UnityEngine;

[CreateAssetMenu(fileName = "GridConfig", menuName = "Config/Grid", order = 0)]
public sealed class GridConfig : ScriptableObject
{
    [Header("Grid Dimensions")]
    [Tooltip("World origin position")]
    public Vector3 position;

    [Tooltip("Rotation of the grid in degrees")]
    public Vector3 rotation;

    [Tooltip("Size of the grid in cells (width, height)")]
    public Vector2Int size;

    [Header("Cell Properties")]
    [Tooltip("Size of each individual cell in world units")]
    public float cellSize;

    [Tooltip("Spacing between cells in world units (X for horizontal, Y for vertical spacing)")]
    public Vector2 cellOffset;

    [Header("Visual Settings")]
    [Tooltip("Prefab to use for grid node visualization")]
    public GameObject nodePrefab;

    public Quaternion RotationQuaternion => Quaternion.Euler(rotation);
}

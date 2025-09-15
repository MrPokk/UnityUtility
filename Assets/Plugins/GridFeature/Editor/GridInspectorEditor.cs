using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(GridInspector))]
public class GridInspectorEditor : Editor
{
    private GridInspector _gridInspector;
    private SerializedProperty _gridConfigProperty;
    private SerializedProperty _drawCoordinatesProperty;
    private SerializedProperty _drawAddButtonsProperty;
    private SerializedProperty _gridColorProperty;
    private SerializedProperty _fontColorProperty;
    private SerializedProperty _addButtonColorProperty;
    private SerializedProperty _fontSizeProperty;

    private SerializedObject _gridConfigSerializedObject;
    private SerializedProperty _cellsProperty;

    private bool _showGridCells = false; // Добавляем переменную для состояния Foldout

    private void OnEnable()
    {
        _gridInspector = (GridInspector)target;
        _gridConfigProperty = serializedObject.FindProperty("gridConfig");
        _drawCoordinatesProperty = serializedObject.FindProperty("drawCoordinates");
        _drawAddButtonsProperty = serializedObject.FindProperty("drawAddButtons");
        _gridColorProperty = serializedObject.FindProperty("gridColor");
        _fontColorProperty = serializedObject.FindProperty("fontColor");
        _addButtonColorProperty = serializedObject.FindProperty("addButtonColor");
        _fontSizeProperty = serializedObject.FindProperty("fontSize");

        UpdateGridConfigReference();
    }

    private void UpdateGridConfigReference()
    {
        if (_gridInspector.gridConfig != null)
        {
            _gridConfigSerializedObject = new SerializedObject(_gridInspector.gridConfig);
            _cellsProperty = _gridConfigSerializedObject.FindProperty("cells");
        }
        else
        {
            _gridConfigSerializedObject = null;
            _cellsProperty = null;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_gridConfigProperty);
        if (EditorGUI.EndChangeCheck())
        {
            UpdateGridConfigReference();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create New Grid Config"))
        {
            CreateNewGridConfig();
        }

        if (_gridInspector.gridConfig != null)
        {
            if (GUILayout.Button("Select Config"))
            {
                Selection.activeObject = _gridInspector.gridConfig;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (_gridInspector.gridConfig == null)
        {
            EditorGUILayout.HelpBox("Assign a GridConfig to visualize the grid.", MessageType.Warning);
        }
        else
        {
            _gridConfigSerializedObject?.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Configuration", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_gridConfigSerializedObject.FindProperty("position"));
            EditorGUILayout.PropertyField(_gridConfigSerializedObject.FindProperty("rotation"));
            EditorGUILayout.PropertyField(_gridConfigSerializedObject.FindProperty("cellSize"));
            EditorGUILayout.PropertyField(_gridConfigSerializedObject.FindProperty("cellOffset"));
            EditorGUILayout.PropertyField(_gridConfigSerializedObject.FindProperty("nodePrefab"));

            EditorGUILayout.Space();

            // Изменяем LabelField на Foldout для Grid Cells
            _showGridCells = EditorGUILayout.Foldout(_showGridCells, "Grid Cells", true, EditorStyles.foldoutHeader);

            if (_showGridCells && _cellsProperty != null)
            {
                EditorGUI.indentLevel++;

                for (int i = 0; i < _cellsProperty.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(_cellsProperty.GetArrayElementAtIndex(i), GUIContent.none);
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        _cellsProperty.DeleteArrayElementAtIndex(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add Cell"))
                {
                    _cellsProperty.arraySize++;
                    _cellsProperty.GetArrayElementAtIndex(_cellsProperty.arraySize - 1).vector2IntValue = Vector2Int.zero;
                }

                EditorGUI.indentLevel--;
            }

            _gridConfigSerializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gizmo Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_drawCoordinatesProperty);
            EditorGUILayout.PropertyField(_drawAddButtonsProperty);
            EditorGUILayout.PropertyField(_gridColorProperty);
            EditorGUILayout.PropertyField(_fontColorProperty);
            EditorGUILayout.PropertyField(_addButtonColorProperty);
            EditorGUILayout.PropertyField(_fontSizeProperty);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void CreateNewGridConfig()
    {
        var newConfig = CreateInstance<GridConfig>();

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Grid Config",
            "NewGridConfig.asset",
            "asset",
            "Please enter a file name to save the Grid Config to");

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(newConfig, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            serializedObject.Update();
            _gridConfigProperty.objectReferenceValue = newConfig;
            serializedObject.ApplyModifiedProperties();

            UpdateGridConfigReference();

            EditorUtility.SetDirty(_gridInspector);
        }
    }

    private void OnSceneGUI()
    {
        if (_gridInspector.gridConfig == null || !_gridInspector.drawAddButtons) return;

        var config = _gridInspector.gridConfig;
        var cellSize = config.cellSize;
        var cellOffset = config.cellOffset;
        var totalCellSize = new Vector2(cellSize, cellSize) + cellOffset;
        var origin = config.position;
        var rotation = config.RotationQuaternion;

        // Find all adjacent positions
        var adjacentPositions = new List<Vector2Int>();
        var existingCells = new HashSet<Vector2Int>(config.cells);

        foreach (var cell in config.cells)
        {
            CheckAndAddAdjacentPosition(cell + Vector2Int.up, existingCells, adjacentPositions);
            CheckAndAddAdjacentPosition(cell + Vector2Int.down, existingCells, adjacentPositions);
            CheckAndAddAdjacentPosition(cell + Vector2Int.left, existingCells, adjacentPositions);
            CheckAndAddAdjacentPosition(cell + Vector2Int.right, existingCells, adjacentPositions);
        }

        // Draw buttons for adjacent positions
        Handles.color = _gridInspector.addButtonColor;
        var style = new GUIStyle();
        style.normal.textColor = _gridInspector.addButtonColor;
        style.fontSize = _gridInspector.fontSize;
        style.alignment = TextAnchor.MiddleCenter;

        foreach (var pos in adjacentPositions)
        {
            var localCellCenter = new Vector2(
                pos.x * totalCellSize.x + cellSize * 0.5f,
                pos.y * totalCellSize.y + cellSize * 0.5f
            );

            var worldCellCenter = rotation * localCellCenter + origin;

            // Draw button
            float buttonSize = cellSize * 0.3f;
            if (Handles.Button(worldCellCenter, rotation, buttonSize, buttonSize, Handles.RectangleHandleCap))
            {
                // Add the new cell
                if (_gridConfigSerializedObject != null && _cellsProperty != null)
                {
                    _gridConfigSerializedObject.Update();
                    _cellsProperty.arraySize++;
                    _cellsProperty.GetArrayElementAtIndex(_cellsProperty.arraySize - 1).vector2IntValue = pos;
                    _gridConfigSerializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(config);
                    _gridInspector.RefreshGrid();
                }
            }

            // Draw plus label
            Handles.Label(worldCellCenter + Vector3.up * buttonSize * 0.7f, "[+]", style);
        }
    }

    private void CheckAndAddAdjacentPosition(Vector2Int position, HashSet<Vector2Int> existingCells, List<Vector2Int> adjacentPositions)
    {
        if (!existingCells.Contains(position) && !adjacentPositions.Contains(position))
        {
            adjacentPositions.Add(position);
        }
    }
}

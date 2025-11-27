#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(GridProvider))]
public class GridProviderEditor : Editor
{
    private GridProvider _gridProvider;
    private GridVisualizerSetting _gridVisualizerSetting;
    private GridEditorSetting _gridEditorSetting;

    private GridConfig GridConfig => _gridProvider.GridConfig;

    private SerializedObject _gridConfigSerializedObject;
    private SerializedProperty _cellsProperty;

    private bool _showGridCells = false;

    private void OnEnable()
    {
        _gridProvider = (GridProvider)target;
        _gridVisualizerSetting = _gridProvider.GridVisualizerSetting;
        _gridEditorSetting = _gridProvider.GridEditorSetting;

        UpdateGridConfigReference();
    }

    private void UpdateGridConfigReference()
    {
        if (GridConfig != null)
        {
            _gridConfigSerializedObject = new SerializedObject(GridConfig);
            _cellsProperty = _gridConfigSerializedObject.FindProperty("_cells");
        }
        else
        {
            _gridConfigSerializedObject = null;
            _cellsProperty = null;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject?.Update();
        DrawDefaultInspector();

        UpdateGridConfigReference();

        if (GridConfig == null)
        {
            EditorGUILayout.HelpBox("Assign a GridConfig to visualize the grid.", MessageType.Warning);
            DrawCreateGridConfigButton();
        }
        else
        {
            if (_gridConfigSerializedObject != null)
            {
                DrawGridConfiguration();
            }
            else
            {
                UpdateGridConfigReference();
                if (_gridConfigSerializedObject != null)
                {
                    DrawGridConfiguration();
                }
                else
                {
                    EditorGUILayout.HelpBox("Failed to serialize GridConfig. Please reassign it.", MessageType.Error);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCreateGridConfigButton()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create New Grid Config"))
        {
            CreateNewGridConfig();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGridConfiguration()
    {
        // Add null check at the beginning of the method
        if (_gridConfigSerializedObject == null)
        {
            UpdateGridConfigReference();
            if (_gridConfigSerializedObject == null)
                return;
        }

        _gridConfigSerializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Configuration", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(_gridConfigSerializedObject.FindProperty("_position"));
        EditorGUILayout.PropertyField(_gridConfigSerializedObject.FindProperty("_rotation"));
        EditorGUILayout.PropertyField(_gridConfigSerializedObject.FindProperty("_cellSize"));
        EditorGUILayout.PropertyField(_gridConfigSerializedObject.FindProperty("_cellOffset"));
        EditorGUILayout.PropertyField(_gridConfigSerializedObject.FindProperty("_nodePrefab"));

        DrawGridCellsSection();

        _gridConfigSerializedObject.ApplyModifiedProperties();
    }

    private void DrawGridCellsSection()
    {
        EditorGUILayout.Space();
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

            EditorGUI.indentLevel--;
        }

        if (!GridConfig.Cells.Any() && GUILayout.Button("Add Cell"))
        {
            _cellsProperty.arraySize++;
            _cellsProperty.GetArrayElementAtIndex(_cellsProperty.arraySize - 1).vector2IntValue = Vector2Int.zero;
        }
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
            serializedObject.FindProperty("_gridConfig").objectReferenceValue = newConfig;
            serializedObject.ApplyModifiedProperties();

            _gridVisualizerSetting.Initialized(newConfig);
            _gridEditorSetting.Initialized(newConfig);

            UpdateGridConfigReference();
        }
    }

    private void OnSceneGUI()
    {
        if (_gridEditorSetting == null || GridConfig == null)
            return;

        if (_gridEditorSetting.DrawAddButtons)
            AddButtonCells();

        if (_gridEditorSetting.DrawRemoveButtons)
            RemoveButtonCells();
    }

    private void RemoveButtonCells()
    {
        Handles.color = _gridEditorSetting.RemoveButtonColor;
        var style = new GUIStyle();
        style.normal.textColor = _gridEditorSetting.RemoveButtonColor;
        style.fontSize = _gridEditorSetting.ButtonFontSize;
        style.alignment = TextAnchor.MiddleCenter;

        foreach (var pos in GridConfig.Cells)
        {
            var worldPosition = _gridEditorSetting.GetWorldPosition(pos);
            var buttonSize = GridConfig.CellSize * 0.3f;

            if (Handles.Button(worldPosition, GridConfig.RotationQuaternion,
                buttonSize, buttonSize, Handles.RectangleHandleCap))
            {
                RemoveCellFromConfig(pos);
            }

            Handles.Label(worldPosition + Vector3.up * buttonSize * 0.7f, "[-]", style);
        }
    }

    private void AddButtonCells()
    {
        Handles.color = _gridEditorSetting.AddButtonColor;
        var style = new GUIStyle();
        style.normal.textColor = _gridEditorSetting.AddButtonColor;
        style.fontSize = _gridEditorSetting.ButtonFontSize;
        style.alignment = TextAnchor.MiddleCenter;

        foreach (var pos in _gridEditorSetting.FindAdjacentPositions())
        {
            var worldPosition = _gridEditorSetting.GetWorldPosition(pos);
            var buttonSize = GridConfig.CellSize * 0.3f;

            if (Handles.Button(worldPosition, GridConfig.RotationQuaternion,
                buttonSize, buttonSize, Handles.RectangleHandleCap))
            {
                AddCellToConfig(pos);
            }

            Handles.Label(worldPosition + Vector3.up * buttonSize * 0.7f, "[+]", style);
        }
    }

    private void RemoveCellFromConfig(Vector2Int cell)
    {
        if (_gridConfigSerializedObject != null && _cellsProperty != null)
        {
            _gridConfigSerializedObject.Update();

            int indexToRemove = -1;
            for (int i = 0; i < _cellsProperty.arraySize; i++)
            {
                var currentCell = _cellsProperty.GetArrayElementAtIndex(i).vector2IntValue;
                if (currentCell == cell)
                {
                    indexToRemove = i;
                    break;
                }
            }

            if (indexToRemove >= 0)
            {
                _cellsProperty.DeleteArrayElementAtIndex(indexToRemove);
                _gridConfigSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(GridConfig);
                _gridEditorSetting.RefreshGrid();
            }
        }
    }

    private void AddCellToConfig(Vector2Int cell)
    {
        if (_gridConfigSerializedObject != null && _cellsProperty != null)
        {
            _gridConfigSerializedObject.Update();
            _cellsProperty.arraySize++;
            _cellsProperty.GetArrayElementAtIndex(_cellsProperty.arraySize - 1).vector2IntValue = cell;
            _gridConfigSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(GridConfig);
            _gridEditorSetting.RefreshGrid();
        }
    }
}
#endif

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridInspector))]
public class GridInspectorEditor : Editor
{
    private GridInspector _gridInspector;
    private SerializedProperty _gridConfigProperty;
    private SerializedProperty _drawCoordinatesProperty;
    private SerializedProperty _gridColorProperty;
    private SerializedProperty _fontColorProperty;
    private SerializedProperty _fontSizeProperty;

    private Vector2Int _tempSize;
    private float _tempCellSize;
    private Vector2 _tempCellOffset;
    private GameObject _tempNodePrefab;
    private Vector3 TempPosition => _gridInspector.transform.position;
    private Vector3 TempRotation => _gridInspector.transform.rotation.eulerAngles;

    private GridConfig _lastGridConfig; // Для отслеживания изменения конфига

    private void OnEnable()
    {
        _gridInspector = (GridInspector)target;
        _gridConfigProperty = serializedObject.FindProperty("gridConfig");
        _drawCoordinatesProperty = serializedObject.FindProperty("_drawCoordinates");
        _gridColorProperty = serializedObject.FindProperty("_gridColor");
        _fontColorProperty = serializedObject.FindProperty("_fontColor");
        _fontSizeProperty = serializedObject.FindProperty("_fontSize");

        if (_gridInspector.gridConfig != null)
        {
            CacheConfigValues();
            _lastGridConfig = _gridInspector.gridConfig;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Проверяем, изменился ли GridConfig
        bool configChanged = false;
        if (_gridInspector.gridConfig != _lastGridConfig)
        {
            configChanged = true;
            _lastGridConfig = _gridInspector.gridConfig;
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_gridConfigProperty);
        if (EditorGUI.EndChangeCheck() || configChanged)
        {
            // Если конфиг изменился, обновляем временные значения
            if (_gridInspector.gridConfig != null)
            {
                CacheConfigValues();
                // Также обновляем позицию и вращение объекта
                _gridInspector.transform.position = _gridInspector.gridConfig.position;
                _gridInspector.transform.rotation = _gridInspector.gridConfig.RotationQuaternion;
            }
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

            if (GUILayout.Button("Apply Config"))
            {
                ApplyConfigChanges();
                _gridInspector.RefreshGrid();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (_gridInspector.gridConfig == null)
        {
            EditorGUILayout.HelpBox("Assign a GridConfig to visualize the grid.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Configuration (Editable)", EditorStyles.boldLabel);

            // Отображаем редактируемые свойства
            EditorGUI.BeginChangeCheck();

            _tempSize = EditorGUILayout.Vector2IntField("Grid Size", _tempSize);
            _tempCellSize = EditorGUILayout.FloatField("Cell Size", _tempCellSize);
            _tempCellOffset = EditorGUILayout.Vector2Field("Cell Offset", _tempCellOffset);
            _tempNodePrefab = (GameObject)EditorGUILayout.ObjectField("Node Prefab", _tempNodePrefab, typeof(GameObject), false);

            EditorGUI.EndChangeCheck();


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gizmo Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_drawCoordinatesProperty);
            EditorGUILayout.PropertyField(_gridColorProperty);
            EditorGUILayout.PropertyField(_fontColorProperty);
            EditorGUILayout.PropertyField(_fontSizeProperty);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Changes will be applied to the GridConfig asset only when you click 'Apply Config'.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void CacheConfigValues()
    {
        if (_gridInspector.gridConfig != null)
        {
            _tempSize = _gridInspector.gridConfig.size;
            _tempCellSize = _gridInspector.gridConfig.cellSize;
            _tempCellOffset = _gridInspector.gridConfig.cellOffset;
            _tempNodePrefab = _gridInspector.gridConfig.nodePrefab;

        }
    }

    private void ApplyConfigChanges()
    {
        if (_gridInspector.gridConfig != null)
        {
            Undo.RecordObject(_gridInspector.gridConfig, "Apply Grid Config Changes");

            _gridInspector.gridConfig.size = _tempSize;
            _gridInspector.gridConfig.cellSize = _tempCellSize;
            _gridInspector.gridConfig.cellOffset = _tempCellOffset;
            _gridInspector.gridConfig.nodePrefab = _tempNodePrefab;
            _gridInspector.gridConfig.position = TempPosition;
            _gridInspector.gridConfig.rotation = TempRotation;

            EditorUtility.SetDirty(_gridInspector.gridConfig);
        }
    }

    private void CreateNewGridConfig()
    {
        var newConfig = CreateInstance<GridConfig>();

        // Если уже есть конфиг, копируем его значения
        if (_gridInspector.gridConfig != null)
        {
            var currentConfig = _gridInspector.gridConfig;
            newConfig.size = currentConfig.size;
            newConfig.cellSize = currentConfig.cellSize;
            newConfig.cellOffset = currentConfig.cellOffset;
            newConfig.nodePrefab = currentConfig.nodePrefab;
            newConfig.position = currentConfig.position;
            newConfig.rotation = currentConfig.rotation;
        }
        else
        {
            // Значения по умолчанию, если конфига нет
            newConfig.size = new Vector2Int(10, 10);
            newConfig.cellSize = 1f;
            newConfig.cellOffset = Vector2.zero;
            newConfig.position = _gridInspector.transform.position;
            newConfig.rotation = _gridInspector.transform.rotation.eulerAngles;
        }

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

            // Кэшируем значения нового конфига
            CacheConfigValues();
            _lastGridConfig = newConfig;

            EditorUtility.SetDirty(_gridInspector);
            Debug.Log($"Grid Config created and saved to: {path}");
        }
    }

    private void OnInspectorUpdate()
    {
        // Если конфиг изменился (например, через другой редактор), обновляем временные значения
        if (_gridInspector.gridConfig != null &&
            (_tempSize != _gridInspector.gridConfig.size ||
             _tempCellSize != _gridInspector.gridConfig.cellSize ||
             _tempCellOffset != _gridInspector.gridConfig.cellOffset ||
             _tempNodePrefab != _gridInspector.gridConfig.nodePrefab ||
             TempPosition != _gridInspector.gridConfig.position ||
             TempRotation != _gridInspector.gridConfig.rotation))
        {
            CacheConfigValues();
            Repaint();
        }
    }
}

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

    private void OnEnable()
    {
        _gridInspector = (GridInspector)target;
        _gridConfigProperty = serializedObject.FindProperty("gridConfig");
        _drawCoordinatesProperty = serializedObject.FindProperty("_drawCoordinates");
        _gridColorProperty = serializedObject.FindProperty("_gridColor");
        _fontColorProperty = serializedObject.FindProperty("_fontColor");
        _fontSizeProperty = serializedObject.FindProperty("_fontSize");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_gridConfigProperty);

        if (_gridInspector.gridConfig == null)
        {
            EditorGUILayout.HelpBox("Assign a GridConfig to visualize the grid.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Configuration", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            var gridConfig = _gridInspector.gridConfig;
            var newSize = EditorGUILayout.Vector2IntField("Grid Size", gridConfig.size);
            var newCellSize = EditorGUILayout.FloatField("Cell Size", gridConfig.cellSize);
            var newCellOffset = EditorGUILayout.Vector2Field("Cell Offset", gridConfig.cellOffset);
            var newNodePrefab = (GameObject)EditorGUILayout.ObjectField("Node Prefab", gridConfig.nodePrefab, typeof(GameObject), false);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(gridConfig, "Modify Grid Config");
                gridConfig.size = newSize;
                gridConfig.cellSize = newCellSize;
                gridConfig.cellOffset = newCellOffset;
                gridConfig.nodePrefab = newNodePrefab;
                EditorUtility.SetDirty(gridConfig);
            }

            // Only show Gizmo settings when GridConfig is assigned
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gizmo Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_drawCoordinatesProperty);
            EditorGUILayout.PropertyField(_gridColorProperty);
            EditorGUILayout.PropertyField(_fontColorProperty);
            EditorGUILayout.PropertyField(_fontSizeProperty);

            EditorGUILayout.Space();
            if (GUILayout.Button("Open Grid Config Asset"))
            {
                Selection.activeObject = gridConfig;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}

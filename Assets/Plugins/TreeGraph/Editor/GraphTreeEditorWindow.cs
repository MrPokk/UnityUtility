using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphTreeEditorWindow : EditorWindow
{
    private GraphTreeAsset _currentAsset;
    private GraphTreeView _graphView;
    private FloatField _gridSizeField;
    private FloatField _worldScaleField;

    [MenuItem("Window/Graph Tree Editor")]
    public static void Open() => GetWindow<GraphTreeEditorWindow>("Graph Tree Editor");

    private void OnEnable()
    {
        GenerateGraphView();
        GenerateTopPanel();
    }

    private void GenerateGraphView()
    {
        _graphView = new GraphTreeView() { style = { flexGrow = 1 } };
        rootVisualElement.Add(_graphView);
    }

    private void GenerateTopPanel()
    {
        var topPanel = GraphUIFactory.CreateCustomToolbar();
        topPanel.Add(CreateAssetField());
        topPanel.Add(CreateGridSizeField());
        topPanel.Add(CreateScaleField());
        topPanel.Add(new VisualElement { style = { flexGrow = 1 } });
        topPanel.Add(GraphUIFactory.CreateButton("Save to Asset", SaveAsset));
        topPanel.Add(GraphUIFactory.CreateButton("Add Node ▼", ShowAddNodeMenu));
        rootVisualElement.Insert(0, topPanel);
    }

    private ObjectField CreateAssetField()
    {
        var field = new ObjectField("Asset")
        {
            objectType = typeof(GraphTreeAsset),
            style = { width = 250 }
        };
        field.labelElement.style.minWidth = 45;
        field.RegisterValueChangedCallback(evt => LoadAsset(evt.newValue as GraphTreeAsset));
        return field;
    }

    private FloatField CreateGridSizeField()
    {
        _gridSizeField = new FloatField("Grid")
        {
            value = 20f,
            style = { width = 110, marginLeft = 15 }
        };
        _gridSizeField.labelElement.style.minWidth = 35;
        _gridSizeField.RegisterValueChangedCallback(evt =>
        {
            _graphView.GridSize = evt.newValue;
            if (_currentAsset) _currentAsset.gridSize = evt.newValue;
        });
        return _gridSizeField;
    }

    private FloatField CreateScaleField()
    {
        _worldScaleField = new FloatField("Scale")
        {
            value = 100f,
            style = { width = 120, marginLeft = 15 }
        };
        _worldScaleField.labelElement.style.minWidth = 45;
        _worldScaleField.RegisterValueChangedCallback(evt =>
        {
            _graphView.WorldScale = evt.newValue;
            if (_currentAsset) _currentAsset.worldScale = evt.newValue;
            _graphView.RefreshAllNodePositions();
        });
        return _worldScaleField;
    }

    private void ShowAddNodeMenu()
    {
        if (!_currentAsset) return;

        var menu = new GenericMenu();
        var types = TypeCache.GetTypesDerivedFrom<GraphNodeData>().Where(t => !t.IsAbstract);

        foreach (var type in types)
        {
            menu.AddItem(new GUIContent(type.Name), false, () =>
            {
                var center = _graphView.contentViewContainer.WorldToLocal(new Vector2(position.width / 2, position.height / 2));
                _graphView.CreateNewTypeNode(type, center);
            });
        }
        menu.ShowAsContext();
    }

    private void SaveAsset()
    {
        if (!_currentAsset) return;

        _currentAsset.nodes = _graphView.nodes.Cast<GraphNodeVisual>().Select(n => n.Data).ToList();

        _currentAsset.connections = _graphView.edges.ToList()
            .Where(e => e.input?.node is GraphNodeVisual && e.output?.node is GraphNodeVisual)
            .Select(e =>
            {
                var fromNode = (GraphNodeVisual)e.output.node;
                var toNode = (GraphNodeVisual)e.input.node;

                return new GraphConnectionData
                {
                    connection = new NodeConnection
                    {
                        id = UnityEngine.Random.Range(1, int.MaxValue),
                        fromGraphId = fromNode.Id,
                        toGraphId = toNode.Id,
                        fromAnchor = CalculateAnchor(fromNode.Position, toNode.Position),
                        toAnchor = CalculateAnchor(toNode.Position, fromNode.Position)
                    }
                };
            }).ToList();

        EditorUtility.SetDirty(_currentAsset);
        AssetDatabase.SaveAssets();
    }

    private NodeAnchor CalculateAnchor(Vector2 from, Vector2 to)
    {
        var dir = to - from;
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        return angle switch
        {
            > 337.5f or <= 22.5f => NodeAnchor.RIGHT,
            > 22.5f and <= 67.5f => NodeAnchor.DOWN_RIGHT,
            > 67.5f and <= 112.5f => NodeAnchor.DOWN,
            > 112.5f and <= 157.5f => NodeAnchor.DOWN_LEFT,
            > 157.5f and <= 202.5f => NodeAnchor.LEFT,
            > 202.5f and <= 247.5f => NodeAnchor.UP_LEFT,
            > 247.5f and <= 292.5f => NodeAnchor.UP,
            > 292.5f and <= 337.5f => NodeAnchor.UP_RIGHT,
            _ => NodeAnchor.RIGHT
        };
    }

    private void LoadAsset(GraphTreeAsset asset)
    {
        _currentAsset = asset;
        _graphView.ClearGraph();
        if (!asset) return;

        _gridSizeField.SetValueWithoutNotify(asset.gridSize);
        _worldScaleField.SetValueWithoutNotify(asset.worldScale);
        _graphView.GridSize = asset.gridSize;
        _graphView.WorldScale = asset.worldScale;

        var nodeMap = asset.nodes.ToDictionary(n => n.idComponent.id, n => _graphView.CreateNodeFromData(n));
        foreach (var conn in asset.connections)
        {
            if (nodeMap.TryGetValue(conn.connection.fromGraphId, out var from) &&
                nodeMap.TryGetValue(conn.connection.toGraphId, out var to))
            {
                _graphView.AddElement(from.OutputPort.ConnectTo(to.InputPort));
            }
        }
    }
}

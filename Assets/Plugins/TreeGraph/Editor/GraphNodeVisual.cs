using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphNodeVisual : Node
{
    public GraphNodeData Data { get; private set; }
    public ref int Id => ref Data.idComponent.id;
    public ref string Key => ref Data.idComponent.key;
    public ref Vector2 Position => ref Data.position.position;

    public Port InputPort { get; private set; }
    public Port OutputPort { get; private set; }

    private Vector2Field _posField;
    private VisualElement _componentsContainer;
    private readonly GraphTreeView _graph;

    public GraphNodeVisual(GraphNodeData data, GraphTreeView graph)
    {
        Data = data;
        _graph = graph;

        ConfigureNode();
        CreatePorts();
        BuildUI();
        RefreshExpandedState();
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        Position = newPos.position / _graph.WorldScale;
        _posField?.SetValueWithoutNotify(Position);
    }

    public void RefreshPositionFromData()
    {
        var screenPos = Position * _graph.WorldScale;
        base.SetPosition(new Rect(screenPos, layout.size));
        _posField?.SetValueWithoutNotify(Position);
    }

    private void ConfigureNode()
    {
        title = string.IsNullOrEmpty(Key) ? $"Graph {Id}" : Key;
        extensionContainer.style.backgroundColor = GraphUIStyle.NodeBackground;
        style.minWidth = 250;
    }

    private void CreatePorts()
    {
        InputPort = AddPort(Direction.Input, "In");
        OutputPort = AddPort(Direction.Output, "Out");
    }

    private Port AddPort(Direction dir, string name)
    {
        var port = InstantiatePort(Orientation.Horizontal, dir, Port.Capacity.Multi, typeof(float));
        port.portName = name;
        (dir == Direction.Input ? inputContainer : outputContainer).Add(port);
        return port;
    }

    private void BuildUI()
    {
        var infoBox = GraphUIStyle.CreateNodeContainer();

        var typeLabel = GraphUIStyle.CreateLabel($"[{Data.GetType().Name}]", true);
        typeLabel.style.color = new Color(0.7f, 0.7f, 1f);
        infoBox.Add(typeLabel);

        infoBox.Add(GraphUIStyle.CreateLabel($"ID: {Id}", bold: true));

        var keyField = new TextField("Key") { value = Key };
        keyField.RegisterValueChangedCallback(e => { Key = e.newValue; title = e.newValue; });
        infoBox.Add(keyField);

        _posField = new Vector2Field("Position") { value = Position };
        _posField.RegisterValueChangedCallback(e =>
        {
            SetPosition(new Rect(e.newValue * _graph.WorldScale, layout.size));
        });
        infoBox.Add(_posField);

        extensionContainer.Add(infoBox);

        var customFieldsBox = new VisualElement();
        GraphUIFactory.DrawComponentFields(customFieldsBox, Data, () => { });
        if (customFieldsBox.childCount > 0)
        {
            customFieldsBox.style.paddingTop = 5;
            customFieldsBox.style.paddingBottom = 5;
            customFieldsBox.style.borderBottomWidth = 1;
            customFieldsBox.style.borderBottomColor = GraphUIStyle.BorderColor;
            extensionContainer.Add(customFieldsBox);
        }

        _componentsContainer = new VisualElement();
        extensionContainer.Add(_componentsContainer);
        RefreshComponents();

        extensionContainer.Add(GraphUIFactory.CreateButton("Add Component", ShowAddComponentMenu));
    }

    private void RefreshComponents()
    {
        _componentsContainer.Clear();
        for (var i = 0; i < Data.components.Count; i++)
        {
            var idx = i;
            var comp = Data.components[i];
            var typeName = comp.GetType().Name.Replace("Component", "");

            var group = GraphUIStyle.CreateBox();

            var header = GraphUIStyle.CreateRow();
            header.Add(GraphUIStyle.CreateHeaderLabel(typeName));

            var buttonsRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            if (idx > 0)
            {
                var upBtn = GraphUIStyle.CreateSmallButton("▲", () =>
                {
                    (Data.components[idx], Data.components[idx - 1]) = (Data.components[idx - 1], Data.components[idx]);
                    RefreshComponents();
                });
                buttonsRow.Add(upBtn);
            }

            if (idx < Data.components.Count - 1)
            {
                var downBtn = GraphUIStyle.CreateSmallButton("▼", () =>
                {
                    (Data.components[idx], Data.components[idx + 1]) = (Data.components[idx + 1], Data.components[idx]);
                    RefreshComponents();
                });
                buttonsRow.Add(downBtn);
            }

            var deleteBtn = GraphUIStyle.CreateDeleteButton(() =>
            {
                Data.components.RemoveAt(idx);
                RefreshComponents();
            });
            buttonsRow.Add(deleteBtn);

            header.Add(buttonsRow);
            group.Add(header);

            GraphUIFactory.DrawComponentFields(group, comp, () => { });

            _componentsContainer.Add(group);
        }
    }

    private void ShowAddComponentMenu()
    {
        var menu = new GenericMenu();
        var types = TypeCache.GetTypesDerivedFrom<IGraphComponent>().Where(t => !t.IsAbstract && t.IsValueType);

        foreach (var type in types)
        {
            if (Data.components.Any(c => c.GetType() == type)) continue;
            menu.AddItem(new GUIContent(type.Name.Replace("Component", "")), false, () =>
            {
                Data.components.Add((IGraphComponent)Activator.CreateInstance(type));
                RefreshComponents();
            });
        }
        menu.ShowAsContext();
    }
}

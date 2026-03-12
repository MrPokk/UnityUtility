using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class GraphTreeView : GraphView
{
    public float WorldScale { get; set; } = 100f;
    public float GridSize { get; set; } = 20f;

    public GraphTreeView()
    {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        Insert(0, new GridBackground { style = { flexGrow = 1 } });
    }

    public GraphNodeVisual CreateNewTypeNode(Type nodeType, Vector2 screenPosition = default)
    {
        var data = (GraphNodeData)Activator.CreateInstance(nodeType);
        data.idComponent = new NodeId { id = GenerateUniqueId(), key = nodeType.Name };
        data.position = new NodePosition { position = screenPosition / WorldScale };

        return CreateNodeFromData(data);
    }

    public GraphNodeVisual CreateNodeFromData(GraphNodeData data)
    {
        var node = new GraphNodeVisual(data, this);
        var finalPos = data.position.position * WorldScale;
        node.SetPosition(new Rect(finalPos, Vector2.zero));

        AddElement(node);
        return node;
    }

    public void RefreshAllNodePositions()
    {
        foreach (var node in nodes.Cast<GraphNodeVisual>())
        {
            node.RefreshPositionFromData();
        }
    }

    private int GenerateUniqueId()
    {
        var id = UnityEngine.Random.Range(1, int.MaxValue);
        while (nodes.ToList().Cast<GraphNodeVisual>().Any(n => n.Id == id))
        {
            id = UnityEngine.Random.Range(1, int.MaxValue);
        }
        return id;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList()
            .Where(p => p.direction != startPort.direction && p.node != startPort.node)
            .ToList();
    }

    public void ClearGraph() => DeleteElements(graphElements);
}

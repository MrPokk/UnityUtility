using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphTree
{
    [CreateAssetMenu(fileName = "NewGraphTree", menuName = "Graph Tree/Tree Data")]
    public class GraphTreeAsset : ScriptableObject
    {
        public float gridSize = 20f;
        public float worldScale = 100f;
        [SerializeReference] public List<GraphNodeData> nodes = new();
        public List<GraphConnectionData> connections = new();
    }

    [Serializable]
    public abstract class GraphNodeData
    {
        public NodeId idComponent;
        public NodePosition position;
        [SerializeReference] public List<IGraphComponent> components = new();

        public GraphNodeData() => components = Registration();

        protected abstract List<IGraphComponent> Registration();
    }

    [Serializable]
    public class GraphConnectionData
    {
        public NodeConnection connection;
    }
}

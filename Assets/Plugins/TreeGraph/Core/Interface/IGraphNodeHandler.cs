using UnityEngine;

namespace GraphTree
{
    public interface IGraphNodeHandler
    {
        void Initialize(GraphNodeData data);
        void AddChild(GameObject child);
    }
}

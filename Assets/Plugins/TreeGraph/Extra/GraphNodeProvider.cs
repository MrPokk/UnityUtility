#if GRAPH_EXTRA
using System.Collections.Generic;
using BitterECS.Core;
using BitterECS.Integration.Unity;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GraphTree
{
    [RequireComponent(typeof(GraphIdComponentProvider))]
    [RequireComponent(typeof(GraphProgressionComponentProvider))]
    [RequireComponent(typeof(GraphUnlockRulesComponentProvider))]
    [RequireComponent(typeof(GraphPositionComponentProvider))]
    [RequireComponent(typeof(SpriteRendererComponentProvider))]
    public class GraphNodeProvider : ProviderEcs<GraphNodePresenter>, IPointerClickHandler, IGraphNodeHandler
    {
        [Header("Authoring (Editor Only)")]
        public List<GameObject> childrenNodes = new();

        public void Initialize(GraphNodeData data)
        {

        }

        public void AddChild(GameObject child)
        {
            if (!childrenNodes.Contains(child))
                childrenNodes.Add(child);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Entity.AddFrame<IsClickToComponent>();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            foreach (var child in childrenNodes)
            {
                if (child != null)
                    Gizmos.DrawLine(transform.position, child.transform.position);
            }
        }
    }

    public class GraphNodePresenter : EcsPresenter
    {

    }
}
#endif

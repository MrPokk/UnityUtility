using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphTree
{
    public class GraphTreeLoader : MonoBehaviour
    {
        public GraphTreeAsset asset;
        public GameObject nodePrefab;
        public Transform container;

        public Dictionary<int, GameObject> SpawnedNodes { get; private set; } = new();

        public event Action<GameObject, GraphNodeData> OnNodeInstantiated;
        public event Action<GameObject, GameObject, GraphConnectionData> OnConnectionCreated;

        public virtual void Load()
        {
            if (asset == null || nodePrefab == null) return;

            Clear();

            foreach (var data in asset.nodes)
            {
                var instance = Instantiate(nodePrefab, container != null ? container : transform);
                instance.transform.localPosition = data.position.position;
                SpawnedNodes[data.idComponent.id] = instance;

                ProcessNode(instance, data);
                OnNodeInstantiated?.Invoke(instance, data);
            }

            foreach (var connData in asset.connections)
            {
                if (SpawnedNodes.TryGetValue(connData.connection.fromGraphId, out var parent) &&
                    SpawnedNodes.TryGetValue(connData.connection.toGraphId, out var child))
                {
                    ProcessConnection(parent, child, connData);
                    OnConnectionCreated?.Invoke(parent, child, connData);
                }
            }
        }

        public virtual void Clear()
        {
            foreach (var node in SpawnedNodes.Values)
            {
                if (node != null) Destroy(node);
            }
            SpawnedNodes.Clear();
        }

        public virtual void Enable() => gameObject.SetActive(true);
        public virtual void Disable() => gameObject.SetActive(false);

        protected virtual void ProcessNode(GameObject instance, GraphNodeData data)
        {
            var handler = instance.GetComponent<IGraphNodeHandler>();
            handler?.Initialize(data);
        }

        protected virtual void ProcessConnection(GameObject parent, GameObject child, GraphConnectionData connectionData)
        {
            var handler = parent.GetComponent<IGraphNodeHandler>();
            handler?.AddChild(child);
        }
    }
}

#if GRAPH_EXTRA
using UnityEngine;
using System.Reflection;
using BitterECS.Core;

namespace GraphTree
{
    public class EcsGraphTreeLoader : GraphTreeLoader
    {
        private MethodInfo _addMethodTemplate;

        public override void Load()
        {
            _addMethodTemplate = typeof(EcsEntity).GetMethod("Add");

            base.Load();

            Debug.Log($"Graph Tree [{asset.name}] loaded with ECS!");
        }

        protected override void ProcessNode(GameObject instance, GraphNodeData data)
        {
            base.ProcessNode(instance, data);

            var provider = instance.GetComponent<GraphNodeProvider>();
            if (provider == null) return;

            var entity = provider.Entity;

            AddComponentToEntity(entity, data.idComponent);
            AddComponentToEntity(entity, data.position);

            foreach (var comp in data.components)
            {
                SetSprite(entity, comp);
                AddComponentToEntity(entity, comp);
            }
        }

        private static void SetSprite(EcsEntity entity, IGraphComponent comp)
        {
            if (comp is not GraphSpriteComponent spriteComp) return;
            if (!entity.TryGet<SpriteRendererComponent>(out var renderCom)) return;

            if (spriteComp.sprite != null)
                renderCom.renderer.sprite = spriteComp.sprite;
        }

        private void AddComponentToEntity(EcsEntity entity, object component)
        {
            if (component == null || _addMethodTemplate == null) return;
            var genericAdd = _addMethodTemplate.MakeGenericMethod(component.GetType());
            genericAdd.Invoke(entity, new[] { component });
        }
    }
}
#endif

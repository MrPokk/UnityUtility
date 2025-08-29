using System;
using System.Reflection;
using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration
{
    public class MonoProvider : MonoBehaviour, ILinkableProvider
    {
        [SerializeField]
        public SerializableType entityType;

        public Type EntityType => entityType.Type;
        public EcsProviderProperty Properties { get; protected set; }

        private ILinkableProvider _linkableProvider;
        private ComponentProvider[] _componentProviders;
        private static readonly MethodInfo s_applyComponentMethod = typeof(MonoProvider).GetMethod(
            nameof(ApplyComponent), BindingFlags.NonPublic | BindingFlags.Instance);

        protected virtual void Awake()
        {
            if (EntityType == null || !typeof(EcsEntity).IsAssignableFrom(EntityType))
            {
                throw new Exception("Invalid entity type: " + EntityType?.Name ?? "null");
            }

            _componentProviders = GetComponents<ComponentProvider>();
            _linkableProvider = this;
            EcsWorld.GetToEntityType(EntityType)
            .AddTo()
            .WithPreInitCallback(ApplyComponentProviders)
            .WithLink(_linkableProvider)
            .Create(EntityType);

        }

        protected void ApplyComponentProviders(EcsEntity entity)
        {
            foreach (var wrapper in _componentProviders)
            {
                var componentType = wrapper.ObjectComponent.GetType();
                var method = s_applyComponentMethod.MakeGenericMethod(componentType);
                method?.Invoke(this, new object[] { entity, wrapper });
            }
        }

        protected void ApplyComponent<TComponent>(EcsEntity entity, ComponentProvider wrapper) where TComponent : struct
        {
            if (wrapper is not ComponentProvider<TComponent> typedWrapper)
            {
                return;
            }

            var component = typedWrapper.Component;
            if (entity.Has<TComponent>())
            {
                entity.Set((ref TComponent comp) => comp = component);
            }
            else
            {
                entity.Add(component);
            }
        }

        public void Init(EcsProviderProperty property) => Properties = property;
        public void Dispose()
        {
            SystemDestroy.Add(gameObject);
        }
    }

    public class MonoProvider<T> : MonoProvider where T : EcsEntity
    {
        protected override void Awake()
        {
            entityType = new SerializableType(typeof(T));
            base.Awake();
        }
    }
}

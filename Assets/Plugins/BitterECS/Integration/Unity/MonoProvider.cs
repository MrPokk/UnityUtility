using System;
using System.Collections.Generic;
using System.Reflection;
using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration
{
    public abstract class MonoProvider<T> : MonoBehaviour, ILinkableProvider where T : EcsEntity
    {
        public EcsProviderProperty Properties { get; private set; }
        private ILinkableProvider _linkableProvider;
        private List<ComponentProvider> _ComponentProviders;
        private void Awake()
        {
            _ComponentProviders = new(GetComponents<ComponentProvider>());
            _linkableProvider = GetComponent<ILinkableProvider>();
            var entity = EcsWorld.GetToEntityType(typeof(T))
                .AddTo<T>()
                .WithLink(_linkableProvider)
                .Create();

            ApplyComponentProviders(entity);
        }

        private void ApplyComponentProviders(T entity)
        {
            foreach (var wrapper in _ComponentProviders)
            {
                var componentType = wrapper.ObjectComponent.GetType();
                var method = typeof(MonoProvider<T>).GetMethod(nameof(ApplyComponent),
                    BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(componentType);

                method.Invoke(this, new object[] { entity, wrapper });
            }
        }

        private void ApplyComponent<TComponent>(T entity, ComponentProvider wrapper) where TComponent : struct
        {
            var ComponentProvider = wrapper as ComponentProvider<TComponent>;
            if (ComponentProvider == null)
                return;

            var component = ComponentProvider.Component;
            if (entity.Has<TComponent>())
                entity.Set((ref TComponent comp) => comp = component);
            else
                entity.Add(component);
        }

        public void Init(EcsProviderProperty property)
        {
            Properties = property;
        }

        public void Dispose()
        {
            if (Properties != null && Properties.Presenter != null && _linkableProvider != null)
                Properties.Presenter.DestroyEntity(_linkableProvider.Entity);
            Properties = null;
            GC.SuppressFinalize(this);
        }
    }
}

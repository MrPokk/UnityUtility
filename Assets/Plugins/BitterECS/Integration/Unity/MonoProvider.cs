using System;
using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration
{
    public class MonoProvider : MonoBehaviour, ILinkableProvider
    {
        public SerializableType presenterType;
        public virtual Type PresenterType => presenterType.Type;

        public EcsProperty Properties { get; protected set; }
        public EcsPresenter Presenter => Properties?.Presenter;
        public EcsEntity Entity => Properties?.Presenter?.Get(Properties.Id);
        public int Id => Properties?.Id ?? 0;

        private ITypedComponentProvider[] _componentProviders;

        protected virtual void Registration() { }

        protected virtual void Awake()
        {
            if (PresenterType == null)
            {
                throw new Exception($"Invalid entity type: null from {gameObject.name}");
            }

            _componentProviders = GetComponents<ITypedComponentProvider>();

            try
            {
                Build.For(PresenterType)
                   .Add<EcsEntity>()
                   .WithPreInitCallback(ApplyComponent)
                   .WithLink(this)
                   .WithForce()
                   .Create();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while creating entity: {ex.Message}");
            }

            Registration();
        }

        protected void ApplyComponent(EcsEntity entity)
        {
            foreach (var provider in _componentProviders)
            {
                provider.Apply(entity);
            }
        }

        protected void SyncComponent(EcsEntity entity)
        {
            foreach (var provider in _componentProviders)
            {
                provider.Sync(entity);
            }
        }

        public void Init(EcsProperty property) => Properties ??= property;

        public void Dispose()
        {
            if (this == null || gameObject == null)
                return;

            if (Properties == null)
                return;

            try
            {
                var entity = Entity;
                if (entity != null)
                {
                    Properties.Presenter?.Remove(entity);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error while removing entity: {ex.Message}");
            }
            finally
            {
                Properties = null;
                Destroy(gameObject);
            }

            GC.SuppressFinalize(this);
        }
    }

    public class MonoProvider<T> : MonoProvider where T : EcsPresenter
    {
        public override Type PresenterType => typeof(T);
    }
}

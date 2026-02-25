using System;
using System.Collections.Generic;
using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration
{
    public interface ITypedComponentProvider
    {
        void Sync(EcsEntity entity);
    }

    public abstract class ProviderEcs : MonoBehaviour, ITypedComponentProvider, ILinkableProvider
    {
        public abstract bool IsPresenter { get; }
        public abstract EcsEntity Entity { get; }

        internal abstract EcsEntity GetEntitySilently();

        public virtual EcsProperty Properties => GetEntitySilently().Presenter != null
            ? new EcsProperty(GetEntitySilently().Presenter, GetEntitySilently().Id)
            : null;

        public abstract void Sync(EcsEntity entity);
        public void Dispose() => DisposeInternal();

        protected abstract void InitInternal(EcsProperty property);
        protected abstract void DisposeInternal();

        void IInitialize<EcsProperty>.Init(EcsProperty property) => InitInternal(property);
    }

    [DisallowMultipleComponent]
    public class ProviderEcs<T> : ProviderEcs where T : new()
    {
        private static readonly bool s_isPresenterType = typeof(EcsPresenter).IsAssignableFrom(typeof(T));
        private static readonly List<ITypedComponentProvider> s_sharedComponentCache = new(16);

        [SerializeField] protected T _value;

        private bool _isDestroying;
        private EcsEntity _linkedEntity;
        private ProviderEcs _cachedRootProvider;

        public override bool IsPresenter => s_isPresenterType;

        public override EcsEntity Entity
        {
            get
            {
                var entity = GetEntitySilently();
                if (entity.Presenter == null)
                {
                    throw new Exception($"[ProviderEcs<{typeof(T).Name}>] Entity is not linked on '{name}'.");
                }
                return entity;
            }
        }

        public ref T Value => ref Entity.Get<T>();

        internal override EcsEntity GetEntitySilently()
        {
            if (s_isPresenterType)
            {
                return _linkedEntity;
            }
            return GetParentEntitySilently();
        }

        public EcsEntity NewEntity() => CreateEntity();

        protected virtual void Awake()
        {
            if (s_isPresenterType)
            {
                InitializeAsPresenter();
            }
            else
            {
                ProcessComponents();
                if (_cachedRootProvider == null)
                {
                    Debug.LogWarning($"[ProviderEcs<{typeof(T).Name}>] No Root Provider found on '{name}'. This component won't sync automatically.");
                }
            }
        }

        private void OnDestroy() => Dispose();

        public override void Sync(EcsEntity entity)
        {
            if (s_isPresenterType || entity.Presenter == null) return;
            entity.Add(_value);
        }

        protected override void InitInternal(EcsProperty property)
        {
            _linkedEntity = new EcsEntity(property.Id, property.Presenter);
        }

        protected override void DisposeInternal()
        {
            if (_isDestroying) return;
            _isDestroying = true;

            if (gameObject != null && gameObject.scene.IsValid())
            {
                var entity = GetEntitySilently();
                if (entity.Presenter != null)
                {
                    entity.Destroy();
                }
                Destroy(gameObject);
            }

            _linkedEntity = default;
            _cachedRootProvider = null;
        }

        private void InitializeAsPresenter()
        {
            if (_isDestroying || _linkedEntity.Presenter != null) return;
            CreateEntity();
        }

        private EcsEntity CreateEntity()
        {
            return Build.For(typeof(T))
                     .Add()
                     .WithPost(e => ProcessComponents(e))
                     .WithLink(this)
                     .Create();
        }

        private void ProcessComponents(EcsEntity entityToSync = default)
        {
            if (_isDestroying) return;
            var isSyncMode = entityToSync.Presenter != null;

            GetComponents(s_sharedComponentCache);
            foreach (var provider in s_sharedComponentCache)
            {
                if (ReferenceEquals(provider, this)) continue;

                if (isSyncMode)
                {
                    provider.Sync(entityToSync);
                }
                else if (provider is ProviderEcs root && root.IsPresenter)
                {
                    _cachedRootProvider = root;
                    break;
                }
            }
            s_sharedComponentCache.Clear();
        }

        private EcsEntity GetParentEntitySilently()
        {
            if (_cachedRootProvider == null) ProcessComponents();
            return _cachedRootProvider != null ? _cachedRootProvider.GetEntitySilently() : default;
        }

        public void PushToEcs()
        {
            var entity = GetEntitySilently();
            if (entity.Presenter != null) entity.Get<T>() = _value;
        }

        public void PullFromEcs()
        {
            var entity = GetEntitySilently();
            if (entity.Presenter != null) _value = entity.Get<T>();
        }
    }
}

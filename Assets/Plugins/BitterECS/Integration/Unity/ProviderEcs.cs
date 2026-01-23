using System;
using System.Collections.Generic;
using System.Reflection;
using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration
{
    public interface ITypedComponentProvider
    {
        void Sync(EcsEntity entity);
    }

    [DisallowMultipleComponent]
    public abstract class ProviderEcs : MonoBehaviour, ITypedComponentProvider, ILinkableProvider
    {
        public abstract bool IsPresenter { get; }
        public abstract EcsEntity Entity { get; }
        public virtual EcsProperty Properties => Entity?.Properties;

        public abstract void Sync(EcsEntity entity);

        protected abstract void InitInternal(EcsProperty property);
        protected abstract void DisposeInternal();

        void IInitialize<EcsProperty>.Init(EcsProperty property) => InitInternal(property);
        public void Dispose() => DisposeInternal();
    }

    public class ProviderEcs<T> : ProviderEcs
    {
        private static readonly bool s_isPresenterType = typeof(EcsPresenter).IsAssignableFrom(typeof(T));
        private static readonly bool s_isValueType = typeof(T).IsValueType;

        private delegate void SyncDelegate(EcsEntity entity, in T value);
        private static readonly SyncDelegate s_syncAction;

        private static readonly List<ITypedComponentProvider> s_componentCache = new(16);

        static ProviderEcs()
        {
            if (s_isValueType && !s_isPresenterType)
            {
                try
                {
                    var methodInfo = typeof(EcsEntity).GetMethod("AddOrReplace", BindingFlags.Instance | BindingFlags.Public);
                    if (methodInfo != null)
                    {
                        var genericMethod = methodInfo.MakeGenericMethod(typeof(T));

                        s_syncAction = (SyncDelegate)Delegate.CreateDelegate(typeof(SyncDelegate), genericMethod);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ProviderEcs] Failed to create sync delegate for {typeof(T)}: {e}");
                }
            }
        }


        [SerializeField]
        protected T _value;

        public ref T Value => ref _value;

        private bool _isDestroying = false;

        private EcsProperty _properties;
        private ProviderEcs _cachedRootProvider;

        public override bool IsPresenter => s_isPresenterType;

        public override EcsProperty Properties => s_isPresenterType ? _properties : base.Properties;

        public override EcsEntity Entity
        {
            get
            {
                if (s_isPresenterType)
                {
                    if (_properties?.Presenter != null)
                        return _properties.Presenter.Get(_properties.Id);

                    return UnLinkingRegistrationEntity();
                }

                if (_cachedRootProvider != null)
                    return _cachedRootProvider.Entity;

                FindAndCacheRoot();
                return _cachedRootProvider?.Entity;
            }
        }


        protected virtual void Awake()
        {
            if (s_isPresenterType)
            {
                HandlePresenterAwake();
            }
            else
            {
                FindAndCacheRoot();
                if (_cachedRootProvider == null)
                {
                    Debug.LogError($"[ProviderEcs<{typeof(T).Name}>] Failed to find a valid Parent Entity (Presenter) on GameObject '{name}'. Check if a Root Provider is attached.", this);
                }
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public override void Sync(EcsEntity entity)
        {
            if (s_isPresenterType) return;
            if (!s_isValueType) return;
            if (entity == null) return;

            s_syncAction?.Invoke(entity, _value);
        }

        private void FindAndCacheRoot()
        {
            if (_isDestroying)
                return;

            GetComponents(s_componentCache);

            for (int i = 0; i < s_componentCache.Count; i++)
            {
                if (s_componentCache[i] is ProviderEcs provider && provider.IsPresenter)
                {
                    _cachedRootProvider = provider;
                    s_componentCache.Clear();
                    return;
                }
            }

            s_componentCache.Clear();
        }

        private void HandlePresenterAwake()
        {
            try
            {
                LinkingRegistrationEntity();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while creating entity for {typeof(T).Name}: {ex.Message}");
            }
        }

        private EcsEntity UnLinkingRegistrationEntity() => Build.For(typeof(T))
            .Add<EcsEntity>()
            .WithPost(RegistrationComponent)
            .Create();

        private EcsEntity LinkingRegistrationEntity() => Build.For(typeof(T))
            .Add<EcsEntity>()
            .WithPost(entity => RegistrationComponent(entity))
            .WithLink(this)
            .Create();

        private void RegistrationComponent(EcsEntity entity)
        {
            if (_isDestroying)
                return;

            GetComponents(s_componentCache);

            var count = s_componentCache.Count;
            for (var i = 0; i < count; i++)
            {
                var provider = s_componentCache[i];
                if (ReferenceEquals(provider, this)) continue;

                provider.Sync(entity);
            }

            s_componentCache.Clear();
        }

        private void HandlePresenterDispose()
        {
            if (_properties == null) return;

            var presenter = _properties.Presenter;
            var id = _properties.Id;

            if (presenter != null && presenter.Has(id))
            {
                var entity = presenter.Get(id);
                _properties = null;
                presenter.Remove(entity);
            }
            else
            {
                _properties = null;
            }
        }

        protected override void InitInternal(EcsProperty property)
        {
            if (s_isPresenterType)
                _properties ??= property;
        }

        protected override void DisposeInternal()
        {
            if (_isDestroying)
                return;

            _isDestroying = true;
            if (s_isPresenterType)
            {
                HandlePresenterDispose();
            }
            else
            {
                Entity?.Dispose();
            }

            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (!s_isPresenterType) ValidateHasPresenter();
        }

        private void OnValidate()
        {
            if (!s_isPresenterType)
            {
                if (!Application.isPlaying) ValidateHasPresenter();

                if (Application.isPlaying && (_cachedRootProvider != null || Entity != null))
                {
                    var entity = Entity;
                    if (entity != null) Sync(entity);
                }
            }
        }

        private void ValidateHasPresenter()
        {
            var hasPresenter = false;
            var providers = GetComponents<ProviderEcs>();
            foreach (var p in providers) { if (p.IsPresenter) { hasPresenter = true; break; } }

            if (!hasPresenter)
            {
                var links = GetComponents<ILinkableProvider>();
                foreach (var l in links)
                {
                    if (!ReferenceEquals(l, this) && l is not ProviderEcs) { hasPresenter = true; break; }
                }
            }

            if (!hasPresenter)
                Debug.LogError($"[ProviderEcs] Configuration Error on '{name}': Component Provider <{typeof(T).Name}> cannot be added without a Presenter/Root Provider!", this);
        }
#endif
    }
}

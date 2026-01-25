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

    public abstract class ProviderEcs : MonoBehaviour, ITypedComponentProvider, ILinkableProvider
    {
        public abstract bool IsPresenter { get; }
        public abstract EcsEntity Entity { get; }
        public virtual EcsProperty Properties => Entity?.Properties;

        public abstract void Sync(EcsEntity entity);
        public void Dispose() => DisposeInternal();

        protected abstract void InitInternal(EcsProperty property);
        protected abstract void DisposeInternal();

        void IInitialize<EcsProperty>.Init(EcsProperty property) => InitInternal(property);
    }

    [DisallowMultipleComponent]
    public class ProviderEcs<T> : ProviderEcs where T : new()
    {
        private delegate void SyncDelegate(EcsEntity entity, in T value);

        private static readonly bool s_isPresenterType = typeof(EcsPresenter).IsAssignableFrom(typeof(T));
        private static readonly bool s_isValueType = typeof(T).IsValueType;
        private static readonly SyncDelegate s_syncAction;
        private static readonly List<ITypedComponentProvider> s_sharedComponentCache = new(16);

        static ProviderEcs()
        {
            if (!s_isValueType || s_isPresenterType) return;

            try
            {
                var methodInfo = typeof(EcsEntity).GetMethod(nameof(EcsEntity.AddOrReplace), BindingFlags.Instance | BindingFlags.Public);
                if (methodInfo != null)
                {
                    var genericMethod = methodInfo.MakeGenericMethod(typeof(T));
                    s_syncAction = (SyncDelegate)Delegate.CreateDelegate(typeof(SyncDelegate), genericMethod);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"[ProviderEcs] Delegate creation failed for {typeof(T)}: {e}");
            }
        }

        [SerializeField] protected T _value;

        private bool _isDestroying;
        private EcsProperty _properties;
        private ProviderEcs _cachedRootProvider;

        public override bool IsPresenter => s_isPresenterType;

        public override EcsProperty Properties => s_isPresenterType ? _properties : base.Properties;

        public ref T Value => ref SyncInspectorValue(ref Entity.Get<T>());

        public override EcsEntity Entity => s_isPresenterType ? EnsureEntityCreated() : GetParentEntity();

        protected virtual void Awake()
        {
            if (s_isPresenterType)
            {
                EnsureEntityCreated();
            }
            else
            {
                ProcessComponents();
                if (_cachedRootProvider == null)
                {
                    throw new Exception($"[ProviderEcs<{typeof(T).Name}>] Missing Root Provider on '{name}'.");
                }
            }
        }

        private void OnDestroy() => Dispose();

        public override void Sync(EcsEntity entity)
        {
            if (s_isPresenterType || !s_isValueType || entity == null) return;
            s_syncAction?.Invoke(entity, _value);
        }

        protected override void InitInternal(EcsProperty property)
        {
            if (s_isPresenterType) _properties ??= property;
        }

        protected override void DisposeInternal()
        {
            if (_isDestroying) return;
            _isDestroying = true;

            if (s_isPresenterType) HandlePresenterDispose();
            else Entity?.Dispose();

            if (gameObject != null) Destroy(gameObject);
        }

        private EcsEntity EnsureEntityCreated()
        {
            if (_isDestroying) return null;

            if (_properties?.Presenter != null)
            {
                var existing = _properties.Presenter.Get(_properties.Id);
                if (existing != null) return existing;
            }

            try
            {
                return Build.For(typeof(T))
                    .Add<EcsEntity>()
                    .WithPost(ProcessComponents)
                    .WithLink(this)
                    .Create();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProviderEcs<{typeof(T).Name}>] Creation error: {ex.Message}");
                return null;
            }
        }

        private void ProcessComponents(EcsEntity entityToSync = null)
        {
            var isSyncMode = entityToSync != null;

            if (_isDestroying) return;
            if (!isSyncMode && _cachedRootProvider != null) return;

            GetComponents(s_sharedComponentCache);

            foreach (var provider in s_sharedComponentCache)
            {
                if (ReferenceEquals(provider, this)) continue;

                if (isSyncMode)
                {
                    provider.Sync(entityToSync);
                }
                else
                {
                    if (provider is ProviderEcs { IsPresenter: true } root)
                    {
                        _cachedRootProvider = root;
                        break;
                    }
                }
            }

            s_sharedComponentCache.Clear();
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

        private ref T SyncInspectorValue(ref T v)
        {
            _value = v;
            return ref v;
        }

        private EcsEntity GetParentEntity()
        {
            ProcessComponents();
            return _cachedRootProvider?.Entity;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (!s_isPresenterType) ValidateHasPresenter();
        }

        private void OnValidate()
        {
            if (s_isPresenterType) return;

            if (!Application.isPlaying)
            {
                ValidateHasPresenter();
            }
            else if (_cachedRootProvider != null || Entity != null)
            {
                var entity = Entity;
                if (entity != null) Sync(entity);
            }
        }

        private void ValidateHasPresenter()
        {
            var components = GetComponents<Component>();
            var hasPresenter = false;

            foreach (var c in components)
            {
                if (c is ProviderEcs { IsPresenter: true })
                {
                    hasPresenter = true;
                    break;
                }

                if (c is ILinkableProvider && !ReferenceEquals(c, this) && c is not ProviderEcs)
                {
                    hasPresenter = true;
                    break;
                }
            }

            if (!hasPresenter)
            {
                Debug.LogError($"[ProviderEcs] Configuration Error on '{name}': Provider <{typeof(T).Name}> requires a Presenter/Root Provider!", this);
            }
        }
#endif
    }
}

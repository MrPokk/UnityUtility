using System.Collections.Generic;
using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration.Unity
{
    public interface ITypedComponentProvider
    {
        void Sync(EcsEntity entity);
    }

    public abstract class ProviderEcs : MonoBehaviour, ITypedComponentProvider, ILinkableProvider
    {
        private static EcsWorld s_activeWorld;
        public static EcsWorld ActiveWorld
        {
            get => s_activeWorld ?? EcsWorldStatic.Instance;
            set => s_activeWorld = value;
        }

        protected static readonly List<ProviderEcs> ProvidersCache = new(16);

        public abstract EcsEntity Entity { get; }

        internal abstract EcsEntity GetEntityRaw();

        public abstract void AddToBuilder(ref EcsBuilder builder);

        public abstract void Sync(EcsEntity entity);

        public void Dispose() => DisposeInternal();
        protected abstract void InitInternal(EcsProperty property);
        protected abstract void DisposeInternal();
        void IInitialize<EcsProperty>.Init(EcsProperty property) => InitInternal(property);

        public virtual EcsProperty Properties => GetEntityRaw().World != null
            ? new EcsProperty(GetEntityRaw().World, GetEntityRaw().Id)
            : default;
    }

    [DisallowMultipleComponent]
    public class ProviderEcs<T> : ProviderEcs where T : new()
    {
        [SerializeField] protected T _value;

        private EcsEntity _linkedEntity;
        private bool _isDestroying;

        public override EcsEntity Entity => GetOrUpdateEntity();

        internal override EcsEntity GetEntityRaw() => _linkedEntity;

        public ref T Value => ref Entity.Get<T>();

        protected virtual void Awake()
        {
            GetOrUpdateEntity();
        }

        private EcsEntity GetOrUpdateEntity()
        {
            if (_linkedEntity.IsAlive) return _linkedEntity;

            GetComponents(ProvidersCache);
            for (var i = 0; i < ProvidersCache.Count; i++)
            {
                var siblingEntity = ProvidersCache[i].GetEntityRaw();
                if (siblingEntity.IsAlive)
                {
                    _linkedEntity = siblingEntity;
                    ProvidersCache.Clear();
                    return _linkedEntity;
                }
            }

            _linkedEntity = CreateEntityForGameObject(ActiveWorld);
            return _linkedEntity;
        }

        private void OnDestroy() => Dispose();

        public override void AddToBuilder(ref EcsBuilder builder)
        {
            builder.With(_value);
        }

        public override void Sync(EcsEntity entity)
        {
            if (entity.World == null) return;
            entity.Add(_value);
        }

        protected override void InitInternal(EcsProperty property)
        {
            _linkedEntity = new EcsEntity(property.World, property.Id);
        }

        protected override void DisposeInternal()
        {
            if (_isDestroying) return;
            _isDestroying = true;

            if (_linkedEntity.IsAlive)
            {
                _linkedEntity.Destroy();
            }

            if (gameObject != null) Destroy(gameObject);
            _linkedEntity = default;
        }

        private EcsEntity CreateEntityForGameObject(EcsWorld world)
        {
            var builder = new EcsBuilder(world);

            if (ProvidersCache.Count == 0) GetComponents(ProvidersCache);

            for (var i = 0; i < ProvidersCache.Count; i++)
            {
                ProvidersCache[i].AddToBuilder(ref builder);

                builder.WithLink(ProvidersCache[i]);
            }

            ProvidersCache.Clear();

            return builder.Create();
        }

        public EcsEntity ToEntity(EcsWorld world = default) => CreateEntityForGameObject(world ?? ActiveWorld);

        public void PushToEcs()
        {
            if (Entity.IsAlive) Entity.Get<T>() = _value;
        }

        public void PullFromEcs()
        {
            if (Entity.Has<T>()) _value = Entity.Get<T>();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying || _isDestroying) return;
            if (Entity.Has<T>()) _value = Entity.Get<T>();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying || _isDestroying) return;
            if (Entity.IsAlive) Entity.Get<T>() = _value;
        }
#endif
    }

    public struct UnityComponent<T> where T : Component
    {
        public T value;

        public static implicit operator T(UnityComponent<T> proxy) => proxy.value;
    }

    public abstract class ProxyProvider<T> : ProviderEcs<UnityComponent<T>> where T : Component
    {
        protected override void Awake()
        {
            _value.value = TryGetComponent<T>(out var component) ? component : gameObject.AddComponent<T>();

            base.Awake();
        }

        public override void AddToBuilder(ref EcsBuilder builder)
        {
            _value.value ??= GetComponent<T>();
            builder.With(_value);
        }
    }
}

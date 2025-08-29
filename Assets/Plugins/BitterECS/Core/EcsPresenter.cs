using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public abstract class EcsPresenter : IDisposable
    {
        private EcsEntity[] _entities;
        private ushort _entitiesCount;
        private readonly Stack<ushort> _freeEntityIds;
        private readonly Dictionary<Type, object> _pools;
        private readonly HashSet<Type> _allowedTypes;
        private readonly Dictionary<EcsEntity, ILinkableProvider> _linkedEntities;

        public int EntityCount => _entitiesCount - _freeEntityIds.Count;
        public int Capacity => _entities.Length;

        protected EcsPresenter()
        {
            _entities = new EcsEntity[EcsConfig.InitialEntitiesCapacity];
            _freeEntityIds = new Stack<ushort>(EcsConfig.InitialEntitiesCapacity);
            _pools = new Dictionary<Type, object>(EcsConfig.InitialPoolCapacity);
            _allowedTypes = new HashSet<Type>();
            _linkedEntities = new Dictionary<EcsEntity, ILinkableProvider>(EcsConfig.InitialLinkedEntitiesCapacity);
            _entitiesCount = 0;

            Registration();
        }

        protected abstract void Registration();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AddLimitedType<T>() where T : EcsEntity => _allowedTypes.Add(typeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTypeAllowed(Type type)
        {
            if (_allowedTypes.Count == 0)
            {
                return true;
            }

            if (_allowedTypes.Contains(type))
            {
                return true;
            }

            foreach (var allowedType in _allowedTypes)
            {
                if (allowedType.IsAssignableFrom(type))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTypeAllowed<T>() where T : EcsEntity => IsTypeAllowed(typeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEntity Get(ushort id) => _entities[id];

        public EcsFilter Filter() => new(this);

        public EntityBuilder AddTo() => new(this);
        public EntityBuilder<T> AddTo<T>() where T : EcsEntity => new(this);

        public EntityDestroyer RemoveTo(EcsEntity entity) => new(this, entity);
        public EntityDestroyer<T> RemoveTo<T>(T entity) where T : EcsEntity => new(this, entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(EcsEntity entity) => Create(entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEntity Add(Type type) => Create(type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEntity Add<T>() where T : EcsEntity => Create<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(EcsEntity entity) => DestroyEntity(entity);

        internal void Create(EcsEntity entity)
        {
            InitEntity(entity);
        }

        internal EcsEntity Create(Type type)
        {
            var entity = (EcsEntity)Activator.CreateInstance(type);
            return InitEntity(entity);
        }

        internal T Create<T>() where T : EcsEntity
        {
            var entity = Activator.CreateInstance<T>();
            return (T)InitEntity(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EcsEntity InitEntity(EcsEntity entity)
        {
            if (!IsTypeAllowed(entity.GetType()))
            {
                throw new InvalidOperationException($"Can't create entity of type {entity.GetType().Name}");
            }

            var entityId = GetNextEntityId();
            entity.Init(new EcsEntityProperty(this, entityId));

            if (entityId >= _entities.Length)
            {
                var newSize = _entities.Length * EcsConfig.PoolGrowthFactor;
                Array.Resize(ref _entities, newSize);
            }

            _entities[entityId] = entity;
            entity.Registration();

            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort GetNextEntityId()
        {
            if (_freeEntityIds.Count > 0)
            {
                return _freeEntityIds.Pop();
            }

            return _entitiesCount++;
        }

        internal void DestroyEntity(EcsEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entityId = entity.Properties.Id;
            if (entityId >= _entities.Length || _entities[entityId] != entity)
            {
                return;
            }

            Unlink(entity);
            RemoveAllComponents(entityId);
            _entities[entityId] = null;
            _freeEntityIds.Push(entityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveAllComponents(ushort entityId)
        {
            foreach (var pool in _pools.Values)
            {
                ((IPoolDestroy)pool).Remove(entityId);
            }
        }

        public EcsPool<T> GetPool<T>() where T : struct
        {
            var poolType = typeof(T);
            if (!_pools.TryGetValue(poolType, out object pool))
            {
                pool = new EcsPool<T>();
                _pools[poolType] = pool;
            }

            return (EcsPool<T>)pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetPool<T>(out EcsPool<T> pool) where T : struct
        {
            pool = null;

            if (_pools.TryGetValue(typeof(T), out object poolObj))
            {
                pool = (EcsPool<T>)poolObj;
                return true;
            }
            return false;
        }

        public void Link(EcsEntity entity, ILinkableProvider provider)
        {
            if (entity == null || provider == null)
                return;

            provider.Init(new EcsProviderProperty(this));
            _linkedEntities[entity] = provider;
        }

        public void Unlink(EcsEntity entity)
        {
            if (entity == null || !_linkedEntities.ContainsKey(entity))
            {
                return;
            }

            if (_linkedEntities.Remove(entity, out var provider))
            {
                provider?.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ILinkableProvider GetProvider(EcsEntity entity)
        {
            return entity != null && _linkedEntities.TryGetValue(entity, out var provider) ? provider : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetProvider<T>(EcsEntity entity) where T : class, ILinkableProvider
        {
            return GetProvider(entity) as T;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEntity Get(ILinkableProvider provider)
        {
            foreach (var kvp in _linkedEntities.Where(kvp => kvp.Value == provider))
            {
                return kvp.Key;
            }

            return null;
        }

        public EcsEntity[] GetAll() => _entities.Where(x => x != null).ToArray();

        public void Dispose()
        {
            for (var i = 0; i < _entitiesCount; i++)
            {
                _entities[i]?.Dispose();
                _entities[i] = null;
            }

            foreach (var pool in _pools.Values)
            {
                ((IDisposable)pool)?.Dispose();
            }

            _entities = null;
            _freeEntityIds.Clear();
            _pools.Clear();
            _allowedTypes.Clear();
            _linkedEntities.Clear();
            GC.SuppressFinalize(this);
        }
    }

    public static class EcsPresenterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateEntities<T>(this EcsPresenter presenter, int count)
            where T : EcsEntity
        {
            for (int i = 0; i < count; i++)
            {
                presenter.Create<T>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyAllEntities(this EcsPresenter presenter)
        {
            var entities = presenter.GetAll().ToArray();
            foreach (var entity in entities)
            {
                presenter.DestroyEntity(entity);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace BitterECS.Core
{
    public abstract partial class EcsPresenter : IDisposable
    {
        private EcsEntity[] _entities;
        private int _entitiesCount;
        private readonly Stack<int> _freeEntityIds;
        private readonly Dictionary<Type, Func<object>> _poolFactories;
        private readonly Dictionary<Type, object> _pools;
        private readonly HashSet<Type> _allowedTypes;
        private readonly Dictionary<EcsEntity, ILinkableProvider> _linkedEntities;

        public int CountEntity => _entitiesCount - _freeEntityIds.Count;
        public int Capacity => _entities.Length;
        public IReadOnlyDictionary<Type, object> Pools => _pools;

        protected EcsPresenter()
        {
            _entities = new EcsEntity[EcsConfig.InitialEntitiesCapacity];
            _freeEntityIds = new Stack<int>(EcsConfig.InitialEntitiesCapacity);
            _linkedEntities = new Dictionary<EcsEntity, ILinkableProvider>(EcsConfig.InitialLinkedEntitiesCapacity);
            _pools = new Dictionary<Type, object>(EcsConfig.InitialPoolCapacity);
            _poolFactories = new Dictionary<Type, Func<object>>();
            _allowedTypes = new HashSet<Type>();
            _entitiesCount = 0;

            Registration();
        }

        protected abstract void Registration();

        protected void AddLimitedType<T>() where T : EcsEntity => _allowedTypes.Add(typeof(T));
        public void AddPoolFactory<T>(Func<EcsPool<T>> factory) where T : struct => _poolFactories[typeof(T)] = factory;
        public void AddCheckEvent<T>() where T : struct => AddPoolFactory(() => new EcsEventPool<T>());

        public bool IsTypeAllowed(Type type)
        {
            if (_allowedTypes.Count == 0 || _allowedTypes.Contains(type))
            {
                return true;
            }

            return _allowedTypes.Any(allowedType => type.IsSubclassOf(allowedType));
        }
        public bool IsTypeAllowed<T>() where T : EcsEntity => IsTypeAllowed(typeof(T));

        public void Add(EcsEntity entity, bool force = false) => Create(entity, force);
        public EcsEntity Add(Type type, bool force = false) => Create(type, force);
        public EcsEntity Add<T>(bool force = false) where T : EcsEntity => Create<T>(force);

        internal void Create(EcsEntity entity, bool force = false) => InitEntity(entity, force);
        internal EcsEntity Create(Type type, bool force = false) => InitEntity((EcsEntity)Activator.CreateInstance(type), force);
        internal T Create<T>(bool force = false) where T : EcsEntity => (T)InitEntity(Activator.CreateInstance<T>(), force);

        private EcsEntity InitEntity(EcsEntity entity, bool force = false)
        {
            if (!IsTypeAllowed(entity.GetType()) && !force)
            {
                throw new InvalidOperationException($"Can't create entity of type {entity.GetType().Name}");
            }

            var entityId = GetNextEntityId();
            entity.Init(new(this, entityId));

            if (entityId >= _entities.Length)
            {
                Array.Resize(ref _entities, (_entities.Length + 1) * EcsConfig.PoolGrowthFactor);
            }

            _entities[entityId] = entity;
            entity.Registration();
            EcsWorld.IncreaseVersion();

            return entity;
        }

        private int GetNextEntityId() => _freeEntityIds.Count > 0 ? _freeEntityIds.Pop() : _entitiesCount++;

        public void Remove(EcsEntity entity) => DestroyEntity(entity);

        internal void DestroyEntity(EcsEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            var entityId = entity.GetID();
            if (entityId >= _entities.Length || _entities[entityId] != entity)
            {
                return;
            }

            _entities[entityId] = null;
            _freeEntityIds.Push(entityId);

            Unlink(entity);
            RemoveAllComponents(entityId);
            EcsWorld.IncreaseVersion();
        }

        private void RemoveAllComponents(int entityId)
        {
            foreach (var pool in _pools.Values)
            {
                ((IPoolDestroy)pool).Remove(entityId);
            }
        }

        public EcsPool<T> GetPool<T>() where T : struct
        {
            var poolType = typeof(T);
            if (_pools.TryGetValue(poolType, out var pool))
            {
                return (EcsPool<T>)pool;
            }

            pool = CreatePool<T>();
            _pools[poolType] = pool;
            return (EcsPool<T>)pool;
        }

        internal object CreatePool<T>() where T : struct
        {
            var poolType = typeof(T);
            return _poolFactories.TryGetValue(poolType, out var factory)
                ? factory()
                : new EcsPool<T>();
        }

        public void Link(EcsEntity entity, ILinkableProvider provider)
        {
            if (entity == null || provider == null)
            {
                return;
            }

            _linkedEntities.TryAdd(entity, provider);
            provider.Init(entity.Properties);
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

        public ILinkableProvider GetProvider(EcsEntity entity)
            => entity != null && _linkedEntities.TryGetValue(entity, out var provider) ? provider : null;

        public EcsEntity Get(ILinkableProvider provider)
            => _linkedEntities.FirstOrDefault(kvp => kvp.Value == provider).Key;

        public EcsEntity Get(int id) => id >= 0 && id < _entities.Length ? _entities[id] : null;

        public bool Has(int id) => id >= 0 && id < _entities.Length && _entities[id] != null;

        public List<EcsEntity> GetAliveEntities()
        {
            return new ArraySegment<EcsEntity>(_entities, 0, _entitiesCount)
                .Where(entity => entity != null)
                .ToList();
        }

        public void Dispose()
        {
            foreach (var pool in _pools.Values)
            {
                ((IDisposable)pool)?.Dispose();
            }

            _entitiesCount = 0;
            _entities = Array.Empty<EcsEntity>();
            _freeEntityIds.Clear();
            _pools.Clear();
            _allowedTypes.Clear();
            _linkedEntities.Clear();
        }
    }
}

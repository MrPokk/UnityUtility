using System;
using System.Collections.Generic;
using System.Linq;

namespace BitterECS.Core
{
    public abstract class EcsPresenter : IDisposable
    {
        private int _nextEntityId = 1;
        private readonly List<EcsEntity> _entities = new(EcsConfig.InitialEntitiesCapacity);
        private readonly Dictionary<Type, object> _pools = new(EcsConfig.InitialPoolCapacity);
        private readonly HashSet<Type> _allowedEntityTypes = new();

        public IReadOnlyCollection<EcsEntity> GetAll() => _entities.AsReadOnly();

        protected void AddLimitedType<T>() where T : EcsEntity => _allowedEntityTypes.Add(typeof(T));

        public EntityBuilder<T> AddEntity<T>() where T : EcsEntity
        {
            return new EntityBuilder<T>(this);
        }

        public EntityDestroyer<T> RemoveEntity<T>(T entity) where T : EcsEntity
        {
            return new EntityDestroyer<T>(this, entity);
        }

        internal T CreateEntity<T>() where T : EcsEntity
        {
            var entity = Activator.CreateInstance<T>();
            entity.Init(new(this, _nextEntityId++));
            entity.Registration();
            _entities.Add(entity);
            return entity;
        }

        internal void DestroyEntity<T>(T entity) where T : EcsEntity
        {
            if (_entities.Remove(entity))
            {
                entity.Dispose();
            }
        }

        public EcsFilter Filter()
        {
            return new EcsFilter(this);
        }

        public EcsPool<T> GetPool<T>() where T : struct
        {
            if (!_pools.TryGetValue(typeof(T), out var pool))
            {
                pool = new EcsPool<T>();
                _pools[typeof(T)] = pool;
            }
            return (EcsPool<T>)pool;
        }

        public bool IsTypeAllowed(Type type)
        {
            return _allowedEntityTypes.Count == 0
                ? typeof(EcsEntity).IsAssignableFrom(type)
                : _allowedEntityTypes.Any(allowedType => allowedType.IsAssignableFrom(type));
        }

        public void Dispose()
        {
            foreach (var entity in _entities)
            {
                entity.Dispose();
            }
            _entities.Clear();

            foreach (var pool in _pools.Values)
            {
                if (pool is IDisposable disposablePool)
                {
                    disposablePool.Dispose();
                }
            }
            _pools.Clear();
            _allowedEntityTypes.Clear();
            GC.SuppressFinalize(this);
        }
    }
}

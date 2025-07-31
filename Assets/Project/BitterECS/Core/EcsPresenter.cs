using System;
using System.Collections.Generic;
using System.Linq;

namespace BitterECS.Core
{
    public abstract class EcsPresenter : IDisposable
    {
        private readonly Dictionary<Type, object> _pools = new();
        private int _nextEntityId = 1;

        private readonly HashSet<Type> _allowedEntityTypes = new();

        public T NewEntity<T>() where T : EcsEntity
        {
            var entity = Activator.CreateInstance<T>();
            entity.Init(new(this, _nextEntityId++));
            entity.Registration();
            return entity;
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

        protected void AddLimitedType<T>() where T : EcsEntity => _allowedEntityTypes.Add(typeof(T));

        public bool IsTypeAllowed(Type type)
        {
            return _allowedEntityTypes.Count == 0
                ? typeof(EcsEntity).IsAssignableFrom(type)
                : _allowedEntityTypes.Any(allowedType => allowedType.IsAssignableFrom(type));
        }

        public void Dispose()
        {
            foreach (var item in _pools)
            {
                var disposable = item.Value as IDisposable;
                disposable.Dispose();
            }
            _pools.Clear();
            GC.SuppressFinalize(this);
        }
    }
}

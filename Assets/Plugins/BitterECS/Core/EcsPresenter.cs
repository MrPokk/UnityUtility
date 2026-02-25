using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public abstract partial class EcsPresenter : IDisposable
    {
        private int[] _aliveIds;
        private int[] _idToIndex;
        private int _entitiesCount;
        private int _aliveCount;

        private readonly Stack<int> _freeEntityIds;
        private readonly Dictionary<Type, Func<object>> _poolFactories;
        private readonly Dictionary<Type, object> _pools;
        private readonly Dictionary<int, ILinkableProvider> _linkedProviders;

        private int[] _componentCounts;

        public int CountEntity => _aliveCount;

        protected EcsPresenter()
        {
            var capacity = EcsConfig.InitialEntitiesCapacity;
            _aliveIds = new int[capacity];
            _idToIndex = new int[capacity];
            Array.Fill(_idToIndex, -1);
            _componentCounts = new int[EcsConfig.InitialEntitiesCapacity];

            _freeEntityIds = new Stack<int>(capacity);
            _pools = new Dictionary<Type, object>(EcsConfig.InitialPoolCapacity);
            _poolFactories = new Dictionary<Type, Func<object>>();
            _linkedProviders = new Dictionary<int, ILinkableProvider>(EcsConfig.InitialLinkedEntitiesCapacity);
            _aliveCount = 0;
            _entitiesCount = 0;

            Registration();
        }

        protected virtual void Registration() { }
        public void AddPoolFactory<T>(Func<EcsPool<T>> factory) where T : new() => _poolFactories[typeof(T)] = factory;
        public void AddCheckEvent<T>() where T : new() => AddPoolFactory(() => new EcsEventPool<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<int> GetAliveIds() => _aliveIds.AsSpan(0, _aliveCount);

        public EcsEntity CreateEntity()
        {
            int id;
            if (_freeEntityIds.Count > 0)
            {
                id = _freeEntityIds.Pop();
            }
            else
            {
                id = _entitiesCount++;
                if (id >= _idToIndex.Length) ResizeIdArrays(id * 2);
            }

            _idToIndex[id] = _aliveCount;
            _aliveIds[_aliveCount] = id;
            _aliveCount++;

            EcsWorld.IncreaseVersion();
            return new EcsEntity(id, this);
        }

        public void Remove(EcsEntity entity)
        {
            var id = entity.Id;
            if (id < 0 || id >= _idToIndex.Length) return;

            var index = _idToIndex[id];
            if (index == -1) return;
            _idToIndex[id] = -1;

            foreach (var pool in _pools.Values)
            {
                ((IPoolDestroy)pool).Remove(id);
            }

            if (_linkedProviders.Remove(id, out var provider))
            {
                provider.Dispose();
            }

            _aliveCount--;
            if (_aliveCount > 0 && index != _aliveCount)
            {
                var lastId = _aliveIds[_aliveCount];
                _aliveIds[index] = lastId;
                _idToIndex[lastId] = index;
            }

            _componentCounts[id] = 0;
            _freeEntityIds.Push(id);

            EcsWorld.IncreaseVersion();
        }

        private void ResizeIdArrays(int newSize)
        {
            Array.Resize(ref _aliveIds, newSize);
            Array.Resize(ref _componentCounts, newSize);
            var oldSize = _idToIndex.Length;
            Array.Resize(ref _idToIndex, newSize);
            for (var i = oldSize; i < newSize; i++)
            {
                _idToIndex[i] = -1;
            }
        }

        public EcsPool<T> GetPool<T>() where T : new()
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

        internal object CreatePool<T>() where T : new()
        {
            var poolType = typeof(T);
            return _poolFactories.TryGetValue(poolType, out var factory)
                ? factory()
                : new EcsPool<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int id) => id >= 0 && id < _idToIndex.Length && _idToIndex[id] != -1;

        public ILinkableProvider GetProvider(EcsEntity entity)
            => _linkedProviders.TryGetValue(entity.Id, out var provider) ? provider : null;

        public void Link(EcsEntity entity, ILinkableProvider provider)
        {
            _linkedProviders[entity.Id] = provider;
            provider.Init(new EcsProperty(this, entity.Id));
        }

        public void Unlink(EcsEntity entity)
        {
            if (_linkedProviders.Remove(entity.Id, out var provider))
            {
                provider.Dispose();
            }
        }

        public EcsEntity Get(int id) => Has(id) ? new EcsEntity(id, this) : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void IncrementCount(int id) => _componentCounts[id]++;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DecrementCount(int id) => _componentCounts[id]--;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetComponentCount(int id) => _componentCounts[id];

        public void Dispose()
        {
            foreach (var pool in _pools.Values)
            {
                if (pool is IDisposable disposable) disposable.Dispose();
            }
            _pools.Clear();

            if (_linkedProviders.Count > 0)
            {
                var providers = new ILinkableProvider[_linkedProviders.Count];
                _linkedProviders.Values.CopyTo(providers, 0);

                for (var i = 0; i < providers.Length; i++)
                {
                    providers[i]?.Dispose();
                }
            }

            _linkedProviders.Clear();
            _freeEntityIds.Clear();
            _poolFactories.Clear();
            _aliveCount = 0;
            _entitiesCount = 0;

            _aliveIds = Array.Empty<int>();
            _idToIndex = Array.Empty<int>();
            _componentCounts = Array.Empty<int>();
        }
    }
}

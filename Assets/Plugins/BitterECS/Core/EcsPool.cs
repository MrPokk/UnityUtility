using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public interface IPoolDestroy
    {
        bool Has(int entityId);
        void Remove(int entityId);
    }

    public class EcsPool<T> : IDisposable, IPoolDestroy where T : struct
    {
        private T[] _components;
        private int[] _entityToDataIndex;
        private int[] _dataIndexToEntity;
        private int _count;
        private Stack<int> _freeIndices;
        private readonly int _initialCapacity;

        public EcsPool(int initialCapacity = -1)
        {
            _initialCapacity = initialCapacity > 0 ? initialCapacity : EcsConfig.InitialPoolCapacity;
            _components = Array.Empty<T>();
            _entityToDataIndex = Array.Empty<int>();
            _dataIndexToEntity = Array.Empty<int>();
            _freeIndices = new Stack<int>(_initialCapacity);
            _count = 0;
        }

        public void Add(int entityId, in T component)
        {
            if (entityId >= _entityToDataIndex.Length)
            {
                var newSize = _entityToDataIndex.Length == 0
                    ? Math.Max(entityId + 1, _initialCapacity)
                    : Math.Max(entityId + 1, _entityToDataIndex.Length * EcsConfig.PoolGrowthFactor);

                Array.Resize(ref _entityToDataIndex, newSize);

                if (newSize > _count)
                {
                    for (int i = _count; i < newSize; i++)
                    {
                        _entityToDataIndex[i] = -1;
                    }
                }
            }

            if (_entityToDataIndex[entityId] != -1)
                return;

            int dataIndex;
            if (_freeIndices.Count > 0)
            {
                dataIndex = _freeIndices.Pop();
            }
            else
            {
                if (_count >= _components.Length)
                {
                    var newCapacity = _components.Length == 0
                        ? _initialCapacity
                        : _components.Length * EcsConfig.PoolGrowthFactor;

                    Array.Resize(ref _components, newCapacity);
                    Array.Resize(ref _dataIndexToEntity, newCapacity);
                }
                dataIndex = _count++;
            }

            _components[dataIndex] = component;
            _entityToDataIndex[entityId] = dataIndex;
            _dataIndexToEntity[dataIndex] = entityId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityId)
        {
#if DEBUG
            if (entityId >= _entityToDataIndex.Length || _entityToDataIndex[entityId] == -1)
            {
                throw new KeyNotFoundException($"Entity {entityId} doesn't have this component");
            }
#endif

            return ref _components[_entityToDataIndex[entityId]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityId)
        {
            return entityId < _entityToDataIndex.Length && _entityToDataIndex[entityId] != -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(int entityId, out T component)
        {
            if (entityId < _entityToDataIndex.Length && _entityToDataIndex[entityId] != -1)
            {
                component = _components[_entityToDataIndex[entityId]];
                return true;
            }

            component = default;
            return false;
        }

        public void Remove(int entityId)
        {
            if (entityId >= _entityToDataIndex.Length || _entityToDataIndex[entityId] == -1)
            {
                return;
            }

            var dataIndex = _entityToDataIndex[entityId];
            _entityToDataIndex[entityId] = -1;

            if (dataIndex < _count - 1)
            {
                _components[dataIndex] = _components[_count - 1];

                var movedEntityId = _dataIndexToEntity[_count - 1];
                _entityToDataIndex[movedEntityId] = dataIndex;
                _dataIndexToEntity[dataIndex] = movedEntityId;
            }
            else
            {
                _components[dataIndex] = default;
            }

            _count--;
            _freeIndices.Push(dataIndex);
        }

        public int Count => _count - _freeIndices.Count;

        public int Capacity => _components.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(this);

        public struct Enumerator
        {
            private readonly EcsPool<T> _pool;
            private int _index;
            private readonly int _count;

            public Enumerator(EcsPool<T> pool)
            {
                _pool = pool;
                _index = -1;
                _count = pool._count;
            }

            public T Current => _pool._components[_index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_index < _count)
                {
                    if (!_pool._freeIndices.Contains(_index))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetByDataIndex(int dataIndex)
        {
            return ref _components[dataIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetEntityId(int dataIndex)
        {
            return _dataIndexToEntity[dataIndex];
        }

        public void Clear()
        {
            if (_count > 0)
            {
                Array.Clear(_components, 0, _count);
                Array.Clear(_dataIndexToEntity, 0, _count);
            }

            for (int i = 0; i < _entityToDataIndex.Length; i++)
            {
                _entityToDataIndex[i] = -1;
            }

            _count = 0;
            _freeIndices.Clear();
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity > _components.Length)
            {
                int newCapacity = Math.Max(capacity, _components.Length * EcsConfig.PoolGrowthFactor);
                Array.Resize(ref _components, newCapacity);
                Array.Resize(ref _dataIndexToEntity, newCapacity);
            }
        }

        public void TrimExcess()
        {
            if (_count < _components.Length * 0.9)
            {
                Array.Resize(ref _components, _count);
                Array.Resize(ref _dataIndexToEntity, _count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsSpan() => new(_components, 0, _count);

        public IEnumerable<int> GetEntityIds()
        {
            for (int i = 0; i < _count; i++)
            {
                if (!_freeIndices.Contains(i))
                {
                    yield return _dataIndexToEntity[i];
                }
            }
        }

        public void Dispose()
        {
            _components = null;
            _entityToDataIndex = null;
            _dataIndexToEntity = null;
            _freeIndices = null;
            GC.SuppressFinalize(this);
        }
    }

    public static class EcsPoolExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddOrReplace<T>(this EcsPool<T> pool, int entityId, in T component) where T : struct
        {
            if (pool.Has(entityId))
            {
                pool.Get(entityId) = component;
            }
            else
            {
                pool.Add(entityId, component);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetOrAddDefault<T>(this EcsPool<T> pool, int entityId) where T : struct
        {
            if (!pool.Has(entityId))
            {
                pool.Add(entityId, default);
            }
            return ref pool.Get(entityId);
        }
    }
}

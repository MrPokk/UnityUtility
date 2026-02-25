using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public class EcsPool<T> : IDisposable, IPoolDestroy where T : new()
    {
        private T[] _components;
        private int[] _entityToDataIndex;
        private int[] _dataIndexToEntity;
        private int _count;
        private readonly int _initialCapacity;

        public int Count => _count;
        public int Capacity => _components.Length;

        public EcsPool(int initialCapacity = -1)
        {
            _initialCapacity = initialCapacity > 0 ? initialCapacity : EcsConfig.InitialPoolCapacity;
            _components = Array.Empty<T>();
            _entityToDataIndex = Array.Empty<int>();
            _dataIndexToEntity = Array.Empty<int>();
            _count = 0;
        }

        public virtual void Add(int entityId, in T component)
        {
            EnsureEntityCapacity(entityId);

            if (Has(entityId))
            {
                return;
            }

            EnsureComponentCapacity();

            var dataIndex = _count;

            _components[dataIndex] = component;
            _entityToDataIndex[entityId] = dataIndex;
            _dataIndexToEntity[dataIndex] = entityId;

            _count++;
            EcsWorld.IncreaseVersion();
        }

        public virtual void Remove(int entityId)
        {
            if (!Has(entityId))
            {
                return;
            }

            var dataIndex = _entityToDataIndex[entityId];
            var lastIndex = _count - 1;

            if (dataIndex != lastIndex)
            {
                _components[dataIndex] = _components[lastIndex];

                var entityMoved = _dataIndexToEntity[lastIndex];
                _dataIndexToEntity[dataIndex] = entityMoved;
                _entityToDataIndex[entityMoved] = dataIndex;
            }

            _components[lastIndex] = default;
            _dataIndexToEntity[lastIndex] = -1;
            _entityToDataIndex[entityId] = -1;

            _count--;
            EcsWorld.IncreaseVersion();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityId)
        {
            return ref _components[_entityToDataIndex[entityId]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityId)
        {
            return entityId < _entityToDataIndex.Length && _entityToDataIndex[entityId] != -1;
        }

        private void EnsureEntityCapacity(int entityId)
        {
            if (entityId >= _entityToDataIndex.Length)
            {
                var oldLength = _entityToDataIndex.Length;
                var newSize = oldLength == 0
                    ? Math.Max(entityId + 1, _initialCapacity)
                    : Math.Max(entityId + 1, oldLength * EcsConfig.PoolGrowthFactor);

                Array.Resize(ref _entityToDataIndex, newSize);

                for (var i = oldLength; i < newSize; i++)
                {
                    _entityToDataIndex[i] = -1;
                }
            }
        }

        private void EnsureComponentCapacity()
        {
            if (_count >= _components.Length)
            {
                var newCapacity = _components.Length == 0
                    ? _initialCapacity
                    : _components.Length * EcsConfig.PoolGrowthFactor;

                if (newCapacity <= _components.Length)
                    newCapacity = _components.Length + 1;

                Array.Resize(ref _components, newCapacity);
                Array.Resize(ref _dataIndexToEntity, newCapacity);
            }
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity > _components.Length)
            {
                Array.Resize(ref _components, capacity);
                Array.Resize(ref _dataIndexToEntity, capacity);
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
        public ReadOnlySpan<T> AsOccupiedSpan()
        {
            return _components.AsSpan(0, _count);
        }

        public IEnumerable<int> GetEntityIds()
        {
            for (var i = 0; i < _count; i++)
            {
                yield return _dataIndexToEntity[i];
            }
        }

        public virtual void Dispose()
        {
            if (_count > 0)
            {
                Array.Clear(_components, 0, _count);
                Array.Clear(_dataIndexToEntity, 0, _count);
            }

            Array.Fill(_entityToDataIndex, -1);
            _count = 0;
        }
    }
}

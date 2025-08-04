using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public sealed class EcsPool<T> : IDisposable where T : struct
    {
        private T[] _components;
        private int[] _entityToDataIndex;
        private int[] _dataIndexToEntity;
        private int _count;
        private Stack<int> _freeIndices = new(EcsConfig.InitialPoolCapacity);

        public EcsPool()
        {
            _components = new T[EcsConfig.InitialPoolCapacity];
            _entityToDataIndex = new int[EcsConfig.InitialPoolCapacity];
            _dataIndexToEntity = new int[EcsConfig.InitialPoolCapacity];
            Array.Fill(_entityToDataIndex, -1);
        }

        public void Add(int entityId, in T component)
        {
            if (entityId >= _entityToDataIndex.Length)
            {
                Array.Resize(ref _entityToDataIndex, Math.Max(entityId + 1, _entityToDataIndex.Length *  EcsConfig.PoolGrowthFactor));
            }

            if (_entityToDataIndex[entityId] != -1)
            {
                throw new ArgumentException($"Entity {entityId} already has this component");
            }

            int dataIndex;
            if (_freeIndices.Count > 0)
            {
                dataIndex = _freeIndices.Pop();
            }
            else
            {
                if (_count >= _components.Length)
                {
                    GrowArrays();
                }
                dataIndex = _count++;
            }

            _components[dataIndex] = component;
            _entityToDataIndex[entityId] = dataIndex;
            _dataIndexToEntity[dataIndex] = entityId;
        }

        private void GrowArrays()
        {
            int newCapacity = _components.Length * EcsConfig.PoolGrowthFactor;
            Array.Resize(ref _components, newCapacity);
            Array.Resize(ref _dataIndexToEntity, newCapacity);
        }

        public ref T Get(int entityId)
        {
            if (entityId >= _entityToDataIndex.Length || _entityToDataIndex[entityId] == -1)
            {
                throw new KeyNotFoundException($"Entity {entityId} doesn't have this component");
            }

            return ref _components[_entityToDataIndex[entityId]];
        }

        public bool Has(int entityId) => entityId < _entityToDataIndex.Length && _entityToDataIndex[entityId] != -1;

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
                var movedEntity = _dataIndexToEntity[_count - 1];
                _entityToDataIndex[movedEntity] = dataIndex;
                _dataIndexToEntity[dataIndex] = movedEntity;
            }

            _count--;
            _freeIndices.Push(dataIndex);
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public struct Enumerator
        {
            private readonly EcsPool<T> _pool;
            private int _index;

            public Enumerator(EcsPool<T> pool)
            {
                _pool = pool;
                _index = -1;
            }

            public ref T Current => ref _pool._components[_index];

            public bool MoveNext()
            {
                while (++_index < _pool._count)
                {
                    if (!_pool._freeIndices.Contains(_index))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void Dispose()
        {
            _components = null;
            _entityToDataIndex = null;
            _dataIndexToEntity = null;
            _freeIndices?.Clear();
            _freeIndices = null;
            GC.SuppressFinalize(this);
        }
    }
}

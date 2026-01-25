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
        private bool[] _isFree;
        private int _count;
        private readonly Stack<int> _freeIndices;
        private readonly int _initialCapacity;

        public int Count => _count - _freeIndices.Count;
        public int Capacity => _components.Length;

        public EcsPool(int initialCapacity = -1)
        {
            _initialCapacity = initialCapacity > 0 ? initialCapacity : EcsConfig.InitialPoolCapacity;
            _components = Array.Empty<T>();
            _entityToDataIndex = Array.Empty<int>();
            _dataIndexToEntity = Array.Empty<int>();
            _isFree = Array.Empty<bool>();
            _freeIndices = new Stack<int>(_initialCapacity);
            _count = 0;
        }

        public virtual void Add(int entityId, in T component)
        {
            EnsureEntityCapacity(entityId);

            if (HasComponentForEntity(entityId))
            {
                return;
            }

            var dataIndex = GetOrCreateDataIndex();
            AssignComponentToEntity(entityId, dataIndex, component);
            EcsWorld.IncreaseVersion();
        }

        private void EnsureEntityCapacity(int entityId)
        {
            if (entityId >= _entityToDataIndex.Length)
            {
                ResizeEntityToDataIndex(entityId + 1);
            }
        }

        private void ResizeEntityToDataIndex(int requiredSize)
        {
            var oldLength = _entityToDataIndex.Length;
            var newSize = CalculateNewSize(oldLength, requiredSize);

            Array.Resize(ref _entityToDataIndex, newSize);
            InitializeNewEntitySlots(oldLength, newSize);
        }

        private int CalculateNewSize(int oldSize, int requiredSize)
        {
            if (oldSize == 0)
            {
                return Math.Max(requiredSize, _initialCapacity);
            }

            return Math.Max(requiredSize, oldSize * EcsConfig.PoolGrowthFactor);
        }

        private void InitializeNewEntitySlots(int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                _entityToDataIndex[i] = -1;
            }
        }

        private bool HasComponentForEntity(int entityId)
        {
            return _entityToDataIndex[entityId] != -1;
        }

        private int GetOrCreateDataIndex()
        {
            if (_freeIndices.Count > 0)
            {
                return GetRecycledIndex();
            }

            EnsureComponentCapacity();
            return CreateNewIndex();
        }

        private int GetRecycledIndex()
        {
            var index = _freeIndices.Pop();
            _isFree[index] = false;
            return index;
        }

        private void EnsureComponentCapacity()
        {
            if (_count >= _components.Length)
            {
                ResizeComponentArrays();
            }
        }

        private void ResizeComponentArrays()
        {
            var newCapacity = _components.Length == 0
                ? _initialCapacity
                : _components.Length * EcsConfig.PoolGrowthFactor;

            Array.Resize(ref _components, newCapacity);
            Array.Resize(ref _dataIndexToEntity, newCapacity);
            Array.Resize(ref _isFree, newCapacity);
        }

        private int CreateNewIndex()
        {
            var index = _count++;
            _isFree[index] = false;
            return index;
        }

        private void AssignComponentToEntity(int entityId, int dataIndex, in T component)
        {
            _components[dataIndex] = component;
            _entityToDataIndex[entityId] = dataIndex;
            _dataIndexToEntity[dataIndex] = entityId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityId)
        {
            ValidateEntityHasComponent(entityId);
            return ref _components[_entityToDataIndex[entityId]];
        }

        private void ValidateEntityHasComponent(int entityId)
        {
            if (entityId >= _entityToDataIndex.Length || _entityToDataIndex[entityId] == -1)
            {
                throw new KeyNotFoundException($"Entity {entityId} doesn't have this component");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityId)
        {
            return entityId < _entityToDataIndex.Length && _entityToDataIndex[entityId] != -1;
        }

        public virtual void Remove(int entityId)
        {
            if (!CanRemove(entityId))
            {
                return;
            }

            var dataIndex = _entityToDataIndex[entityId];
            _entityToDataIndex[entityId] = -1;

            if (dataIndex < _count - 1)
            {
                SwapWithLastElement(dataIndex);
            }
            else
            {
                ClearLastElement(dataIndex);
            }

            MarkIndexAsFree(dataIndex);
            EcsWorld.IncreaseVersion();
        }

        private bool CanRemove(int entityId)
        {
            return entityId < _entityToDataIndex.Length && _entityToDataIndex[entityId] != -1;
        }

        private void SwapWithLastElement(int dataIndex)
        {
            var lastIndex = _count - 1;

            _components[dataIndex] = _components[lastIndex];
            _dataIndexToEntity[dataIndex] = _dataIndexToEntity[lastIndex];

            UpdateEntityMappingForSwappedElement(dataIndex);

            ClearElement(lastIndex);
        }

        private void UpdateEntityMappingForSwappedElement(int newDataIndex)
        {
            var movedEntityId = _dataIndexToEntity[newDataIndex];
            _entityToDataIndex[movedEntityId] = newDataIndex;
        }

        private void ClearLastElement(int dataIndex)
        {
            _components[dataIndex] = default;
            _dataIndexToEntity[dataIndex] = -1;
        }

        private void ClearElement(int index)
        {
            _components[index] = default;
            _dataIndexToEntity[index] = -1;
            _isFree[index] = true;
        }

        private void MarkIndexAsFree(int dataIndex)
        {
            _isFree[dataIndex] = true;
            _freeIndices.Push(dataIndex);
            _count--;
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity > _components.Length)
            {
                ResizeToCapacity(capacity);
            }
        }

        private void ResizeToCapacity(int capacity)
        {
            var newCapacity = Math.Max(capacity, _components.Length * EcsConfig.PoolGrowthFactor);
            Array.Resize(ref _components, newCapacity);
            Array.Resize(ref _dataIndexToEntity, newCapacity);
            Array.Resize(ref _isFree, newCapacity);
        }

        public void TrimExcess()
        {
            if (ShouldTrim())
            {
                CompactArrays();
            }
        }

        private bool ShouldTrim()
        {
            var occupiedCount = _count - _freeIndices.Count;
            return occupiedCount < _components.Length * 0.9;
        }

        private void CompactArrays()
        {
            var occupiedCount = _count - _freeIndices.Count;
            var newArrays = CreateCompactArrays(occupiedCount);

            CopyUsedElementsToNewArrays(newArrays);
            ReplaceArrays(newArrays, occupiedCount);
        }

        private (T[] components, int[] indexToEntity, bool[] isFree) CreateCompactArrays(int size)
        {
            return (
                new T[size],
                new int[size],
                new bool[size]
            );
        }

        private void CopyUsedElementsToNewArrays((T[] components, int[] indexToEntity, bool[] isFree) newArrays)
        {
            var newIndex = 0;
            for (var i = 0; i < _count; i++)
            {
                if (!_isFree[i])
                {
                    newArrays.components[newIndex] = _components[i];
                    newArrays.indexToEntity[newIndex] = _dataIndexToEntity[i];
                    newArrays.isFree[newIndex] = false;

                    _entityToDataIndex[newArrays.indexToEntity[newIndex]] = newIndex;

                    newIndex++;
                }
            }
        }

        private void ReplaceArrays((T[] components, int[] indexToEntity, bool[] isFree) newArrays, int newCount)
        {
            _components = newArrays.components;
            _dataIndexToEntity = newArrays.indexToEntity;
            _isFree = newArrays.isFree;
            _count = newCount;
            _freeIndices.Clear();
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
                if (!_isFree[i])
                {
                    yield return _dataIndexToEntity[i];
                }
            }
        }

        public virtual void Dispose()
        {
            ClearComponentArrays();
            ResetEntityMappings();
            ResetCounters();
        }

        private void ClearComponentArrays()
        {
            if (_count > 0)
            {
                Array.Clear(_components, 0, _count);
                Array.Clear(_dataIndexToEntity, 0, _count);
                Array.Clear(_isFree, 0, _count);
            }
        }

        private void ResetEntityMappings()
        {
            for (var i = 0; i < _entityToDataIndex.Length; i++)
            {
                _entityToDataIndex[i] = -1;
            }
        }

        private void ResetCounters()
        {
            _count = 0;
            _freeIndices.Clear();
        }
    }
}

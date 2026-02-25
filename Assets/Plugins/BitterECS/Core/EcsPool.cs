using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public class EcsPool<T> : IDisposable, IPool where T : new()
    {
        private T[] _components;
        private int[][] _sparsePages;
        private int[] _denseEntities;
        private int _count;

        public EcsPresenter Presenter { get; internal set; }
        public int Count => _count;

        public EcsPool(int initialCapacity = -1)
        {
            var cap = initialCapacity > 0 ? initialCapacity : EcsConfig.InitialPoolCapacity;
            _components = new T[cap];
            _denseEntities = new int[cap];
            _sparsePages = Array.Empty<int[]>();
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int entityId, in T component)
        {
            if (Has(entityId)) return;

            if (_count >= _components.Length)
            {
                var newCap = _components.Length > 0 ? _components.Length * EcsConfig.PoolGrowthFactor : EcsConfig.InitialPoolCapacity;
                Array.Resize(ref _components, newCap);
                Array.Resize(ref _denseEntities, newCap);
            }

            var page = entityId / EcsConfig.SparsePageSize;
            var index = entityId % EcsConfig.SparsePageSize;

            if (page >= _sparsePages.Length)
            {
                Array.Resize(ref _sparsePages, page + 1);
            }

            if (_sparsePages[page] == null)
            {
                _sparsePages[page] = new int[EcsConfig.SparsePageSize];
                Array.Fill(_sparsePages[page], -1);
            }

            _components[_count] = component;
            _denseEntities[_count] = entityId;
            _sparsePages[page][index] = _count;
            _count++;

            EcsWorld.IncreaseVersion();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(int entityId)
        {
            var page = entityId / EcsConfig.SparsePageSize;
            if (page >= _sparsePages.Length || _sparsePages[page] == null) return;

            var index = entityId % EcsConfig.SparsePageSize;
            var denseIndex = _sparsePages[page][index];

            if (denseIndex == -1) return;

            var lastDenseIndex = _count - 1;
            if (denseIndex != lastDenseIndex)
            {
                var lastEntity = _denseEntities[lastDenseIndex];
                _components[denseIndex] = _components[lastDenseIndex];
                _denseEntities[denseIndex] = lastEntity;

                var lastPage = lastEntity / EcsConfig.SparsePageSize;
                var lastIndex = lastEntity % EcsConfig.SparsePageSize;
                _sparsePages[lastPage][lastIndex] = denseIndex;
            }

            _components[lastDenseIndex] = default;
            _sparsePages[page][index] = -1;
            _count--;

            EcsWorld.IncreaseVersion();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityId)
        {
            var page = entityId / EcsConfig.SparsePageSize;
            return ref _components[_sparsePages[page][entityId % EcsConfig.SparsePageSize]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityId)
        {
            var page = entityId / EcsConfig.SparsePageSize;
            return page < _sparsePages.Length && _sparsePages[page] != null && _sparsePages[page][entityId % EcsConfig.SparsePageSize] != -1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<int> GetDenseEntities() => new(_denseEntities, 0, _count);

        public void Dispose()
        {
            Array.Clear(_components, 0, _count);
            _sparsePages = Array.Empty<int[]>();
            _count = 0;
        }
    }

    public class EcsEventPool<T> : EcsPool<T> where T : new()
    {
        private IEcsEvent[] _subscriptions = Array.Empty<IEcsEvent>();
        private int _subCount;
        private bool _isSorted;

        public void Subscribe(IEcsEvent eventTo)
        {
            if (_subCount >= _subscriptions.Length) Array.Resize(ref _subscriptions, Math.Max(2, _subscriptions.Length * 2));
            _subscriptions[_subCount++] = eventTo;
            _isSorted = false;
        }

        public void Unsubscribe(IEcsEvent eventTo)
        {
            var idx = Array.IndexOf(_subscriptions, eventTo, 0, _subCount);
            if (idx >= 0)
            {
                _subscriptions[idx] = _subscriptions[--_subCount];
                _subscriptions[_subCount] = null;
                _isSorted = false;
            }
        }

        private void EnsureSorted()
        {
            if (_isSorted)
            {
                return;
            }

            Array.Sort(_subscriptions, 0, _subCount, PriorityUtility.Sort());
            _isSorted = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new void Add(int entityId, in T component)
        {
            base.Add(entityId, in component);
            if (_subCount == 0) return;
            EnsureSorted();
            for (var i = 0; i < _subCount; i++) _subscriptions[i].Added?.Invoke(new(Presenter, entityId));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new void Remove(int entityId)
        {
            if (_subCount > 0 && Has(entityId))
            {
                EnsureSorted();
                var e = new EcsEntity(Presenter, entityId);
                for (var i = 0; i < _subCount; i++) _subscriptions[i].Removed?.Invoke(new(Presenter, entityId));
            }
            base.Remove(entityId);
        }
    }
}

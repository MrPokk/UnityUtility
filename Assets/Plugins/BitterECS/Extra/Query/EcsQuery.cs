using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BitterECS.Core;

namespace BitterECS.Extra
{
    public sealed class EcsQuery : IDisposable
    {
        private readonly EcsFilter _filter;
        private readonly List<EcsEntity> _cachedEntities = new();
        private EcsEntity[] _cachedArray;
        private bool _isDirty = true;
        private readonly EcsPresenter _presenter;

        public ReadOnlySpan<EcsEntity> Entities
        {
            get
            {
                EnsureUpdated();
                return _cachedArray ?? Array.Empty<EcsEntity>();
            }
        }

        public int Count
        {
            get
            {
                EnsureUpdated();
                return _cachedArray?.Length ?? 0;
            }
        }

        public EcsQuery(EcsPresenter presenter, EcsFilter filter)
        {
            _presenter = presenter;
            _filter = filter;
            _cachedArray = null;
            SubscribeToEvents();
        }

        public EcsQuery(EcsPresenter presenter, Func<EcsFilter, EcsFilter> filterBuilder)
            : this(presenter, filterBuilder(presenter.Filter()))
        {
        }

        private void SubscribeToEvents()
        {
            foreach (var pool in _presenter.Pools.Values)
            {
                if (pool is IPoolWithEvents eventPool)
                {
                    eventPool.Events.OnComponentAdded += MarkDirty;
                    eventPool.Events.OnComponentRemoved += MarkDirty;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MarkDirty(int entityId) => _isDirty = true;

        private void EnsureUpdated()
        {
            if (_isDirty)
            {
                Refresh();
                _isDirty = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Refresh()
        {
            _cachedEntities.Clear();
            foreach (var entity in _filter.Collect())
                _cachedEntities.Add(entity);

            if (_cachedArray == null || _cachedArray.Length != _cachedEntities.Count)
            {
                _cachedArray = new EcsEntity[_cachedEntities.Count];
            }

            _cachedEntities.CopyTo(_cachedArray);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            EnsureUpdated();
            return new Enumerator(_cachedArray);
        }

        public ref struct Enumerator
        {
            private readonly EcsEntity[] _array;
            private int _index;
            private readonly int _length;

            public Enumerator(EcsEntity[] array)
            {
                _array = array;
                _length = array?.Length ?? 0;
                _index = -1;
            }

            public EcsEntity Current => _array[_index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                int index = _index + 1;
                if (index < _length)
                {
                    _index = index;
                    return true;
                }
                return false;
            }
        }

        public void Dispose()
        {
            foreach (var pool in _presenter.Pools.Values)
            {
                if (pool is IPoolWithEvents eventPool)
                {
                    eventPool.Events.OnComponentAdded -= MarkDirty;
                    eventPool.Events.OnComponentRemoved -= MarkDirty;
                }
            }

            _cachedArray = null;
            _cachedEntities.Clear();
        }
    }
}

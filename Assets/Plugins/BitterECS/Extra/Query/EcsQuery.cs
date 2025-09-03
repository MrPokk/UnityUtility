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
        private bool _isDirty = true;
        private readonly EcsPresenter _presenter;

        public ReadOnlySpan<EcsEntity> Entities
        {
            get
            {
                if (_isDirty)
                    Refresh();
                return _cachedEntities.ToArray();
            }
        }

        public int Count => Entities.Length;

        public EcsQuery(EcsPresenter presenter, EcsFilter filter)
        {
            _presenter = presenter;
            _filter = filter;
            SubscribeToEvents();
        }

        public EcsQuery(EcsPresenter presenter, Func<EcsFilter, EcsFilter> filterBuilder)
            : this(presenter, filterBuilder(presenter.Filter()))
        {
        }

        private void SubscribeToEvents()
        {
            var pools = _presenter.Pools.Values;
            foreach (var pool in pools)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Refresh()
        {
            _cachedEntities.Clear();
            foreach (var entity in _filter.Collect())
                _cachedEntities.Add(entity);
            _isDirty = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(this);

        public ref struct Enumerator
        {
            private readonly EcsQuery _query;
            private int _index;

            public Enumerator(EcsQuery query)
            {
                _query = query;
                _index = -1;
            }

            public EcsEntity Current => _query.Entities[_index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _query.Entities.Length;
        }

        public void Dispose()
        {
            var pools = _presenter.Pools.Values;
            foreach (var pool in pools)
            {
                if (pool is IPoolWithEvents eventPool)
                {
                    eventPool.Events.OnComponentAdded -= MarkDirty;
                    eventPool.Events.OnComponentRemoved -= MarkDirty;
                }
            }
        }
    }

}

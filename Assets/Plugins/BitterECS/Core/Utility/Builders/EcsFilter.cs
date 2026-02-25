using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public struct EcsFilter
    {
        private readonly EcsPresenter _presenter;
        private ComponentMask _includeMask;
        private ComponentMask _excludeMask;
        private ComponentMask _orMask;
        private int _smallestPoolId;

        private Predicate<int>[] _predicates;
        private int _predicateCount;

        private RefWorldVersion _refWorld;
        private int[] _filteredCache;
        private int _filteredLength;

        public EcsFilter(EcsPresenter presenter)
        {
            _presenter = presenter;
            _includeMask = new ComponentMask();
            _excludeMask = new ComponentMask();
            _orMask = new ComponentMask();
            _smallestPoolId = -1;

            _predicates = null;
            _predicateCount = 0;

            _refWorld = new RefWorldVersion(-1);
            _filteredCache = Array.Empty<int>();
            _filteredLength = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPredicate(Predicate<int> predicate)
        {
            if (_predicates == null) _predicates = new Predicate<int>[4];
            else if (_predicateCount == _predicates.Length) Array.Resize(ref _predicates, _predicateCount * 2);

            _predicates[_predicateCount++] = predicate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<T>() where T : new()
        {
            var id = ComponentTypeId<T>.Id;
            _includeMask.Set(id);
            UpdateSmallestPool(id);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<T>(Predicate<T> predicate) where T : new()
        {
            Include<T>();
            var presenter = _presenter;
            AddPredicate(entityId => predicate(presenter.GetPool<T>().Get(entityId)));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Exclude<T>() where T : new()
        {
            _excludeMask.Set(ComponentTypeId<T>.Id);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Or<T>() where T : new()
        {
            _orMask.Set(ComponentTypeId<T>.Id);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Where(Predicate<EcsEntity> predicate)
        {
            var presenter = _presenter;
            AddPredicate(entityId => predicate(new(presenter, entityId)));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter WhereProvider<T>(Predicate<T> predicate) where T : class, ILinkableProvider
        {
            var presenter = _presenter;
            AddPredicate(entityId =>
            {
                return new EcsEntity(presenter, entityId).TryGetProvider<T>(out var p) && predicate(p);
            });
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter WhereProvider<T>() where T : class, ILinkableProvider
        {
            var presenter = _presenter;
            AddPredicate(entityId => new EcsEntity(presenter, entityId).IsProviding);
            return this;
        }

        private void UpdateSmallestPool(int componentId)
        {
            if (_smallestPoolId == -1)
            {
                _smallestPoolId = componentId;
                return;
            }
            var currentPool = _presenter.GetPoolById(_smallestPoolId);
            var newPool = _presenter.GetPoolById(componentId);
            var count1 = currentPool?.Count ?? int.MaxValue;
            var count2 = newPool?.Count ?? int.MaxValue;
            if (count2 < count1) _smallestPoolId = componentId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidationCacheOnFilter()
        {
            if (_refWorld == EcsWorld.GetRefWorld()) return;
            RebuildCache();
            _refWorld = EcsWorld.GetRefWorld();
        }

        private void RebuildCache()
        {
            _filteredLength = 0;
            ReadOnlySpan<int> candidates;

            if (_smallestPoolId != -1)
            {
                var pool = _presenter.GetPoolById(_smallestPoolId);
                candidates = pool != null ? pool.GetDenseEntities() : ReadOnlySpan<int>.Empty;
            }
            else
            {
                candidates = _presenter.GetAliveIds();
            }

            if (_filteredCache.Length < candidates.Length)
                Array.Resize(ref _filteredCache, candidates.Length);

            var checkOr = !_orMask.IsEmpty();

            for (var i = 0; i < candidates.Length; i++)
            {
                var entityId = candidates[i];
                ref var mask = ref _presenter.GetEntityMask(entityId);

                if (!mask.HasAll(in _includeMask)) continue;
                if (_excludeMask.HasAny(in mask)) continue;
                if (checkOr && !mask.HasAny(in _orMask)) continue;

                var passed = true;
                for (var p = 0; p < _predicateCount; p++)
                {
                    if (!_predicates[p](entityId))
                    {
                        passed = false;
                        break;
                    }
                }

                if (passed)
                {
                    _filteredCache[_filteredLength++] = entityId;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Enumerator GetFastEnumerator()
        {
            ValidationCacheOnFilter();
            return new Enumerator(_presenter, _filteredCache, _filteredLength);
        }

        public Enumerator GetEnumerator() => GetFastEnumerator();

        public ref struct Enumerator
        {
            private readonly EcsPresenter _presenter;
            private readonly int[] _entities;
            private readonly int _count;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(EcsPresenter p, int[] entities, int count)
            {
                _presenter = p;
                _entities = entities;
                _count = count;
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _count;

            public readonly EcsEntity Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_presenter, _entities[_index]);
            }
        }
    }

    public struct EcsFilter<T> where T : EcsPresenter, new()
    {
        private EcsFilter? _filter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            if (_filter != null) return;
            _filter = new EcsFilter(EcsWorld.Get<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<TComponent>() where TComponent : new()
        {
            EnsureInitialized();
            return _filter.Value.Include<TComponent>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<TComponent>(Predicate<TComponent> predicate) where TComponent : new()
        {
            EnsureInitialized();
            return _filter.Value.Include(predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Exclude<TComponent>() where TComponent : new()
        {
            EnsureInitialized();
            return _filter.Value.Exclude<TComponent>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Where(Predicate<EcsEntity> predicate)
        {
            EnsureInitialized();
            return _filter.Value.Where(predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter WhereProvider<TComponent>(Predicate<TComponent> predicate) where TComponent : class, ILinkableProvider
        {
            EnsureInitialized();
            return _filter.Value.WhereProvider(predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter WhereProvider<TComponent>() where TComponent : class, ILinkableProvider
        {
            EnsureInitialized();
            return _filter.Value.WhereProvider<TComponent>();
        }

        public EcsFilter.Enumerator GetEnumerator()
        {
            EnsureInitialized();
            return _filter.Value.GetEnumerator();
        }
    }
}

using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public struct EcsFilter
    {
        private readonly EcsWorld _world;
        public readonly EcsWorld World => _world ?? EcsWorldStatic.Instance;

        private EcsComponentMask _includeMask;
        private EcsComponentMask _excludeMask;
        private EcsComponentMask _orMask;
        private int _smallestPoolId;

        private Predicate<int>[] _predicates;
        private int _predicateCount;

        private RefWorldVersion _refWorld;
        private int[] _filteredCache;
        private int _filteredLength;

        public int Count => ValidationCacheOnFilter();

        public EcsFilter(EcsWorld world = default)
        {
            _world = world;
            _includeMask = new EcsComponentMask();
            _excludeMask = new EcsComponentMask();
            _orMask = new EcsComponentMask();
            _smallestPoolId = -1;

            _predicates = null;
            _predicateCount = 0;

            _refWorld = new RefWorldVersion(-1);
            _filteredCache = null;
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
        public EcsFilter Has<T>() where T : new()
        {
            var id = EcsComponentTypeId<T>.Id;
            _includeMask.Set(id);
            UpdateSmallestPool(id);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Has<T>(Predicate<T> predicate) where T : new()
        {
            Has<T>();
            var world = World;
            AddPredicate(entityId => predicate(world.GetPool<T>().Get(entityId)));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Not<T>() where T : new()
        {
            _excludeMask.Set(EcsComponentTypeId<T>.Id);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Or<T>() where T : new()
        {
            _orMask.Set(EcsComponentTypeId<T>.Id);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Where(Predicate<EcsEntity> predicate)
        {
            var world = World;
            AddPredicate(entityId => predicate(new EcsEntity(world, entityId)));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter WhereProvider<T>(Predicate<T> predicate) where T : class, ILinkableProvider
        {
            var world = World;
            AddPredicate(entityId => new EcsEntity(world, entityId).TryGetProvider<T>(out var p) && predicate(p));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter WhereProvider<T>() where T : class, ILinkableProvider
        {
            var world = World;
            AddPredicate(entityId => new EcsEntity(world, entityId).HasProvider<T>());
            return this;
        }

        private void UpdateSmallestPool(int componentId)
        {
            if (_smallestPoolId <= 0)
            {
                _smallestPoolId = componentId;
                return;
            }

            var currentPool = World.GetPoolById(_smallestPoolId);
            var newPool = World.GetPoolById(componentId);
            var count1 = currentPool?.Count ?? int.MaxValue;
            var count2 = newPool?.Count ?? int.MaxValue;
            if (count2 < count1) _smallestPoolId = componentId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ValidationCacheOnFilter()
        {
            if (_filteredCache == null && _refWorld.Version == 0) return 0;
            if (_refWorld == World.GetVersion() && _filteredCache != null) return _filteredLength;

            RebuildCache();
            _refWorld = World.GetVersion();
            return _filteredLength;
        }

        private void RebuildCache()
        {
            _filteredLength = 0;
            ReadOnlySpan<int> candidates;

            if (_smallestPoolId > 0)
            {
                var pool = World.GetPoolById(_smallestPoolId);
                candidates = pool != null ? pool.GetDenseEntities() : ReadOnlySpan<int>.Empty;
            }
            else
            {
                candidates = World.GetAliveIds();
            }

            if (_filteredCache == null)
            {
                _filteredCache = new int[Math.Max(candidates.Length, EcsDefinitions.InitialEntitiesCapacity)];
            }
            else if (_filteredCache.Length < candidates.Length)
            {
                Array.Resize(ref _filteredCache, candidates.Length);
            }

            var checkOr = !_orMask.IsEmpty();

            for (var i = 0; i < candidates.Length; i++)
            {
                var entityId = candidates[i];
                ref var mask = ref World.GetEntityMask(entityId);

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
        public EcsEntity First()
        {
            if (ValidationCacheOnFilter() > 0) return new EcsEntity(World, _filteredCache[0]);
            return new EcsEntity(null, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEntity First(Predicate<EcsEntity> predicate)
        {
            if (ValidationCacheOnFilter() > 0)
            {
                for (var i = 0; i < _filteredLength; i++)
                {
                    var entity = new EcsEntity(World, _filteredCache[i]);
                    if (predicate(entity)) return entity;
                }
            }
            return new EcsEntity(null, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEntity Last()
        {
            if (ValidationCacheOnFilter() > 0) return new EcsEntity(World, _filteredCache[_filteredLength - 1]);
            return new EcsEntity(null, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEntity Last(Predicate<EcsEntity> predicate)
        {
            if (ValidationCacheOnFilter() > 0)
            {
                for (int i = _filteredLength - 1; i >= 0; i--)
                {
                    var entity = new EcsEntity(World, _filteredCache[i]);
                    if (predicate(entity)) return entity;
                }
            }
            return new EcsEntity(null, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void For(EcsFilterFor action)
        {
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext()) action(enumerator.Current);
        }

        public Enumerator GetEnumerator()
        {
            ValidationCacheOnFilter();
            return new Enumerator(World, _filteredCache, _filteredLength);
        }

        public ref struct Enumerator
        {
            private readonly EcsWorld _world;
            private readonly int[] _entities;
            private readonly int _count;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(EcsWorld world, int[] entities, int count)
            {
                _world = world;
                _entities = entities;
                _count = count;
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _count;

            public readonly EcsEntity Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_world, _entities[_index]);
            }
        }
    }

    public delegate void EcsFilterFor(EcsEntity entity);
    public delegate void EcsFilterFor<T1>(EcsEntity entity, ref T1 c1) where T1 : new();
    public delegate void EcsFilterFor<T1, T2>(EcsEntity entity, ref T1 c1, ref T2 c2) where T1 : new() where T2 : new();
    public delegate void EcsFilterFor<T1, T2, T3>(EcsEntity entity, ref T1 c1, ref T2 c2, ref T3 c3) where T1 : new() where T2 : new() where T3 : new();
    public delegate void EcsFilterFor<T1, T2, T3, T4>(EcsEntity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4) where T1 : new() where T2 : new() where T3 : new() where T4 : new();

    public struct EcsFilter<T1> where T1 : new()
    {
        private EcsFilter _filter;
        public int Count => _filter.Count;

        public EcsFilter(EcsWorld world) => _filter = new EcsFilter(world).Has<T1>();

        public EcsFilter<T1> Exclude<T2>() where T2 : new() { _filter = _filter.Not<T2>(); return this; }
        public EcsFilter<T1> Include<T2>(Predicate<T2> predicate) where T2 : new() { _filter = _filter.Has(predicate); return this; }
        public EcsFilter<T1> Where(Predicate<EcsEntity> predicate) { _filter = _filter.Where(predicate); return this; }
        public EcsFilter<T1> WhereProvider<TComponent>() where TComponent : class, ILinkableProvider { _filter = _filter.WhereProvider<TComponent>(); return this; }
        public EcsFilter.Enumerator GetEnumerator() => _filter.GetEnumerator();

        public EcsEntity First() => _filter.First();
        public EcsEntity First(Predicate<EcsEntity> predicate) => _filter.First(predicate);
        public EcsEntity Last() => _filter.Last();
        public EcsEntity Last(Predicate<EcsEntity> predicate) => _filter.Last(predicate);

        public void For(EcsFilterFor<T1> action)
        {
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                action(current, ref current.Get<T1>());
            }
        }
    }

    public struct EcsFilter<T1, T2> where T1 : new() where T2 : new()
    {
        private EcsFilter _filter;
        public int Count => _filter.Count;

        public EcsFilter(EcsWorld world) => _filter = new EcsFilter(world).Has<T1>().Has<T2>();

        public EcsFilter<T1, T2> Exclude<T3>() where T3 : new() { _filter = _filter.Not<T3>(); return this; }
        public EcsFilter<T1, T2> Where(Predicate<EcsEntity> predicate) { _filter = _filter.Where(predicate); return this; }
        public EcsFilter.Enumerator GetEnumerator() => _filter.GetEnumerator();

        public EcsEntity First() => _filter.First();
        public EcsEntity First(Predicate<EcsEntity> predicate) => _filter.First(predicate);
        public EcsEntity Last() => _filter.Last();
        public EcsEntity Last(Predicate<EcsEntity> predicate) => _filter.Last(predicate);

        public void For(EcsFilterFor<T1, T2> action)
        {
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                action(current, ref current.Get<T1>(), ref current.Get<T2>());
            }
        }
    }

    public struct EcsFilter<T1, T2, T3> where T1 : new() where T2 : new() where T3 : new()
    {
        private EcsFilter _filter;
        public int Count => _filter.Count;

        public EcsFilter(EcsWorld world) => _filter = new EcsFilter(world).Has<T1>().Has<T2>().Has<T3>();

        public EcsFilter<T1, T2, T3> Exclude<T4>() where T4 : new() { _filter = _filter.Not<T4>(); return this; }
        public EcsFilter<T1, T2, T3> Where(Predicate<EcsEntity> predicate) { _filter = _filter.Where(predicate); return this; }
        public EcsFilter.Enumerator GetEnumerator() => _filter.GetEnumerator();

        public EcsEntity First() => _filter.First();
        public EcsEntity First(Predicate<EcsEntity> predicate) => _filter.First(predicate);
        public EcsEntity Last() => _filter.Last();
        public EcsEntity Last(Predicate<EcsEntity> predicate) => _filter.Last(predicate);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void For(EcsFilterFor<T1, T2, T3> action)
        {
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                action(current, ref current.Get<T1>(), ref current.Get<T2>(), ref current.Get<T3>());
            }
        }
    }

    public struct EcsFilter<T1, T2, T3, T4> where T1 : new() where T2 : new() where T3 : new() where T4 : new()
    {
        private EcsFilter _filter;
        public int Count => _filter.Count;

        public EcsFilter(EcsWorld world) => _filter = new EcsFilter(world).Has<T1>().Has<T2>().Has<T3>().Has<T4>();

        public EcsFilter<T1, T2, T3, T4> Exclude<T5>() where T5 : new() { _filter = _filter.Not<T5>(); return this; }
        public EcsFilter<T1, T2, T3, T4> Where(Predicate<EcsEntity> predicate) { _filter = _filter.Where(predicate); return this; }
        public EcsFilter.Enumerator GetEnumerator() => _filter.GetEnumerator();

        public EcsEntity First() => _filter.First();
        public EcsEntity First(Predicate<EcsEntity> predicate) => _filter.First(predicate);
        public EcsEntity Last() => _filter.Last();
        public EcsEntity Last(Predicate<EcsEntity> predicate) => _filter.Last(predicate);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void For(EcsFilterFor<T1, T2, T3, T4> action)
        {
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                action(current, ref current.Get<T1>(), ref current.Get<T2>(), ref current.Get<T3>(), ref current.Get<T4>());
            }
        }
    }
}

using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public struct EcsFilter
    {
        private readonly EcsPresenter _presenter;
        private ICondition[] _includeConditions;
        private ICondition[] _excludeConditions;
        private int _includeCount;
        private int _excludeCount;

        private RefWorldVersion _refWorld;
        private EcsEntity[] _filteredCache;
        private int _filteredLength;

        public readonly ReadOnlySpan<ICondition> IncludeSpan => new(_includeConditions, 0, _includeCount);
        public readonly ReadOnlySpan<ICondition> ExcludeSpan => new(_excludeConditions, 0, _excludeCount);

        public EcsFilter(EcsPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _includeConditions = new ICondition[EcsConfig.FilterConditionInclude];
            _excludeConditions = new ICondition[EcsConfig.FilterConditionExclude];
            _includeCount = 0;
            _excludeCount = 0;

            _refWorld = new();

            _filteredCache = new EcsEntity[GetRequiredCapacity(presenter)];
            _filteredLength = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<T>() where T : struct
        {
            AddCondition(_includeConditions, new HasComponentCondition<T>(), ref _includeCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<T>(Predicate<T> predicate) where T : struct
        {
            AddCondition(_includeConditions, new ComponentPredicateCondition<T>(predicate), ref _includeCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Exclude<T>() where T : struct
        {
            AddCondition(_excludeConditions, new NotHasComponentCondition<T>(), ref _excludeCount);
            return this;
        }

        private void AddCondition(ICondition[] conditions, ICondition newCondition, ref int count)
        {
            if (count >= conditions.Length)
            {
                Array.Resize(ref conditions, conditions.Length * 2);
                if (conditions == _includeConditions)
                {
                    _includeConditions = conditions;
                }
                else
                {
                    _excludeConditions = conditions;
                }
            }

            conditions[count] = newCondition;
            count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool MatchesAllConditions(EcsEntity entity)
        {
            for (var i = 0; i < IncludeSpan.Length; i++)
            {
                if (!IncludeSpan[i].Check(entity))
                {
                    return false;
                }
            }

            for (var i = 0; i < ExcludeSpan.Length; i++)
            {
                if (!ExcludeSpan[i].Check(entity))
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetRequiredCapacity(EcsPresenter presenter) => presenter.CountEntity + (presenter.CountEntity / 4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCacheCapacity()
        {
            var requiredCapacity = GetRequiredCapacity(_presenter);
            var requiredCapacityMax = requiredCapacity + (requiredCapacity / 2);
            if (_filteredCache.Length >= requiredCapacity && _filteredCache.Length < requiredCapacityMax)
            {
                return;
            }

            Array.Resize(ref _filteredCache, requiredCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<EcsEntity> ValidationCacheOnFilter()
        {
            EnsureCacheCapacity();

            if (_refWorld != EcsWorld.GetRefWorld())
            {
                RebuildCache();
                _refWorld = EcsWorld.GetRefWorld();
            }

            var filteringSpan = new ReadOnlySpan<EcsEntity>(_filteredCache, 0, _filteredLength);
            return filteringSpan;
        }

        private void RebuildCache()
        {
            var aliveEntities = _presenter.GetAliveEntities();
            ResetFilteredCache();

            for (var i = 0; i < aliveEntities.Count; i++)
            {
                var entity = aliveEntities[i];
                if (!MatchesAllConditions(entity))
                {
                    continue;
                }

                AddToFilteredCache(entity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetFilteredCache() => _filteredLength = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToFilteredCache(EcsEntity entity = default)
        {
            _filteredCache[_filteredLength] = entity;
            _filteredLength++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Enumerator Collect() => new(this);
        public ReadOnlySpan<EcsEntity>.Enumerator GetEnumerator() => ValidationCacheOnFilter().GetEnumerator();
        public readonly int Count() => _filteredLength;
        
        public ref struct Enumerator
        {
            private EcsFilter _filter;
            public Enumerator(in EcsFilter filter) => _filter = filter;
            public ReadOnlySpan<EcsEntity>.Enumerator GetEnumerator() => _filter.ValidationCacheOnFilter().GetEnumerator();
            public readonly int Count() => _filter.ValidationCacheOnFilter().Length;
        }
    }

    public interface ICondition
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Check(EcsEntity entity);
    }

    public readonly struct HasComponentCondition<T> : ICondition where T : struct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(EcsEntity entity) => entity.Has<T>();
    }

    public readonly struct NotHasComponentCondition<T> : ICondition where T : struct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(EcsEntity entity) => !entity.Has<T>();
    }

    public readonly struct ComponentPredicateCondition<T> : ICondition where T : struct
    {
        private readonly Predicate<T> _predicate;

        public ComponentPredicateCondition(Predicate<T> predicate) => _predicate = predicate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(EcsEntity entity) => entity.Has<T>() && _predicate(entity.Get<T>());
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly List<int[]> _filteredCache;

        public readonly ReadOnlySpan<ICondition> IncludeSpan => new(_includeConditions, 0, _includeCount);
        public readonly ReadOnlySpan<ICondition> ExcludeSpan => new(_excludeConditions, 0, _excludeCount);

        public EcsFilter(EcsPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _includeConditions = new ICondition[EcsConfig.FilterConditionInclude];
            _excludeConditions = new ICondition[EcsConfig.FilterConditionExclude];
            _includeCount = 0;
            _excludeCount = 0;

            var filteredCacheSize = presenter.CountEntity * EcsConfig.PoolGrowthFactor;
            _filteredCache = new List<int[]>(filteredCacheSize);
            _filteredCache.ForEach(li => li = new int[1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<T>() where T : struct
        {
            AddCondition(_includeConditions, new HasComponentCondition<T>(), ref _includeCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Exclude<T>() where T : struct
        {
            AddCondition(_excludeConditions, new NotHasComponentCondition<T>(), ref _excludeCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Where<T>(Func<T, bool> predicate) where T : struct
        {
            AddCondition(_includeConditions, new ComponentPredicateCondition<T>(predicate), ref _includeCount);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void EnsureCacheCapacity()
        {
            var requiredCapacity = _presenter.CountEntity * EcsConfig.PoolGrowthFactor;
            if (_filteredCache.Count >= requiredCapacity)
            {
                return;
            }

            _filteredCache.Add(new int[1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly IReadOnlyCollection<EcsEntity> ValidationCacheOnFilter()
        {
            EnsureCacheCapacity();

            var filteredEntitiesCurrent = new Stack<EcsEntity>();
            for (var i = 0; i < _filteredCache.Count; i++)
            {
                var entity = _presenter.Get(i);
                if (entity == null)
                {
                    continue;
                }

                var entityComponentCount = entity.GetCountComponents();
                var entityComponentCountCash = _filteredCache[i][0];

                if (entityComponentCountCash != entityComponentCount)
                {
                    if (!MatchesAllConditions(entity))
                    {
                        continue;
                    }

                    _filteredCache[i][0] = entity.GetCountComponents();
                }
                else
                {
                    filteredEntitiesCurrent.Push(entity);
                    continue;
                }
            }

            return filteredEntitiesCurrent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly IReadOnlyCollection<EcsEntity> Collect()
        {
            return ValidationCacheOnFilter();
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
        private readonly Func<T, bool> _predicate;

        public ComponentPredicateCondition(Func<T, bool> predicate)
        {
            _predicate = predicate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(EcsEntity entity)
        {
            return entity.Has<T>() && _predicate(entity.Get<T>());
        }
    }
}

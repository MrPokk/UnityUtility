using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public ref struct EcsFilter
    {
        private readonly EcsPresenter _presenter;
        private ICondition[] _includeConditions;
        private ICondition[] _excludeConditions;
        private int _includeCount;
        private int _excludeCount;

        private EcsEntity[] _filteredCache;
        private int _filteredCount;
        private bool _isCacheValid;
        private int _lastEntityCount;

        public readonly ReadOnlySpan<ICondition> IncludeSpan => new(_includeConditions, 0, _includeCount);
        public readonly ReadOnlySpan<ICondition> ExcludeSpan => new(_excludeConditions, 0, _excludeCount);
        public readonly ReadOnlySpan<EcsEntity> Entities => _presenter.GetAll();
        public EcsFilter(EcsPresenter presenter)
        {
            _presenter = presenter;
            _includeConditions = new ICondition[EcsConfig.FilterConditionInclude];
            _excludeConditions = new ICondition[EcsConfig.FilterConditionExclude];
            _includeCount = 0;
            _excludeCount = 0;
            _filteredCache = null;
            _filteredCount = 0;
            _isCacheValid = false;
            _lastEntityCount = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<T>() where T : struct
        {
            AddCondition(_includeConditions, new HasComponentCondition<T>(), ref _includeCount);
            InvalidateCache();
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Exclude<T>() where T : struct
        {
            AddCondition(_excludeConditions, new NotHasComponentCondition<T>(), ref _excludeCount);
            InvalidateCache();
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Where<T>(Func<T, bool> predicate) where T : struct
        {
            AddCondition(_includeConditions, new ComponentPredicateCondition<T>(predicate), ref _includeCount);
            InvalidateCache();
            return this;
        }

        private void AddCondition(ICondition[] conditions, ICondition newCondition, ref int count)
        {
            if (count >= conditions.Length)
            {
                Array.Resize(ref conditions, conditions.Length * 2);
                if (conditions == _includeConditions)
                    _includeConditions = conditions;
                else
                    _excludeConditions = conditions;
            }

            conditions[count] = newCondition;
            count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidateCache()
        {
            _isCacheValid = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool MatchesAllConditions(EcsEntity entity)
        {
            for (int i = 0; i < IncludeSpan.Length; i++)
            {
                if (!IncludeSpan[i].Check(entity))
                    return false;
            }

            for (int i = 0; i < ExcludeSpan.Length; i++)
            {
                if (!ExcludeSpan[i].Check(entity))
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RebuildCacheIfNeeded()
        {
            var entities = Entities;

            if (_isCacheValid && _lastEntityCount == entities.Length)
                return;

            var estimatedSize = entities.Length;
            if (_filteredCache == null || _filteredCache.Length < estimatedSize)
            {
                _filteredCache = new EcsEntity[estimatedSize];
            }

            _filteredCount = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                if (MatchesAllConditions(entities[i]))
                {
                    _filteredCache[_filteredCount++] = entities[i];
                }
            }

            _isCacheValid = true;
            _lastEntityCount = entities.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FilterEnumerator Collect()
        {
            RebuildCacheIfNeeded();
            return new FilterEnumerator(_filteredCache, _filteredCount);
        }

        public ref struct FilterEnumerator
        {
            private readonly ReadOnlySpan<EcsEntity> _entities;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal FilterEnumerator(EcsEntity[] entities, int count)
            {
                _entities = new ReadOnlySpan<EcsEntity>(entities, 0, count);
                _index = -1;
            }

            public readonly EcsEntity Current => _entities[_index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return ++_index < _entities.Length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterEnumerator GetEnumerator() => this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly int Count() => _entities.Length;
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

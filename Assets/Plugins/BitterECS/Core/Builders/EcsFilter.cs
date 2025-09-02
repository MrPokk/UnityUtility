using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public struct EcsFilter
    {
        private readonly EcsPresenter _presenter;
        private readonly ICondition[] _includeConditions;
        private readonly ICondition[] _excludeConditions;
        private int _includeCount;
        private int _excludeCount;

        public EcsFilter(EcsPresenter presenter)
        {
            _presenter = presenter;
            _includeConditions = new ICondition[EcsConfig.FilterConditionInclude];
            _excludeConditions = new ICondition[EcsConfig.FilterConditionExclude];
            _includeCount = 0;
            _excludeCount = 0;
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
                throw new IndexOutOfRangeException($"Maximum number of conditions ({conditions.Length}) exceeded");

            conditions[count] = newCondition;
            count++;
        }

        public FilterEnumerator Collect()
        {
            return new FilterEnumerator(_presenter.GetAll(), _includeConditions, _excludeConditions, _includeCount, _excludeCount);
        }

        public ref struct FilterEnumerator
        {
            private readonly EcsEntity[] _entities;
            private readonly ICondition[] _includeConditions;
            private readonly ICondition[] _excludeConditions;
            private readonly int _includeCount;
            private readonly int _excludeCount;
            private int _index;
            private EcsEntity _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal FilterEnumerator(EcsEntity[] entities, ICondition[] includeConditions, ICondition[] excludeConditions, int includeCount, int excludeCount)
            {
                _entities = entities;
                _includeConditions = includeConditions;
                _excludeConditions = excludeConditions;
                _includeCount = includeCount;
                _excludeCount = excludeCount;
                _index = -1;
                _current = default;
            }

            public readonly EcsEntity Current => _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_index < _entities.Length)
                {
                    var entity = _entities[_index];
                    if (MatchesAllConditions(entity))
                    {
                        _current = entity;
                        return true;
                    }
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private readonly bool MatchesAllConditions(EcsEntity entity)
            {
                for (int i = 0; i < _includeCount; i++)
                {
                    if (!_includeConditions[i].Check(entity))
                    {
                        return false;
                    }
                }

                for (int i = 0; i < _excludeCount; i++)
                {
                    if (!_excludeConditions[i].Check(entity))
                    {
                        return false;
                    }
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterEnumerator GetEnumerator() => this;
        }
    }

    public interface ICondition
    {
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

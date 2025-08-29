using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public readonly struct EcsFilter
    {
        private readonly EcsPresenter _presenter;
        private readonly List<Func<EcsEntity, bool>> _includeConditions;
        private readonly List<Func<EcsEntity, bool>> _excludeConditions;

        public EcsFilter(EcsPresenter presenter)
        {
            _presenter = presenter;
            _includeConditions = new(EcsConfig.FilterConditionInclude);
            _excludeConditions = new(EcsConfig.FilterConditionExclude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<T>() where T : struct
        {
            _includeConditions.Add(entity => entity.Has<T>());
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Exclude<T>() where T : struct
        {
            _excludeConditions.Add(entity => !entity.Has<T>());
            return this;
        }

        public FilterEnumerator Collect()
        {
            return new FilterEnumerator(_presenter, _includeConditions, _excludeConditions);
        }

        public struct FilterEnumerator
        {
            private readonly List<Func<EcsEntity, bool>> _includeConditions;
            private readonly List<Func<EcsEntity, bool>> _excludeConditions;
            private IEnumerator<EcsEntity> _entityEnumerator;
            private EcsEntity _current;
            private bool _started;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal FilterEnumerator(
                EcsPresenter presenter,
                List<Func<EcsEntity, bool>> includeConditions,
                List<Func<EcsEntity, bool>> excludeConditions)
            {
                _includeConditions = includeConditions;
                _excludeConditions = excludeConditions;
                _entityEnumerator = presenter.GetAll().GetEnumerator();
                _current = default;
                _started = false;
            }

            public readonly EcsEntity Current => _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (!_started)
                {
                    _started = true;
                    return MoveToNextValid();
                }

                if (!_entityEnumerator.MoveNext())
                    return false;

                return MoveToNextValid();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool MoveToNextValid()
            {
                while (_entityEnumerator.MoveNext())
                {
                    var entity = _entityEnumerator.Current;
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
                if (_includeConditions != null)
                {
                    foreach (var condition in _includeConditions)
                    {
                        if (!condition(entity))
                            return false;
                    }
                }

                if (_excludeConditions != null)
                {
                    foreach (var condition in _excludeConditions)
                    {
                        if (!condition(entity))
                            return false;
                    }
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterEnumerator GetEnumerator() => this;
        }
    }
}

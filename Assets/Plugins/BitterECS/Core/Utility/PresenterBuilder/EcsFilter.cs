using System;
using System.Collections.Generic;
using System.Linq;

namespace BitterECS.Core
{
    public class EcsFilter
    {
        private readonly EcsPresenter _presenter;
        private readonly List<Func<EcsEntity, bool>> _includeConditions = new(EcsConfig.FilterConditionFactor);
        private readonly List<Func<EcsEntity, bool>> _excludeConditions = new(EcsConfig.FilterConditionFactor);

        public EcsFilter(EcsPresenter presenter)
        {
            _presenter = presenter;
        }

        public EcsFilter Include<T>() where T : struct
        {
            _includeConditions.Add(entity => entity.Has<T>());
            return this;
        }

        public EcsFilter Exclude<T>() where T : struct
        {
            _excludeConditions.Add(entity => !entity.Has<T>());
            return this;
        }

        public IEnumerable<EcsEntity> Collect()
        {
            var entities = _presenter.GetAll();

            if (_includeConditions.Count == 0 && _excludeConditions.Count == 0)
                return entities;

            return entities.Where(entity =>
                (_includeConditions.Count == 0 || _includeConditions.All(condition => condition(entity))) &&
                (_excludeConditions.Count == 0 || _excludeConditions.All(condition => condition(entity)))
            );
        }
    }
}

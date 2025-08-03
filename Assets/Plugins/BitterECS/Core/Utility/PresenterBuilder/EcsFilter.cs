using System;
using System.Collections.Generic;
using System.Linq;

namespace BitterECS.Core
{
    public class EcsFilter
    {
        private readonly EcsPresenter _presenter;
        private readonly List<Func<EcsEntity, bool>> _includeConditions = new();
        private readonly List<Func<EcsEntity, bool>> _excludeConditions = new();

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
            return _presenter.GetAll().Where(entity =>
                (_includeConditions.Count == 0 || _includeConditions.All(condition => condition(entity))) &&
                (_excludeConditions.Count == 0 || _excludeConditions.All(condition => condition(entity)))
            );
        }
    }
}

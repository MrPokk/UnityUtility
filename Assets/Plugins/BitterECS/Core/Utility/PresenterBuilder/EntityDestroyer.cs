using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public class EntityDestroyer<T> where T : EcsEntity
    {
        private readonly EcsPresenter _presenter;
        private readonly T _entity;
        private Action<T> _preDestroyCallback;
        private Action<T> _postDestroyCallback;
        private readonly List<Action<T>> _componentRemoveCallbacks = new(EcsConfig.EntityCallbackFactor);

        internal EntityDestroyer(EcsPresenter presenter, T entity)
        {
            _presenter = presenter;
            _entity = entity;
        }

        public EntityDestroyer<T> WithPreDestroyCallback(Action<T> callback)
        {
            _preDestroyCallback = callback;
            return this;
        }

        public EntityDestroyer<T> WithPostDestroyCallback(Action<T> callback)
        {
            _postDestroyCallback = callback;
            return this;
        }

        public EntityDestroyer<T> RemoveComponent<C>() where C : struct
        {
            _componentRemoveCallbacks.Add(entity =>
            {
                var pool = _presenter.GetPool<C>();
                if (pool.Has(entity.Properties.Id))
                {
                    pool.Remove(entity.Properties.Id);
                }
            });
            return this;
        }

        public void Destroy()
        {
            _preDestroyCallback?.Invoke(_entity);

            foreach (var callback in _componentRemoveCallbacks)
            {
                callback(_entity);
            }

            if (_entity.Has<ViewComponent>())
            {
                _entity.Get<ViewComponent>().current?.Dispose();
                EcsLinker.Unlink(_entity);
            }

            _presenter.DestroyEntity(_entity);

            _postDestroyCallback?.Invoke(_entity);
        }
    }
}

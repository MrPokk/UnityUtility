using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public class EntityBuilder<T> where T : EcsEntity
    {
        private readonly EcsPresenter _presenter;
        private Action<T> _postInitCallback;
        private Action<T> _preInitCallback;
        private readonly List<Action<T>> _componentAddCallbacks = new(EcsConfig.EntityCallbackFactor);
        private readonly Dictionary<Type, List<Action<T, object>>> _componentAddedCallbacks = new(EcsConfig.EntityCallbackFactor);
        private ILinkableView _linkableView;

        internal EntityBuilder(EcsPresenter presenter)
        {
            _presenter = presenter;
        }

        public EntityBuilder<T> WithLink(ILinkableView view)
        {
            _linkableView = view;
            return this;
        }

        public EntityBuilder<T> WithPostInitCallback(Action<T> callback)
        {
            _postInitCallback = callback;
            return this;
        }

        public EntityBuilder<T> WithPreInitCallback(Action<T> initAction)
        {
            _preInitCallback = initAction;
            return this;
        }

        public EntityBuilder<T> WithComponent<C>(C component) where C : struct
        {
            _componentAddCallbacks.Add(entity =>
            {
                var pool = _presenter.GetPool<C>();
                pool.Add(entity.Properties.Id, component);

                if (_componentAddedCallbacks.TryGetValue(typeof(C), out var callbacks))
                {
                    foreach (var callback in callbacks)
                    {
                        callback(entity, component);
                    }
                }
            });
            return this;
        }

        public EntityBuilder<T> WithComponentAddedCallback<C>(Action<T, C> callback) where C : struct
        {
            if (!_componentAddedCallbacks.TryGetValue(typeof(C), out var callbacks))
            {
                callbacks = new List<Action<T, object>>();
                _componentAddedCallbacks.Add(typeof(C), callbacks);
            }

            callbacks.Add((entity, component) => callback(entity, (C)component));
            return this;
        }

        public ILinkableEntity CreateToLinkable()
        {
            return Create();
        }

        public T Create()
        {
            var entity = _presenter.CreateEntity<T>();
            _preInitCallback?.Invoke(entity);

            foreach (var callback in _componentAddCallbacks)
            {
                callback(entity);
            }

            if (_linkableView != null)
            {
                EcsLinker.Link(entity, _linkableView);
            }

            _postInitCallback?.Invoke(entity);

            return entity;
        }
    }
}

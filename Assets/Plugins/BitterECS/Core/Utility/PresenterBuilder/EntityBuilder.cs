using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public class EntityBuilder<T> where T : EcsEntity
    {
        private readonly EcsPresenter _presenter;
        private Action<T> _creationCallback;
        private Action<T> _initializationCallback;
        private readonly List<Action<T>> _componentAddCallbacks = new();

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

        public EntityBuilder<T> WithCallback(Action<T> callback)
        {
            _creationCallback = callback;
            return this;
        }

        public EntityBuilder<T> WithInitialization(Action<T> initAction)
        {
            _initializationCallback = initAction;
            return this;
        }

        public EntityBuilder<T> WithComponent<C>(C component) where C : struct
        {
            _componentAddCallbacks.Add(entity =>
            {
                var pool = _presenter.GetPool<C>();
                pool.Add(entity.Properties.Id, component);
            });
            return this;
        }

        public EntityBuilder<T> WithComponent<C>() where C : struct
        {
            return WithComponent<C>(default);
        }

        public ILinkableEntity CreateToLinkable()
        {
            return Create();
        }

        public T Create()
        {
            var entity = _presenter.CreateEntity<T>();

            _initializationCallback?.Invoke(entity);

            foreach (var callback in _componentAddCallbacks)
            {
                callback(entity);
            }

            _creationCallback?.Invoke(entity);

            ref var viewComponent = ref entity.Get<ViewComponent>();

            if (viewComponent.current != null)
            {
                EcsLinker.Link(entity, viewComponent.current);
            }
            else if (_linkableView != null)
            {
                EcsLinker.Link(entity, _linkableView);
            }

            return entity;
        }
    }
}

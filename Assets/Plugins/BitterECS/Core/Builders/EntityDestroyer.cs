using System;
using System.Reflection;

namespace BitterECS.Core
{
    public struct EntityDestroyer
    {
        private readonly EcsPresenter _presenter;
        private readonly EcsEntity _entity;
        private Action<EcsEntity> _preDestroyCallback;
        private Action<EcsEntity> _postDestroyCallback;
        private ComponentRemoveOperations _componentRemoveOps;

        internal EntityDestroyer(EcsPresenter presenter, EcsEntity entity)
        {
            _presenter = presenter;
            _entity = entity;
            _preDestroyCallback = null;
            _postDestroyCallback = null;
            _componentRemoveOps = default;
        }

        public EntityDestroyer WithPreDestroyCallback(Action<EcsEntity> callback)
        {
            _preDestroyCallback = callback;
            return this;
        }

        public EntityDestroyer WithPostDestroyCallback(Action<EcsEntity> callback)
        {
            _postDestroyCallback = callback;
            return this;
        }

        public EntityDestroyer RemoveComponent(Type componentType)
        {
            _componentRemoveOps.Add(componentType, _presenter);
            return this;
        }

        public EntityDestroyer RemoveComponent<C>() where C : struct
        {
            return RemoveComponent(typeof(C));
        }

        public void Destroy()
        {
            _preDestroyCallback?.Invoke(_entity);
            _componentRemoveOps.Execute(_entity);
            _presenter.DestroyEntity(_entity);
            _postDestroyCallback?.Invoke(_entity);
        }

        private struct ComponentRemoveOperations
        {
            private ComponentRemoveOperation[] _operations;
            private int _count;

            public void Add(Type componentType, EcsPresenter presenter)
            {
                if (_operations == null)
                {
                    _operations = new ComponentRemoveOperation[EcsConfig.EntityCallbackFactor];
                }
                else if (_count == _operations.Length)
                {
                    Array.Resize(ref _operations, _operations.Length * 2);
                }

                _operations[_count++] = new ComponentRemoveOperation
                {
                    componentType = componentType,
                    presenter = presenter
                };
            }

            public void Add<C>(EcsPresenter presenter) where C : struct
            {
                Add(typeof(C), presenter);
            }

            public void Execute(EcsEntity entity)
            {
                for (int i = 0; i < _count; i++)
                {
                    ExecuteOperation(ref _operations[i], entity);
                }
            }

            private static void ExecuteOperation(ref ComponentRemoveOperation op, EcsEntity entity)
            {
                var method = typeof(ComponentRemoveOperations).GetMethod(nameof(RemoveComponentInternal),
                    BindingFlags.NonPublic | BindingFlags.Static);
                var generic = method.MakeGenericMethod(op.componentType);
                generic.Invoke(null, new object[] { entity, op.presenter });
            }

            private static void RemoveComponentInternal<C>(EcsEntity entity, EcsPresenter presenter) where C : struct
            {
                var pool = presenter.GetPool<C>();
                if (pool.Has(entity.Properties.Id))
                {
                    pool.Remove(entity.Properties.Id);
                }
            }

            private struct ComponentRemoveOperation
            {
                public Type componentType;
                public EcsPresenter presenter;
            }
        }
    }

    public struct EntityDestroyer<T> where T : EcsEntity
    {
        private EntityDestroyer _destroyer;

        internal EntityDestroyer(EcsPresenter presenter, T entity)
        {
            _destroyer = new EntityDestroyer(presenter, entity);
        }

        public EntityDestroyer<T> WithPreDestroyCallback(Action<T> callback)
        {
            _destroyer.WithPreDestroyCallback(e => callback((T)e));
            return this;
        }

        public EntityDestroyer<T> WithPostDestroyCallback(Action<T> callback)
        {
            _destroyer.WithPostDestroyCallback(e => callback((T)e));
            return this;
        }

        public EntityDestroyer<T> RemoveComponent<C>() where C : struct
        {
            _destroyer.RemoveComponent<C>();
            return this;
        }

        public void Destroy()
        {
            _destroyer.Destroy();
        }
    }
}

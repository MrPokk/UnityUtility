using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public struct EntityDestroyer
    {
        private readonly EcsPresenter _presenter;
        private readonly EcsEntity _entity;
        private Action<EcsEntity> _preDestroyCallback;
        private Action<EcsEntity> _postDestroyCallback;
        private ComponentRemoveOperations _componentRemoveOps;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EntityDestroyer(EcsPresenter presenter, EcsEntity entity)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _entity = entity;
            _preDestroyCallback = null;
            _postDestroyCallback = null;
            _componentRemoveOps = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer WithPreDestroyCallback(Action<EcsEntity> callback)
        {
            _preDestroyCallback = callback;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer WithPostDestroyCallback(Action<EcsEntity> callback)
        {
            _postDestroyCallback = callback;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer RemoveComponent(Type componentType)
        {
            _componentRemoveOps.Add(componentType, _presenter);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add<C>(EcsPresenter presenter) where C : struct
            {
                Add(typeof(C), presenter);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void Execute(EcsEntity entity)
            {
                for (var i = 0; i < _count; i++)
                {
                    ExecuteOperation(ref _operations[i], entity);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void ExecuteOperation(ref ComponentRemoveOperation op, EcsEntity entity)
            {
                var method = typeof(ComponentRemoveOperations).GetMethod(nameof(RemoveComponentInternal),
                    BindingFlags.NonPublic | BindingFlags.Static);
                var generic = method.MakeGenericMethod(op.componentType);
                generic.Invoke(null, new object[] { entity, op.presenter });
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void RemoveComponentInternal<C>(EcsEntity entity, EcsPresenter presenter) where C : struct
            {
                var pool = presenter.GetPool<C>();
                if (pool.Has(entity.GetID()))
                {
                    pool.Remove(entity.GetID());
                }
            }

            private struct ComponentRemoveOperation
            {
                public Type componentType;
                public EcsPresenter presenter;
            }
        }
    }

    public struct EntityDestroyer<E> where E : EcsEntity
    {
        private EntityDestroyer _destroyer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EntityDestroyer(EcsPresenter presenter, E entity)
        {
            _destroyer = new EntityDestroyer(presenter, entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer<E> WithPreDestroyCallback(Action<E> callback)
        {
            _destroyer.WithPreDestroyCallback(e => callback((E)e));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer<E> WithPostDestroyCallback(Action<E> callback)
        {
            _destroyer.WithPostDestroyCallback(e => callback((E)e));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer<E> RemoveComponent<C>() where C : struct
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

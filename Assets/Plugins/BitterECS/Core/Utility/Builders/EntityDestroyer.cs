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
            if (_entity == null) return;

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
                if (_operations == null) _operations = new ComponentRemoveOperation[EcsConfig.EntityCallbackFactor];
                else if (_count == _operations.Length) Array.Resize(ref _operations, _operations.Length * 2);

                _operations[_count++] = new ComponentRemoveOperation
                {
                    componentType = componentType,
                    presenter = presenter
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add<C>(EcsPresenter presenter) where C : struct => Add(typeof(C), presenter);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void Execute(EcsEntity entity)
            {
                for (var i = 0; i < _count; i++) ExecuteOperation(ref _operations[i], entity);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void ExecuteOperation(ref ComponentRemoveOperation op, EcsEntity entity)
            {
                // Примечание: Reflection внутри цикла может быть медленным. 
                // Если возможно, лучше вызывать типизированные методы напрямую, но оставляю как в оригинале.
                var method = typeof(ComponentRemoveOperations).GetMethod(nameof(RemoveComponentInternal),
                    BindingFlags.NonPublic | BindingFlags.Static);
                var generic = method.MakeGenericMethod(op.componentType);
                generic.Invoke(null, new object[] { entity, op.presenter });
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void RemoveComponentInternal<C>(EcsEntity entity, EcsPresenter presenter) where C : struct
            {
                var pool = presenter.GetPool<C>();
                if (pool.Has(entity.GetID())) pool.Remove(entity.GetID());
            }

            private struct ComponentRemoveOperation
            {
                public Type componentType;
                public EcsPresenter presenter;
            }
        }
    }

    public struct EntityDestroyer<TPresenter, TEntity>
        where TPresenter : EcsPresenter
        where TEntity : EcsEntity
    {
        private EntityDestroyer? _destroyer;
        private readonly TEntity _entity;

        public EntityDestroyer(TEntity entity)
        {
            _entity = entity;
            _destroyer = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            if (!_destroyer.HasValue)
            {
                if (_entity == null) throw new InvalidOperationException("Cannot destroy null entity.");
                var presenter = EcsWorld.Get(typeof(TPresenter));
                _destroyer = new EntityDestroyer(presenter, _entity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer<TPresenter, TEntity> WithPreDestroyCallback(Action<TEntity> callback)
        {
            EnsureInitialized();
            _destroyer = _destroyer.Value.WithPreDestroyCallback(e => callback((TEntity)e));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer<TPresenter, TEntity> WithPostDestroyCallback(Action<TEntity> callback)
        {
            EnsureInitialized();
            _destroyer = _destroyer.Value.WithPostDestroyCallback(e => callback((TEntity)e));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer<TPresenter, TEntity> RemoveComponent<C>() where C : struct
        {
            EnsureInitialized();
            _destroyer = _destroyer.Value.RemoveComponent<C>();
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy()
        {
            EnsureInitialized();
            _destroyer.Value.Destroy();
        }
    }

    public struct EntityDestroyer<TPresenter> where TPresenter : EcsPresenter
    {
        private EntityDestroyer? _destroyer;
        private readonly EcsEntity _entity;

        public EntityDestroyer(EcsEntity entity)
        {
            _entity = entity;
            _destroyer = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            if (!_destroyer.HasValue)
            {
                if (_entity == null) throw new InvalidOperationException("Cannot destroy null entity.");
                var presenter = EcsWorld.Get(typeof(TPresenter));
                _destroyer = new EntityDestroyer(presenter, _entity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer<TPresenter> WithPreDestroyCallback(Action<EcsEntity> callback)
        {
            EnsureInitialized();
            _destroyer = _destroyer.Value.WithPreDestroyCallback(callback);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer<TPresenter> WithPostDestroyCallback(Action<EcsEntity> callback)
        {
            EnsureInitialized();
            _destroyer = _destroyer.Value.WithPostDestroyCallback(callback);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDestroyer<TPresenter> RemoveComponent<C>() where C : struct
        {
            EnsureInitialized();
            _destroyer = _destroyer.Value.RemoveComponent<C>();
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy()
        {
            EnsureInitialized();
            _destroyer.Value.Destroy();
        }
    }
}

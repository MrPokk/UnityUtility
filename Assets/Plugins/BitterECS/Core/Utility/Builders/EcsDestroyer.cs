using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public struct EcsDestroyer
    {
        private readonly EcsPresenter _presenter;
        private readonly EcsEntity _entity;
        private Action<EcsEntity> _preDestroyCallback;
        private Action<EcsEntity> _postDestroyCallback;
        private ComponentRemoveOperations _componentRemoveOps;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsDestroyer(EcsPresenter presenter, EcsEntity entity)
        {
            _presenter = presenter;
            _entity = entity;
            _preDestroyCallback = null;
            _postDestroyCallback = null;
            _componentRemoveOps = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsDestroyer WithPreDestroyCallback(Action<EcsEntity> callback)
        {
            _preDestroyCallback = callback;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsDestroyer WithPostDestroyCallback(Action<EcsEntity> callback)
        {
            _postDestroyCallback = callback;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsDestroyer RemoveComponent<C>() where C : new()
        {
            _componentRemoveOps.Add(_presenter.GetPool<C>());
            return this;
        }

        public void Destroy()
        {
            if (!_entity.IsAlive) return;

            _preDestroyCallback?.Invoke(_entity);
            _componentRemoveOps.Execute(_entity.Id);
            _presenter.Remove(_entity);
            _postDestroyCallback?.Invoke(_entity);
        }

        private struct ComponentRemoveOperations
        {
            private IPool[] _pools;
            private int _count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(IPool pool)
            {
                if (_pools == null) _pools = new IPool[EcsConfig.EntityCallbackFactor];
                if (_count == _pools.Length) Array.Resize(ref _pools, _count * 2);
                _pools[_count++] = pool;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void Execute(int entityId)
            {
                for (var i = 0; i < _count; i++)
                {
                    _pools[i].Remove(entityId);
                }
            }
        }
    }

    public struct EcsDestroyer<TPresenter> where TPresenter : EcsPresenter, new()
    {
        private EcsDestroyer _builder;

        public EcsDestroyer(EcsEntity entity)
        {
            _builder = new EcsDestroyer(EcsWorld.Get<TPresenter>(), entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsDestroyer<TPresenter> WithPreDestroyCallback(Action<EcsEntity> callback)
        {
            _builder = _builder.WithPreDestroyCallback(callback);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsDestroyer<TPresenter> WithPostDestroyCallback(Action<EcsEntity> callback)
        {
            _builder = _builder.WithPostDestroyCallback(callback);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsDestroyer<TPresenter> RemoveComponent<C>() where C : new()
        {
            _builder = _builder.RemoveComponent<C>();
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy() => _builder.Destroy();
    }
}

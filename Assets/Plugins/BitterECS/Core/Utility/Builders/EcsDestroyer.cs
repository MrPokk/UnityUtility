using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public struct EcsDestroyer
    {
        private readonly EcsWorld _world;
        public readonly EcsWorld World => _world ?? EcsWorldStatic.Instance;

        private readonly EcsEntity _entity;
        private Action<EcsEntity> _preDestroyCallback;
        private Action<EcsEntity> _postDestroyCallback;
        private ComponentRemoveOperations _componentRemoveOps;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsDestroyer(EcsEntity entity, EcsWorld world = default)
        {
            _world = world;
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
            _componentRemoveOps.Add(World.GetPool<C>());
            return this;
        }

        public void Destroy()
        {
            if (!_entity.IsAlive) return;

            _preDestroyCallback?.Invoke(_entity);
            _componentRemoveOps.Execute(_entity.Id);
            World.Remove(_entity);
            _postDestroyCallback?.Invoke(_entity);
        }

        private struct ComponentRemoveOperations
        {
            private IPool[] _pools;
            private int _count; [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(IPool pool)
            {
                _pools ??= new IPool[EcsDefinitions.EntityCallbackFactor];
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
}

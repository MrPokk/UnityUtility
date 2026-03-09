using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public struct EcsBuilder
    {
        private readonly EcsWorld _world;
        public readonly EcsWorld World => _world ?? EcsWorldStatic.Instance;

        private Action<EcsEntity> _postInitCallback;
        private Action<EcsEntity> _preInitCallback;
        private ComponentAddOperations _componentAddOps;
        private ComponentAddedCallbacks _componentAddedCallbacks;
        private ILinkableProvider _linkableProvider;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsBuilder(EcsWorld world = default)
        {
            _world = world;
            _postInitCallback = null;
            _preInitCallback = null;
            _componentAddOps = default;
            _componentAddedCallbacks = default;
            _linkableProvider = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsBuilder WithLink(ILinkableProvider provider)
        {
            _linkableProvider = provider;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsBuilder WithPost(Action<EcsEntity> callback)
        {
            _postInitCallback = callback;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsBuilder WithPre(Action<EcsEntity> initAction)
        {
            _preInitCallback = initAction;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsBuilder With<C>(C component = default) where C : new()
        {
            _componentAddOps.Add(component);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsBuilder WithAdded<C>(Action<EcsEntity, C> callback) where C : new()
        {
            _componentAddedCallbacks.Add(callback);
            return this;
        }

        public EcsEntity Create()
        {
            var entity = World.CreateEntity();

            _preInitCallback?.Invoke(entity);
            _componentAddOps.Execute(entity, World, ref _componentAddedCallbacks);

            if (_linkableProvider != null)
            {
                World.Link(entity, _linkableProvider);
            }

            _postInitCallback?.Invoke(entity);

            return entity;
        }

        public void Create(int count)
        {
            for (var i = 0; i < count; i++)
            {
                Create();
            }
        }

        private struct ComponentAddOperations
        {
            private IComponentOperation[] _operations;
            private int _count;

            private interface IComponentOperation
            {
                void Execute(EcsEntity entity, EcsWorld world, ref ComponentAddedCallbacks callbacks);
            }

            private struct ComponentOperation<C> : IComponentOperation where C : new()
            {
                public C component;
                public void Execute(EcsEntity entity, EcsWorld world, ref ComponentAddedCallbacks callbacks)
                {
                    entity.Add(component);
                    callbacks.Invoke(entity, component);
                }
            }

            public void Add<C>(C component) where C : new()
            {
                if (_operations == null) _operations = new IComponentOperation[EcsDefinitions.EntityCallbackFactor];
                else if (_count == _operations.Length) Array.Resize(ref _operations, _operations.Length * 2);
                _operations[_count++] = new ComponentOperation<C> { component = component };
            }

            public readonly void Execute(EcsEntity entity, EcsWorld world, ref ComponentAddedCallbacks callbacks)
            {
                for (var i = 0; i < _count; i++) _operations[i].Execute(entity, world, ref callbacks);
            }
        }

        private struct ComponentAddedCallbacks
        {
            private IComponentCallback[] _callbacks;
            private int _count;

            private interface IComponentCallback
            {
                void Invoke(EcsEntity entity, object component);
            }

            private struct ComponentCallback<C> : IComponentCallback where C : new()
            {
                public Action<EcsEntity, C> callback;
                public void Invoke(EcsEntity entity, object component) => callback(entity, (C)component);
            }

            public void Add<C>(Action<EcsEntity, C> callback) where C : new()
            {
                if (_callbacks == null) _callbacks = new IComponentCallback[EcsDefinitions.EntityCallbackFactor];
                else if (_count == _callbacks.Length) Array.Resize(ref _callbacks, _callbacks.Length * 2);
                _callbacks[_count++] = new ComponentCallback<C> { callback = callback };
            }

            public readonly void Invoke<C>(EcsEntity entity, C component) where C : new()
            {
                if (_callbacks == null) return;
                for (var i = 0; i < _count; i++) _callbacks[i].Invoke(entity, component);
            }
        }
    }
}

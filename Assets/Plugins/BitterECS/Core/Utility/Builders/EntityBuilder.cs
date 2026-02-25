using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public struct EntityBuilder
    {
        private readonly EcsPresenter _presenter;
        private Action<EcsEntity> _postInitCallback;
        private Action<EcsEntity> _preInitCallback;
        private ComponentAddOperations _componentAddOps;
        private ComponentAddedCallbacks _componentAddedCallbacks;
        private ILinkableProvider _linkableProvider;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EntityBuilder(EcsPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _postInitCallback = null;
            _preInitCallback = null;
            _componentAddOps = default;
            _componentAddedCallbacks = default;
            _linkableProvider = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithLink(ILinkableProvider provider)
        {
            _linkableProvider = provider;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithPost(Action<EcsEntity> callback)
        {
            _postInitCallback = callback;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithPre(Action<EcsEntity> initAction)
        {
            _preInitCallback = initAction;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder With<C>(C component) where C : new()
        {
            _componentAddOps.Add(component);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithAdded<C>(Action<EcsEntity, C> callback) where C : new()
        {
            _componentAddedCallbacks.Add(callback);
            return this;
        }

        public EcsEntity Create()
        {
            var entity = _presenter.CreateEntity();

            _preInitCallback?.Invoke(entity);
            _componentAddOps.Execute(entity, _presenter, ref _componentAddedCallbacks);

            if (_linkableProvider != null)
            {
                _presenter.Link(entity, _linkableProvider);
            }

            _postInitCallback?.Invoke(entity);

            return entity;
        }

        private struct ComponentAddOperations
        {
            private IComponentOperation[] _operations;
            private int _count;

            private interface IComponentOperation
            {
                void Execute(EcsEntity entity, EcsPresenter presenter, ref ComponentAddedCallbacks callbacks);
            }

            private struct ComponentOperation<C> : IComponentOperation where C : new()
            {
                public C component;
                public void Execute(EcsEntity entity, EcsPresenter presenter, ref ComponentAddedCallbacks callbacks)
                {
                    entity.Add(component);
                    callbacks.Invoke(entity, component);
                }
            }

            public void Add<C>(C component) where C : new()
            {
                if (_operations == null) _operations = new IComponentOperation[EcsConfig.EntityCallbackFactor];
                else if (_count == _operations.Length) Array.Resize(ref _operations, _operations.Length * 2);
                _operations[_count++] = new ComponentOperation<C> { component = component };
            }

            public readonly void Execute(EcsEntity entity, EcsPresenter presenter, ref ComponentAddedCallbacks callbacks)
            {
                for (var i = 0; i < _count; i++) _operations[i].Execute(entity, presenter, ref callbacks);
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
                if (_callbacks == null) _callbacks = new IComponentCallback[EcsConfig.EntityCallbackFactor];
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

    public struct EntityBuilder<TPresenter> where TPresenter : EcsPresenter, new()
    {
        private EntityBuilder? _builder;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            if (!_builder.HasValue) _builder = new EntityBuilder(EcsWorld.Get<TPresenter>());
        }

        public EntityBuilder<TPresenter> WithLink(ILinkableProvider provider)
        {
            EnsureInitialized();
            _builder = _builder.Value.WithLink(provider);
            return this;
        }

        public EntityBuilder<TPresenter> WithPost(Action<EcsEntity> callback)
        {
            EnsureInitialized();
            _builder = _builder.Value.WithPost(callback);
            return this;
        }

        public EntityBuilder<TPresenter> WithPre(Action<EcsEntity> initAction)
        {
            EnsureInitialized();
            _builder = _builder.Value.WithPre(initAction);
            return this;
        }

        public EntityBuilder<TPresenter> With<C>(C component) where C : new()
        {
            EnsureInitialized();
            _builder = _builder.Value.With(component);
            return this;
        }

        public EntityBuilder<TPresenter> WithAdded<C>(Action<EcsEntity, C> callback) where C : new()
        {
            EnsureInitialized();
            _builder = _builder.Value.WithAdded(callback);
            return this;
        }

        public EcsEntity Create()
        {
            EnsureInitialized();
            return _builder.Value.Create();
        }
    }

    // Упрощенный билдер для использования через Build.For().Add<T>()
    public struct EntityBuilderGeneric<TEntity> where TEntity : struct
    {
        private EntityBuilder _builder;

        internal EntityBuilderGeneric(EcsPresenter presenter) => _builder = new EntityBuilder(presenter);

        public EntityBuilderGeneric<TEntity> WithLink(ILinkableProvider provider) { _builder.WithLink(provider); return this; }
        public EntityBuilderGeneric<TEntity> WithPost(Action<EcsEntity> callback) { _builder.WithPost(callback); return this; }
        public EntityBuilderGeneric<TEntity> WithPre(Action<EcsEntity> initAction) { _builder.WithPre(initAction); return this; }
        public EntityBuilderGeneric<TEntity> With<C>(C component) where C : new() { _builder.With(component); return this; }
        public EntityBuilderGeneric<TEntity> WithAdded<C>(Action<EcsEntity, C> callback) where C : new() { _builder.WithAdded(callback); return this; }

        public TEntity Create()
        {
            var entity = _builder.Create();
            return (TEntity)(object)entity;
        }
    }
}

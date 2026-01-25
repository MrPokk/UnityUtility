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
        private bool _isForce;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EntityBuilder(EcsPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _postInitCallback = null;
            _preInitCallback = null;
            _componentAddOps = default;
            _componentAddedCallbacks = default;
            _linkableProvider = null;
            _isForce = false;
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
        public EntityBuilder With<C>(C component) where C : struct
        {
            _componentAddOps.Add(component);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithAdded<C>(Action<EcsEntity, C> callback) where C : struct
        {
            _componentAddedCallbacks.Add(callback);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithForce()
        {
            _isForce = true;
            return this;
        }

        public EcsEntity Create(Type entityType)
        {
            var entity = _isForce
                ? _presenter.Create(entityType, _isForce)
                : _presenter.Create(entityType);

            _preInitCallback?.Invoke(entity);
            _componentAddOps.Execute(entity, _presenter, ref _componentAddedCallbacks);
            LinkProviderIfNeeded(entity);
            _postInitCallback?.Invoke(entity);

            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void LinkProviderIfNeeded(EcsEntity entity)
        {
            if (_linkableProvider != null)
            {
                _presenter.Link(entity, _linkableProvider);
            }
        }

        private struct ComponentAddOperations
        {
            private IComponentOperation[] _operations;
            private int _count;

            private interface IComponentOperation
            {
                void Execute(EcsEntity entity, EcsPresenter presenter, ref ComponentAddedCallbacks callbacks);
            }

            private struct ComponentOperation<C> : IComponentOperation where C : struct
            {
                public C component;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public readonly void Execute(EcsEntity entity, EcsPresenter presenter, ref ComponentAddedCallbacks callbacks)
                {
                    entity.Add(component);
                    callbacks.Invoke(entity, component);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add<C>(C component) where C : struct
            {
                if (_operations == null) _operations = new IComponentOperation[EcsConfig.EntityCallbackFactor];
                else if (_count == _operations.Length) Array.Resize(ref _operations, _operations.Length * 2);
                _operations[_count++] = new ComponentOperation<C> { component = component };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            private struct ComponentCallback<C> : IComponentCallback where C : struct
            {
                public Action<EcsEntity, C> callback;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public readonly void Invoke(EcsEntity entity, object component) => callback(entity, (C)component);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add<C>(Action<EcsEntity, C> callback) where C : struct
            {
                if (_callbacks == null) _callbacks = new IComponentCallback[EcsConfig.EntityCallbackFactor];
                else if (_count == _callbacks.Length) Array.Resize(ref _callbacks, _callbacks.Length * 2);
                _callbacks[_count++] = new ComponentCallback<C> { callback = callback };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void Invoke<C>(EcsEntity entity, C component) where C : struct
            {
                if (_callbacks == null) return;
                for (var i = 0; i < _count; i++) _callbacks[i].Invoke(entity, component);
            }
        }
    }

    public struct EntityBuilder<TPresenter, TEntity>
        where TPresenter : EcsPresenter
        where TEntity : EcsEntity
    {
        private EntityBuilder? _builder;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            if (_builder.HasValue)
            {
                return;
            }

            _builder = new EntityBuilder(EcsWorld.Get(typeof(TPresenter)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TPresenter, TEntity> WithLink(ILinkableProvider provider)
        {
            EnsureInitialized();
            _builder = _builder.Value.WithLink(provider);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TPresenter, TEntity> WithPost(Action<TEntity> callback)
        {
            EnsureInitialized();
            _builder = _builder.Value.WithPost(e => callback((TEntity)e));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TPresenter, TEntity> WithPre(Action<TEntity> initAction)
        {
            EnsureInitialized();
            _builder = _builder.Value.WithPre(e => initAction((TEntity)e));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TPresenter, TEntity> With<C>(C component) where C : struct
        {
            EnsureInitialized();
            _builder = _builder.Value.With(component);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TPresenter, TEntity> WithAdded<C>(Action<TEntity, C> callback) where C : struct
        {
            EnsureInitialized();
            _builder = _builder.Value.WithAdded<C>((entity, component) => callback((TEntity)entity, component));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TPresenter, TEntity> WithForce()
        {
            EnsureInitialized();
            _builder = _builder.Value.WithForce();
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TEntity Create()
        {
            EnsureInitialized();
            return (TEntity)_builder.Value.Create(typeof(TEntity));
        }
    }

    public struct EntityBuilder<TEntity> where TEntity : EcsEntity
    {
        private EntityBuilder _builder;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EntityBuilder(EcsPresenter presenter)
        {
            _builder = new EntityBuilder(presenter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TEntity> WithLink(ILinkableProvider provider)
        {
            _builder.WithLink(provider);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TEntity> WithPost(Action<TEntity> callback)
        {
            _builder.WithPost(e => callback((TEntity)e));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TEntity> WithPre(Action<TEntity> initAction)
        {
            _builder.WithPre(e => initAction((TEntity)e));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TEntity> With<C>(C component) where C : struct
        {
            _builder.With(component);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TEntity> WithAdded<C>(Action<TEntity, C> callback) where C : struct
        {
            _builder.WithAdded<C>((entity, component) => callback((TEntity)entity, component));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<TEntity> WithForce()
        {
            _builder.WithForce();
            return this;
        }

        public TEntity Create()
        {
            return (TEntity)_builder.Create(typeof(TEntity));
        }
    }
}

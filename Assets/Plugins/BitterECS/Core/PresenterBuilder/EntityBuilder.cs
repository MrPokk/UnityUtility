using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

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
            _presenter = presenter;
            _postInitCallback = null;
            _preInitCallback = null;
            _componentAddOps = default;
            _componentAddedCallbacks = default;
            _linkableProvider = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithLink(ILinkableProvider Provider)
        {
            _linkableProvider = Provider;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithPostInitCallback(Action<EcsEntity> callback)
        {
            _postInitCallback = callback;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithPreInitCallback(Action<EcsEntity> initAction)
        {
            _preInitCallback = initAction;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithComponent(Type componentType, object component)
        {
            _componentAddOps.Add(componentType, component, _presenter, ref _componentAddedCallbacks);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithComponent<C>(C component) where C : struct
        {
            return WithComponent(typeof(C), component);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithComponentAddedCallback(Type componentType, Action<EcsEntity, object> callback)
        {
            _componentAddedCallbacks.Add(componentType, callback);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder WithComponentAddedCallback<C>(Action<EcsEntity, C> callback) where C : struct
        {
            _componentAddedCallbacks.Add(typeof(C), (entity, component) => callback(entity, (C)component));
            return this;
        }

        public EcsEntity Create(Type entityType)
        {
            var entity = _presenter.Create(entityType);
            _preInitCallback?.Invoke(entity);

            _componentAddOps.Execute(entity);

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
            private ComponentAddOperation[] _operations;
            private int _count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(Type componentType, object component, EcsPresenter presenter, ref ComponentAddedCallbacks callbacks)
            {
                if (_operations == null)
                {
                    _operations = new ComponentAddOperation[EcsConfig.EntityCallbackFactor];
                }
                else if (_count == _operations.Length)
                {
                    Array.Resize(ref _operations, _operations.Length * 2);
                }

                _operations[_count++] = new ComponentAddOperation
                {
                    componentType = componentType,
                    component = component,
                    presenter = presenter,
                    callbacks = callbacks
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add<C>(C component, EcsPresenter presenter, ref ComponentAddedCallbacks callbacks) where C : struct
            {
                Add(typeof(C), component, presenter, ref callbacks);
            }

            public void Execute(EcsEntity entity)
            {
                for (int i = 0; i < _count; i++)
                {
                    ExecuteOperation(ref _operations[i], entity);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void ExecuteOperation(ref ComponentAddOperation op, EcsEntity entity)
            {
                var method = typeof(ComponentAddOperations).GetMethod(nameof(AddComponentInternal),
                    BindingFlags.NonPublic | BindingFlags.Static);
                var generic = method.MakeGenericMethod(op.componentType);
                generic.Invoke(null, new object[] { entity, op.component, op.presenter, op.callbacks });
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void AddComponentInternal<C>(EcsEntity entity, object component, EcsPresenter presenter, ComponentAddedCallbacks callbacks) where C : struct
            {
                var pool = presenter.GetPool<C>();
                pool.Add(entity.Properties.Id, (C)component);
                callbacks.Invoke<C>(entity, (C)component);
            }

            private struct ComponentAddOperation
            {
                public Type componentType;
                public object component;
                public EcsPresenter presenter;
                public ComponentAddedCallbacks callbacks;
            }
        }

        private struct ComponentAddedCallbacks
        {
            private Dictionary<Type, List<Action<EcsEntity, object>>> _callbacks;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(Type componentType, Action<EcsEntity, object> callback)
            {
                _callbacks ??= new Dictionary<Type, List<Action<EcsEntity, object>>>(EcsConfig.EntityCallbackFactor);

                if (!_callbacks.TryGetValue(componentType, out var list))
                {
                    list = new List<Action<EcsEntity, object>>(EcsConfig.EntityCallbackFactor);
                    _callbacks[componentType] = list;
                }

                list.Add(callback);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Invoke<C>(EcsEntity entity, C component) where C : struct
            {
                if (_callbacks == null || !_callbacks.TryGetValue(typeof(C), out var list))
                {
                    return;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    list[i](entity, component);
                }
            }
        }
    }

    public struct EntityBuilder<T> where T : EcsEntity
    {
        private EntityBuilder _builder;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EntityBuilder(EcsPresenter presenter)
        {
            _builder = new EntityBuilder(presenter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<T> WithLink(ILinkableProvider Provider)
        {
            _builder.WithLink(Provider);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<T> WithPostInitCallback(Action<T> callback)
        {
            _builder.WithPostInitCallback(e => callback((T)e));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<T> WithPreInitCallback(Action<T> initAction)
        {
            _builder.WithPreInitCallback(e => initAction((T)e));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<T> WithComponent<C>(C component) where C : struct
        {
            _builder.WithComponent<C>(component);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityBuilder<T> WithComponentAddedCallback<C>(Action<T, C> callback) where C : struct
        {
            _builder.WithComponentAddedCallback<C>((entity, component) => callback((T)entity, component));
            return this;
        }

        public T Create()
        {
            return (T)_builder.Create(typeof(T));
        }
    }
}

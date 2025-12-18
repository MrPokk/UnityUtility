using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public readonly struct EcsEvent
    {
        private readonly EcsPresenter _presenter;
        private readonly Dictionary<Type, IEcsEvent> _subscriptions;
        public readonly int Count => _subscriptions.Count;

        public EcsEvent(EcsPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException($"{nameof(presenter)} is Presenter null");
            _subscriptions = new();
        }

        public readonly EcsEvent Subscribe<T>(Action<EcsEntity> added, Action<EcsEntity> removed = null) where T : struct
        {
            _presenter.AddCheckEvent<T>();
            var eventPool = _presenter.GetPool<T>() as EcsEventPool<T> ?? throw new OperationCanceledException($"Is not Subscribe Pool");
            var eventToPool = new ObjectEvent<T>(_presenter, added, removed);
            eventPool.Subscribe(eventToPool);

            _subscriptions.Add(typeof(T), eventToPool);

            return this;
        }

        public readonly EcsEvent Unsubscribe<T>() where T : struct
        {
            var eventPool = _presenter.GetPool<T>() as EcsEventPool<T> ?? throw new OperationCanceledException($"Is not Subscribe Pool");
            var eventToPool = _subscriptions.GetValueOrDefault(typeof(T));
            eventPool.Unsubscribe(eventToPool);

            _subscriptions.Remove(typeof(T));

            return this;
        }
    }

    public class ObjectEvent<T> : IEcsEvent where T : struct
    {
        private readonly EcsPresenter _presenter;
        private readonly Action<EcsEntity> _added;
        private readonly Action<EcsEntity> _removed;

        public EcsPresenter Presenter => _presenter;
        public Action<EcsEntity> Added => _added;
        public Action<EcsEntity> Removed => _removed;

        public ObjectEvent(EcsPresenter presenter, Action<EcsEntity> added, Action<EcsEntity> removed)
        {
            _presenter = presenter;
            _added = added;
            _removed = removed;
        }
    }

    public interface IEcsEvent
    {
        EcsPresenter Presenter { get; }
        Action<EcsEntity> Added { get; }
        Action<EcsEntity> Removed { get; }
    }
}


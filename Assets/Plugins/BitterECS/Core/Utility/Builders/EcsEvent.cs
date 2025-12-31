using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public readonly struct EcsEvent
    {
        private readonly EcsPresenter _presenter;
        private readonly Dictionary<Guid, IConditionalEvent> _conditionalSubscriptions;
        
        public readonly int Count => _conditionalSubscriptions.Count;

        public EcsEvent(EcsPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException($"{nameof(presenter)} is Presenter null");
            _conditionalSubscriptions = new();
        }

        public readonly EcsEvent Subscribe<T>(Action<EcsEntity> added, Action<EcsEntity> removed = null) where T : struct
        {
            return SubscribeWhere<T>(entity => entity.Has<T>(), added, removed);
        }

        public readonly EcsEvent SubscribeWhere<T>(Func<EcsEntity, bool> condition, Action<EcsEntity> added, Action<EcsEntity> removed = null) 
            where T : struct
        {
            var subscriptionId = Guid.NewGuid();
            var subscription = new ConditionalEvent<T>(_presenter, condition, added, removed);
            _conditionalSubscriptions[subscriptionId] = subscription;
            return this;
        }

        public readonly EcsEvent SubscribeWhere<T1, T2>(Func<EcsEntity, bool> condition, Action<EcsEntity> added, Action<EcsEntity> removed = null) 
            where T1 : struct
            where T2 : struct
        {
            var subscriptionId = Guid.NewGuid();
            var subscription = new ConditionalEvent<T1, T2>(_presenter, condition, added, removed);
            _conditionalSubscriptions[subscriptionId] = subscription;
            return this;
        }

        public readonly EcsEvent SubscribeWhere<T1, T2, T3>(Func<EcsEntity, bool> condition, Action<EcsEntity> added, Action<EcsEntity> removed = null) 
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            var subscriptionId = Guid.NewGuid();
            var subscription = new ConditionalEvent<T1, T2, T3>(_presenter, condition, added, removed);
            _conditionalSubscriptions[subscriptionId] = subscription;
            return this;
        }

        public readonly EcsEvent SubscribeWhere<T1, T2, T3, T4>(Func<EcsEntity, bool> condition, Action<EcsEntity> added, Action<EcsEntity> removed = null) 
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            var subscriptionId = Guid.NewGuid();
            var subscription = new ConditionalEvent<T1, T2, T3, T4>(_presenter, condition, added, removed);
            _conditionalSubscriptions[subscriptionId] = subscription;
            return this;
        }

        public readonly void Clear()
        {
            foreach (var subscription in _conditionalSubscriptions.Values)
            {
                subscription.Dispose();
            }
            _conditionalSubscriptions.Clear();
        }
    }

    internal interface IConditionalEvent : IDisposable { }

    internal class ConditionalEvent<T1> : IConditionalEvent where T1 : struct
    {
        private readonly EcsPresenter _presenter;
        private readonly Func<EcsEntity, bool> _condition;
        private readonly Action<EcsEntity> _added;
        private readonly Action<EcsEntity> _removed;
        private readonly HashSet<int> _activeEntities;
        private readonly ComponentSubscription<T1> _subscription1;

        public ConditionalEvent(
            EcsPresenter presenter,
            Func<EcsEntity, bool> condition,
            Action<EcsEntity> added,
            Action<EcsEntity> removed)
        {
            _presenter = presenter;
            _condition = condition;
            _added = added;
            _removed = removed;
            _activeEntities = new HashSet<int>();
            
            _subscription1 = new ComponentSubscription<T1>(presenter, OnComponentChanged);

            CheckAllEntities();
        }

        private void OnComponentChanged(EcsEntity entity)
        {
            CheckEntity(entity);
        }

        private void CheckEntity(EcsEntity entity)
        {
            if (entity == null) return;

            var entityId = entity.Properties.Id;
            var conditionSatisfied = _condition(entity);

            var wasActive = _activeEntities.Contains(entityId);
            
            if (conditionSatisfied && !wasActive)
            {
                _activeEntities.Add(entityId);
                _added?.Invoke(entity);
            }
            else if (!conditionSatisfied && wasActive)
            {
                _activeEntities.Remove(entityId);
                _removed?.Invoke(entity);
            }
        }

        private void CheckAllEntities()
        {
            var allEntities = _presenter.GetAll();
            foreach (var entity in allEntities)
            {
                CheckEntity(entity);
            }
        }

        public void Dispose()
        {
            _subscription1?.Dispose();
            _activeEntities.Clear();
        }
    }

    internal class ConditionalEvent<T1, T2> : IConditionalEvent 
        where T1 : struct
        where T2 : struct
    {
        private readonly EcsPresenter _presenter;
        private readonly Func<EcsEntity, bool> _condition;
        private readonly Action<EcsEntity> _added;
        private readonly Action<EcsEntity> _removed;
        private readonly HashSet<int> _activeEntities;
        private readonly ComponentSubscription<T1> _subscription1;
        private readonly ComponentSubscription<T2> _subscription2;

        public ConditionalEvent(
            EcsPresenter presenter,
            Func<EcsEntity, bool> condition,
            Action<EcsEntity> added,
            Action<EcsEntity> removed)
        {
            _presenter = presenter;
            _condition = condition;
            _added = added;
            _removed = removed;
            _activeEntities = new HashSet<int>();

            _subscription1 = new ComponentSubscription<T1>(presenter, OnComponentChanged);
            _subscription2 = new ComponentSubscription<T2>(presenter, OnComponentChanged);

            CheckAllEntities();
        }

        private void OnComponentChanged(EcsEntity entity)
        {
            CheckEntity(entity);
        }

        private void CheckEntity(EcsEntity entity)
        {
            if (entity == null) return;

            var entityId = entity.Properties.Id;
            var conditionSatisfied = _condition(entity);

            var wasActive = _activeEntities.Contains(entityId);
            
            if (conditionSatisfied && !wasActive)
            {
                _activeEntities.Add(entityId);
                _added?.Invoke(entity);
            }
            else if (!conditionSatisfied && wasActive)
            {
                _activeEntities.Remove(entityId);
                _removed?.Invoke(entity);
            }
        }

        private void CheckAllEntities()
        {
            var allEntities = _presenter.GetAll();
            foreach (var entity in allEntities)
            {
                CheckEntity(entity);
            }
        }

        public void Dispose()
        {
            _subscription1?.Dispose();
            _subscription2?.Dispose();
            _activeEntities.Clear();
        }
    }

    internal class ConditionalEvent<T1, T2, T3> : IConditionalEvent 
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        private readonly EcsPresenter _presenter;
        private readonly Func<EcsEntity, bool> _condition;
        private readonly Action<EcsEntity> _added;
        private readonly Action<EcsEntity> _removed;
        private readonly HashSet<int> _activeEntities;
        private readonly ComponentSubscription<T1> _subscription1;
        private readonly ComponentSubscription<T2> _subscription2;
        private readonly ComponentSubscription<T3> _subscription3;

        public ConditionalEvent(
            EcsPresenter presenter,
            Func<EcsEntity, bool> condition,
            Action<EcsEntity> added,
            Action<EcsEntity> removed)
        {
            _presenter = presenter;
            _condition = condition;
            _added = added;
            _removed = removed;
            _activeEntities = new HashSet<int>();

            // Создаем подписки на компоненты
            _subscription1 = new ComponentSubscription<T1>(presenter, OnComponentChanged);
            _subscription2 = new ComponentSubscription<T2>(presenter, OnComponentChanged);
            _subscription3 = new ComponentSubscription<T3>(presenter, OnComponentChanged);

            // Проверяем существующие сущности
            CheckAllEntities();
        }

        private void OnComponentChanged(EcsEntity entity)
        {
            CheckEntity(entity);
        }

        private void CheckEntity(EcsEntity entity)
        {
            if (entity == null) return;

            var entityId = entity.Properties.Id;
            var conditionSatisfied = _condition(entity);

            var wasActive = _activeEntities.Contains(entityId);
            
            if (conditionSatisfied && !wasActive)
            {
                _activeEntities.Add(entityId);
                _added?.Invoke(entity);
            }
            else if (!conditionSatisfied && wasActive)
            {
                _activeEntities.Remove(entityId);
                _removed?.Invoke(entity);
            }
        }

        private void CheckAllEntities()
        {
            var allEntities = _presenter.GetAll();
            foreach (var entity in allEntities)
            {
                CheckEntity(entity);
            }
        }

        public void Dispose()
        {
            _subscription1?.Dispose();
            _subscription2?.Dispose();
            _subscription3?.Dispose();
            _activeEntities.Clear();
        }
    }

    internal class ConditionalEvent<T1, T2, T3, T4> : IConditionalEvent 
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        private readonly EcsPresenter _presenter;
        private readonly Func<EcsEntity, bool> _condition;
        private readonly Action<EcsEntity> _added;
        private readonly Action<EcsEntity> _removed;
        private readonly HashSet<int> _activeEntities;
        private readonly ComponentSubscription<T1> _subscription1;
        private readonly ComponentSubscription<T2> _subscription2;
        private readonly ComponentSubscription<T3> _subscription3;
        private readonly ComponentSubscription<T4> _subscription4;

        public ConditionalEvent(
            EcsPresenter presenter,
            Func<EcsEntity, bool> condition,
            Action<EcsEntity> added,
            Action<EcsEntity> removed)
        {
            _presenter = presenter;
            _condition = condition;
            _added = added;
            _removed = removed;
            _activeEntities = new HashSet<int>();

            _subscription1 = new ComponentSubscription<T1>(presenter, OnComponentChanged);
            _subscription2 = new ComponentSubscription<T2>(presenter, OnComponentChanged);
            _subscription3 = new ComponentSubscription<T3>(presenter, OnComponentChanged);
            _subscription4 = new ComponentSubscription<T4>(presenter, OnComponentChanged);

            CheckAllEntities();
        }

        private void OnComponentChanged(EcsEntity entity)
        {
            CheckEntity(entity);
        }

        private void CheckEntity(EcsEntity entity)
        {
            if (entity == null) return;

            var entityId = entity.Properties.Id;
            var conditionSatisfied = _condition(entity);

            var wasActive = _activeEntities.Contains(entityId);
            
            if (conditionSatisfied && !wasActive)
            {
                _activeEntities.Add(entityId);
                _added?.Invoke(entity);
            }
            else if (!conditionSatisfied && wasActive)
            {
                _activeEntities.Remove(entityId);
                _removed?.Invoke(entity);
            }
        }

        private void CheckAllEntities()
        {
            var allEntities = _presenter.GetAll();
            foreach (var entity in allEntities)
            {
                CheckEntity(entity);
            }
        }

        public void Dispose()
        {
            _subscription1?.Dispose();
            _subscription2?.Dispose();
            _subscription3?.Dispose();
            _subscription4?.Dispose();
            _activeEntities.Clear();
        }
    }

    internal class ComponentSubscription<T> : IConditionalEvent where T : struct
    {
        private readonly EcsEventPool<T> _pool;
        private readonly ObjectEvent<T> _eventObject;

        public ComponentSubscription(EcsPresenter presenter, Action<EcsEntity> onChanged)
        {
            presenter.AddCheckEvent<T>();
            _pool = presenter.GetPool<T>() as EcsEventPool<T> ?? throw new InvalidOperationException($"Pool for {typeof(T)} is not an event pool");
            
            _eventObject = new ObjectEvent<T>(presenter, onChanged, onChanged);
            _pool.Subscribe(_eventObject);
        }

        public void Dispose()
        {
            _pool.Unsubscribe(_eventObject);
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

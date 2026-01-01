using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public readonly struct EcsEvent
    {
        private readonly EcsPresenter _presenter;
        private readonly Dictionary<Guid, IConditionalEvent> _conditionalSubscriptions;

        public readonly int Count => _conditionalSubscriptions?.Count ?? 0;

        public EcsEvent(EcsPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException($"{nameof(presenter)} is Presenter null");
            _conditionalSubscriptions = new();
        }

        public readonly EcsEvent Subscribe<T>(Action<EcsEntity> added, Action<EcsEntity> removed = null) where T : struct
        {
            return SubscribeWhere<T>(component => true, added, removed);
        }

        public readonly EcsEvent SubscribeWhere<T>(Predicate<T> condition, Action<EcsEntity> added, Action<EcsEntity> removed = null)
            where T : struct
        {
            AddSubscription(new Type[] { typeof(T) }, condition, added, removed);
            return this;
        }

        public readonly EcsEvent SubscribeWhere<T1, T2>(
            Func<T1, T2, bool> condition, 
            Action<EcsEntity> added, 
            Action<EcsEntity> removed = null)
            where T1 : struct
            where T2 : struct
        {
            AddSubscription(new Type[] { typeof(T1), typeof(T2) }, condition, added, removed);
            return this;
        }

        public readonly EcsEvent SubscribeWhere<T1, T2, T3>(
            Func<T1, T2, T3, bool> condition, 
            Action<EcsEntity> added, 
            Action<EcsEntity> removed = null)
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            AddSubscription(new Type[] { typeof(T1), typeof(T2), typeof(T3) }, condition, added, removed);
            return this;
        }

        public readonly EcsEvent SubscribeWhere<T1, T2, T3, T4>(
            Func<T1, T2, T3, T4, bool> condition, 
            Action<EcsEntity> added, 
            Action<EcsEntity> removed = null)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            AddSubscription(new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, condition, added, removed);
            return this;
        }

        public readonly EcsEvent SubscribeWhereEntity<T>(Predicate<EcsEntity> condition, Action<EcsEntity> added, Action<EcsEntity> removed = null)
            where T : struct
        {
            AddSubscriptionEntity(new Type[] { typeof(T) }, condition, added, removed);
            return this;
        }

        public readonly EcsEvent SubscribeWhereEntity<T1, T2>(Predicate<EcsEntity> condition, Action<EcsEntity> added, Action<EcsEntity> removed = null)
            where T1 : struct
            where T2 : struct
        {
            AddSubscriptionEntity(new Type[] { typeof(T1), typeof(T2) }, condition, added, removed);
            return this;
        }

        private readonly void AddSubscription<T>(
            Type[] componentTypes, 
            Predicate<T> condition, 
            Action<EcsEntity> added, 
            Action<EcsEntity> removed)
            where T : struct
        {
            var subscriptionId = Guid.NewGuid();
            var subscription = new ConditionalEventWithComponent<T>(
                _presenter, 
                componentTypes, 
                condition, 
                added, 
                removed);
            _conditionalSubscriptions[subscriptionId] = subscription;
        }

        private readonly void AddSubscription<T1, T2>(
            Type[] componentTypes, 
            Func<T1, T2, bool> condition, 
            Action<EcsEntity> added, 
            Action<EcsEntity> removed)
            where T1 : struct
            where T2 : struct
        {
            var subscriptionId = Guid.NewGuid();
            var subscription = new ConditionalEventWithComponents<T1, T2>(
                _presenter, 
                componentTypes, 
                condition, 
                added, 
                removed);
            _conditionalSubscriptions[subscriptionId] = subscription;
        }

        private readonly void AddSubscription<T1, T2, T3>(
            Type[] componentTypes, 
            Func<T1, T2, T3, bool> condition, 
            Action<EcsEntity> added, 
            Action<EcsEntity> removed)
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            var subscriptionId = Guid.NewGuid();
            var subscription = new ConditionalEventWithComponents<T1, T2, T3>(
                _presenter, 
                componentTypes, 
                condition, 
                added, 
                removed);
            _conditionalSubscriptions[subscriptionId] = subscription;
        }

        private readonly void AddSubscription<T1, T2, T3, T4>(
            Type[] componentTypes, 
            Func<T1, T2, T3, T4, bool> condition, 
            Action<EcsEntity> added, 
            Action<EcsEntity> removed)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            var subscriptionId = Guid.NewGuid();
            var subscription = new ConditionalEventWithComponents<T1, T2, T3, T4>(
                _presenter, 
                componentTypes, 
                condition, 
                added, 
                removed);
            _conditionalSubscriptions[subscriptionId] = subscription;
        }

        private readonly void AddSubscriptionEntity(Type[] componentTypes, Predicate<EcsEntity> condition, Action<EcsEntity> added, Action<EcsEntity> removed)
        {
            var subscriptionId = Guid.NewGuid();
            var subscription = new ConditionalEventWithEntity(_presenter, componentTypes, condition, added, removed);
            _conditionalSubscriptions[subscriptionId] = subscription;
        }

        public void Dispose()
        {
            foreach (var subscription in _conditionalSubscriptions.Values)
            {
                subscription.Dispose();
            }
            _conditionalSubscriptions.Clear();
        }
    }

    internal interface IConditionalEvent : IDisposable { }

    internal abstract class ConditionalEventBase<TDelegate> : IConditionalEvent where TDelegate : Delegate
    {
        protected readonly EcsPresenter _presenter;
        protected readonly TDelegate _condition;
        protected readonly Action<EcsEntity> _added;
        protected readonly Action<EcsEntity> _removed;
        protected readonly HashSet<int> _activeEntities;
        protected readonly List<IComponentSubscription> _subscriptions;

        protected ConditionalEventBase(
            EcsPresenter presenter,
            Type[] componentTypes,
            TDelegate condition,
            Action<EcsEntity> added,
            Action<EcsEntity> removed)
        {
            _presenter = presenter;
            _condition = condition;
            _added = added;
            _removed = removed;
            _activeEntities = new HashSet<int>();
            _subscriptions = new List<IComponentSubscription>();

            foreach (var componentType in componentTypes)
            {
                var subscription = CreateComponentSubscription(componentType);
                _subscriptions.Add(subscription);
            }

            CheckAllEntities();
        }

        protected abstract bool CheckCondition(EcsEntity entity);

        private IComponentSubscription CreateComponentSubscription(Type componentType)
        {
            var subscriptionType = typeof(ComponentSubscription<>).MakeGenericType(componentType);
            return (IComponentSubscription)Activator.CreateInstance(
                subscriptionType,
                _presenter,
                (Action<EcsEntity>)OnComponentChanged);
        }

        protected void OnComponentChanged(EcsEntity entity)
        {
            CheckEntity(entity);
        }

        private void CheckEntity(EcsEntity entity)
        {
            if (entity == null) return;

            var entityId = entity.Properties.Id;
            var conditionSatisfied = CheckCondition(entity);

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
            var allEntities = _presenter.GetAliveEntities();
            foreach (var entity in allEntities)
            {
                CheckEntity(entity);
            }
        }

        public virtual void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();
            _activeEntities.Clear();
        }
    }

    internal class ConditionalEventWithComponent<T> : ConditionalEventBase<Predicate<T>> where T : struct
    {
        public ConditionalEventWithComponent(
            EcsPresenter presenter,
            Type[] componentTypes,
            Predicate<T> condition,
            Action<EcsEntity> added,
            Action<EcsEntity> removed)
            : base(presenter, componentTypes, condition, added, removed)
        {
        }

        protected override bool CheckCondition(EcsEntity entity)
        {
            if (!entity.Has<T>()) return false;
            var component = entity.Get<T>();
            return _condition(component);
        }
    }

    internal class ConditionalEventWithComponents<T1, T2> : ConditionalEventBase<Func<T1, T2, bool>> 
        where T1 : struct
        where T2 : struct
    {
        public ConditionalEventWithComponents(
            EcsPresenter presenter,
            Type[] componentTypes,
            Func<T1, T2, bool> condition,
            Action<EcsEntity> added,
            Action<EcsEntity> removed)
            : base(presenter, componentTypes, condition, added, removed)
        {
        }

        protected override bool CheckCondition(EcsEntity entity)
        {
            if (!entity.Has<T1>() || !entity.Has<T2>()) return false;
            var component1 = entity.Get<T1>();
            var component2 = entity.Get<T2>();
            return _condition(component1, component2);
        }
    }

    internal class ConditionalEventWithComponents<T1, T2, T3> : ConditionalEventBase<Func<T1, T2, T3, bool>> 
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        public ConditionalEventWithComponents(
            EcsPresenter presenter,
            Type[] componentTypes,
            Func<T1, T2, T3, bool> condition,
            Action<EcsEntity> added,
            Action<EcsEntity> removed)
            : base(presenter, componentTypes, condition, added, removed)
        {
        }

        protected override bool CheckCondition(EcsEntity entity)
        {
            if (!entity.Has<T1>() || !entity.Has<T2>() || !entity.Has<T3>()) return false;
            var component1 = entity.Get<T1>();
            var component2 = entity.Get<T2>();
            var component3 = entity.Get<T3>();
            return _condition(component1, component2, component3);
        }
    }

    internal class ConditionalEventWithComponents<T1, T2, T3, T4> : ConditionalEventBase<Func<T1, T2, T3, T4, bool>> 
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        public ConditionalEventWithComponents(
            EcsPresenter presenter,
            Type[] componentTypes,
            Func<T1, T2, T3, T4, bool> condition,
            Action<EcsEntity> added,
            Action<EcsEntity> removed)
            : base(presenter, componentTypes, condition, added, removed)
        {
        }

        protected override bool CheckCondition(EcsEntity entity)
        {
            if (!entity.Has<T1>() || !entity.Has<T2>() || !entity.Has<T3>() || !entity.Has<T4>()) return false;
            var component1 = entity.Get<T1>();
            var component2 = entity.Get<T2>();
            var component3 = entity.Get<T3>();
            var component4 = entity.Get<T4>();
            return _condition(component1, component2, component3, component4);
        }
    }

    internal class ConditionalEventWithEntity : ConditionalEventBase<Predicate<EcsEntity>>
    {
        public ConditionalEventWithEntity(
            EcsPresenter presenter,
            Type[] componentTypes,
            Predicate<EcsEntity> condition,
            Action<EcsEntity> added,
            Action<EcsEntity> removed)
            : base(presenter, componentTypes, condition, added, removed)
        {
        }

        protected override bool CheckCondition(EcsEntity entity)
        {
            return _condition(entity);
        }
    }

    internal interface IComponentSubscription : IDisposable { }

    internal class ComponentSubscription<T> : IComponentSubscription where T : struct
    {
        private readonly EcsEventPool<T> _pool;
        private readonly ObjectEvent<T> _eventObject;

        public ComponentSubscription(EcsPresenter presenter, Action<EcsEntity> onChanged)
        {
            presenter.AddCheckEvent<T>();
            _pool = presenter.GetPool<T>() as EcsEventPool<T>
                ?? throw new InvalidOperationException($"Pool for {typeof(T)} is not an event pool");

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

    public static class EcsConditions
    {
        public static Func<T1, T2, bool> And<T1, T2>(Predicate<T1> condition1, Predicate<T2> condition2)
        {
            return (c1, c2) => condition1(c1) && condition2(c2);
        }

        public static Func<T1, T2, bool> Or<T1, T2>(Predicate<T1> condition1, Predicate<T2> condition2)
        {
            return (c1, c2) => condition1(c1) || condition2(c2);
        }

        public static Func<T1, T2, T3, bool> And<T1, T2, T3>(Predicate<T1> condition1, Predicate<T2> condition2, Predicate<T3> condition3)
        {
            return (c1, c2, c3) => condition1(c1) && condition2(c2) && condition3(c3);
        }

        public static Func<T1, T2, T3, T4, bool> And<T1, T2, T3, T4>(
            Predicate<T1> condition1, 
            Predicate<T2> condition2, 
            Predicate<T3> condition3, 
            Predicate<T4> condition4)
        {
            return (c1, c2, c3, c4) => condition1(c1) && condition2(c2) && condition3(c3) && condition4(c4);
        }

        public static Predicate<T> GreaterThan<T>(T value) where T : IComparable<T>
        {
            return component => component.CompareTo(value) > 0;
        }

        public static Predicate<T> LessThan<T>(T value) where T : IComparable<T>
        {
            return component => component.CompareTo(value) < 0;
        }

        public static Predicate<T> EqualTo<T>(T value) where T : IEquatable<T>
        {
            return component => component.Equals(value);
        }

        public static Predicate<T> NotNull<T>() where T : class
        {
            return component => component != null;
        }
    }
}

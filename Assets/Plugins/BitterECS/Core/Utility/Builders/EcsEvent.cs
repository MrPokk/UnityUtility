using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public readonly struct EcsEvent : IDisposable
    {
        private readonly Priority _priority;
        private readonly EcsPresenter _presenter;
        private readonly Dictionary<Guid, ConditionalEvent> _subscriptions;

        public int Count => _subscriptions?.Count ?? 0;

        public EcsEvent(EcsPresenter presenter, Priority priority)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _subscriptions = new Dictionary<Guid, ConditionalEvent>();
            _priority = priority;
        }

        public EcsEvent Subscribe<T>(Action<EcsEntity> added = null, Action<EcsEntity> removed = null) where T : struct
            => SubscribeWhere<T>(_ => true, added, removed);

        public EcsEvent SubscribeWhere<T>(Predicate<T> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null) where T : struct
        {
            Register(new[] { typeof(T) }, e => e.TryGet<T>(out var c) && condition(c), added, removed);
            return this;
        }

        public EcsEvent SubscribeWhere<T1, T2>(Func<T1, T2, bool> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : struct where T2 : struct
        {
            Register(new[] { typeof(T1), typeof(T2) }, e => e.TryGet<T1>(out var c1) && e.TryGet<T2>(out var c2) && condition(c1, c2), added, removed);
            return this;
        }

        public EcsEvent SubscribeWhere<T1, T2, T3>(Func<T1, T2, T3, bool> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : struct where T2 : struct where T3 : struct
        {
            Register(new[] { typeof(T1), typeof(T2), typeof(T3) }, e => e.TryGet<T1>(out var c1) && e.TryGet<T2>(out var c2) && e.TryGet<T3>(out var c3) && condition(c1, c2, c3), added, removed);
            return this;
        }

        public EcsEvent SubscribeWhere<T1, T2, T3, T4>(Func<T1, T2, T3, T4, bool> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        {
            Register(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, e => e.TryGet<T1>(out var c1) && e.TryGet<T2>(out var c2) && e.TryGet<T3>(out var c3) && e.TryGet<T4>(out var c4) && condition(c1, c2, c3, c4), added, removed);
            return this;
        }

        public EcsEvent SubscribeWhereEntity<T>(Predicate<EcsEntity> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null) where T : struct
        {
            Register(new[] { typeof(T) }, e => { try { return e.Has<T>() && condition(e); } catch { return false; } }, added, removed);
            return this;
        }

        public EcsEvent SubscribeWhereEntity<T1, T2>(Predicate<EcsEntity> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : struct where T2 : struct
        {
            Register(new[] { typeof(T1), typeof(T2) }, e => { try { return e.Has<T1>() && e.Has<T2>() && condition(e); } catch { return false; } }, added, removed);
            return this;
        }

        private void Register(Type[] types, Predicate<EcsEntity> check, Action<EcsEntity> added, Action<EcsEntity> removed)
        {
            _subscriptions[Guid.NewGuid()] = new ConditionalEvent(_priority, _presenter, types, check, added, removed);
        }

        public void Dispose()
        {
            if (_subscriptions == null) return;
            foreach (var sub in _subscriptions.Values) sub.Dispose();
            _subscriptions.Clear();
        }
    }

    public struct EcsEvent<T> : IDisposable where T : EcsPresenter, new()
    {
        private EcsEvent? _event;
        private readonly Priority _priority;

        public EcsEvent(Priority priority = Priority.Medium)
        {
            _priority = priority;
            _event = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            if (_event.HasValue)
            {
                return;
            }

            _event = new EcsEvent(EcsWorld.Get<T>(), _priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEvent Subscribe<TComponent>(Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where TComponent : struct
        {
            EnsureInitialized();
            return _event.Value.Subscribe<TComponent>(added, removed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEvent SubscribeWhere<TComponent>(Predicate<TComponent> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where TComponent : struct
        {
            EnsureInitialized();
            return _event.Value.SubscribeWhere(condition, added, removed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEvent SubscribeWhere<T1, T2>(Func<T1, T2, bool> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : struct where T2 : struct
        {
            EnsureInitialized();
            return _event.Value.SubscribeWhere(condition, added, removed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEvent SubscribeWhere<T1, T2, T3>(Func<T1, T2, T3, bool> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : struct where T2 : struct where T3 : struct
        {
            EnsureInitialized();
            return _event.Value.SubscribeWhere(condition, added, removed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEvent SubscribeWhere<T1, T2, T3, T4>(Func<T1, T2, T3, T4, bool> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        {
            EnsureInitialized();
            return _event.Value.SubscribeWhere(condition, added, removed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEvent SubscribeWhereEntity<TComponent>(Predicate<EcsEntity> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where TComponent : struct
        {
            EnsureInitialized();
            return _event.Value.SubscribeWhereEntity<TComponent>(condition, added, removed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEvent SubscribeWhereEntity<T1, T2>(Predicate<EcsEntity> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : struct where T2 : struct
        {
            EnsureInitialized();
            return _event.Value.SubscribeWhereEntity<T1, T2>(condition, added, removed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_event.HasValue)
            {
                _event.Value.Dispose();
            }
        }
    }

    internal sealed class ConditionalEvent : IDisposable
    {
        private readonly Action<EcsEntity> _added;
        private readonly Action<EcsEntity> _removed;
        private readonly Predicate<EcsEntity> _condition;
        private readonly HashSet<int> _activeEntities = new();
        private readonly IEcsEvent[] _subscriptions;

        public ConditionalEvent(Priority priority, EcsPresenter presenter, Type[] types, Predicate<EcsEntity> condition, Action<EcsEntity> added, Action<EcsEntity> removed)
        {
            _condition = condition;
            _added = added;
            _removed = removed;
            _subscriptions = new IEcsEvent[types.Length];

            for (int i = 0; i < types.Length; i++)
            {
                var subType = typeof(ComponentSubscription<>).MakeGenericType(types[i]);
                _subscriptions[i] = (IEcsEvent)Activator.CreateInstance(subType, priority, presenter, (Action<EcsEntity>)OnComponentChanged);
            }

            foreach (var entity in presenter.GetAliveEntities()) CheckEntity(entity);
        }

        private void OnComponentChanged(EcsEntity entity) => CheckEntity(entity);

        private void CheckEntity(EcsEntity entity)
        {
            if (entity == null) return;
            var id = entity.GetID();
            var satisfied = _condition(entity);
            var wasActive = _activeEntities.Contains(id);

            if (satisfied && !wasActive)
            {
                _activeEntities.Add(id);
                _added?.Invoke(entity);
            }
            else if (!satisfied && wasActive)
            {
                _activeEntities.Remove(id);
                _removed?.Invoke(entity);
            }
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _activeEntities.Clear();
        }
    }

    internal class ComponentSubscription<T> : IEcsEvent where T : struct
    {
        private readonly EcsEventPool<T> _pool;

        public Priority Priority { get; set; }
        public EcsPresenter Presenter { get; }
        public Action<EcsEntity> Added { get; }
        public Action<EcsEntity> Removed { get; }

        public ComponentSubscription(Priority priority, EcsPresenter presenter, Action<EcsEntity> onChanged)
        {
            Priority = priority;
            Presenter = presenter;
            Added = onChanged;
            Removed = onChanged;

            presenter.AddCheckEvent<T>();
            _pool = presenter.GetPool<T>() as EcsEventPool<T>
                ?? throw new InvalidOperationException($"Pool for {typeof(T)} is not an event pool");
            _pool.Subscribe(this);
        }

        public void Dispose() => _pool.Unsubscribe(this);

    }

    public static class EcsConditions
    {
        public static Predicate<T> GreaterThan<T>(T value) where T : IComparable<T> => c => c.CompareTo(value) > 0;
        public static Predicate<T> LessThan<T>(T value) where T : IComparable<T> => c => c.CompareTo(value) < 0;
        public static Predicate<T> EqualTo<T>(T value) where T : IEquatable<T> => c => c.Equals(value);
        public static Predicate<T> NotNull<T>() where T : class => c => c != null;

        public static bool HasProvider<T>(EcsEntity e) where T : ILinkableProvider => e.HasProvider<T>();

        public static bool Has<T1>(EcsEntity e) where T1 : struct => e.Has<T1>();
        public static bool Has<T1, T2>(EcsEntity e) where T1 : struct where T2 : struct => Has<T1>(e) && e.Has<T2>();
        public static bool Has<T1, T2, T3>(EcsEntity e) where T1 : struct where T2 : struct where T3 : struct => Has<T1, T2>(e) && e.Has<T3>();
        public static bool Has<T1, T2, T3, T4>(EcsEntity e) where T1 : struct where T2 : struct where T3 : struct where T4 : struct => Has<T1, T2, T3>(e) && e.Has<T4>();
        public static bool Has<T1, T2, T3, T4, T5>(EcsEntity e) where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct => Has<T1, T2, T3, T4>(e) && e.Has<T5>();

        public static bool Not<T1>(EcsEntity e) where T1 : struct => !Has<T1>(e);
        public static bool Not<T1, T2>(EcsEntity e) where T1 : struct where T2 : struct => !Has<T1, T2>(e);
        public static bool Not<T1, T2, T3>(EcsEntity e) where T1 : struct where T2 : struct where T3 : struct => !Has<T1, T2, T3>(e);
        public static bool Not<T1, T2, T3, T4>(EcsEntity e) where T1 : struct where T2 : struct where T3 : struct where T4 : struct => !Has<T1, T2, T3, T4>(e);
        public static bool Not<T1, T2, T3, T4, T5>(EcsEntity e) where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct => !Has<T1, T2, T3, T4, T5>(e);
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public struct EcsEvent : IDisposable
    {
        private readonly EcsWorld _world;
        public readonly EcsWorld World => _world ?? EcsWorldStatic.Instance;

        private readonly Priority _priority;
        private readonly Priority CurrentPriority => _priority == 0 ? Priority.Medium : _priority;

        private List<ConditionalEvent> _subscriptions;

        public readonly int Count => _subscriptions?.Count ?? 0;

        public EcsEvent(EcsWorld world = null, Priority priority = Priority.Medium)
        {
            _world = world;
            _subscriptions = new List<ConditionalEvent>();
            _priority = priority;
        }

        public EcsEvent Subscribe<T>(Action<EcsEntity> added = null, Action<EcsEntity> removed = null) where T : new()
            => SubscribeWhere<T>(_ => true, added, removed);

        public EcsEvent Subscribe<T1, T2>(Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : new() where T2 : new()
            => SubscribeWhere<T1, T2>((_, _) => true, added, removed);

        public EcsEvent Subscribe<T1, T2, T3>(Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : new() where T2 : new() where T3 : new()
            => SubscribeWhere<T1, T2, T3>((_, _, _) => true, added, removed);

        public EcsEvent Subscribe<T1, T2, T3, T4>(Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : new() where T2 : new() where T3 : new() where T4 : new()
            => SubscribeWhere<T1, T2, T3, T4>((_, _, _, _) => true, added, removed);

        public EcsEvent SubscribeAny<T1, T2>(Action<EcsEntity> added = null, Action<EcsEntity> removed = null) where T1 : new() where T2 : new()
        {
            Register(new[] { typeof(T1), typeof(T2) }, e => e.IsAlive && (e.Has<T1>() || e.Has<T2>()), added, removed);
            return this;
        }

        public EcsEvent SubscribeAny<T1, T2, T3>(Action<EcsEntity> added = null, Action<EcsEntity> removed = null) where T1 : new() where T2 : new() where T3 : new()
        {
            Register(new[] { typeof(T1), typeof(T2), typeof(T3) }, e => e.IsAlive && (e.Has<T1>() || e.Has<T2>() || e.Has<T3>()), added, removed);
            return this;
        }

        public EcsEvent SubscribeWhere<T>(Predicate<T> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null) where T : new()
        {
            Register(new[] { typeof(T) }, e => e.TryGet<T>(out var c) && condition(c), added, removed);
            return this;
        }

        public EcsEvent SubscribeWhere<T1, T2>(Func<T1, T2, bool> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : new() where T2 : new()
        {
            Register(new[] { typeof(T1), typeof(T2) }, e => e.TryGet<T1>(out var c1) && e.TryGet<T2>(out var c2) && condition(c1, c2), added, removed);
            return this;
        }

        public EcsEvent SubscribeWhere<T1, T2, T3>(Func<T1, T2, T3, bool> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : new() where T2 : new() where T3 : new()
        {
            Register(new[] { typeof(T1), typeof(T2), typeof(T3) }, e => e.TryGet<T1>(out var c1) && e.TryGet<T2>(out var c2) && e.TryGet<T3>(out var c3) && condition(c1, c2, c3), added, removed);
            return this;
        }

        public EcsEvent SubscribeWhere<T1, T2, T3, T4>(Func<T1, T2, T3, T4, bool> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : new() where T2 : new() where T3 : new() where T4 : new()
        {
            Register(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, e => e.TryGet<T1>(out var c1) && e.TryGet<T2>(out var c2) && e.TryGet<T3>(out var c3) && e.TryGet<T4>(out var c4) && condition(c1, c2, c3, c4), added, removed);
            return this;
        }

        public EcsEvent SubscribeWhereEntity<T>(Predicate<EcsEntity> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null) where T : new()
        {
            Register(new[] { typeof(T) }, e => e.IsAlive && e.Has<T>() && condition(e), added, removed);
            return this;
        }

        public EcsEvent SubscribeWhereEntity<T1, T2>(Predicate<EcsEntity> condition, Action<EcsEntity> added = null, Action<EcsEntity> removed = null)
            where T1 : new() where T2 : new()
        {
            Register(new[] { typeof(T1), typeof(T2) }, e => e.IsAlive && e.Has<T1>() && e.Has<T2>() && condition(e), added, removed);
            return this;
        }

        private void Register(Type[] types, Predicate<EcsEntity> check, Action<EcsEntity> added, Action<EcsEntity> removed)
        {
            _subscriptions ??= new List<ConditionalEvent>();
            _subscriptions.Add(new ConditionalEvent(CurrentPriority, World, types, check, added, removed));
        }

        public void Dispose()
        {
            if (_subscriptions == null) return;
            for (var i = 0; i < _subscriptions.Count; i++) _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }
    }

    internal sealed class ConditionalEvent : IDisposable
    {
        private readonly Action<EcsEntity> _added;
        private readonly Action<EcsEntity> _removed;
        private readonly Predicate<EcsEntity> _condition;
        private readonly HashSet<int> _activeEntities = new();
        private readonly IEcsEvent[] _subscriptions;
        private readonly Action<EcsEntity> _onChangedCache;

        public ConditionalEvent(Priority priority, EcsWorld world, Type[] types, Predicate<EcsEntity> condition, Action<EcsEntity> added, Action<EcsEntity> removed)
        {
            _condition = condition;
            _added = added;
            _removed = removed;
            _subscriptions = new IEcsEvent[types.Length];
            _onChangedCache = OnComponentChanged;

            for (var i = 0; i < types.Length; i++)
            {
                var subType = typeof(ComponentSubscription<>).MakeGenericType(types[i]);
                _subscriptions[i] = (IEcsEvent)Activator.CreateInstance(subType, priority, world, _onChangedCache);
            }

            foreach (var entityId in world.GetAliveIds()) CheckEntity(world.Get(entityId));
        }

        private void OnComponentChanged(EcsEntity entity) => CheckEntity(entity); [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckEntity(EcsEntity entity)
        {
            if (entity.World == null || !entity.World.Has(entity.Id)) return;

            var id = entity.Id;
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
            for (var i = 0; i < _subscriptions.Length; i++) _subscriptions[i].Dispose();
            _activeEntities.Clear();
        }
    }

    internal class ComponentSubscription<T> : IEcsEvent where T : new()
    {
        private readonly EcsEventPool<T> _pool;
        public Priority Priority { get; set; }
        public EcsWorld World { get; }
        public Action<EcsEntity> Added { get; }
        public Action<EcsEntity> Removed { get; }

        public ComponentSubscription(Priority priority, EcsWorld world, Action<EcsEntity> onChanged)
        {
            Priority = priority;
            World = world;
            Added = onChanged;
            Removed = onChanged;
            world.AddCheckEvent<T>();
            _pool = world.GetPool<T>() as EcsEventPool<T> ?? throw new InvalidOperationException($"Pool for {typeof(T)} is not an event pool");
            _pool.Subscribe(this);
        }

        public void Dispose() => _pool.Unsubscribe(this);
    }
}

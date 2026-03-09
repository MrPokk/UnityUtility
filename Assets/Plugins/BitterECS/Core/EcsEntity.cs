using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public readonly struct EcsEntity : IEquatable<EcsEntity>
    {
        public readonly int Id;
        public readonly EcsWorld World;
        public bool IsAlive => World != null && World.Has(Id);
        public bool IsProviding => IsAlive && World.HasProvider(Id);

        public EcsEntity(EcsWorld world, int id = -1)
        {
            Id = id;
            World = world;
        }

        public void AddFrame<T>(in T component = default) where T : new()
        { if (!IsAlive) return; Add(component); Remove<T>(); }

        public void AddFrame<T>(Action action, in T component = default) where T : new()
        { if (!IsAlive) return; Add(component); action(); Remove<T>(); }

        public void AddPredicate<T>(T value, Predicate<T> predicate) where T : new()
        { if (predicate(value)) Add(value); }

        public void AddPredicate<T, P>(in T value, P predicateValue, Predicate<P> predicate) where T : new()
        { if (predicate(predicateValue)) Add(value); }

        public void AddOrRemove<T, P>(in T value, P predicateValue, Predicate<P> predicate) where T : new()
        { if (predicate(predicateValue)) Add(value); else Remove<T>(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(in T component = default) where T : new()
        {
            if (Has<T>())
            {
                Get<T>() = component;
            }
            else
            {
                World.SetMaskBit(Id, EcsComponentTypeId<T>.Id);
                World.GetPool<T>().Add(Id, component);
            }
        }

        public void Set<T>(RefAction<T> modifier) where T : new() => modifier(ref Get<T>());
        public delegate void RefAction<T>(ref T component) where T : new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get<T>() where T : new() => ref World.GetPool<T>().Get(Id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet<T>(out T component) where T : new() =>
        Has<T>() ? (component = Get<T>()) is var _ : (component = default) is null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetOrAdd<T>() where T : new()
        { if (!Has<T>()) Add(new T()); return ref Get<T>(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has<T>() where T : new() => World != null && World.GetEntityMask(Id).Has(EcsComponentTypeId<T>.Id);

        public bool Has<T>(Predicate<T> predicate) where T : new() => Has<T>() && predicate(Get<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove<T>() where T : new()
        {
            if (Has<T>())
            {
                World.RemoveMaskBit(Id, EcsComponentTypeId<T>.Id);
                World.GetPool<T>().Remove(Id);
            }
        }

        public void Destroy() => World.Remove(this);
        public T GetProvider<T>() where T : class, ILinkableProvider => World.GetProvider(this) as T;

        public bool TryGetProvider<T>(out T provider) where T : class, ILinkableProvider
        { provider = GetProvider<T>(); return HasProvider<T>(); }

        public bool HasProvider<T>() where T : ILinkableProvider => World.GetProvider(this) is not null and T;

        public bool Equals(EcsEntity other) => Id == other.Id && World == other.World;
        public override bool Equals(object obj) => obj is EcsEntity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, World);
        public static bool operator ==(EcsEntity left, EcsEntity right) => left.Equals(right);
        public static bool operator !=(EcsEntity left, EcsEntity right) => !left.Equals(right);
    }
}

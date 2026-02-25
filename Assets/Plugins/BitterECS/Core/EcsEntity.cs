using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public readonly struct EcsEntity : IEquatable<EcsEntity>
    {
        public readonly int Id;
        public readonly EcsPresenter Presenter;
        public bool IsNull => Presenter == null;

        public EcsEntity(int id, EcsPresenter presenter)
        {
            Id = id;
            Presenter = presenter;
        }

        public bool IsAlive() => Presenter != null && Presenter.Has(Id);

        public void AddFrame<T>(in T component) where T : new() { Add(component); Remove<T>(); }
        public void AddFrame<T>(in T component, Action action) where T : new() { Add(component); action(); Remove<T>(); }
        public void AddPredicate<T>(T value, Predicate<T> predicate) where T : new() { if (predicate(value)) Add(value); }
        public void AddPredicate<T, P>(T value, P predicateValue, Predicate<P> predicate) where T : new() { if (predicate(predicateValue)) Add(value); }
        public void AddOrRemove<T, P>(T value, P predicateValue, Predicate<P> predicate) where T : new() { if (predicate(predicateValue)) Add(value); else Remove<T>(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(in T component = default) where T : new() { if (Has<T>()) Get<T>() = component; else { Presenter.GetPool<T>().Add(Id, component); Presenter.IncrementCount(Id); } }
        public void Set<T>(RefAction<T> modifier) where T : new()
             => modifier(ref Get<T>());
        public delegate void RefAction<T>(ref T component) where T : new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get<T>() where T : new() => ref Presenter.GetPool<T>().Get(Id);
        public bool TryGet<T>(out T component) where T : new() => Has<T>() ? (component = Get<T>()) is var _ : (component = default) is null;
        public ref T GetOrAdd<T>() where T : new() { if (!Has<T>()) Add(new T()); return ref Get<T>(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has<T>() where T : new() => !IsNull && Presenter.GetPool<T>().Has(Id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove<T>() where T : new() { if (Has<T>()) { Presenter.GetPool<T>().Remove(Id); Presenter.DecrementCount(Id); } }

        public void Destroy() => Presenter.Remove(this);

        public int GetID() => Id;
        public EcsPresenter GetPresenter() => Presenter;
        public int GetCountComponents() => Presenter.GetComponentCount(Id);

        public T GetProvider<T>() where T : class, ILinkableProvider
            => Presenter.GetProvider(this) as T;

        public bool HasProvider<T>() where T : ILinkableProvider
            => Presenter.GetProvider(this) is not null and T;

        public bool Has<T>(Predicate<T> predicate) where T : new()
            => Has<T>() && predicate(Get<T>());

        public bool TryGetProvider<T>(out T provider) where T : class, ILinkableProvider
            => (provider = GetProvider<T>()) != null;

        public bool Equals(EcsEntity other) => Id == other.Id && Presenter == other.Presenter;
        public override bool Equals(object obj) => obj is EcsEntity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Presenter);

        public static bool operator ==(EcsEntity left, EcsEntity right) => left.Equals(right);
        public static bool operator !=(EcsEntity left, EcsEntity right) => !left.Equals(right);
    }
}

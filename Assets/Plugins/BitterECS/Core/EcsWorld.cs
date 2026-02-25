using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public readonly struct RefWorldVersion : IEquatable<RefWorldVersion>
    {
        private readonly int _version;
        public int Version => _version;
        internal RefWorldVersion(int version = -1) => _version = version;
        public RefWorldVersion Increment() => new(_version + 1);
        public bool Equals(RefWorldVersion other) => _version == other._version;
        public override bool Equals(object obj) => obj is RefWorldVersion other && Equals(other);
        public override int GetHashCode() => _version;
        public static bool operator ==(RefWorldVersion left, RefWorldVersion right) => left.Equals(right);
        public static bool operator !=(RefWorldVersion left, RefWorldVersion right) => !left.Equals(right);
    }

    public sealed class EcsWorld : IDisposable
    {
        private static EcsWorld s_instance;
        public static EcsWorld Instance => s_instance ??= new EcsWorld();

        private readonly Dictionary<Type, EcsPresenter> _ecsPresenters = new(EcsDefinitions.InitialPresentersCapacity);
        private RefWorldVersion _world = new(0);

        private EcsWorld() { }

        internal void RegisterPresenter(Type type, EcsPresenter presenter) => _ecsPresenters[type] = presenter;

        internal T GetInternal<T>() where T : EcsPresenter, new()
        {
            if (_ecsPresenters.TryGetValue(typeof(T), out var value)) return (T)value;
            var inst = new T();
            _ecsPresenters[typeof(T)] = inst;
            return inst;
        }

        internal EcsPresenter GetInternal(Type type) => _ecsPresenters.TryGetValue(type, out var value) ? value : null;

        public RefWorldVersion GetWorld() => _world;
        public RefWorldVersion IncreaseWorldVersion() => _world = _world.Increment();

        public void Dispose()
        {
            foreach (var presenter in _ecsPresenters.Values) presenter.Dispose();
            _ecsPresenters.Clear();
            s_instance = null;
        }

        public static void Clear() => Instance.Dispose();
        public static EcsPresenter Get(Type type) => Instance.GetInternal(type);
        public static T Get<T>() where T : EcsPresenter, new() => Instance.GetInternal<T>();
        public static RefWorldVersion GetRefWorld() => Instance.GetWorld();
        public static RefWorldVersion IncreaseVersion() => Instance.IncreaseWorldVersion();
    }
}

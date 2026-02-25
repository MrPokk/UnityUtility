using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public readonly struct RefWorldVersion
    {
        private readonly int _version;

        public int Version => _version;

        internal RefWorldVersion(int version = -1)
        {
            _version = version;
        }

        public RefWorldVersion Increment() => new(_version + 1);

        public bool Equals(RefWorldVersion other) => _version == other._version;
        public override bool Equals(object obj) => obj is RefWorldVersion other && Equals(other);
        public override int GetHashCode() => _version;
        public override string ToString() => $"World(v{_version})";

        public static bool operator ==(RefWorldVersion left, RefWorldVersion right) => left.Equals(right);

        public static bool operator !=(RefWorldVersion left, RefWorldVersion right) => !left.Equals(right);

        public static RefWorldVersion operator ++(RefWorldVersion world) => world.Increment();
    }

    public sealed class EcsWorld : IDisposable
    {
        private const int Version = 0;
        private static EcsWorld s_instance;
        public static EcsWorld Instance => s_instance ??= new EcsWorld();
        private readonly Dictionary<Type, EcsPresenter> _ecsPresenters = new(EcsConfig.InitialPresentersCapacity);

        private RefWorldVersion _world = new(Version);

        private EcsWorld() => LoadAllPresenters();

        private void LoadAllPresenters()
        {
            _ecsPresenters.Clear();

            var presenterTypes = ReflectionUtility.FindAllImplement<EcsPresenter>();
            foreach (var type in presenterTypes)
            {
                if (Activator.CreateInstance(type) is EcsPresenter presenter)
                {
                    _ecsPresenters.TryAdd(type, presenter);
                }
            }
        }

        internal EcsPresenter GetInternal(Type type)
        {
            if (_ecsPresenters.TryGetValue(type, out var value))
            {
                return value;
            }

            throw new Exception($"Presenter not found");
        }

        internal T GetInternal<T>() where T : EcsPresenter, new()
        {
            if (_ecsPresenters.TryGetValue(typeof(T), out var value))
            {
                return (T)value;
            }

            throw new Exception($"Presenter not found: {typeof(T)} count: {_ecsPresenters.Count}");
        }

        internal EcsPresenter GetToEntityTypeInternal(Type type)
        {
            foreach (var presenter in _ecsPresenters.Values)
            {
                return presenter;
            }

            throw new Exception($"No presenter found that can handle type: {type} count: {_ecsPresenters.Count}");
        }

        internal ICollection<EcsPresenter> GetAllInternal() => _ecsPresenters.Values;

        public RefWorldVersion GetWorld() => _world;
        public RefWorldVersion IncreaseWorldVersion() => _world = _world.Increment();

        public void Dispose()
        {
            foreach (var presenter in _ecsPresenters.Values)
            {
                presenter.Dispose();
            }

            _ecsPresenters.Clear();
            s_instance = null;
        }

        public static void Clear() => Instance.Dispose();
        public static EcsPresenter Get(Type type) => Instance.GetInternal(type);
        public static T Get<T>() where T : EcsPresenter, new() => Instance.GetInternal<T>();
        public static EcsPresenter GetToEntityType(Type type) => Instance.GetToEntityTypeInternal(type);
        public static ICollection<EcsPresenter> GetAll() => Instance.GetAllInternal();
        public static RefWorldVersion GetRefWorld() => Instance.GetWorld();
        public static RefWorldVersion IncreaseVersion() => Instance.IncreaseWorldVersion();
    }
}

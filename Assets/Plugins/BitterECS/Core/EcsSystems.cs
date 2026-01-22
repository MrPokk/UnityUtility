using System;
using System.Collections.Generic;

namespace BitterECS.Core
{

    public sealed class EcsSystems : IDisposable
    {
        private static EcsSystems s_instance;
        public static EcsSystems Instance => s_instance ??= new EcsSystems();

        private readonly SortedSet<IEcsSystem> _systems = new(PriorityUtility.Sort());
        private readonly Dictionary<Type, IEcsSystem[]> _cachedInstanceSystems = new(EcsConfig.InitialSystemsCapacity);

        private EcsSystems() => LoadAllSystems();

        private void LoadAllSystems()
        {
            _systems.Clear();
            _cachedInstanceSystems.Clear();

            var systemTypes = ReflectionUtility.FindAllAssignments<IEcsAutoImplement>();
            foreach (var type in systemTypes)
            {
#if UNITY_2020_1_OR_NEWER
                if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                {
                    continue;
                }
#endif
                if (Activator.CreateInstance(type) is IEcsAutoImplement system)
                {
                    AddToSystemInternal(system);
                }
            }
        }

        internal void AddSystemInternal(IEcsSystem system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            AddToSystemInternal(system);
            _cachedInstanceSystems.Clear();
        }

        internal void AddSystemsInternal(params IEcsSystem[] systems)
        {
            if (systems == null)
            {
                throw new ArgumentNullException(nameof(systems));
            }

            foreach (var system in systems)
            {
                AddToSystemInternal(system);
            }

            _cachedInstanceSystems.Clear();
        }

        private void AddToSystemInternal(IEcsSystem system)
        {
            if (_systems.Contains(system))
            {
                return;
            }

            _systems.Add(system);
        }

        internal void RunInternal<T>(Action<T> action) where T : class, IEcsSystem
        {
            var systems = GetSystemsInternal<T>();
            foreach (var system in systems)
            {
                action(system);
            }
        }

        internal IReadOnlyCollection<T> GetSystemsInternal<T>() where T : class, IEcsSystem
        {
            var type = typeof(T);

            if (_cachedInstanceSystems.TryGetValue(type, out var cached))
            {
                return (T[])cached;
            }

            var result = new List<T>();
            foreach (var system in _systems)
            {
                if (system is T typedSystem)
                {
                    result.Add(typedSystem);
                }
            }

            var cachedResult = result.ToArray();
            _cachedInstanceSystems[type] = cachedResult;

            return cachedResult;
        }

        public void Dispose()
        {
            foreach (var system in _systems)
            {
                if (system is IDisposable disposableSystem)
                {
                    disposableSystem.Dispose();
                }
            }

            _systems.Clear();
            _cachedInstanceSystems.Clear();
            s_instance = null;
        }

        public static void Clear() => Instance.Dispose();
        public static void AddSystem(IEcsSystem system) => Instance.AddSystemInternal(system);
        public static void AddSystems(params IEcsSystem[] systems) => Instance.AddSystemsInternal(systems);
        public static void Run<T>(Action<T> action) where T : class, IEcsSystem => Instance.RunInternal(action);
        public static IReadOnlyCollection<T> GetSystems<T>() where T : class, IEcsSystem => Instance.GetSystemsInternal<T>();
    }
}

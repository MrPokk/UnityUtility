using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public sealed class EcsSystems : IDisposable
    {
        private static EcsSystems s_instance;
        public static EcsSystems Instance => s_instance ??= new EcsSystems();

        private readonly List<IEcsSystem> _allSystems = new(EcsDefinitions.InitialSystemsCapacity);
        private readonly Dictionary<Type, IEcsSystem> _systemsByType = new(EcsDefinitions.InitialSystemsCapacity);
        private readonly Dictionary<Type, IEcsSystem[]> _cachedInstanceSystems = new(EcsDefinitions.InitialSystemsCapacity);

        private EcsSystems() => LoadAllSystems();

        private void LoadAllSystems()
        {
            _allSystems.Clear();
            _systemsByType.Clear();
            _cachedInstanceSystems.Clear();

            var systemTypes = ReflectionUtility.FindAllAssignments<IEcsAutoImplement>();
            foreach (var type in systemTypes)
            {
                if (Activator.CreateInstance(type) is IEcsAutoImplement system)
                {
                    AddToSystemInternal(system);
                }
            }

            _allSystems.Sort(PriorityUtility.Sort());
        }

        internal void AddSystemInternal(IEcsSystem system)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));

            AddToSystemInternal(system);
            _allSystems.Sort(PriorityUtility.Sort());
            _cachedInstanceSystems.Clear();
        }

        internal void AddSystemsInternal(params IEcsSystem[] systems)
        {
            if (systems == null) throw new ArgumentNullException(nameof(systems));

            foreach (var system in systems) AddToSystemInternal(system);

            _allSystems.Sort(PriorityUtility.Sort());
            _cachedInstanceSystems.Clear();
        }

        private void AddToSystemInternal(IEcsSystem system)
        {
            var type = system.GetType();

            if (_systemsByType.TryGetValue(type, out var oldSystem))
            {
                _allSystems.Remove(oldSystem);
            }

            _systemsByType[type] = system;
            _allSystems.Add(system);
        }

        internal void RunInternal<T>(Action<T> action) where T : class, IEcsSystem
        {
            var systems = GetSystemsInternal<T>();
            for (var i = 0; i < systems.Length; i++)
            {
                action((T)systems[i]);
            }
        }

        internal T[] GetSystemsInternal<T>() where T : class, IEcsSystem
        {
            var type = typeof(T);

            if (_cachedInstanceSystems.TryGetValue(type, out var cached))
                return (T[])cached;

            var filteredList = new List<T>();
            for (var i = 0; i < _allSystems.Count; i++)
            {
                if (_allSystems[i] is T typedSystem)
                {
                    filteredList.Add(typedSystem);
                }
            }

            var result = filteredList.ToArray();
            _cachedInstanceSystems[type] = (IEcsSystem[])(object)result;
            return result;
        }

        public void Dispose()
        {
            for (var i = 0; i < _allSystems.Count; i++)
            {
                if (_allSystems[i] is IDisposable disposableSystem)
                    disposableSystem.Dispose();
            }

            _allSystems.Clear();
            _systemsByType.Clear();
            _cachedInstanceSystems.Clear();
            s_instance = null;
        }

        public static void Clear() => Instance.Dispose();
        public static void AddSystem(IEcsSystem system) => Instance.AddSystemInternal(system);
        public static void AddSystems(params IEcsSystem[] systems) => Instance.AddSystemsInternal(systems);
        public static void Run<T>(Action<T> action) where T : class, IEcsSystem => Instance.RunInternal(action);
        public static T[] GetSystems<T>() where T : class, IEcsSystem => Instance.GetSystemsInternal<T>();
    }
}

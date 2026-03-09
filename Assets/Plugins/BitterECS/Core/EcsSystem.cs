using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public static class EcsSystemStatic
    {
        private static EcsSystem s_instance;
        public static EcsSystem Instance => s_instance ??= new EcsSystem();

        public static void Load() => Instance.Load();
        public static void AddSystem(IEcsSystem system) => Instance.AddSystem(system);
        public static void AddSystems(params IEcsSystem[] systems) => Instance.AddSystems(systems);
        public static void AddSystem<T>(T system) where T : class, IEcsSystem => Instance.AddSystem(system);
        public static void Run<T>(Action<T> action) where T : class, IEcsSystem => Instance.Run(action);
        public static T[] GetSystems<T>() where T : class, IEcsSystem => Instance.GetSystems<T>();
        public static void Dispose()
        {
            s_instance?.Dispose();
            s_instance = null;
        }
    }

    public sealed class EcsSystem : IDisposable
    {
        private readonly List<IEcsSystem> _allSystems;
        private readonly Dictionary<Type, IEcsSystem> _systemsByType;
        private readonly Dictionary<Type, IEcsSystem[]> _cachedInstanceSystems;

        public EcsSystem()
        {
            _allSystems = new(EcsDefinitions.InitialSystemsCapacity);
            _systemsByType = new(EcsDefinitions.InitialSystemsCapacity);
            _cachedInstanceSystems = new(EcsDefinitions.InitialSystemsCapacity);
        }

        public void Load()
        {
            _allSystems.Clear();
            _systemsByType.Clear();
            _cachedInstanceSystems.Clear();

            var systemTypes = ReflectionUtility.FindAllAssignments<IEcsAutoImplement>();
            foreach (var type in systemTypes)
            {
                if (Activator.CreateInstance(type) is IEcsAutoImplement system)
                {
                    AddToSystem(system);
                }
            }

            _allSystems.Sort(PriorityUtility.Sort());
        }

        public void AddSystem(IEcsSystem system)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));

            AddToSystem(system);
            _allSystems.Sort(PriorityUtility.Sort());
            _cachedInstanceSystems.Clear();
        }

        public void AddSystems(params IEcsSystem[] systems)
        {
            if (systems == null) throw new ArgumentNullException(nameof(systems));

            foreach (var system in systems) AddToSystem(system);

            _allSystems.Sort(PriorityUtility.Sort());
            _cachedInstanceSystems.Clear();
        }

        private void AddToSystem(IEcsSystem system)
        {
            var type = system.GetType();

            if (_systemsByType.TryGetValue(type, out var oldSystem))
            {
                _allSystems.Remove(oldSystem);
            }

            _systemsByType[type] = system;
            _allSystems.Add(system);
        }

        public void Run<T>(Action<T> action) where T : class, IEcsSystem
        {
            var systems = GetSystems<T>();
            for (var i = 0; i < systems.Length; i++)
            {
                action(systems[i]);
            }
        }

        public T[] GetSystems<T>() where T : class, IEcsSystem
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
        }
    }
}

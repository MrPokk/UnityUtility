using System;
using System.Collections.Generic;
using BitterECS.Utility;

namespace BitterECS.Core
{
    public sealed class EcsSystems : IDisposable
    {
        private static readonly List<IEcsSystem> s_systems = new(EcsConfig.InitialSystemsCapacity);
        private static readonly Dictionary<Type, IEcsSystem[]> s_cachedInstanceSystems = new(EcsConfig.InitialSystemsCapacity);

        public EcsSystems() => LoadAllSystems();

        private void LoadAllSystems()
        {
            s_systems.Clear();

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
                    s_systems.Add(system);
                }
            }

            s_systems.Sort((left, right) => (int)left.PrioritySystem - (int)right.PrioritySystem);
            s_cachedInstanceSystems.Clear();
        }

        public static void AddSystem(IEcsSystem system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            s_systems.Add(system);
            s_systems.Sort((left, right) => (int)left.PrioritySystem - (int)right.PrioritySystem);
        }

        public static void AddSystems(params IEcsSystem[] systems)
        {
            if (systems == null)
                throw new ArgumentNullException(nameof(systems));

            s_systems.AddRange(systems);
            s_systems.Sort((left, right) => (int)left.PrioritySystem - (int)right.PrioritySystem);
        }

        public static void Run<T>(Action<T> action) where T : class, IEcsSystem
        {
            var systems = GetSystems<T>();
            foreach (var system in systems)
            {
                action(system);
            }
        }

        public static IReadOnlyCollection<T> GetSystems<T>() where T : class, IEcsSystem
        {
            var type = typeof(T);

            if (s_cachedInstanceSystems.TryGetValue(type, out var cached))
            {
                return (T[])cached;
            }

            var result = new Stack<T>();
            foreach (var system in s_systems)
            {
                if (system is T typedSystem)
                {
                    result.Push(typedSystem);
                }
            }

            var cachedResult = result.ToArray();
            s_cachedInstanceSystems[type] = cachedResult;

            return cachedResult;
        }

        public void Dispose()
        {
            foreach (var system in s_systems)
            {
                if (system is IDisposable disposableSystem)
                {
                    disposableSystem.Dispose();
                }
            }

            s_systems.Clear();
            s_cachedInstanceSystems.Clear();
            GC.SuppressFinalize(this);
        }
    }
}

using System;
using System.Collections.Generic;
using BitterECS.Utility;

namespace BitterECS.Core
{
    public sealed class EcsSystems : IInitialize, IDisposable
    {
        private readonly List<IEcsSystem> _systems;
        private static readonly Dictionary<Type, IEcsSystem[]> s_cachedInstanceSystems = new();


        public EcsSystems(int maxSystems = 64)
        {
            _systems = new(maxSystems);
        }

        public void Init()
        {
            LoadAllSystems();
        }

        public void Run<T>(Action<T> action) where T : class, IEcsSystem
        {
            if (action == null)
            {
                return;
            }

            var systems = GetSystems<T>();
            foreach (var system in systems)
            {
                action(system);
            }
        }

        public IReadOnlyCollection<T> GetSystems<T>() where T : class, IEcsSystem
        {
            var type = typeof(T);

            if (s_cachedInstanceSystems.TryGetValue(type, out var cached))
            {
                return (T[])cached;
            }

            var result = new List<T>(_systems.Count);

            foreach (var system in _systems)
            {
                if (system is T typedSystem)
                {
                    result.Add(typedSystem);
                }
            }

            var cachedResult = result.ToArray();
            s_cachedInstanceSystems[type] = cachedResult;

            return cachedResult;
        }


        private void LoadAllSystems()
        {
            var systemTypes = ReflectionUtility.FindAllAssignments<IEcsSystem>();
            foreach (var type in systemTypes)
            {
#if UNITY_2020_1_OR_NEWER
                if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                {
                    continue;
                }
#endif
                if (Activator.CreateInstance(type) is IEcsSystem system)
                {
                    _systems.Add(system);
                }
            }

            _systems.Sort((left, right) => (int)left.PrioritySystem - (int)right.PrioritySystem);

            s_cachedInstanceSystems.Clear();
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
            s_cachedInstanceSystems.Clear();
            GC.SuppressFinalize(this);
        }
    }
}

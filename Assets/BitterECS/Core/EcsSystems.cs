using System;
using System.Collections.Generic;
using System.Linq;
using BitterECS.Utility;

namespace BitterECS.Core
{
    public sealed class EcsSystems : IInitialize, IDisposable
    {
        private readonly List<IEcsSystem> _systems;
        private static readonly Dictionary<Type, List<IEcsSystem>> s_cachedInstanceSystems = new();


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
            var systems = GetSystems<T>();
            foreach (var system in systems)
            {
                action?.Invoke(system);
            }
        }

        public IReadOnlyCollection<T> GetSystems<T>() where T : class, IEcsSystem
        {
            var type = typeof(T);

            if (s_cachedInstanceSystems.TryGetValue(type, out var cached))
            {
                return cached.Cast<T>().ToList().AsReadOnly();
            }

            var result = new List<T>();

            foreach (var interaction in _systems)
            {
                if (interaction is T typedInteraction)
                {
                    result.Add(typedInteraction);
                }
            }

            _systems.Sort((left, right) => (int)left.PrioritySystem - (int)right.PrioritySystem);

            var cachedResult = result.Cast<IEcsSystem>().ToList();
            s_cachedInstanceSystems[type] = cachedResult;

            return result.AsReadOnly();
        }


        private void LoadAllSystems()
        {
            var systemTypes = ReflectionUtility.FindAllAssignments<IEcsSystem>();
            foreach (var type in systemTypes)
            {
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
            _systems.Clear();
            s_cachedInstanceSystems.Clear();
        }
    }
}

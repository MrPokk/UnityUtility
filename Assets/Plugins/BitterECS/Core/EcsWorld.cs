using System;
using System.Collections.Generic;
using BitterECS.Utility;

namespace BitterECS.Core
{
    public sealed class EcsWorld : IInitialize, IDisposable
    {
        private readonly static Dictionary<Type, EcsPresenter> s_ecsPresenters;

        static EcsWorld()
        {
            s_ecsPresenters = new Dictionary<Type, EcsPresenter>();
        }

        public void Init()
        {
            LoadAllPresenters();
        }

        public static T Get<T>() where T : EcsPresenter
        {
            if (s_ecsPresenters.TryGetValue(typeof(T), out var value))
            {
                return value as T;
            }

            throw new Exception("Presenter not found");
        }

        public static EcsPresenter GetToEntityType(Type entityType)
        {
            foreach (var presenter in s_ecsPresenters.Values)
            {
                if (presenter.IsTypeAllowed(entityType))
                {
                    return presenter;
                }
            }
            throw new Exception($"No presenter found that can handle type {entityType.Name}");
        }

        private static void LoadAllPresenters()
        {
            var presenterTypes = ReflectionUtility.FindAllImplement<EcsPresenter>();
            foreach (var type in presenterTypes)
            {
                if (Activator.CreateInstance(type) is EcsPresenter presenter)
                {
                    s_ecsPresenters.Add(type, presenter);
                }
            }
        }

        public void Dispose()
        {
            foreach (var presenter in s_ecsPresenters.Values)
            {
                presenter.Dispose();
            }
            s_ecsPresenters.Clear();
            GC.SuppressFinalize(this);
        }
    }
}

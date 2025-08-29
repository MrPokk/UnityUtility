using System;
using System.Collections.Generic;
using BitterECS.Utility;

namespace BitterECS.Core
{
    public sealed class EcsWorld : IDisposable
    {
        private readonly static Dictionary<Type, EcsPresenter> s_ecsPresenters = new(EcsConfig.InitialPresentersCapacity);

        public EcsWorld() => LoadAllPresenters();

        private static void LoadAllPresenters()
        {
            var presenterTypes = ReflectionUtility.FindAllImplement<EcsPresenter>();
            foreach (var type in presenterTypes)
            {
                if (Activator.CreateInstance(type) is EcsPresenter presenter)
                {
                    s_ecsPresenters.TryAdd(type, presenter);
                }
            }
        }

        public static EcsPresenter Get(Type type)
        {
            if (s_ecsPresenters.TryGetValue(type, out var value))
            {
                return value;
            }

            throw new Exception($"Presenter not found");
        }

        public static T Get<T>() where T : EcsPresenter, new()
        {
            if (s_ecsPresenters.TryGetValue(typeof(T), out var value))
            {
                return (T)value;
            }

            throw new Exception($"Presenter not found: {typeof(T)} count: {s_ecsPresenters.Count}");
        }

        public static EcsPresenter GetToEntityType(Type type)
        {
            foreach (var presenter in s_ecsPresenters.Values)
            {
                if (presenter.IsTypeAllowed(type))
                {
                    return presenter;
                }
            }

            throw new Exception($"No presenter found that can handle type: {type} count: {s_ecsPresenters.Count}");
        }

        public static EcsPresenter GetToEntityType<T>() where T : EcsEntity
        {
            foreach (var presenter in s_ecsPresenters.Values)
            {
                if (presenter.IsTypeAllowed<T>())
                {
                    return presenter;
                }
            }

            throw new Exception($"No presenter found that can handle type: {typeof(T)} count: {s_ecsPresenters.Count}");
        }

        public void Dispose()
        {
            foreach (var presenter in s_ecsPresenters.Values) presenter.Dispose();

            s_ecsPresenters.Clear();
            GC.SuppressFinalize(this);
        }
    }
}

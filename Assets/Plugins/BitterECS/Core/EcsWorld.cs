using System;
using System.Collections.Generic;
using BitterECS.Utility;

namespace BitterECS.Core
{
    public sealed class EcsWorld : IDisposable
    {
        private static EcsWorld s_instance;
        public static EcsWorld Instance => s_instance ??= new EcsWorld();
        private readonly Dictionary<Type, EcsPresenter> _ecsPresenters = new(EcsConfig.InitialPresentersCapacity);

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

        public EcsPresenter GetInternal(Type type)
        {
            if (_ecsPresenters.TryGetValue(type, out var value))
            {
                return value;
            }

            throw new Exception($"Presenter not found");
        }

        public T GetInternal<T>() where T : EcsPresenter, new()
        {
            if (_ecsPresenters.TryGetValue(typeof(T), out var value))
            {
                return (T)value;
            }

            throw new Exception($"Presenter not found: {typeof(T)} count: {_ecsPresenters.Count}");
        }

        public EcsPresenter GetToEntityTypeInternal(Type type)
        {
            foreach (var presenter in _ecsPresenters.Values)
            {
                if (presenter.IsTypeAllowed(type))
                {
                    return presenter;
                }
            }

            throw new Exception($"No presenter found that can handle type: {type} count: {_ecsPresenters.Count}");
        }

        public EcsPresenter GetToEntityTypeInternal<T>() where T : EcsEntity
        {
            foreach (var presenter in _ecsPresenters.Values)
            {
                if (presenter.IsTypeAllowed<T>())
                {
                    return presenter;
                }
            }

            throw new Exception($"No presenter found that can handle type: {typeof(T)} count: {_ecsPresenters.Count}");
        }

        public ICollection<EcsPresenter> GetAllInternal()
        {
            return _ecsPresenters.Values;
        }

        public void Dispose()
        {
            foreach (var presenter in _ecsPresenters.Values)
            {
                presenter.Dispose();
            }

            _ecsPresenters.Clear();
            GC.SuppressFinalize(this);
            s_instance = null;
        }

        public static EcsPresenter Get(Type type) => Instance.GetInternal(type);
        public static T Get<T>() where T : EcsPresenter, new() => Instance.GetInternal<T>();
        public static EcsPresenter GetToEntityType(Type type) => Instance.GetToEntityTypeInternal(type);
        public static EcsPresenter GetToEntityType<T>() where T : EcsEntity => Instance.GetToEntityTypeInternal<T>();
        public static ICollection<EcsPresenter> GetAll() => Instance.GetAllInternal();
    }
}

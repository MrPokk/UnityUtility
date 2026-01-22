using System;

namespace BitterECS.Core
{
    public class EcsEntity : IInitialize<EcsProperty>, IDisposable
    {
        private EcsProperty _properties;
        public EcsProperty Properties => _properties;

        internal void Init(EcsProperty property) => _properties = property;
        protected internal virtual void Registration() { }

        public void Add<T>(in T component) where T : struct
        {
            _properties.Presenter.GetPool<T>().Add(_properties.Id, component);
            _properties.CountComponents++;
        }

        public void AddOrReplace<T>(in T component) where T : struct
        {
            if (!Has<T>())
                Add(component);
            else
                Get<T>() = component;
        }

        public void AddPredicate<T>(T value, Predicate<T> predicate) where T : struct
        {
            if (!predicate(value)) return;
            AddOrReplace(value);
        }

        public void AddPredicate<T, P>(T value, P predicateValue, Predicate<P> predicate) where T : struct
        {
            if (!predicate(predicateValue)) return;
            AddOrReplace(value);
        }

        public void AddOrRemove<T, P>(T value, P predicateValue, Predicate<P> predicate) where T : struct
        {
            if (!predicate(predicateValue))
            {
                Remove<T>();
                return;
            }

            AddOrReplace(value);
        }

        public int GetID() => _properties.Id;

        public ref T Get<T>() where T : struct
            => ref _properties.Presenter.GetPool<T>().Get(GetID());

        public bool TryGet<T>(out T component) where T : struct
        {
            if (!Has<T>())
            {
                component = default;
                return false;
            }

            component = Get<T>();
            return true;
        }

        public int GetCountComponents()
            => _properties.CountComponents;

        public EcsPresenter GetPresenter()
            => _properties.Presenter;

        public T GetProvider<T>() where T : class, ILinkableProvider
            => (T)_properties.Presenter.GetProvider(this);

        public bool TryGetProvider<T>(out T provider) where T : class, ILinkableProvider
           => (provider = GetProvider<T>()) is not null and T;

        public void Set<T>(RefAction<T> modifier) where T : struct
            => modifier(ref Get<T>());

        public void Remove<T>() where T : struct
        {
            _properties.Presenter.GetPool<T>().Remove(GetID());
            _properties.CountComponents--;
        }

        public bool Has<T>() where T : struct
            => _properties.Presenter.GetPool<T>().Has(GetID());

        public bool Has<T>(Predicate<T> predicate) where T : struct
            => Has<T>() && predicate(Get<T>());

        public bool HasProvider<T>()
            => _properties.Presenter.GetProvider(this) is not null and T;

        public void Dispose() => _properties.Presenter.Remove(this);

        void IInitialize<EcsProperty>.Init(EcsProperty property)
        {
            Init(property);
        }

        public delegate void RefAction<T>(ref T component) where T : struct;
    }
}

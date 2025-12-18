using System;

namespace BitterECS.Core
{
    public class EcsEntity : IInitialize<EcsProperty>, IDisposable
    {
        public EcsProperty Properties { get; private set; }
        public ILinkableProvider Provider => Properties?.Presenter?.GetProvider(this);
        void IInitialize<EcsProperty>.Init(EcsProperty property) => Init(property);

        internal void Init(EcsProperty property) => Properties = property;

        protected internal virtual void Registration() { }

        public void Add<T>(in T component) where T : struct
        {
            Properties.Presenter.GetPool<T>().Add(Properties.Id, component);
            Properties.CountComponents++;
        }

        public ref T Get<T>() where T : struct => ref Properties.Presenter.GetPool<T>().Get(Properties.Id);
        public int GetCountComponents() => Properties.CountComponents;

        public delegate void RefAction<T>(ref T obj) where T : struct;
        public void Set<T>(RefAction<T> modifier) where T : struct => modifier(ref Get<T>());

        public void Remove<T>() where T : struct
        {
            Properties.Presenter.GetPool<T>().Remove(Properties.Id);
            Properties.CountComponents--;
        }

        public bool Has<T>() where T : struct => Properties.Presenter.GetPool<T>().Has(Properties.Id);

        public void Dispose()
        {
            Properties.Presenter.Remove(this);
            Properties = null;
        }
    }
}

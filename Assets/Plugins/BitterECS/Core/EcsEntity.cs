using System;

namespace BitterECS.Core
{
    public class EcsEntity : IInitialize<EcsEntityProperty>, IDisposable
    {
        public EcsEntityProperty Properties { get; private set; }

        public ILinkableProvider Provider => Properties?.Presenter?.GetProvider(this);

        void IInitialize<EcsEntityProperty>.Init(EcsEntityProperty property) => Init(property);

        internal void Init(EcsEntityProperty property) => Properties = property;

        protected internal virtual void Registration() { }

        public void Add<T>(in T component) where T : struct
        {
            Properties.Presenter.GetPool<T>().Add(Properties.Id, component);
        }

        public ref T Get<T>() where T : struct
        {
            return ref Properties.Presenter.GetPool<T>().Get(Properties.Id);
        }

        public delegate void RefAction<T>(ref T obj) where T : struct;

        public void Set<T>(RefAction<T> modifier) where T : struct
        {
            modifier(ref Get<T>());
        }

        public void Remove<T>() where T : struct
        {
            Properties.Presenter.GetPool<T>().Remove(Properties.Id);
        }

        public bool Has<T>() where T : struct
        {
            return Properties.Presenter.GetPool<T>().Has(Properties.Id);
        }

        public void Dispose()
        {
            Properties?.Presenter?.Remove(this);
            Properties = null;

            GC.SuppressFinalize(this);
        }
    }
}

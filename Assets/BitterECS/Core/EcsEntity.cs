using System;


namespace BitterECS.Core
{
    public abstract class EcsEntity : IInitialize<EcsEntityProperty>, IDisposable
    {
        public EcsEntityProperty Properties { get; set; }

        public void Init(EcsEntityProperty property)
        {
            Properties ??= property;
        }

        public abstract void Registration();

        public void Add<T>(in T component) where T : struct
        {
            Properties.Presenter.GetPool<T>().Add(Properties.Id, component);
        }

        public ref T Get<T>() where T : struct
        {
            return ref Properties.Presenter.GetPool<T>().Get(Properties.Id);
        }

        public void Remove<T>() where T : struct
        {
            Properties.Presenter.GetPool<T>().Remove(Properties.Id);
        }

        public void Dispose()
        {
            Properties.Presenter.Dispose();
            Properties = null;
            GC.SuppressFinalize(this);
        }
    }


    public class EcsEntityProperty : IInitializeProperty
    {
        public readonly EcsPresenter Presenter;
        public readonly int Id = -1;

        public EcsEntityProperty(EcsPresenter presenter, int id)
        {
            Presenter = presenter;
            Id = id;
        }
    }
}

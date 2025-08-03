using System;

namespace BitterECS.Core
{
    #region IEcsSystems

    public interface IEcsSystem
    {
        public Priority PrioritySystem { get; }
    }

    public interface IEcsPreInitSystem : IEcsSystem
    {
        public void PreInit();
    }

    public interface IEcsInitSystem : IEcsSystem
    {
        public void Init();
    }

    public interface IEcsRunSystem : IEcsSystem
    {
        public void Run();
    }

    public interface IEcsFixedRunSystem : IEcsSystem
    {
        public void FixedRun();
    }

    public interface IEcsPostRunSystem : IEcsSystem
    {
        public void PostRun();
    }

    public interface IEcsDestroySystem : IEcsSystem
    {
        public void Destroy();
    }

    public interface IEcsPostDestroySystem : IEcsSystem
    {
        public void PostDestroy();
    }

    public interface IEcsIntegrationRoot :
     IEcsPreInitSystem,
     IEcsInitSystem,
     IEcsRunSystem,
     IEcsFixedRunSystem,
     IEcsPostRunSystem,
     IEcsDestroySystem,
     IEcsPostDestroySystem
    { }

    #endregion

    #region Helper

    public interface ILinkableEntity : IInitialize<EcsEntityProperty>, IDisposable
    {
        public ILinkableView View => EcsLinker.GetView(this);
        bool Has<T>() where T : struct;
        void Add<T>(in T component) where T : struct;
        void Remove<T>() where T : struct;
        ref T Get<T>() where T : struct;
    }

    public interface ILinkableView : IInitialize<EcsViewProperty>, IDisposable
    {
        public ILinkableEntity Entity => EcsLinker.GetEntity(this);
    }

    public interface IInitializeProperty { }

    public interface IInitialize
    {
        public void Init();
    }

    public interface IInitialize<T> where T : class, IInitializeProperty
    {
        public T Properties { get; set; }
        public void Init(T property);
        public T ValidateProperty(T property) { return property; }
    }

    public enum Priority : int
    {
        FIRST_TASK = -10000,
        High = 1,
        Medium = 2,
        Low = 3,
        LAST_TASK = 10000,
    }

    public struct EntityProperties
    {
        public EcsPresenter Presenter;
    }

    public class EcsViewProperty : IInitializeProperty
    {
        public readonly EcsPresenter Presenter;

        public EcsViewProperty(EcsPresenter presenter)
        {
            Presenter = presenter;
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

    #endregion

}

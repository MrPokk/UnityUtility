using System;

namespace BitterECS.Core
{
    #region IEcsSystems

    public interface IEcsSystem
    {
        public Priority PrioritySystem { get; }
    }

    public interface IEcsAutoImplement : IEcsSystem
    { }

    public interface IEcsPreInitSystem : IEcsAutoImplement
    {
        public void PreInit();
    }

    public interface IEcsInitSystem : IEcsAutoImplement
    {
        public void Init();
    }

    public interface IEcsRunSystem : IEcsAutoImplement
    {
        public void Run();
    }

    public interface IEcsFixedRunSystem : IEcsAutoImplement
    {
        public void FixedRun();
    }

    public interface IEcsPostRunSystem : IEcsAutoImplement
    {
        public void PostRun();
    }

    public interface IEcsDestroySystem : IEcsAutoImplement
    {
        public void Destroy();
    }

    public interface IEcsPostDestroySystem : IEcsAutoImplement
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

    public interface ILinkableProvider : IInitialize<EcsProviderProperty>, IDisposable
    {
        public EcsEntity Entity => Properties?.Presenter?.Get(this);
    }

    public interface IInitializeProperty { }

    public interface IInitialize<T> where T : IInitializeProperty
    {
        public T Properties { get; }
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

    public record EcsProviderProperty : IInitializeProperty
    {
        public EcsPresenter Presenter { get; }
        public ushort Id { get; }

        public EcsProviderProperty(EcsPresenter presenter, ushort id)
        {
            Presenter = presenter;
            Id = id;
        }
    }

    public record EcsEntityProperty : IInitializeProperty
    {
        public EcsPresenter Presenter { get; }
        public ushort Id { get; }

        public EcsEntityProperty(EcsPresenter presenter, ushort id = 0)
        {
            Presenter = presenter;
            Id = id;
        }
    }

    #endregion

}

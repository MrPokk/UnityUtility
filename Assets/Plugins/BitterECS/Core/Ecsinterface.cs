using System;

namespace BitterECS.Core
{
    #region IEcsSystems

    public interface IEcsSystem : IEcsPriority
    { }

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

    internal interface IPoolDestroy
    {
        bool Has(int entityId);
        void Remove(int entityId);
    }

    #endregion

    #region Helper

    public interface IEcsPriority
    {
        public Priority Priority { get; }
    }

    public interface IEcsEvent : IEcsPriority, IDisposable
    {
        EcsPresenter Presenter { get; }
        Action<EcsEntity> Added { get; }
        Action<EcsEntity> Removed { get; }
    }

    public interface ILinkableProvider : IInitialize<EcsProperty>, IDisposable
    {
        public EcsEntity Entity { get; }
    }

    public interface IInitializeProperty { }

    public interface IInitialize<T> where T : IInitializeProperty
    {
        public T Properties { get; }
        public void Init(T property);
        public T ValidateProperty(T property) => property;
    }

    public enum Priority : int
    {
        FIRST_TASK = -10000,
        High = 1,
        Medium = 2,
        Low = 3,
        LAST_TASK = 10000,
    }

    public record EcsProperty : IInitializeProperty
    {
        public EcsPresenter Presenter { get; }
        public int Id { get; }
        public int CountComponents { get; internal set; }

        public EcsProperty(EcsPresenter presenter, int id)
        {
            Presenter = presenter;
            Id = id;
        }
    }

    #endregion
}

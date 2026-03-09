using System;

namespace BitterECS.Core
{
    public interface IEcsSystem : IEcsPriority { }
    public interface IEcsAutoImplement : IEcsSystem { }
    public interface IEcsPreInitSystem : IEcsAutoImplement { void PreInit(); }
    public interface IEcsInitSystem : IEcsAutoImplement { void Init(); }
    public interface IEcsRunSystem : IEcsAutoImplement { void Run(); }
    public interface IEcsFixedRunSystem : IEcsAutoImplement { void FixedRun(); }
    public interface IEcsPostRunSystem : IEcsAutoImplement { void PostRun(); }
    public interface IEcsDestroySystem : IEcsAutoImplement { void Destroy(); }
    public interface IEcsPostDestroySystem : IEcsAutoImplement { void PostDestroy(); }

    internal interface IPool
    {
        int Count { get; }
        ReadOnlySpan<int> GetDenseEntities();
        bool Has(int entityId);
        void Remove(int entityId);
    }

    public interface IEcsPriority { Priority Priority { get; } }

    public interface IEcsEvent : IEcsPriority, IDisposable
    {
        EcsPresenter Presenter { get; }
        Action<EcsEntity> Added { get; }
        Action<EcsEntity> Removed { get; }
    }

    public interface ILinkableProvider : IInitialize<EcsProperty>, IDisposable
    {
        EcsEntity Entity { get; }
    }

    public interface IInitializeProperty { }

    public interface IInitialize<T> where T : IInitializeProperty
    {
        T Properties { get; }
        void Init(T property);
        T ValidateProperty(T property) => property;
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

        public EcsProperty(EcsPresenter presenter, int id)
        {
            Presenter = presenter;
            Id = id;
        }
    }
}

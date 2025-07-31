namespace BitterECS.Core
{

    #region IEcsEntity
    public interface IEcsComponent { }

    #endregion

    #region IEcsSystems

    public interface IEcsSystem
    {
        public virtual Priority PrioritySystem => Priority.Medium;
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

    #endregion

    #region Helper

    public interface IInitializeProperty { }

    public interface IInitialize
    {
        public void Init();
    }

    public interface IInitialize<T> where T : IInitializeProperty
    {
        public T Properties { get; set; }

        public void Init(T property);
        public T ValidateProperty(T property) { return property; }
    }

    public enum Priority
    {
        FIRST_TASK = -10000,
        High = 1,
        Medium = 2,
        Low = 3,
        LAST_TASK = 10000,
    }

    #endregion

}

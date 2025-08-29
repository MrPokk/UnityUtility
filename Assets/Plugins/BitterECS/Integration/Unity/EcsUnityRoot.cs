using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration
{
    public abstract class EcsUnityRoot : MonoBehaviour, IEcsIntegrationRoot
    {
        private static EcsWorld s_ecsWorld;
        private static EcsSystems s_ecsSystems;

        public Priority PrioritySystem => Priority.FIRST_TASK;

        protected virtual void Bootstrap() { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Integration()
        {
            s_ecsWorld = new EcsWorld();
            s_ecsSystems = new EcsSystems();
        }

        private void Awake()
        {
            PreInit();
            Bootstrap();

        }

        private void Start()
        {
            Init();
        }

        private void Update()
        {
            Run();
        }

        private void FixedUpdate()
        {
            FixedRun();
        }

        private void LateUpdate()
        {
            PostRun();
        }

        private void OnDestroy()
        {
            Destroy();
            PostDestroy();

            s_ecsWorld?.Dispose();
            s_ecsSystems?.Dispose();
        }

        public virtual void PreInit()
        {
            EcsSystems.Run<IEcsPreInitSystem>(system => system.PreInit());
        }

        public virtual void Init()
        {
            EcsSystems.Run<IEcsInitSystem>(system => system.Init());
        }

        public virtual void Run()
        {
            EcsSystems.Run<IEcsRunSystem>(system => system.Run());
        }

        public virtual void FixedRun()
        {
            EcsSystems.Run<IEcsFixedRunSystem>(system => system.FixedRun());
        }

        public virtual void PostRun()
        {
            EcsSystems.Run<IEcsPostRunSystem>(system => system.PostRun());
        }

        public virtual void Destroy()
        {
            EcsSystems.Run<IEcsDestroySystem>(system => system.Destroy());
        }

        public virtual void PostDestroy()
        {
            EcsSystems.Run<IEcsPostDestroySystem>(system => system.PostDestroy());
        }
    }
}

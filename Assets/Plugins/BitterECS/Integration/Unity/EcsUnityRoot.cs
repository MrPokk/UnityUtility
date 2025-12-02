using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration
{
    [DefaultExecutionOrder(int.MinValue)]
    [DisallowMultipleComponent]
    public class EcsUnityRoot : MonoBehaviour, IEcsIntegrationRoot
    {
        public Priority PrioritySystem => Priority.FIRST_TASK;

        private static EcsUnityRoot s_instance;
        public static EcsUnityRoot Instance
        {
            get
            {
                if (s_instance == null)
                {
                    var instanceFind = FindFirstObjectByType<EcsUnityRoot>();
                    if (instanceFind == null)
                    {
                        s_instance = new GameObject("[EcsUnityRoot]").AddComponent<EcsUnityRoot>();
                    }
                    else
                    {
                        s_instance = instanceFind;
                    }
                }
                return s_instance;
            }
        }

        protected virtual void Bootstrap() { }
        protected virtual void PostBootstrap() { }

        private void Awake()
        {
            PreInit();
            Bootstrap();
        }

        private void Start()
        {
            Init();
            PostBootstrap();
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

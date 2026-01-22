using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration
{
    [DefaultExecutionOrder(int.MinValue)]
    [DisallowMultipleComponent]
    public class EcsUnityRoot : MonoBehaviour
    {
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
                        s_instance = new GameObject($"[EcsUnityRoot]").AddComponent<EcsUnityRoot>();
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

        protected virtual void Awake()
        {
            EcsSystems.Run<IEcsPreInitSystem>(system => system.PreInit());
            Bootstrap();
        }

        protected virtual void Start()
        {
            EcsSystems.Run<IEcsInitSystem>(system => system.Init());
            PostBootstrap();
        }

        protected virtual void Update()
        {
            EcsSystems.Run<IEcsRunSystem>(system => system.Run());
        }

        protected virtual void FixedUpdate()
        {
            EcsSystems.Run<IEcsFixedRunSystem>(system => system.FixedRun());
        }

        protected virtual void LateUpdate()
        {
            EcsSystems.Run<IEcsPostRunSystem>(system => system.PostRun());
        }

        protected virtual void OnDestroy()
        {
            EcsSystems.Run<IEcsDestroySystem>(system => system.Destroy());
            EcsSystems.Run<IEcsPostDestroySystem>(system => system.PostDestroy());

            EcsWorld.Clear();
            EcsSystems.Clear();
        }
    }
}

using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration.Unity
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
            EcsSystemStatic.Load();
            EcsSystemStatic.Run<IEcsPreInitSystem>(system => system.PreInit());
            Bootstrap();
        }

        protected virtual void Start()
        {
            EcsSystemStatic.Run<IEcsInitSystem>(system => system.Init());
            PostBootstrap();
        }

        protected virtual void Update()
        {
            EcsSystemStatic.Run<IEcsRunSystem>(system => system.Run());
        }

        protected virtual void FixedUpdate()
        {
            EcsSystemStatic.Run<IEcsFixedRunSystem>(system => system.FixedRun());
        }

        protected virtual void LateUpdate()
        {
            EcsSystemStatic.Run<IEcsPostRunSystem>(system => system.PostRun());
        }

        protected virtual void OnDestroy()
        {
            EcsSystemStatic.Run<IEcsDestroySystem>(system => system.Destroy());
            EcsSystemStatic.Run<IEcsPostDestroySystem>(system => system.PostDestroy());

            EcsWorldStatic.Dispose();
            EcsSystemStatic.Dispose();
        }
    }
}

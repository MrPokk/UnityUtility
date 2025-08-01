using BitterECS.Core;
using UnityEngine;

public abstract class EcsUnityRoot : MonoBehaviour, IEcsIntegrationRoot
{
    public static EcsWorld EcsWorld { get; private set; }
    public static EcsSystems EcsSystems { get; private set; }

    protected abstract void Bootstrap();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Integration()
    {
        EcsWorld = new EcsWorld();
        EcsWorld.Init();

        EcsSystems = new EcsSystems();
        EcsSystems.Init();
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

        EcsWorld.Dispose();
        EcsSystems.Dispose();   
    }

    public void PreInit()
    {
        EcsSystems.Run<IEcsPreInitSystem>(system => system.PreInit());
    }

    public void Init()
    {
        EcsSystems.Run<IEcsInitSystem>(system => system.Init());
    }

    public void Run()
    {
        EcsSystems.Run<IEcsRunSystem>(system => system.Run());
    }

    public void FixedRun()
    {
        EcsSystems.Run<IEcsFixedRunSystem>(system => system.FixedRun());
    }

    public void PostRun()
    {
        EcsSystems.Run<IEcsPostRunSystem>(system => system.PostRun());
    }

    public void Destroy()
    {
        EcsSystems.Run<IEcsDestroySystem>(system => system.Destroy());
    }

    public void PostDestroy()
    {
        EcsSystems.Run<IEcsPostDestroySystem>(system => system.PostDestroy());
    }
}

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

        EcsSystems.Run<IEcsPreInitSystem>(system => system.PreInit());
    }

    private void Awake()
    {
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

    private void LateUpdate()
    {
        PostRun();
    }

    private void OnDestroy()
    {
        Destroy();
        PostDestroy();
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

interface IEcsIntegrationRoot :
IEcsPreInitSystem,
IEcsInitSystem,
IEcsRunSystem,
IEcsPostRunSystem,
IEcsDestroySystem,
IEcsPostDestroySystem
{ }

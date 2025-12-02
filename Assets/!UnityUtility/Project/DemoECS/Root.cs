using System.Linq;
using BitterECS.Core;
using BitterECS.Integration;
using UnityEngine;

public class Root : EcsUnityRoot
{
    [SerializeField]
    private GridConfig _gridConfigs;

    private EcsUnityRoot _world;

    [SerializeField]
    private MonoProvider _monoProvider;
    protected override void Bootstrap()
    {
        EcsSystems.AddSystems(new Test(), new Tesst(), new Tesstassd(), new Tesstasd());
        EcsSystems.Run<Itest>(system => system.Init());
    }

    protected override void PostBootstrap()
    {
        _monoProvider.Dispose();
        Debug.Log(_monoProvider);
    }
}

public class Test : Itest
{
    public Priority PrioritySystem => Priority.FIRST_TASK;

    public void Init()
    {
        Debug.Log("Test");
    }
}

public interface Itest : IEcsSystem
{
    public void Init();
}

public class Tesstasd : Itest
{
    public Priority PrioritySystem => Priority.Low;

    public void Init()
    {
        Debug.Log("das");
    }
}

public class Tesstassd : Itest
{
    public Priority PrioritySystem => Priority.Low;

    public void Init()
    {
        Debug.Log("asd");
    }
}


public class Tesst : Itest
{
    public Priority PrioritySystem => Priority.High;

    public void Init()
    {
        Debug.Log("Tesdasdast");
    }
}

using BitterECS.Core;
using BitterECS.Integration;
using UnityEngine;

public class Root : EcsUnityRoot
{
    [SerializeField]
    private GridConfig _gridConfig;
    protected override void Bootstrap()
    {

    }

    protected override void PostBootstrap()
    {
        EcsWorld.Get<EcsPresenterTest>().g



    }
}

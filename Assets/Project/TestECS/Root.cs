using BitterECS.Core;
using UnityEngine;

public class Root : EcsUnityRoot
{
    protected override void Bootstrap()
    {

    }
}

public class TestSystem : IEcsInitSystem
{
    public void Init()
    {
        Debug.Log("TEST SYSTEM");
    }
}

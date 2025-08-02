using BitterECS.Core.Integration;
using UnityEngine;

public class Root : EcsUnityRoot
{
    protected override void Bootstrap()
    {
        Debug.Log("Bootstrap");
    }
}

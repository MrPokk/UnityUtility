using BitterECS.Core;
using BitterECS.Core.Integration;
using UnityEngine;

public class Root : EcsUnityRoot
{
    protected override void Bootstrap()
    {
        var globalPresenter = EcsWorld.Get<GlobalPresenter>();

        var entities = globalPresenter.Filter().Include<ViewComponent>().Collect();

        foreach (var entitys in entities)
        {
            Debug.Log(entitys);
        }
    }
}

public class GlobalPresenter : EcsPresenter
{

}

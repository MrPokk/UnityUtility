using BitterECS.Core;
using BitterECS.Core.Integration;
using UnityEngine;

public class Root : EcsUnityRoot
{
    protected override void Bootstrap()
    {
        var globalPresenter = EcsWorld.Get<GlobalPresenter>();

        globalPresenter.AddEntity<TestEntity>().WithLink(EcsUnityViewDatabase.Get<VIEWEntity>()).Create();
        globalPresenter.AddEntity<TestEntity>().WithLink(EcsUnityViewDatabase.Get<VIEWEntity>()).Create();
    }
}

public class GlobalPresenter : EcsPresenter
{

}

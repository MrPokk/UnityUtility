using BitterECS.Core;
using BitterECS.Core.Integration;
using UnityEngine;

public class Root : EcsUnityRoot
{
    protected override void Bootstrap()
    {
        var globalPresenter = EcsWorld.Get<GlobalPresenter>();

        globalPresenter.AddEntity<TestEntity>().WithLink(EcsUnityViewDatabase.GetInstance<VIEWEntity>()).Create();
        globalPresenter.AddEntity<TestEntity>().WithLink(EcsUnityViewDatabase.GetInstance<VIEWEntity>()).Create();

        var entities = globalPresenter.Filter()
         .Include<ViewComponent>()
         .Include<Tescomonent>()
         .Collect();
        foreach (var entity in entities)
        {
            Debug.Log(entity.Get<ViewComponent>().current);
        }
    }
}


public class GlobalPresenter : EcsPresenter
{

}

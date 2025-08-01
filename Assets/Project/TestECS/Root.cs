using BitterECS.Core;
using UnityEngine;

public class Root : EcsUnityRoot
{
    protected override void Bootstrap()
    {

        var entity = EcsWorld.Get<CYKA>().NewEntity<TestEntity>();


         var component = entity.Get<TestComponent>();
    }
}

public class CYKA : EcsPresenter
{




}

using BitterECS.Core;
using UnityEngine;

public class Root : EcsUnityRoot
{
    protected override void Bootstrap()
    {

        var entity = EcsWorld.Get<CYKA>().NewEntity<TestEntity>();


        ref var component = ref entity.Get<TestComponent>();

    }
}

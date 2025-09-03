using System.Collections.Generic;
using BitterECS.Core;
using BitterECS.Integration;
using UnityEngine;

public class Root : EcsUnityRoot
{
    [SerializeField]
    private List<GridConfig> _gridConfigs = new();

    protected override void Bootstrap()
    {
        var countInitial = 100000;

        var world = EcsWorld.Get<EcsPresenterTest>();
        for (int i = 0; i < countInitial; i++)
        {
            world.AddTo<EcsEntity>().WithComponent<TestComponent>(new()).WithForce().Create();
        }
        for (int i = 0; i < countInitial; i++)
        {
            var entities = world
               .Filter()
               .Include<TestComponent>()
               .Collect();
            foreach (var entity in entities)
            {
                ref var component = ref entity.Get<TestComponent>();
            }
        }
    }
}

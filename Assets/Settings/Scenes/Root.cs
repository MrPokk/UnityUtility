using BitterCMS.UnityIntegration;
using BitterECS.Core;
using UnityEngine;

public class Root : RootMonoBehavior
{
    protected override void GlobalStart()
    {
        new EcsWorld().Init();
        new EcsSystems().Init();

        var presenter = EcsWorld.Get<TestPresenter>();
        var entity = presenter.NewEntity<TestEntity>();
        ref var component = ref entity.Get<Component1>();
    }
}


public class TestPresenter : EcsPresenter
{
    public TestPresenter()
    {
        AddLimitedType<TestEntity>();
    }
}

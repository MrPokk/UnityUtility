using BitterECS.Core;
using BitterECS.Integration;

public class Root : EcsUnityRoot
{
    protected override void Bootstrap()
    {
        var entities = EcsWorld.Get<EcsPresenterTest>();

        var ecsEntities = entities.Filter()
          .Include<TestComponent>()
          .Collect();

        foreach (var entity in ecsEntities)
        {
            entity.Dispose();
        }
    }
}

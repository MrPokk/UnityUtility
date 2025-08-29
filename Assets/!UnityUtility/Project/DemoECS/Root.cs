using BitterECS.Core;
using BitterECS.Integration;
using UnityEngine;

public class Root : EcsUnityRoot
{
    protected override void Bootstrap()
    {
        var entities = EcsWorld.Get<EcsPresenterTest>();

        entities.Add<TestEntity>();

        var ecsEntities = entities.Filter()
       .Include<TestComponent>()
       .Collect();

        Debug.Log(entities.EntityCount);

        foreach (var entity in ecsEntities)
        {
            var monoBehaviour = (MonoBehaviour)entity.Provider;
            if (monoBehaviour != null)
                Destroy(monoBehaviour.gameObject);
        }

        Debug.Log(entities.EntityCount);
    }
}

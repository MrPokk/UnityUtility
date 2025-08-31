using BitterECS.Integration;
using UnityEngine;

public class Root : EcsUnityRoot
{
    [SerializeField]
    private GridConfig _gridConfig;

    [SerializeField]
    private MonoProvider _monoProvider;

    protected override void Bootstrap()
    {

    }

    protected override void PostBootstrap()
    {
        Debug.Log($"Component value: {_monoProvider.Entity.Get<TestComponent>().value}");
        Debug.Log($"Entity value: {_monoProvider.Presenter.EntityCount}");
    }
}

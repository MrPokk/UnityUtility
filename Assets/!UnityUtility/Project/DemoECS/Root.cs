using BitterECS.Core;
using BitterECS.Integration;
using UnityEngine;

public class Root : EcsUnityRoot
{
    [SerializeField]
    private GridConfig _gridConfigs;

    protected override void Bootstrap()
    {
        var gridPresenter = new GridPresenter<EcsEntity>(_gridConfigs);
    }
}

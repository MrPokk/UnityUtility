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
        foreach (var gridConfig in _gridConfigs)
        {
            new GridPresenter<int>(gridConfig);
        }
    }
}

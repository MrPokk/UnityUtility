using BitterECS.Integration;
using BitterECS.Integration.Unity;
using UnityEngine;

public class MonoGridModel : GridModel<ProviderEcs>
{
    public MonoGridModel(GridConfig gridConfig) : base(gridConfig)
    { }
}

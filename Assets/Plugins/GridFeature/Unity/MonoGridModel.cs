using BitterECS.Integration;
using UnityEngine;

public class MonoGridModel : GridModel<ProviderEcs>
{
    public MonoGridModel(GridConfig gridConfig) : base(gridConfig)
    { }
}

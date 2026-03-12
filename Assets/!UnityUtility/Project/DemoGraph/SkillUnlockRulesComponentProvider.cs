using System;
using BitterECS.Integration.Unity;

[Serializable]
public struct GraphUnlockRulesComponent : IGraphComponent
{
    public int resourceAmount;
}

public class GraphUnlockRulesComponentProvider : ProviderEcs<GraphUnlockRulesComponent>
{
}

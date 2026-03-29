using System;
using BitterECS.Integration.Unity;
using GraphTree;

[Serializable]
public struct GraphProgressionComponent : IGraphComponent
{
    public int currentAmount;
    public int maxAmount;
}

public class GraphProgressionComponentProvider : ProviderEcs<GraphProgressionComponent>
{
}

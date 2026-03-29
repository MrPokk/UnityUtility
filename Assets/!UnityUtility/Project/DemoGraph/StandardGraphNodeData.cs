
using System;
using System.Collections.Generic;
using GraphTree;

[Serializable]
public class StandardGraphNodeData : GraphNodeData
{
    public StandardGraphNodeData()
    {
        components = new List<IGraphComponent>
        {
            new GraphProgressionComponent(),
            new GraphUnlockRulesComponent(),
            new GraphSpriteComponent(),
        };
    }

    protected override List<IGraphComponent> Registration()
    {
        return new()
        {
            new GraphProgressionComponent(),
            new GraphUnlockRulesComponent(),
            new GraphSpriteComponent()
        };
    }
}

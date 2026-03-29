
using System;
using BitterECS.Integration.Unity;
using GraphTree;
using UnityEngine;

[Serializable]
public struct GraphSpriteComponent : IGraphComponent
{
    public Sprite sprite;
}

public class GraphSpriteComponentProvider : ProviderEcs<GraphSpriteComponent>
{

}

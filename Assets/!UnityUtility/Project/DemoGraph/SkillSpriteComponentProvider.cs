
using System;
using BitterECS.Integration.Unity;
using UnityEngine;

[Serializable]
public struct GraphSpriteComponent : IGraphComponent
{
    public Sprite sprite;
}

public class GraphSpriteComponentProvider : ProviderEcs<GraphSpriteComponent>
{

}

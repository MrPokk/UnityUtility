using System;
using BitterECS.Core;
using UnityEngine;

public abstract class EcsUnityView : MonoBehaviour, ILinkableView
{
    public EcsViewProperty Properties { get; set; }

    public void Init(EcsViewProperty property)
    {
        Properties ??= property;
    }

    public void Dispose()
    {
        Properties = null;
        Destroy(gameObject);
        GC.SuppressFinalize(this);
    }
}

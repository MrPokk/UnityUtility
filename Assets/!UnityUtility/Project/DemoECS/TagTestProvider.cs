
using System;
using BitterECS.Integration.Unity;
using UnityEngine;

[Serializable]
public struct TagTest
{

}

public class TagTestProvider : ProviderEcs<TagTest>
{
    private void Start()
    {
    }
}

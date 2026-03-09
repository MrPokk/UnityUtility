using System;
using BitterECS.Integration.Unity;
using UnityEngine;

[Serializable]
public struct TestComponent
{
    public int value;
}


public class TestComponentProvider : ProviderEcs<TestComponent>
{

}

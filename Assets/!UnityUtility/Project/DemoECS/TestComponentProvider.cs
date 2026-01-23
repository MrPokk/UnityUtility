using System;
using BitterECS.Integration;

[Serializable]
public struct TestComponent
{
    public int value;
}


public class TestComponentProvider : ProviderEcs<TestComponent> { }

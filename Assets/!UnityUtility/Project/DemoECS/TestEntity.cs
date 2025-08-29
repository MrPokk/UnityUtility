using System;
using BitterECS.Core;

public class TestEntity : EcsEntity
{
    protected override void Registration()
    {
        Add<TestComponent>(new());
    }
}

[Serializable]
public struct TestComponent
{
    public int value;
}

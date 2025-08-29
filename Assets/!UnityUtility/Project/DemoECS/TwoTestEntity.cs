
using System;
using BitterECS.Core;

public class TwoTestEntity : EcsEntity
{
    protected override void Registration()
    {
        Add<TestComponent>(new());
    }
}

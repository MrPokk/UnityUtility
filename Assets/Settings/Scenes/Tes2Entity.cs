using System.Collections.Generic;
using BitterECS.Core;

public class Tes2Entity : EcsEntity
{
    public override void Registration()
    {
        Add(new Component1() { Name = "Test", Numbers = new List<int>() { 1, 2, 3 } });
    }
}

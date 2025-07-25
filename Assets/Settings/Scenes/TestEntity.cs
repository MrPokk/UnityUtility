using System.Collections.Generic;
using System.Security.Cryptography;
using BitterECS.Core;

public class TestEntity : EcsEntity
{
    public override void Registration()
    {
        Add(new Component1() { Name = "Test", Numbers = new List<int>() { 1, 2, 3 } });
    }
}
public struct Component1
{
    public string Name;
    public List<int> Numbers;
}

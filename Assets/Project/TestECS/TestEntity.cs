using BitterECS.Core;

public class TestEntity : EcsEntity
{
    public override void Registration()
    {
        Add(new TestComponent() { Name = "Test" });
    }
}


public struct TestComponent
{
    public string Name { get; set; }
}

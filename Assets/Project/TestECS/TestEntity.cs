using BitterECS.Core;

public class TestEntity : EcsEntity
{
    public override void Registration()
    {
        Add(new TestComponent() { Name = "Test" });
        Add(new TestComponent() { Name = "Test" });
    }
}


public struct TestComponent : IEcsComponent
{
    public string Name { get; set; }
}

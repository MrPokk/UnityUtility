using BitterECS.Core;

public class TestEntity : EcsEntity
{
    public override void Registration()
    {
        Add(new TestComponent() { Name = "Test" });
    }
}

public class TestEntity2 : TestEntity
{
    override public void Registration()
    {
        base.Registration();
    }
}


public struct TestComponent
{
    public string Name { get; set; }
}

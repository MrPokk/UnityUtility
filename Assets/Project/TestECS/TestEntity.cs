using BitterECS.Core;
using BitterECS.Core.Integration;

class TestEntity : EcsEntity
{
    public override void Registration()
    {
        Add(new ViewComponent());
    }
}

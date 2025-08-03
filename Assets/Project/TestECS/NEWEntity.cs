using BitterECS.Core;
using BitterECS.Core.Integration;

public class NEWEntity : EcsEntity
{
    public override void Registration()
    {
        Add(new ViewComponent(EcsUnityViewDatabase.Get<VIEWEntity>()));
    }
}

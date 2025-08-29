using BitterECS.Core;

public class EcsPresenterTest : EcsPresenter
{
    protected override void Registration()
    {
        AddLimitedType<TestEntity>();
    }
}

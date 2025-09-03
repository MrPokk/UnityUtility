using System.Linq;
using BitterECS.Core;
using BitterECS.Extra;
using UnityEngine;

public class EcsPresenterTest : EcsPresenter
{
    protected override void Registration()
    {
        RegisterPoolFactory(() => new EcsEventPool<TestComponent>());
    }
}

public class EcsPredasdassenterTest : EcsPresenter
{
    protected override void Registration()
    {

    }
}

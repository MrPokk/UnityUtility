using BitterECS.Core;
using BitterECS.Integration.Unity;
using Unity.VisualScripting;
using UnityEngine;

public class Root : EcsUnityRoot
{
    private EcsFilter<UnityComponent<Light>> _lightFilter;
    protected override void PostBootstrap()
    {
        base.PostBootstrap();

        _lightFilter.For((EcsEntity e, ref UnityComponent<Light> light) =>
         {
             light.value.intensity = Mathf.PingPong(Time.time, 1f);

             Light l = light;
             l.enabled = true;
         });
    }
}

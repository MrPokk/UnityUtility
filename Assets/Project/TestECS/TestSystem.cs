using BitterECS.Core;
using UnityEngine;

public class TestSystem : IEcsDestroySystem
{
    public void Destroy()
    {
        Debug.Log("Destroy");
    }
}

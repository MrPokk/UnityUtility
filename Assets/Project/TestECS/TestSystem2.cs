using BitterECS.Core;
using UnityEngine;

public class TestSystem2 : IEcsInitSystem
{
    public Priority PrioritySystem => Priority.Medium;

    public void Init()
    {
        Debug.Log("Init");
    }
}



public class TestSystem6 : IEcsInitSystem
{
    public Priority PrioritySystem => Priority.High;

    public void Init()
    {
        Debug.Log("Init_2");
    }
}

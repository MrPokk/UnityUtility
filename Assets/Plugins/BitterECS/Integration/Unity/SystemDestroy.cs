using System.Collections.Generic;
using System.Linq;
using BitterECS.Core;
using UnityEngine;

public class SystemDestroy : IEcsRunSystem
{
    public Priority PrioritySystem => Priority.LAST_TASK;

    private static readonly Stack<GameObject> s_gameObjects = new();

    public void Run()
    {
        if (s_gameObjects.Any())
            Object.Destroy(s_gameObjects.Pop());
    }

    public static void Add(GameObject gameObject)
    {
        Debug.Log("Destroy " + gameObject.name);
        s_gameObjects.Push(gameObject);
    }

}

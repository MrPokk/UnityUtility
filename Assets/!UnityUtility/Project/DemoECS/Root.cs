using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BitterECS.Core;
using BitterECS.Extra;
using BitterECS.Integration;
using UnityEngine;
using static BitterECS.Core.EcsFilter;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class Root : EcsUnityRoot
{
    [SerializeField]
    private GridConfig _gridConfigs;

    private EcsUnityRoot _world;

    [SerializeField]
    protected override void Bootstrap()
    {

    }

    protected override void PostBootstrap()
    {

        var provider = new Loader<TestProvider>(EntitiesPaths.TEST).GetInstance();
        Debug.Log(provider.Entity.Get<TestComponent>());
    }
}

public class TestPresenter : EcsPresenter
{
    protected override void Registration()
    { }
}

public class PerformanceTest : IEcsInitSystem
{
    private int ENTITY_COUNT = 10000;

    public Priority Priority => Priority.FIRST_TASK;

    public void Run()
    {

        if (Input.GetKeyDown(KeyCode.C))
        {
            EcsWorld.Get<TestPresenter>().Dispose();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            TestFilterPerformance();
        }
    }

    private EcsEvent _event =
     Build.For<TestPresenter>()
        .Event()
        .SubscribeWhere<Damage>(components => components.value > 10, AddedDamage);

    private static void AddedDamage(EcsEntity entity)
    {
    }

    private EcsFilter Filter =>
    Build.For<TestPresenter>()
        .Filter()
        .Include<Damage>();

    private EcsFilter Filter2 =>
    Build.For<TestPresenter>()
        .Filter()
        .Include<Health>()
        .Exclude<Damage>();


    public void TestFilterPerformance()
    {
        var filterTimerSpawn = Stopwatch.StartNew();

        filterTimerSpawn.Stop();

        var filterTimer = Stopwatch.StartNew();

        for (int i = 0; i < 1; i++)
        {
            var count = 0;
            foreach (var item in Filter)
            {
                if (count % 2 == 0)
                {
                    item.Dispose();
                }
                count++;
            }
        }



        filterTimer.Stop();

        var filterTimer2 = Stopwatch.StartNew();

        for (int i = 0; i < 1; i++)
        {
            foreach (var item in Filter2)
            {

            }
        }

        filterTimer2.Stop();
        Debug.Log(Filter.Count());

        Debug.Log($"Filter to iteration first Spawn  time: {filterTimerSpawn.ElapsedMilliseconds}ms");
        Debug.Log($"Filter to iteration first  time: {filterTimer.ElapsedMilliseconds}ms");
        Debug.Log($"Filter to iteration second time: {filterTimer2.ElapsedMilliseconds}ms");
    }

    public void Init()
    {
        //     TestFilterPerformance();
    }

    private struct Position { public int X, Y; }
    private struct Health { public int Value; }
}
public struct Damage
{
    public int value;

    public Damage(int value)
    {
        this.value = value;
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BitterECS.Core;
using BitterECS.Extra;
using BitterECS.Integration;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Root : EcsUnityRoot
{
    [SerializeField]
    private GridConfig _gridConfigs;

    private EcsUnityRoot _world;

    [SerializeField]
    private MonoProvider _monoProvider;
    protected override void Bootstrap()
    {

    }

    protected override void PostBootstrap()
    {
        var test = new PerformanceTest();
        test.TestFilterPerformance();
    }
}

public class TestPresenter : EcsPresenter
{
    protected override void Registration()
    { }
}

public class PerformanceTest : IEcsRunSystem
{
    private int ENTITY_COUNT = ushort.MaxValue;

    public Priority PrioritySystem => Priority.FIRST_TASK;

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

    public void TestFilterPerformance()
    {
        var filterTimerSpawn = Stopwatch.StartNew();

        Build.For<TestPresenter>()
        .Event()
        .SubscribeWhere<Health>(e =>
            EcsConditions.ComponentValue<Health>(e, h => h.Value % 2 == 0),
            OnAddHealth,
            OnRemoveHealth
        );

        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            var entity = Build.For<TestPresenter>()
               .Add<TestEntity>()
               .WithComponent(new Damage())
               .Create();
        }
        filterTimerSpawn.Stop();

        var filterTimer = Stopwatch.StartNew();
        var filter =
        Build.For<TestPresenter>()
        .Filter()
        .Where<Health>(h => h.Value % 2 == 0)
        .Include<Damage>()
        .Collect();

        Build.For<TestPresenter>()
        .Event()
        .SubscribeWhere<Health>(e =>
            EcsConditions.ComponentValue<Health>(e, h => h.Value % 2 == 0),
            OnAddHealth
        );


        for (int i = 0; i < 1; i++)
        {
            var count = 0;
            foreach (var item in filter)
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
            foreach (var item in filter)
            {
            }
        }

        filterTimer2.Stop();
        Debug.Log(filter.Count());

        Debug.Log($"Filter to iteration first Spawn  time: {filterTimerSpawn.ElapsedMilliseconds}ms");
        Debug.Log($"Filter to iteration first  time: {filterTimer.ElapsedMilliseconds}ms");
        Debug.Log($"Filter to iteration second time: {filterTimer2.ElapsedMilliseconds}ms");
    }

    private void OnRemoveHealth(EcsEntity entity)
    {
        
    }

    private void OnAddHealth(EcsEntity entity)
    {
        
    }

    private struct Position { public int X, Y; }
    private struct Health { public int Value; }
}
public class StaleDataExample : MonoBehaviour
{
    private TestPresenter _presenter;

    public void Start()
    {
        _presenter = new TestPresenter();
        DemonstrateStaleData();
    }

    void DemonstrateStaleData()
    {
        Debug.Log("=== ДЕМОНСТРАЦИЯ УСТАРЕВШИХ ДАННЫХ В Query ===");

        Debug.Log("Создаем 5 сущностей...");
        for (int i = 0; i < 5; i++)
        {
            var entity = _presenter.Add<TestEntity>();
            entity.Add(new Position { X = i, Y = i });
        }

        var filterCount = Build.For<TestPresenter>()
            .Filter()
            .Include<Position>()
            .Collect();
    }
}

// Компоненты
public struct Position { public int X; public int Y; }


public struct Damage { public int Value; }

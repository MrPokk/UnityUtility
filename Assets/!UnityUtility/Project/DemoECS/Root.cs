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
        PerformanceTest test = new PerformanceTest();
        test.TestFilterPerformance();
    }
}

public class TestPresenter : EcsPresenter
{
    protected override void Registration()
    { }
}

public class PerformanceTest
{
    private const int ENTITY_COUNT = ushort.MaxValue;

    public void TestFilterPerformance()
    {
        // Создаем сущности
        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            Build.For<TestPresenter>()
            .Add<TestEntity>()
            .WithComponent(new Position { X = i, Y = i })
            .WithComponent(new Health { Value = 100 })
            .Create();
        }

        var test = EcsWorld.Get<TestPresenter>();
        Debug.Log(test.CountEntity);

        var filterTimer = Stopwatch.StartNew();
        var filter =
        Build.For<TestPresenter>()
        .Filter()
        .Include<Position>()
        .Include<Health>();
        for (int i = 0; i < 1000; i++)
        {
            foreach (var item in filter.Collect())
            {
            }
        }

        filterTimer.Stop();

        for (int i = 0; i < ENTITY_COUNT; i += 2)
        {
            var entity = EcsWorld.Get<TestPresenter>();
            entity.Remove(entity.Get(i));
        }

        Debug.Log($"Filter to iteration first  time: {filterTimer.ElapsedMilliseconds}ms");
        Debug.Log($"Delete entity {filter.Collect().Count()}");
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

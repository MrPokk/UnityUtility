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
    private int ENTITY_COUNT = 1000;

    public void TestFilterPerformance()
    {

        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            Build.For<TestPresenter>()
             .Add<TestEntity>()
             .WithComponent(new Health { Value = i })
             .WithComponent(new Damage())
             .Create();
        }

        var filterTimer = Stopwatch.StartNew();
        var filter =
        Build.For<TestPresenter>()
        .Filter()
        .Where<Health>(h => h.Value > 10)
        .Include<Damage>()
        .Collect();


        for (int i = 0; i < 1; i++)
        {
            foreach (var item in filter)
            {
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

        Debug.Log($"Filter to iteration first  time: {filterTimer.ElapsedMilliseconds}ms");
        Debug.Log($"Filter to iteration second time: {filterTimer2.ElapsedMilliseconds}ms");
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

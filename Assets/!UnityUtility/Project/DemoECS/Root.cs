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
        PerformanceTest test = new PerformanceTest(new TestPresenter());
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
    private EcsPresenter _presenter;
    private const int ENTITY_COUNT = 10000;

    public PerformanceTest(EcsPresenter presenter)
    {
        _presenter = presenter;
    }

    public void TestFilterPerformance()
    {
        // Создаем сущности
        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            var entity = _presenter.Add<TestEntity>();
            entity.Add(new Position { X = i, Y = i });
            entity.Add(new Health { Value = 100 });
        }


        // Создаем фильтр один раз



        var filterTimer = Stopwatch.StartNew();
        var filter = _presenter.Filter()
        .Include<Position>()
        .Include<Health>();

        // Выполняем 1000 итераций
        for (int i = 0; i < 1000; i++)
        {
            var enumerator = filter.Collect();
            foreach (var item in enumerator)
            {
            }
        }
        filterTimer.Stop();

        Debug.Log($"Filter to iteration first  time: {filterTimer.ElapsedMilliseconds}ms");

        var filterTimerDelete = Stopwatch.StartNew();
        for (int i = ENTITY_COUNT - 1; i >= 0; i -= 2)
        {
            var entity = _presenter.Get((ushort)i);
            _presenter.Remove(entity);
        }

        for (int i = 0; i < 1000; i++)
        {
            var enumerator = filter.Collect();
            foreach (var item in enumerator)
            {
            }
        }
        filterTimerDelete.Stop();

        Debug.Log($"Delete entity {filter.Collect().Count()}");
        Debug.Log($"Filter to delete time: {filterTimerDelete.ElapsedMilliseconds}ms");
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

        var filterCount = _presenter.Filter()
            .Include<Position>()
            .Collect();



    }
}

// Компоненты
public struct Position { public int X; public int Y; }

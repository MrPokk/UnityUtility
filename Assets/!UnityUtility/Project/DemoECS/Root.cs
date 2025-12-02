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
        var test = new PerformanceTest(EcsWorld.Get<TestPresenter>());
        test.TestFilterVsQuery();
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
    private const int ENTITY_COUNT = 1000;

    public PerformanceTest(EcsPresenter presenter)
    {
        _presenter = presenter;
    }


    public void TestFilterVsQuery()
    {
        // Создаем сущности
        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            var entity = _presenter.Add<TestEntity>();
            entity.Add(new Position { X = i, Y = i });
            entity.Add(new Health { Value = 100 });
        }

        // Создаем Query ДО удаления
        var query = _presenter.QueryInclude<Position, Health>();

        // Удаляем каждую вторую сущность
        for (int i = ENTITY_COUNT - 1; i >= 0; i -= 2)
        {
            var entity = _presenter.Get((ushort)i);
            _presenter.Remove(entity);
        }

        // 🔥 ВАЖНО: обновляем Query!
        //    query.Refresh();

        // Тест Filter (актуальные данные)
        var filterTimer = Stopwatch.StartNew();
        var filter = _presenter.Filter()
            .Include<Position>()
            .Include<Health>()
            .Collect();

        int filterValidCount = 0;
        int filterInvalidCount = 0;

        for (int i = 0; i < 1000; i++)
        {
            foreach (var entity in filter)
            {
                if (entity.Properties == null)
                    filterInvalidCount++;
                else
                    filterValidCount++;
            }
        }
        filterTimer.Stop();

        var queryTimer = Stopwatch.StartNew();
        int queryValidCount = 0;
        int queryInvalidCount = 0;

        for (int i = 0; i < 1000; i++)
        {
            foreach (var entity in query)
            {
                if (entity.Properties == null)
                    queryInvalidCount++;
                else
                    queryValidCount++;
            }
        }
        queryTimer.Stop();

        Debug.Log($"Filter: {filterTimer.ElapsedMilliseconds}ms");
        Debug.Log($"Query: {queryTimer.ElapsedMilliseconds}ms");
        Debug.Log($"Filter valid/invalid: {filterValidCount}/{filterInvalidCount}");
        Debug.Log($"Query valid/invalid: {queryValidCount}/{queryInvalidCount}");

        query.Dispose();
    }

    private struct Position
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
    private struct Health
    {
        public int Value { get; set; }
    }
}

public class StaleDataExample : MonoBehaviour
{
    private TestPresenter _presenter;

    void Start()
    {
        _presenter = new TestPresenter();
        DemonstrateStaleData();
    }

    void DemonstrateStaleData()
    {
        Debug.Log("=== ДЕМОНСТРАЦИЯ УСТАРЕВШИХ ДАННЫХ В Query ===");

        // Шаг 1: Используем обычные пулы (по умолчанию)
        // Ничего не настраиваем - пулы будут обычными EcsPool<T>

        // Шаг 2: Создаем 5 сущностей
        Debug.Log("Создаем 5 сущностей...");
        for (int i = 0; i < 5; i++)
        {
            var entity = _presenter.Add<TestEntity>();
            entity.Add(new Position { X = i, Y = i });
        }

        // Шаг 3: Создаем Query
        Debug.Log("Создаем Query для Position...");
        var query = _presenter.QueryInclude<Position>();

        // Шаг 4: Проверяем начальное состояние
        Debug.Log($"Query после создания: {query.Count} сущностей");
        // Output: 5 сущностей ✓

        // Шаг 5: Добавляем НОВУЮ сущность ПОСЛЕ создания Query
        Debug.Log("\nДобавляем новую сущность с Position...");
        var newEntity = _presenter.Add<TestEntity>();
        newEntity.Add(new Position { X = 999, Y = 999 });

        // Шаг 6: Проверяем Query БЕЗ Refresh()
        Debug.Log($"Query без Refresh(): {query.Count} сущностей");
        // Output: 5 сущностей (НО ДОЛЖНО БЫТЬ 6!) ❌
        // Query НЕ ВИДИТ новую сущность!

        // Шаг 7: Проверяем Filter (для сравнения)
        var filterCount = _presenter.Filter()
            .Include<Position>()
            .Collect()
            .Count();

        Debug.Log($"Filter: {filterCount} сущностей");
        // Output: 6 сущностей ✓

        // Шаг 8: Принудительно обновляем Query
        Debug.Log("\nВызываем query.Refresh()...");
        query.Refresh();
        Debug.Log($"Query после Refresh(): {query.Count} сущностей");
        // Output: 6 сущностей ✓

        // Шаг 9: Удаляем сущность и проверяем снова
        Debug.Log("\nУдаляем одну сущность...");
        _presenter.Remove(newEntity);

        Debug.Log($"Query без Refresh(): {query.Count} сущностей");
        // Output: 6 сущностей (НО ДОЛЖНО БЫТЬ 5!) ❌

        Debug.Log($"Filter: {_presenter.Filter().Include<Position>().Collect().Count()} сущностей");
        // Output: 5 сущностей ✓

        query.Dispose();
    }
}

// Компоненты
public struct Position { public int X; public int Y; }

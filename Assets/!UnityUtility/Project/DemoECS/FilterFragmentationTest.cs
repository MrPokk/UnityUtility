using System;
using System.Diagnostics;
using BitterECS.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

public struct TagA { }
public struct TagB { }
public struct TagC { }

public class FilterFragmentationTest : IEcsInitSystem, IEcsRunSystem
{
    private const int V = 2_000_000; // Немного понизил для адекватных тестов в Unity (2 млн)
    public Priority Priority => Priority.Medium;

    public EcsWorld World => EcsWorldStatic.Instance;

    private EcsEvent _event = new EcsEvent().Subscribe<TagA>(added: Test);

    private static void Test(EcsEntity entity)
    {
        var id = entity.Id;
    }

    private EcsFilter<TagA, TagB, TagC> _complexFilter;

    public void Init()
    {
        // Создаем 2 000 000 "мусорных" сущностей, чтобы разбросать память и увеличить пулы
        for (int i = 0; i < V; i++)
        {
            var e = World.CreateEntity();
            if (i % 2 == 0) e.Add(new TagA());
            if (i % 3 == 0) e.Add(new TagB());
        }

        // Создаем всего 100 сущностей, подходящих под сложный фильтр
        for (int i = 0; i < 101; i++)
        {
            var e = World.CreateEntity();
            e.Add(new TagA());
            e.Add(new TagB());
            e.Add(new TagC());
        }

        // Инициализируем фильтр 
        _complexFilter = new EcsFilter<TagA, TagB, TagC>(World);
    }

    public void Run()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;

        var sw = Stopwatch.StartNew();
        int count = 0;


        foreach (var entity in _complexFilter)
        {
            count++;
        }

        sw.Stop();
        Debug.Log($"[Filter Fragmentation] Found {count} among {V} entities in: {sw.Elapsed.TotalMilliseconds:F4} ms");
    }
}

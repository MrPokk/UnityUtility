using System.Diagnostics;
using BitterECS.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

public struct TagA { }
public struct TagB { }
public struct TagC { }

public class FilterFragmentationTest : IEcsInitSystem, IEcsRunSystem
{
    private const int V = 20000 * 100;
    private EcsFilter _complexFilter;
    public Priority Priority => Priority.Medium;

    public void Init()
    {
        var presenter = EcsWorld.Get<TestPresenter>();

        // Создаем 20 000 "мусорных" сущностей
        for (int i = 0; i < V; i++)
        {
            var e = presenter.CreateEntity();
            if (i % 2 == 0) e.Add(new TagA());
            if (i % 3 == 0) e.Add(new TagB());
        }

        // Создаем всего 100 сущностей, подходящих под сложный фильтр
        for (int i = 0; i < 101; i++)
        {
            var e = presenter.CreateEntity();
            e.Add(new TagA());
            e.Add(new TagB());
            e.Add(new TagC());
        }

        _complexFilter = new EcsFilter(presenter)
            .Include<TagA>()
            .Include<TagB>()
            .Include<TagC>();
    }

    public void Run()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;

        var sw = Stopwatch.StartNew();
        int count = 0;

        foreach (var entity in _complexFilter)
        {

        }

        sw.Stop();
        Debug.Log($"[Filter Fragmentation] Found {count} among {V} entities in: {sw.Elapsed.TotalMilliseconds:F4} ms");
    }
}

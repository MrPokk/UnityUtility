using System;
using BitterECS.Core;
using UnityEngine;

public class GCMemoryTest
{
    public Priority Priority => Priority.Medium;

    public void Run()
    {
        // Выполняем действия КАЖДЫЙ кадр
        var presenter = EcsWorld.Get<TestPresenter>();
        var filter = new EcsFilter(presenter).Include<MoveComponent>();

        // 1. Итерация фильтра
        foreach (var e in filter) { e.Get<MoveComponent>().x += 1; }

        // 2. Постоянное создание и удаление (стресс для Sparse Set)
        var temp = presenter.CreateEntity();
        temp.Add(new MoveComponent());
        temp.Destroy();

        if (Time.frameCount % 60 == 0)
        {
            // Смотрим на GC (в профайлере Unity это будет видно лучше)
            long mem = GC.GetTotalMemory(false);
            // Debug.Log($"[GC Check] Frame {Time.frameCount}, Total Memory: {mem / 1024} KB");
        }
    }
}

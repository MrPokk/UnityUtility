using System.Diagnostics;
using BitterECS.Core;
using UnityEngine;

public struct MoveComponent { public float x, y; }
public struct VelocityComponent { public float vx, vy; }

public class RawPerformanceTest : IEcsRunSystem, IEcsInitSystem
{
    private const int COUNT = 10000;
    private EcsFilter _filter;

    public Priority Priority => Priority.Medium;

    public void Init()
    {
        var presenter = EcsWorld.Get<TestPresenter>();
        for (int i = 0; i < COUNT; i++)
        {
            var e = presenter.CreateEntity();
            e.Add(new MoveComponent { x = i, y = i });
            e.Add(new VelocityComponent { vx = 0.1f, vy = 0.1f });
        }
        _filter = new EcsFilter(presenter).Include<MoveComponent>().Include<VelocityComponent>();
    }

    public void Run()
    {
        if (!Input.GetKeyDown(KeyCode.P)) return;

        var sw = Stopwatch.StartNew();

        // Симуляция тяжелого цикла обновления
        foreach (var entity in _filter)
        {
            ref var move = ref entity.Get<MoveComponent>();
            ref var vel = ref entity.Get<VelocityComponent>();

            move.x += vel.vx;
            move.y += vel.vy;
        }

        sw.Stop();
        UnityEngine.Debug.Log($"[Raw SoA] {COUNT} entities updated in: {sw.Elapsed.TotalMilliseconds:F4} ms");
    }
}

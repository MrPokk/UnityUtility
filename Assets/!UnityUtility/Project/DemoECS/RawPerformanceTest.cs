using System.Diagnostics;
using BitterECS.Core;
using UnityEngine;

public struct MoveComponent { public float x, y; }
public struct VelocityComponent { public float vx, vy; }

public class RawPerformanceTest : IEcsRunSystem, IEcsInitSystem
{
    private const int EntityCount = 100_000;  // количество сущностей
    private EcsFilter _filter = new EcsFilter<TestPresenter>().Include<MoveComponent>();

    public Priority Priority => Priority.Medium;

    public void Init()
    {
        new EcsBuilder<TestPresenter>()
       .With<MoveComponent>()
       .With<VelocityComponent>()
       .Create(EntityCount);
    }

    public void Run()
    {
        if (!Input.GetKeyDown(KeyCode.P)) return;

        var sw = Stopwatch.StartNew();

        foreach (var entity in _filter)
        {

        }

        sw.Stop();
        UnityEngine.Debug.Log($"[Raw SoA] {EntityCount} entities updated in: {sw.Elapsed.TotalMilliseconds:F4} ms");
    }
}

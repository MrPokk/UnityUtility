using System.Diagnostics;
using BitterECS.Core;
using UnityEngine;

public struct MoveComponent { public float x, y; }
public struct VelocityComponent { public float vx, vy; }

public class RawPerformanceTest : IEcsRunSystem, IEcsInitSystem
{
    private const int EntityCount = 100_000;
    private EcsFilter _filter = new EcsFilter().Has<MoveComponent>();
    private EcsFilter<MoveComponent> _ecsEntities = new();
    public Priority Priority => Priority.Medium;

    public void Init()
    {
        var test = new EcsBuilder()
       .With<MoveComponent>()
       .With<VelocityComponent>();
        for (int i = 0; i < EntityCount; i++)
        {
            test.Create();
        }
    }

    public void Run()
    {
        if (!Input.GetKeyDown(KeyCode.P)) return;

        var sw = Stopwatch.StartNew();

        foreach (var entity in _ecsEntities)
        {

        }

        sw.Stop();
        UnityEngine.Debug.Log($"[Raw SoA] {EntityCount} entities updated in: {sw.Elapsed.TotalMilliseconds:F4} ms");
    }
}

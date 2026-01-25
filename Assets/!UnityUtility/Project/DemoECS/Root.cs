using System.Diagnostics;
using BitterECS.Core;
using BitterECS.Integration;
using UnityEngine;
using static BitterECS.Core.EcsFilter;
using Debug = UnityEngine.Debug;

public class Root : EcsUnityRoot
{
    [SerializeField]
    private GridConfig _gridConfigs;

    private EcsUnityRoot _world;

    private Filter EcsEntities =>
    new EcsFilter<TestPresenter>()
    .Include<TestComponent>()
    .Include<TestComponent>()
    .Entities();

    private Filter Dsd =>
    new EcsFilter<TestPresenter>()
    .Include<TestComponent>()
    .Entities();

    [SerializeField]
    protected override void Bootstrap()
    {

    }

    protected override void PostBootstrap()
    {
        var testFreeEntity = new Loader<TestProvider>(EntitiesPaths.TEST).GetInstance();
        var testFreeEntityds = new Loader<TestProvider>(EntitiesPaths.TEST).GetPrefab();
        var dsd = testFreeEntityds.Entity;
        testFreeEntity.GetComponent<TestComponentProvider>().Value.value = 100;
        foreach (var item in EcsEntities)
        {
            Debug.Log(item.Get<TestComponent>().value);
        }
    }
}

public class TestPresenter : EcsPresenter
{
    protected override void Registration()
    { }
}

public class PerformanceTest : IEcsInitSystem, IEcsRunSystem
{
    private int ENTITY_COUNT = 10000;

    public Priority Priority => Priority.FIRST_TASK;

    public void Run()
    {

        if (Input.GetKeyDown(KeyCode.C))
        {
            EcsWorld.Get<TestPresenter>().Dispose();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            TestFilterPerformance();
        }
    }

    private EcsEvent _event =
     Build.For<TestPresenter>()
        .Event()
        .SubscribeWhere<Damage>(components => components.value > 10, AddedDamage);

    private static void AddedDamage(EcsEntity entity)
    {
    }

    private Filter Filter =>
    new EcsFilter<TestPresenter>()
       .Include<Damage>()
       .Entities();

    private Filter Filter2 =>
    new EcsFilter<TestPresenter>()
       .Include<Damage>()
       .Entities();

    public void TestFilterPerformance()
    {
        var filterTimerSpawn = Stopwatch.StartNew();

        filterTimerSpawn.Stop();

        var filterTimer = Stopwatch.StartNew();

        for (int i = 0; i < 1; i++)
        {
            var count = 0;
            foreach (var item in Filter)
            {
                if (count % 2 == 0)
                {
                    item.Dispose();
                }
                count++;
            }
        }



        filterTimer.Stop();

        var filterTimer2 = Stopwatch.StartNew();

        for (int i = 0; i < 1; i++)
        {
            foreach (var item in Filter2)
            {

            }
        }

        filterTimer2.Stop();
        Debug.Log(Filter.Count());

        Debug.Log($"Filter to iteration first Spawn  time: {filterTimerSpawn.ElapsedMilliseconds}ms");
        Debug.Log($"Filter to iteration first  time: {filterTimer.ElapsedMilliseconds}ms");
        Debug.Log($"Filter to iteration second time: {filterTimer2.ElapsedMilliseconds}ms");
    }

    public void Init()
    {
        //     TestFilterPerformance();
    }

    private struct Position { public int X, Y; }
    private struct Health { public int Value; }
}
public struct Damage
{
    public int value;

    public Damage(int value)
    {
        this.value = value;
    }
}

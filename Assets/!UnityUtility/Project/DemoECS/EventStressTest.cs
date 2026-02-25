using System.Diagnostics;
using BitterECS.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

public struct TriggerComponent { }

public class EventStressTest : IEcsRunSystem
{
    private const int BATCH_SIZE = 5000;
    private int _addedCount = 0;
    public Priority Priority => Priority.Medium;

    // Подписываемся на добавление компонента
    private EcsEvent _event = Build.For<TestPresenter>()
        .Event()
        .Subscribe<TriggerComponent>(added: OnTriggered);

    private static void OnTriggered(EcsEntity entity)
    {
        // Какая-то логика при срабатывании
        var id = entity.Id;
    }

    public void Run()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        var presenter = EcsWorld.Get<TestPresenter>();
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var e = presenter.CreateEntity();
            e.Add(new TriggerComponent()); // Это триггерит OnEvent
        }

        sw.Stop();
        Debug.Log($"[Event Stress] Created {BATCH_SIZE} entities with events in: {sw.Elapsed.TotalMilliseconds:F4} ms");
    }
}

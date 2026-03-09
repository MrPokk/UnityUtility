using System.Diagnostics;
using BitterECS.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

public struct TriggerComponent { }

public class EventStressTest : IEcsInitSystem, IEcsRunSystem
{
    private const int BATCH_SIZE = 5000;
    public Priority Priority => Priority.Medium;

    // Предполагается, что Root/фреймворк прокидывает сюда ссылку на EcsWorld перед Init()

    private EcsEvent _event = new();

    public void Init()
    {
        // Теперь EcsEvent инициализируется от World
        _event = new EcsEvent().Subscribe<TriggerComponent>(added: OnTriggered);
    }

    private static void OnTriggered(EcsEntity entity)
    {
        // Какая-то логика при срабатывании
        var id = entity.Id;
    }

    public void Run()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var e = EcsWorldStatic.Instance.CreateEntity();
            e.Add(new TriggerComponent()); // Это триггерит OnEvent
        }

        sw.Stop();
        Debug.Log($"[Event Stress] Created {BATCH_SIZE} entities with events in: {sw.Elapsed.TotalMilliseconds:F4} ms");
    }
}

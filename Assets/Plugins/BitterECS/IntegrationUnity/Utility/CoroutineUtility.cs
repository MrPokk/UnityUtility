using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CoroutineUtility : MonoBehaviour
{
    private static CoroutineUtility _instance;
    public static CoroutineUtility Instance
    {
        get
        {
            if (_instance == null)
            {
                var instanceFind = FindFirstObjectByType<CoroutineUtility>();
                if (instanceFind == null)
                    _instance = new GameObject("[CoroutineUtility]").AddComponent<CoroutineUtility>();
                else
                    _instance = instanceFind;
            }
            return _instance;
        }
    }

    private readonly Dictionary<int, ActiveCoroutine> _activeCoroutines = new Dictionary<int, ActiveCoroutine>();
    private int _nextCoroutineId = 1;

    private event Action<int> OnCoroutineStopped;
    private event Action OnAllCoroutinesStopped;

    public static CoroutineHandle Run(IEnumerator coroutine)
    {
        if (coroutine == null)
        {
            Debug.LogWarning("Attempted to start a null coroutine.");
            return CoroutineHandle.Invalid;
        }

        return Instance.StartNewCoroutine(coroutine);
    }

    public static void Stop(CoroutineHandle handle)
    {
        if (!handle.IsValid) return;
        Instance.StopCoroutineInternal(handle);
    }

    public static void StopAll()
    {
        Instance.StopAllCoroutinesInternal();
    }

    public static void SubscribeToStop(Action<CoroutineHandle> callback)
    {
        if (callback == null) return;
        Instance.OnCoroutineStopped += (id) => callback(new CoroutineHandle(id));
    }

    public static void SubscribeToAllStop(Action callback)
    {
        if (callback == null) return;
        Instance.OnAllCoroutinesStopped += callback;
    }

    public static bool HasActiveCoroutines() => Instance._activeCoroutines.Count > 0;

    private CoroutineHandle StartNewCoroutine(IEnumerator coroutine)
    {
        int id = _nextCoroutineId++;
        var handle = new CoroutineHandle(id);

        var unityCoroutine = StartCoroutine(RunCoroutineWrapper(id, coroutine));
        _activeCoroutines.Add(id, new ActiveCoroutine(unityCoroutine, handle));

        return handle;
    }

    private IEnumerator RunCoroutineWrapper(int id, IEnumerator coroutine)
    {
        yield return coroutine;

        if (_activeCoroutines.Remove(id, out var activeCoroutine))
        {
            OnCoroutineStopped?.Invoke(id);
            if (_activeCoroutines.Count == 0)
                OnAllCoroutinesStopped?.Invoke();
        }
    }

    private void StopCoroutineInternal(CoroutineHandle handle)
    {
        if (!_activeCoroutines.Remove(handle.Id, out var activeCoroutine))
            return;

        StopCoroutine(activeCoroutine.UnityCoroutine);
        OnCoroutineStopped?.Invoke(handle.Id);

        if (_activeCoroutines.Count == 0)
            OnAllCoroutinesStopped?.Invoke();
    }

    private void StopAllCoroutinesInternal()
    {
        foreach (var entry in _activeCoroutines.Values)
        {
            StopCoroutine(entry.UnityCoroutine);
            OnCoroutineStopped?.Invoke(entry.Handle.Id);
        }

        _activeCoroutines.Clear();
        OnAllCoroutinesStopped?.Invoke();
    }

    private readonly struct ActiveCoroutine
    {
        public readonly Coroutine UnityCoroutine;
        public readonly CoroutineHandle Handle;

        public ActiveCoroutine(Coroutine unityCoroutine, CoroutineHandle handle)
        {
            UnityCoroutine = unityCoroutine;
            Handle = handle;
        }
    }
}


public readonly struct CoroutineHandle : IEquatable<CoroutineHandle>
{
    public static readonly CoroutineHandle Invalid = new CoroutineHandle(-1);

    public readonly int Id;
    public bool IsValid => Id != -1;

    public CoroutineHandle(int id)
    {
        Id = id;
    }

    public bool Equals(CoroutineHandle other) => Id == other.Id;
    public override bool Equals(object obj) => obj is CoroutineHandle other && Equals(other);
    public override int GetHashCode() => Id;
    public static bool operator ==(CoroutineHandle a, CoroutineHandle b) => a.Equals(b);
    public static bool operator !=(CoroutineHandle a, CoroutineHandle b) => !a.Equals(b);
}

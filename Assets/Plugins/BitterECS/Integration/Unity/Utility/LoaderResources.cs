using System;
using UnityEngine;
using Object = UnityEngine.Object;

public readonly ref struct Loader<T> where T : Object
{
    private readonly string _path;
    private T Asset => Resources.Load<T>(_path) ?? throw new Exception($"Asset not found at path: {_path}");

    public Loader(string path)
    {
        _path = path;
    }

    public T GetPrefab() => Asset;
    public T GetInstance() => Object.Instantiate(GetPrefab());
    public static implicit operator T(Loader<T> loader) => Object.Instantiate(loader.Asset);
}

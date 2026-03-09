using System;
using UnityEngine;
using Object = UnityEngine.Object;
namespace BitterECS.Integration.Unity
{
    public readonly ref struct Loader<T> where T : Object
    {
        private readonly string _path;
        private T Asset => Resources.Load<T>(_path) ?? throw new Exception($"Asset not found at path: {_path}");

        public Loader(string path)
        {
            _path = path;
        }

        public T Prefab() => Asset;
        public T New() => Object.Instantiate(Prefab());
        public T New(Transform parent) => Object.Instantiate(Prefab(), parent);
        public T New(Vector3 position, Quaternion rotation) => Object.Instantiate(Prefab(), position, rotation);
        public T New(Vector3 position, Quaternion rotation, Transform parent) => Object.Instantiate(Prefab(), position, rotation, parent);
        public static implicit operator T(Loader<T> loader) => Object.Instantiate(loader.Asset);
    }
}
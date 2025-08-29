using UnityEngine;
using System;

namespace BitterECS.Integration
{
    [RequireComponent(typeof(MonoProvider)), Serializable]
    public class ComponentProvider<T> : ComponentProvider where T : struct
    {
        [SerializeField]
        protected T _component;

        public ref T Component => ref _component;

        public override object ObjectComponent { get => _component; }
    }

    [RequireComponent(typeof(MonoProvider)), Serializable]
    public abstract class ComponentProvider : MonoBehaviour
    {
        public abstract object ObjectComponent { get; }
    }
}

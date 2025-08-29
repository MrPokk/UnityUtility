using UnityEngine;
using System;

namespace BitterECS.Integration
{
    [RequireComponent(typeof(MonoProvider<>)), Serializable]
    public class ComponentProvider<T> : ComponentProvider where T : struct
    {
        [SerializeField]
        protected T component;

        public ref T Component => ref component;

        public override object ObjectComponent { get => component; }

        public void SetComponent(T newComponent) => component = newComponent;
    }

    [RequireComponent(typeof(MonoProvider<>)), Serializable]
    public abstract class ComponentProvider : MonoBehaviour
    {
        public abstract object ObjectComponent { get; }
    }
}

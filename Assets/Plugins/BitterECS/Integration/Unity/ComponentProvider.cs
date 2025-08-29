using UnityEngine;
using System;

namespace BitterECS.Integration
{
    [RequireComponent(typeof(MonoProvider)), Serializable]
    public class ComponentProvider<T> : ComponentProvider where T : struct
    {
        [SerializeField]
        protected T component;

        public ref T Component => ref component;

        public override object ObjectComponent { get => component; }
    }

    [RequireComponent(typeof(MonoProvider)), Serializable]
    public abstract class ComponentProvider : MonoBehaviour
    {
        public abstract object ObjectComponent { get; }
    }
}

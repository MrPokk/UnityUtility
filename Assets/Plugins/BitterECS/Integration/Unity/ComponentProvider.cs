using UnityEngine;
using System;
using BitterECS.Core;

namespace BitterECS.Integration
{
    public interface ITypedComponentProvider
    {
        public void Apply(EcsEntity entity);
        public void Sync(EcsEntity entity);
    }

    [RequireComponent(typeof(MonoProvider)), Serializable]
    public abstract class ComponentProvider<T> : MonoBehaviour, ITypedComponentProvider where T : struct
    {
        [SerializeField]
        protected T _component;
        public ref T Component => ref _component;

        public void Apply(EcsEntity entity)
        {
            if (entity.Has<T>())
            {
                entity.Set((ref T comp) => comp = _component);
            }
            else
            {
                entity.Add(_component);
            }
        }

        public void Sync(EcsEntity entity)
        {
            if (entity.Has<T>())
            {
                _component = entity.Get<T>();
            }
        }
    }

}

using UnityEngine;
using System;
using BitterECS.Core;

namespace BitterECS.Integration
{
    public interface ITypedComponentProvider
    {
        public void Sync(EcsEntity entity);
        public void Registration();
    }

    [RequireComponent(typeof(MonoProvider)), Serializable]
    public abstract class ComponentProvider<T> : MonoBehaviour, ITypedComponentProvider where T : struct
    {
        [SerializeField] protected T _value;
        public ref T Value => ref _value;

        private void OnValidate()
        {
            var entity = GetComponent<MonoProvider>().Entity;
            if (entity != null)
                Sync(entity);
        }

        public void Sync(EcsEntity entity)
        {
            entity.AddOrReplace(_value);
        }

        public virtual void Registration()
        { }
    }
}

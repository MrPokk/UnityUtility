using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Extra
{
    public sealed class EcsComponentEvents
    {
        public event Action<int> OnComponentAdded;
        public event Action<int> OnComponentRemoved;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeAdded(int entityId) => OnComponentAdded?.Invoke(entityId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeRemoved(int entityId) => OnComponentRemoved?.Invoke(entityId);
    }


    public interface IPoolWithEvents
    {
        EcsComponentEvents Events { get; }
    }
}

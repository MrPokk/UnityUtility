using System.Runtime.CompilerServices;
using BitterECS.Core;

namespace BitterECS.Extra
{
    public sealed class EcsEventPool<T> : EcsPool<T> where T : struct
    {
        public EcsComponentEvents Events { get; } = new EcsComponentEvents();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new void Add(int entityId, in T component)
        {
            base.Add(entityId, component);
            Events.InvokeAdded(entityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new void Remove(int entityId)
        {
            if (Has(entityId))
            {
                base.Remove(entityId);
                Events.InvokeRemoved(entityId);
            }
        }
    }

}

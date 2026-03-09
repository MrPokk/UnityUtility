using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public static class EcsDefinitions
    {
        public const ushort InitialPoolCapacity = 64;
        public const ushort PoolGrowthFactor = 2;
        public const ushort EntityCallbackFactor = 4;
        public const ushort InitialPresentersCapacity = 16;
        public const ushort InitialEntitiesCapacity = 128;
        public const ushort InitialLinkedEntitiesCapacity = 64;
        public const ushort InitialSystemsCapacity = 64;
        public const int SparsePageSize = 256;
    }

    internal static class EcsComponentTypes
    {
        public static int NextId = 0;
    }

    public static class EcsComponentTypeId<T>
    {
        public static readonly int Id = EcsComponentTypes.NextId++;
    }

    public struct EcsComponentMask
    {
        public ulong[] bits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int id)
        {
            var index = id >> 6;

            if (bits == null)
            {
                bits = new ulong[Math.Max(4, index + 1)];
            }
            else if (index >= bits.Length)
            {
                Array.Resize(ref bits, Math.Max(bits.Length * 2, index + 1));
            }

            bits[index] |= 1UL << (id & 63);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(int id)
        {
            if (bits == null) return;

            var index = id >> 6;
            if (index < bits.Length)
            {
                bits[index] &= ~(1UL << (id & 63));
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Has(int id)
        {
            if (bits == null) return false;

            var index = id >> 6;
            return index < bits.Length && ((bits[index] & (1UL << (id & 63))) != 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasAll(in EcsComponentMask other)
        {
            if (other.bits == null) return true;
            if (bits == null) return other.IsEmpty();

            for (var i = 0; i < other.bits.Length; i++)
            {
                var otherBits = other.bits[i];
                if (otherBits == 0) continue;

                if (i >= bits.Length) return false;

                if ((bits[i] & otherBits) != otherBits) return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasAny(in EcsComponentMask other)
        {
            if (bits == null || other.bits == null) return false;

            var minLen = Math.Min(bits.Length, other.bits.Length);
            for (var i = 0; i < minLen; i++)
            {
                if ((bits[i] & other.bits[i]) != 0) return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsEmpty()
        {
            if (bits == null) return true;
            for (var i = 0; i < bits.Length; i++)
            {
                if (bits[i] != 0) return false;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (bits != null)
            {
                Array.Clear(bits, 0, bits.Length);
            }
        }
    }
}


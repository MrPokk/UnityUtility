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
        public ulong bits0, bits1, bits2, bits3;

        public void Set(int id)
        {
            if (id < 64) bits0 |= 1UL << id;
            else if (id < 128) bits1 |= 1UL << (id - 64);
            else if (id < 192) bits2 |= 1UL << (id - 128);
            else if (id < 256) bits3 |= 1UL << (id - 192);
        }

        public void Remove(int id)
        {
            if (id < 64) bits0 &= ~(1UL << id);
            else if (id < 128) bits1 &= ~(1UL << (id - 64));
            else if (id < 192) bits2 &= ~(1UL << (id - 128));
            else if (id < 256) bits3 &= ~(1UL << (id - 192));
        }

        public readonly bool Has(int id)
        {
            if (id < 64) return (bits0 & (1UL << id)) != 0;
            if (id < 128) return (bits1 & (1UL << (id - 64))) != 0;
            if (id < 192) return (bits2 & (1UL << (id - 128))) != 0;
            if (id < 256) return (bits3 & (1UL << (id - 192))) != 0;
            return false;
        }

        public readonly bool HasAll(in EcsComponentMask other) =>
            (bits0 & other.bits0) == other.bits0 &&
            (bits1 & other.bits1) == other.bits1 &&
            (bits2 & other.bits2) == other.bits2 &&
            (bits3 & other.bits3) == other.bits3;

        public readonly bool HasAny(in EcsComponentMask other) =>
            (bits0 & other.bits0) != 0 ||
            (bits1 & other.bits1) != 0 ||
            (bits2 & other.bits2) != 0 ||
            (bits3 & other.bits3) != 0;

        public readonly bool IsEmpty() => bits0 == 0 && bits1 == 0 && bits2 == 0 && bits3 == 0;
    }
}

namespace BitterECS.Core
{
    public static class EcsConfig
    {
        // Component pool settings
        public const ushort InitialPoolCapacity = 64;
        public const ushort PoolGrowthFactor = 2;

        // Filter settings
        public const ushort FilterConditionInclude = 6;
        public const ushort FilterConditionExclude = 2;

        // EntityBuilder and EntityDestroyer settings
        public const ushort EntityCallbackFactor = 4;

        // EcsPresenter settings
        public const ushort InitialPresentersCapacity = 16;
        public const ushort InitialEntitiesCapacity = 128;
        public const ushort InitialLinkedEntitiesCapacity = 64;
        public const ushort AllowedTypesCapacity = 4;

        // EcsSystems settings
        public const ushort InitialSystemsCapacity = 64;
    }
}

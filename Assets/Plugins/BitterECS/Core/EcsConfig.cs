namespace BitterECS.Core
{
    public static class EcsConfig
    {
        // Component pool settings
        public const int InitialPoolCapacity = 64;
        public const int PoolGrowthFactor = 2;

        // Filter settings
        public const int FilterConditionFactor = 4;

        // EntityBuilder and EntityDestroyer settings
        public const int EntityCallbackFactor = 4;

        // EcsPresenter settings
        public const int InitialPresentersCapacity = 16;
        public const int InitialEntitiesCapacity = 128;

        // EcsLinker settings
        public const int InitialLinkedEntitiesCapacity = 64;

        // EcsSystems settings
        public const int InitialSystemsCapacity = 64;
    }
}

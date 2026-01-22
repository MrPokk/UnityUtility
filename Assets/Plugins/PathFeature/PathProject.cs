namespace BitterECS.Extra
{
    public static class PathProject
    {
#if UNITY_EDITOR || UNITY_2017_1_OR_NEWER
        public static string RootPath { get; set; } = "Resources";
        public static string DataPath { get; set; } = UnityEngine.Application.dataPath;
        public static string ProductName { get; set; } = UnityEngine.Application.productName;
#else
        public static string RootPath { get; set; } = "Resources";
        public static string DataPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
        public static string ProductName { get; set; } = "App";
#endif

        public const string DATA = "Data/";
        public const string UI = DATA + "UI/";
        public const string SETTING = "Settings/";
        public const string ENTITIES = DATA + "Entities/";
        public const string UI_FONTS = UI + "Fonts/";
        public const string UI_PREFABS = UI + "Prefabs/";
        public const string POPUPS = UI + "Core/Popups/";
        public const string ITEM_PREFAB = ENTITIES + "Items/";
        public const string SCREENS = UI + "Core/Screens/";
        public const string GENERAL_PREFAB = ENTITIES + "General/";
        public const string ENEMIES_PREFAB = ENTITIES + "Enemies/";
        public const string PLAYABLE_PREFAB = ENTITIES + "Playables/";
        public const string EFFECTS = GENERAL_PREFAB + "Effects/";
    }
}

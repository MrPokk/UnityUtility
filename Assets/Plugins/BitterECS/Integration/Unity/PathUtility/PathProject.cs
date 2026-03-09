using System;
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

        public const string ENTITIES = "Entities/";
        public const string SETTINGS = "!Settings/";
        public const string UI = "UI/";
        public const string MOBILES = ENTITIES + "Mobiles/";
        public const string PICKUPS = ENTITIES + "Pickups/";
        public const string PREFAB_OBJECTS = ENTITIES + "Prefabs/";
        public const string SKILLS_OBJECTS = PREFAB_OBJECTS + "Skills/";
        public const string GAMEPLAY_SETTING = SETTINGS + "Gameplay/";
        public const string UI_POPUPS = UI + "!Popups/";
        public const string UI_PREFABS = UI + "Prefabs/";
        public const string UI_SCREENS = UI + "!Screens/";
        public const string UI_ICON = UI_PREFABS + "Icon/";
    }
}

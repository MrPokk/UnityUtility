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
        public const string ITEMS = "Items/";
        public const string SETTINGS = "Settings/";
        public const string PREFAB_OBJECTS = ENTITIES + "Prefabs/";
    }
}

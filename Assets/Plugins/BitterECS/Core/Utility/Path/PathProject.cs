namespace BitterECS.Utility
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
        public const string ENTITIES = DATA + "Entities/";
        public const string PREFABS = "Prefabs/";
        public const string SETTING = "Settings/";
        public const string VIEWS = PREFABS + "Views/";
    }
}

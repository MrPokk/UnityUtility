using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BitterECS.Utility
{
    public static class PathUtility
    {
        private static string[] s_cachedPaths = null;
        private static int s_cachedFieldCount = 0;

        public static void GenerationConstPath()
        {
            try
            {
                var allPath = GetAllPaths();
                foreach (var path in allPath)
                {
                    var finalPath = GetFullPath(path);   
                    if (!Directory.Exists(finalPath))
                        Directory.CreateDirectory(finalPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static string[] GetAllPaths()
        {
            if (IsCacheValid())
                return s_cachedPaths;

            s_cachedPaths = GetValidPathsFromFields();
            s_cachedFieldCount = s_cachedPaths.Length;

            return s_cachedPaths;
        }

        private static bool IsCacheValid()
        {
            return s_cachedPaths != null && s_cachedPaths.Length == s_cachedFieldCount;
        }

        private static string[] GetValidPathsFromFields()
        {
            var fields = typeof(PathProject)
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            return fields
                .Where(field => field.FieldType == typeof(string) && field.IsLiteral && !field.IsInitOnly)
                .Select(field => (string)field.GetValue(null))
                .ToArray();
        }

        private static string GetFullPath(string pathBase)
        {
            var allBasePath = GetAllPaths();

            if (!allBasePath.Contains(pathBase))
                throw new ArgumentException($"ERROR: Path not base: {pathBase}");
            
            var fullPath = Path.Combine(
                PathProject.DataPath,
                $"!{PathProject.ProductName}",
                PathProject.RootPath,
                pathBase);

            return fullPath;
        }
    }
}
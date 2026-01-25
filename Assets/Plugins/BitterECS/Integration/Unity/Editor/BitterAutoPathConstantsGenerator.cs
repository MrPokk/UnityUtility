using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using BitterECS.Integration;

namespace BitterECS.Extra.Editor
{
    public static class BitterAutoPathConstantsGenerator
    {
        private const string GENERATED_SCRIPTS_FOLDER = "Assets/GeneratedConstants";

        [MenuItem("BitterECS/Tools/Generate All Path Constants", priority = 0)]
        public static void GenerateAll()
        {
            PathUtility.GenerationConstPath();

            var pathFields = typeof(PathProject)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .ToList();

            if (!Directory.Exists(GENERATED_SCRIPTS_FOLDER))
            {
                Directory.CreateDirectory(GENERATED_SCRIPTS_FOLDER);
            }

            var allDefinedSystemPaths = new List<string>();
            foreach (var field in pathFields)
            {
                var val = (string)field.GetValue(null);
                if (!string.IsNullOrEmpty(val))
                {
                    allDefinedSystemPaths.Add(NormalizePath(ResolveFullPath(val)));
                }
            }

            var generatedCount = 0;

            foreach (var field in pathFields)
            {
                var pathValue = (string)field.GetValue(null);
                var fieldName = field.Name;

                if (string.IsNullOrEmpty(pathValue)) continue;

                GenerateClassForPath(fieldName, pathValue, allDefinedSystemPaths, ref generatedCount);
            }

            if (generatedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[BitterECS]</color> Generated {generatedCount} constant classes in {GENERATED_SCRIPTS_FOLDER}");
            }
            else
            {
                Debug.Log($"[BitterECS] No prefabs found in specified paths.");
            }
        }

        private static void GenerateClassForPath(string fieldName, string relativeResourcePath, List<string> allDefinedPaths, ref int counter)
        {
            var fullSystemPath = ResolveFullPath(relativeResourcePath);

            if (!Directory.Exists(fullSystemPath)) return;

            var normalizedCurrentPath = NormalizePath(fullSystemPath);
            var prefabFiles = Directory.GetFiles(fullSystemPath, "*.prefab", SearchOption.AllDirectories);

            if (prefabFiles.Length == 0) return;

            var validItems = new List<(string constName, string loadPath, string summaryInfo)>();

            foreach (var file in prefabFiles)
            {
                var normalizedFile = NormalizePath(file);

                bool belongsToMoreSpecificPath = allDefinedPaths.Any(otherPath =>
                    otherPath != normalizedCurrentPath &&
                    otherPath.Length > normalizedCurrentPath.Length &&
                    normalizedFile.StartsWith(otherPath)
                );

                if (belongsToMoreSpecificPath) continue;

                var assetPath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null) continue;

                string summaryInfo = null;

                var provider = prefab.GetComponent<ITypedComponentProvider>();
                if (provider != null)
                {
                    summaryInfo = $"Provider: {provider.GetType().Name}";
                }
                else
                {
                    var mb = prefab.GetComponent<MonoBehaviour>();
                    if (mb != null)
                    {
                        summaryInfo = $"Component: {mb.GetType().Name}";
                    }
                }

                if (summaryInfo != null)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var constName = ToUpperSnakeCase(fileName);

                    var fileDirectory = Path.GetDirectoryName(file);
                    var relativeDir = string.Empty;

                    if (fileDirectory != null && fileDirectory.Length >= fullSystemPath.Length)
                    {
                        relativeDir = fileDirectory.Substring(fullSystemPath.Length).Replace('\\', '/').TrimStart('/');
                    }

                    var loadPath = string.IsNullOrEmpty(relativeDir)
                        ? fileName
                        : Path.Combine(relativeResourcePath, relativeDir, fileName).Replace('\\', '/');

                    if (!loadPath.StartsWith(relativeResourcePath))
                    {
                        loadPath = Path.Combine(relativeResourcePath, fileName).Replace('\\', '/');
                    }

                    validItems.Add((constName, loadPath, summaryInfo));
                }
            }

            if (validItems.Count == 0) return;

            var className = ConvertFieldNameToClassName(fieldName) + "Paths";
            WriteScriptFile(className, validItems);
            counter++;
        }

        private static string ResolveFullPath(string relativeResourcePath)
        {
            try
            {
                return PathUtility.GetFullPath(relativeResourcePath);
            }
            catch (Exception)
            {
                return Path.Combine(PathProject.DataPath, $"!{PathProject.ProductName}", PathProject.RootPath, relativeResourcePath);
            }
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').TrimEnd('/');
        }

        private static void WriteScriptFile(string className, List<(string name, string path, string summary)> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("// [AUTO-GENERATED] by BitterECS AutoPathConstantsGenerator");
            sb.AppendLine("// Defines paths for prefabs containing BitterECS Providers or MonoBehaviours");
            sb.AppendLine();
            sb.AppendLine($"public static class {className}");
            sb.AppendLine("{");

            foreach (var item in items)
            {
                sb.AppendLine($"    /// <summary> {item.summary} </summary>");
                sb.AppendLine($"    public const string {item.name} = \"{item.path}\";");
            }

            sb.AppendLine("}");

            var filePath = Path.Combine(GENERATED_SCRIPTS_FOLDER, $"{className}.cs");
            try
            {
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not write {filePath}: {e.Message}");
            }
        }

        private static string ConvertFieldNameToClassName(string constantName)
        {
            var parts = constantName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                sb.Append(char.ToUpper(part[0]));
                if (part.Length > 1) sb.Append(part.Substring(1).ToLower());
            }
            return sb.ToString();
        }

        private static string ToUpperSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var result = new StringBuilder();
            result.Append(char.ToUpper(input[0]));
            for (var i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]) && !char.IsUpper(input[i - 1]) && input[i - 1] != '_')
                {
                    result.Append('_');
                }
                result.Append(char.ToUpper(input[i]));
            }
            return result.ToString().Replace(" ", "_").Replace("-", "_");
        }
    }
}

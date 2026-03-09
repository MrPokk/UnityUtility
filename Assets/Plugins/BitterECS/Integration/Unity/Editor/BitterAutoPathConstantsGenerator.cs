using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using BitterECS.Extra;

namespace BitterECS.Integration.Unity.Editor
{
    public static class BitterAutoPathConstantsGenerator
    {
        private const string GENERATED_SCRIPTS_FOLDER = "Assets/GeneratedConstants"; [MenuItem("BitterECS/Tools/Generate All Path Constants", priority = 0)]
        public static void GenerateAll()
        {
            PathUtility.GenerationConstPath();
            EnsureDirectoryExists(GENERATED_SCRIPTS_FOLDER);

            var pathFields = GetValidPathFields();
            var allDefinedSystemPaths = pathFields
                .Select(f => (string)f.GetValue(null))
                .Where(val => !string.IsNullOrEmpty(val))
                .Select(val => NormalizePath(ResolveFullPath(val)))
                .ToList();

            var generatedCount = 0;

            foreach (var field in pathFields)
            {
                var pathValue = (string)field.GetValue(null);
                if (string.IsNullOrEmpty(pathValue)) continue;

                if (GenerateClassForPath(field.Name, pathValue, allDefinedSystemPaths))
                {
                    generatedCount++;
                }
            }

            if (generatedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[BitterECS]</color> Generated {generatedCount} constant classes in {GENERATED_SCRIPTS_FOLDER}");
            }
            else
            {
                Debug.Log($"[BitterECS] No assets found in specified paths to generate.");
            }
        }

        private static bool GenerateClassForPath(string fieldName, string relativeResourcePath, List<string> allDefinedPaths)
        {
            var fullSystemPath = ResolveFullPath(relativeResourcePath);
            if (!Directory.Exists(fullSystemPath)) return false;

            var normalizedCurrentPath = NormalizePath(fullSystemPath);
            var filesToProcess = GetTargetFiles(fullSystemPath);

            if (filesToProcess.Length == 0) return false;

            var validItems = new List<(string constName, string loadPath, string summaryInfo)>();

            foreach (var file in filesToProcess)
            {
                var normalizedFile = NormalizePath(file);

                if (BelongsToNestedPath(normalizedFile, normalizedCurrentPath, allDefinedPaths))
                    continue;

                var assetPath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                var summaryInfo = ExtractAssetSummary(assetPath);

                if (summaryInfo != null)
                {
                    validItems.Add(CreateConstantEntry(file, fullSystemPath, relativeResourcePath, summaryInfo));
                }
            }

            if (validItems.Count == 0) return false;

            var className = ConvertFieldNameToClassName(fieldName) + "Paths";
            WriteScriptFile(className, validItems);
            return true;
        }

        #region Helper Methods

        private static string[] GetTargetFiles(string directory)
        {
            var prefabs = Directory.GetFiles(directory, "*.prefab", SearchOption.AllDirectories);
            var assets = Directory.GetFiles(directory, "*.asset", SearchOption.AllDirectories);
            return prefabs.Concat(assets).ToArray();
        }

        private static bool BelongsToNestedPath(string normalizedFile, string normalizedCurrentPath, List<string> allDefinedPaths)
        {
            return allDefinedPaths.Any(otherPath =>
                otherPath != normalizedCurrentPath &&
                otherPath.Length > normalizedCurrentPath.Length &&
                normalizedFile.StartsWith(otherPath));
        }

        private static string ExtractAssetSummary(string assetPath)
        {
            if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null) return null;

                var provider = prefab.GetComponent<ITypedComponentProvider>();
                if (provider != null) return $"Provider: {provider.GetType().Name}";

                var mb = prefab.GetComponent<MonoBehaviour>();
                if (mb != null) return $"Component: {mb.GetType().Name}";
            }
            else if (assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (so != null) return $"ScriptableObject: {so.GetType().Name}";
            }
            return null;
        }

        private static (string constName, string loadPath, string summaryInfo) CreateConstantEntry(
            string filePath, string fullSystemPath, string relativeResourcePath, string summaryInfo)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var constName = ToUpperSnakeCase(fileName);
            var fileDirectory = Path.GetDirectoryName(filePath);

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

            loadPath = loadPath.Replace("//", "/");

            return (constName, loadPath, summaryInfo);
        }

        private static List<FieldInfo> GetValidPathFields()
        {
            return typeof(PathProject)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .ToList();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private static string ResolveFullPath(string relativeResourcePath)
        {
            try
            {
                return PathUtility.GetFullPath(relativeResourcePath);
            }
            catch
            {
                return Path.Combine(PathProject.DataPath, $"!{PathProject.ProductName}", PathProject.RootPath, relativeResourcePath);
            }
        }

        private static string NormalizePath(string path) => path.Replace('\\', '/').TrimEnd('/');

        private static void WriteScriptFile(string className, List<(string name, string path, string summary)> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("// [AUTO-GENERATED] by BitterECS AutoPathConstantsGenerator");
            sb.AppendLine("// Defines paths for Prefabs (Providers/MonoBehaviours) and ScriptableObjects");
            sb.AppendLine();
            sb.AppendLine($"public static class {className}");
            sb.AppendLine("{");

            items = items.OrderBy(x => x.name).ThenBy(x => x.path).ToList();

            var nameCounts = new Dictionary<string, int>();
            var processedItems = new List<(string uniqueName, string path, string summary)>();

            foreach (var (name, path, summary) in items)
            {
                var finalName = name;

                if (nameCounts.ContainsKey(name))
                {
                    nameCounts[name]++;
                    finalName = $"{name}_{nameCounts[name]}";
                }
                else
                {
                    nameCounts[name] = 1;
                }

                processedItems.Add((finalName, path, summary));
            }

            foreach (var (uniqueName, path, summary) in processedItems)
            {
                sb.AppendLine($"    /// <summary> {summary} </summary>");
                sb.AppendLine($"    public const string {uniqueName} = \"{path}\";");
            }

            sb.AppendLine();
            sb.AppendLine("    public static readonly string[] AllPaths = new string[]");
            sb.AppendLine("    {");
            foreach (var (uniqueName, path, summary) in processedItems)
            {
                sb.AppendLine($"        {uniqueName},");
            }

            sb.AppendLine("    };");
            sb.AppendLine();
            sb.AppendLine($"    public const int COUNT = {processedItems.Count};");
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
            result.Append(char.IsLetterOrDigit(input[0]) ? char.ToUpper(input[0]) : '_');

            for (var i = 1; i < input.Length; i++)
            {
                var c = input[i];
                var prev = input[i - 1];

                var isCamelCaseTransition = char.IsUpper(c) && !char.IsUpper(prev) && prev != '_';
                var isNumberTransition = char.IsDigit(c) && !char.IsDigit(prev) && prev != '_';

                if (isCamelCaseTransition || isNumberTransition)
                {
                    result.Append('_');
                }

                result.Append(char.IsLetterOrDigit(c) ? char.ToUpper(c) : '_');
            }

            return result.ToString().Replace("__", "_").Trim('_');
        }

        #endregion
    }
}

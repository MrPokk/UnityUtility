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
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            if (!Directory.Exists(GENERATED_SCRIPTS_FOLDER))
            {
                Directory.CreateDirectory(GENERATED_SCRIPTS_FOLDER);
            }

            var generatedCount = 0;

            foreach (var field in pathFields)
            {
                var pathValue = (string)field.GetValue(null);
                var fieldName = field.Name;

                if (string.IsNullOrEmpty(pathValue)) continue;

                GenerateClassForPath(fieldName, pathValue, ref generatedCount);
            }

            if (generatedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[BitterECS]</color> Generated {generatedCount} constant classes in {GENERATED_SCRIPTS_FOLDER}");
            }
            else
            {
                Debug.Log($"[BitterECS] No prefabs with providers found in specified paths.");
            }
        }

        private static void GenerateClassForPath(string fieldName, string relativeResourcePath, ref int counter)
        {
            string fullSystemPath;
            try
            {
                fullSystemPath = PathUtility.GetFullPath(relativeResourcePath);
            }
            catch (Exception)
            {
                fullSystemPath = Path.Combine(PathProject.DataPath, $"!{PathProject.ProductName}", PathProject.RootPath, relativeResourcePath);
            }

            if (!Directory.Exists(fullSystemPath)) return;

            var prefabFiles = Directory.GetFiles(fullSystemPath, "*.prefab", SearchOption.AllDirectories);
            if (prefabFiles.Length == 0) return;

            var validItems = new List<(string constName, string loadPath, string providerType)>();

            foreach (var file in prefabFiles)
            {
                var assetPath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null) continue;

                var component = prefab.GetComponent<ITypedComponentProvider>();

                if (component != null)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);

                    var constName = ToUpperSnakeCase(fileName);

                    var loadPath = Path.Combine(relativeResourcePath, fileName).Replace('\\', '/');

                    validItems.Add((constName, loadPath, component.GetType().Name));
                }
            }

            if (validItems.Count == 0) return;

            var className = ConvertFieldNameToClassName(fieldName) + "Paths";

            WriteScriptFile(className, validItems);
            counter++;
        }

        private static void WriteScriptFile(string className, List<(string name, string path, string type)> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("// [AUTO-GENERATED] by BitterECS AutoPathConstantsGenerator");
            sb.AppendLine("// Defines paths for prefabs containing BitterECS Providers");
            sb.AppendLine();
            sb.AppendLine($"public static class {className}");
            sb.AppendLine("{");

            foreach (var item in items)
            {
                sb.AppendLine($"    /// <summary> Provider: {item.type} </summary>");
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

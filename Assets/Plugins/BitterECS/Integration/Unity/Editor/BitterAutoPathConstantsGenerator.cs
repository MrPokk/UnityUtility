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
            // Убедимся, что пути PathProject актуальны
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
                Debug.Log($"[BitterECS] No assets found in specified paths.");
            }
        }

        private static void GenerateClassForPath(string fieldName, string relativeResourcePath, List<string> allDefinedPaths, ref int counter)
        {
            var fullSystemPath = ResolveFullPath(relativeResourcePath);

            if (!Directory.Exists(fullSystemPath)) return;

            var normalizedCurrentPath = NormalizePath(fullSystemPath);

            // 1. Ищем и префабы, и ассеты (ScriptableObject)
            var prefabFiles = Directory.GetFiles(fullSystemPath, "*.prefab", SearchOption.AllDirectories);
            var assetFiles = Directory.GetFiles(fullSystemPath, "*.asset", SearchOption.AllDirectories);

            var allFiles = prefabFiles.Concat(assetFiles).ToArray();

            if (allFiles.Length == 0) return;

            var validItems = new List<(string constName, string loadPath, string summaryInfo)>();

            foreach (var file in allFiles)
            {
                var normalizedFile = NormalizePath(file);

                // Проверка вложенности путей (чтобы не дублировать константы для вложенных папок, если они определены отдельно)
                bool belongsToMoreSpecificPath = allDefinedPaths.Any(otherPath =>
                    otherPath != normalizedCurrentPath &&
                    otherPath.Length > normalizedCurrentPath.Length &&
                    normalizedFile.StartsWith(otherPath)
                );

                if (belongsToMoreSpecificPath) continue;

                var assetPath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                string summaryInfo = null;

                // 2. Логика разделения по типу файла
                if (file.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab == null) continue;

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
                }
                else if (file.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    // Загружаем как ScriptableObject
                    var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                    if (so != null)
                    {
                        summaryInfo = $"ScriptableObject: {so.GetType().Name}";
                    }
                }

                // 3. Если информация найдена, добавляем в список генерации
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

                    // Формируем путь загрузки (без расширения)
                    var loadPath = string.IsNullOrEmpty(relativeDir)
                        ? fileName
                        : Path.Combine(relativeResourcePath, relativeDir, fileName).Replace('\\', '/');

                    // Корректировка пути, если он не начинается с базового пути (защита от лишних слэшей)
                    if (!loadPath.StartsWith(relativeResourcePath))
                    {
                        loadPath = Path.Combine(relativeResourcePath, fileName).Replace('\\', '/');
                    }

                    // Убираем двойные слэши если возникли
                    loadPath = loadPath.Replace("//", "/");

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
                // Fallback, если PathUtility недоступен или выдал ошибку
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
            sb.AppendLine("// Defines paths for Prefabs (Providers/MonoBehaviours) and ScriptableObjects");
            sb.AppendLine();
            sb.AppendLine($"public static class {className}");
            sb.AppendLine("{");

            // Сортируем для красоты
            items = items.OrderBy(x => x.name).ToList();

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

            // Обработка первого символа
            if (char.IsLetterOrDigit(input[0]))
                result.Append(char.ToUpper(input[0]));
            else
                result.Append('_');

            for (var i = 1; i < input.Length; i++)
            {
                char c = input[i];
                char prev = input[i - 1];

                // Вставляем подчеркивание перед заглавной буквой, если предыдущая буква строчная
                // или если это начало последовательности цифр
                if ((char.IsUpper(c) && !char.IsUpper(prev) && prev != '_') ||
                    (char.IsDigit(c) && !char.IsDigit(prev) && prev != '_'))
                {
                    result.Append('_');
                }

                if (char.IsLetterOrDigit(c))
                    result.Append(char.ToUpper(c));
                else
                    result.Append('_');
            }

            return result.ToString().Replace(" ", "_").Replace("-", "_").Replace("__", "_");
        }
    }
}
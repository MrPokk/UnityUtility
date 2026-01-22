using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BitterECS.Extra.Editor
{
    public abstract class AbstractConstantsGenerator
    {
        protected static void GenerateConstants(
            string resourcesPath,
            string className,
            string generatorScriptFileName,
            Type requiredComponent = null)
        {
            var fullSystemPath = PathUtility.GetFullPath(resourcesPath);

            if (!Directory.Exists(fullSystemPath))
            {
                Debug.LogError($"[Generator] Folder not found: {fullSystemPath}");
                return;
            }

            var prefabFiles = Directory.GetFiles(fullSystemPath, "*.prefab", SearchOption.AllDirectories);
            var validItems = new List<(string constantName, string resourcePath)>();

            foreach (var file in prefabFiles)
            {
                var assetPath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab != null)
                {
                    if (requiredComponent == null || prefab.GetComponent(requiredComponent) != null)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var finalResourcePath = Path.Combine(resourcesPath, fileName).Replace('\\', '/');

                        if (finalResourcePath.StartsWith("/")) finalResourcePath = finalResourcePath.Substring(1);

                        var constantName = fileName.Replace(" ", "_").ToUpper();

                        if (!validItems.Exists(x => x.constantName == constantName))
                        {
                            validItems.Add((constantName, finalResourcePath));
                        }
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("// [AUTO] Generation by AbstractConstantsGenerator");
            sb.AppendLine($"public static class {className}");
            sb.AppendLine("{");

            foreach (var (name, path) in validItems)
            {
                sb.AppendLine($"    public const string {name} = \"{path}\";");
            }

            sb.AppendLine("}");

            var guids = AssetDatabase.FindAssets($"{generatorScriptFileName} t:Script");
            if (guids.Length > 0)
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var directoryPath = Path.GetDirectoryName(scriptPath);
                var fullOutputPath = Path.Combine(directoryPath, $"{className}.cs");

                try
                {
                    File.WriteAllText(fullOutputPath, sb.ToString());
                    Debug.Log($"Generated {className} at {fullOutputPath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to write file: {e.Message}");
                }

                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogError($"Could not find generator script file: {generatorScriptFileName}");
            }
        }
    }
}

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitterECS.Utility
{
    public class PathProjectEditor : EditorWindow
    {
        [Serializable]
        class PathNode
        {
            public string Path;
            [NonSerialized] public List<PathNode> Children = new();
            public bool Expanded;
            public PathNode(string path) => Path = path;
        }

        [SerializeField] PathNode _rootNode;
        static Texture2D _folderIcon, _folderOpenedIcon;

        [MenuItem("Tools/BitterECS/Path Project Settings")]
        static void ShowWindow() => GetWindow<PathProjectEditor>("Path Project Settings").minSize = new Vector2(300, 400);

        void OnEnable()
        {
            LoadIcons();
            RefreshPaths();
        }

        void LoadIcons()
        {
            try
            {
                _folderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
                _folderOpenedIcon = EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D;
            }
            catch
            {
                _folderIcon = CreateColorTexture(Color.blue);
                _folderOpenedIcon = CreateColorTexture(Color.green);
            }
        }

        Texture2D CreateColorTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        void RefreshPaths()
        {
            var paths = typeof(PathProject)
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(string) && f.IsLiteral && !f.IsInitOnly)
                .Select(f => (string)f.GetValue(null))
                .Where(p => !string.IsNullOrEmpty(p));

            _rootNode = new PathNode("ROOT");
            BuildTree(_rootNode, paths);
        }

        void BuildTree(PathNode parent, IEnumerable<string> paths)
        {
            var groups = paths
                .Select(p => p.Split('/', StringSplitOptions.RemoveEmptyEntries))
                .GroupBy(parts => parts[0]);

            foreach (var group in groups.OrderBy(g => g.Key))
            {
                var node = new PathNode(group.Key);
                parent.Children.Add(node);

                var subPaths = group
                    .Where(parts => parts.Length > 1)
                    .Select(parts => string.Join("/", parts, 1, parts.Length - 1));

                if (subPaths.Any()) BuildTree(node, subPaths);
            }
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Path Settings", EditorStyles.boldLabel);

            PathProject.RootPath = EditorGUILayout.TextField("Root Path", PathProject.RootPath);
            PathProject.DataPath = EditorGUILayout.TextField("Data Path", PathProject.DataPath);
            PathProject.ProductName = EditorGUILayout.TextField("Product Name", PathProject.ProductName);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Path Hierarchy", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (_rootNode != null) DrawNode(_rootNode, 0);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Generate All Paths"))
            {
                PathUtility.GenerationConstPath();
                AssetDatabase.Refresh();
                Debug.Log("Paths generated successfully!");
            }
        }

        void DrawNode(PathNode node, int indent)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 15);

            var icon = node.Expanded ? _folderOpenedIcon : _folderIcon;
            var content = new GUIContent(node.Path, icon);

            if (node.Children.Count > 0)
                node.Expanded = EditorGUILayout.Foldout(node.Expanded, content);
            else
                EditorGUILayout.LabelField(content);

            EditorGUILayout.EndHorizontal();

            if (!node.Expanded || node.Children.Count == 0) return;

            foreach (var child in node.Children)
                DrawNode(child, indent + 1);
        }
    }
}
#endif
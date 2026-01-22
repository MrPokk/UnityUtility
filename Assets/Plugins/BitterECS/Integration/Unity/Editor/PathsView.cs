#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BitterECS.Extra;

namespace BitterECS.Editor
{
    public class PathsView
    {
        private EditorWindow _owner;
        [Serializable] class PathNode { public string path; public List<PathNode> children = new(); public bool expanded; public PathNode(string path) => this.path = path; }
        [Serializable]
        class PathDefinition
        {
            public string Name; public string Suffix; public string ParentName;
            public string GetFinalValue(List<PathDefinition> allDefs)
            {
                if (string.IsNullOrEmpty(ParentName) || ParentName == "None") return Suffix;
                var parent = allDefs.FirstOrDefault(x => x.Name == ParentName);
                return parent != null ? parent.GetFinalValue(allDefs) + Suffix : Suffix;
            }
        }

        private PathNode _rootNode;
        private List<PathDefinition> _pathDefinitions = new();
        private Vector2 _scrollPos;

        public PathsView(EditorWindow owner) => _owner = owner;

        public void OnEnable()
        {
            LoadCurrentConstants();
            RefreshTree();
        }

        public void Draw()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            using (new EditorGUILayout.VerticalScope(BitterStyle.Card))
            {
                GUILayout.Label("Core Configuration", BitterStyle.HeaderLabel);
                GUILayout.Space(5);

                EditorGUI.BeginChangeCheck();
                PathProject.RootPath = EditorGUILayout.TextField("Root Path", PathProject.RootPath);

                EditorGUILayout.BeginHorizontal();
                PathProject.DataPath = EditorGUILayout.TextField("Data Path", PathProject.DataPath);
                if (GUILayout.Button(BitterStyle.IconFolder, GUILayout.Width(30), GUILayout.Height(19)))
                {
                    string rawPath = EditorUtility.OpenFolderPanel("Select Data Path", PathProject.DataPath, "");
                    if (!string.IsNullOrEmpty(rawPath)) { PathProject.DataPath = rawPath; GUI.FocusControl(null); }
                }
                if (GUILayout.Button(new GUIContent("R", "Reset to Assets"), GUILayout.Width(25), GUILayout.Height(19)))
                {
                    PathProject.DataPath = Application.dataPath; GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();

                PathProject.ProductName = EditorGUILayout.TextField("Product Name", PathProject.ProductName);
                if (EditorGUI.EndChangeCheck()) { }
            }

            GUILayout.Space(10);

            // Section 2: Definitions Table
            using (new EditorGUILayout.VerticalScope(BitterStyle.Card))
            {
                GUILayout.Label("Constants Definition", BitterStyle.HeaderLabel);
                GUILayout.Space(5);

                // Table Header
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("Const Name", GUILayout.Width(140));
                    GUILayout.Label("Parent", GUILayout.Width(100));
                    GUILayout.Label("Value / Suffix");
                    GUILayout.Space(30); // for delete button
                }

                for (int i = 0; i < _pathDefinitions.Count; i++) DrawDefinitionRow(i);

                GUILayout.Space(5);
                if (GUILayout.Button("+ Add New Path Constant", GUILayout.Height(24)))
                    _pathDefinitions.Add(new PathDefinition { Name = "NEW_PATH", ParentName = "None", Suffix = "New/" });
            }

            GUILayout.Space(10);

            // Section 3: Preview & Actions
            using (new EditorGUILayout.VerticalScope(BitterStyle.Card))
            {
                GUILayout.Label("Hierarchy Preview", BitterStyle.HeaderLabel);
                GUILayout.Space(5);
                using (new EditorGUILayout.VerticalScope(BitterStyle.Container))
                {
                    if (_rootNode != null) DrawNode(_rootNode, 0);
                    else GUILayout.Label("No paths defined", EditorStyles.centeredGreyMiniLabel);
                }

                GUILayout.Space(10);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Refresh Preview", GUILayout.Height(30))) RefreshTree();
                    if (GUILayout.Button("Create Directories on Disk", GUILayout.Height(30)))
                    {
                        PathUtility.GenerationConstPath();
                        AssetDatabase.Refresh();
                        EditorUtility.DisplayDialog("Success", "Directories generated!", "OK");
                    }
                }

                GUILayout.Space(5);
                GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
                if (GUILayout.Button("SAVE & GENERATE SCRIPT", GUILayout.Height(35))) GenerateScript();
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
        }

        private void LoadCurrentConstants()
        {
            _pathDefinitions.Clear();
            var rawFields = typeof(PathProject).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => new { Name = f.Name, Value = (string)f.GetValue(null) })
                .OrderBy(x => x.Value.Length).ToList();

            foreach (var field in rawFields)
            {
                var def = new PathDefinition { Name = field.Name };
                var possibleParent = _pathDefinitions
                    .Where(p => field.Value.StartsWith(p.GetFinalValue(_pathDefinitions)) && field.Value != p.GetFinalValue(_pathDefinitions))
                    .OrderByDescending(p => p.GetFinalValue(_pathDefinitions).Length).FirstOrDefault();

                if (possibleParent != null) { def.ParentName = possibleParent.Name; def.Suffix = field.Value.Substring(possibleParent.GetFinalValue(_pathDefinitions).Length); }
                else { def.ParentName = "None"; def.Suffix = field.Value; }
                _pathDefinitions.Add(def);
            }
        }

        private void RefreshTree()
        {
            var paths = _pathDefinitions.Select(p => p.GetFinalValue(_pathDefinitions)).Where(p => !string.IsNullOrEmpty(p));
            _rootNode = new PathNode("ROOT");
            BuildTree(_rootNode, paths);
        }

        private void BuildTree(PathNode parent, IEnumerable<string> paths)
        {
            var groups = paths.Select(p => p.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)).GroupBy(parts => parts[0]);
            foreach (var group in groups.OrderBy(g => g.Key))
            {
                var node = new PathNode(group.Key);
                parent.children.Add(node);
                var subPaths = group.Where(parts => parts.Length > 1).Select(parts => string.Join("/", parts, 1, parts.Length - 1));
                if (subPaths.Any()) BuildTree(node, subPaths);
            }
        }

        private void DrawDefinitionRow(int index)
        {
            var def = _pathDefinitions[index];
            using (new EditorGUILayout.HorizontalScope())
            {
                string newName = EditorGUILayout.TextField(def.Name, GUILayout.Width(140));
                def.Name = Regex.Replace(newName, @"[^a-zA-Z0-9_]", "").ToUpper();

                var options = new List<string> { "None" };
                options.AddRange(_pathDefinitions.Where(x => x != def).Select(x => x.Name));
                int currentIndex = options.IndexOf(def.ParentName);
                if (currentIndex == -1) currentIndex = 0;
                int newIndex = EditorGUILayout.Popup(currentIndex, options.ToArray(), GUILayout.Width(100));
                def.ParentName = options[newIndex];

                def.Suffix = EditorGUILayout.TextField(def.Suffix);

                if (GUILayout.Button("X", GUILayout.Width(25))) { _pathDefinitions.RemoveAt(index); GUIUtility.ExitGUI(); }
            }
        }

        private void DrawNode(PathNode node, int indent)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(indent * 15);
                var icon = node.expanded ? BitterStyle.IconFolderOpen : BitterStyle.IconFolder;
                var content = new GUIContent($" {node.path}", icon.image);

                if (node.children.Count > 0)
                    node.expanded = EditorGUILayout.Foldout(node.expanded, content, true);
                else
                    GUILayout.Label(content, GUILayout.Height(20));
            }

            if (node.expanded && node.children.Count > 0)
            {
                foreach (var child in node.children) DrawNode(child, indent + 1);
            }
        }

        private void GenerateScript()
        {
            string scriptPath = FindPathProjectScript();
            if (string.IsNullOrEmpty(scriptPath)) { Debug.LogError("Could not find PathProject.cs"); return; }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("namespace BitterECS.Extra");
            sb.AppendLine("{");
            sb.AppendLine("    public static class PathProject");
            sb.AppendLine("    {");
            sb.AppendLine("#if UNITY_EDITOR || UNITY_2017_1_OR_NEWER");
            sb.AppendLine($"        public static string RootPath {{ get; set; }} = \"{PathProject.RootPath}\";");
            sb.AppendLine("        public static string DataPath { get; set; } = UnityEngine.Application.dataPath;");
            sb.AppendLine("        public static string ProductName { get; set; } = UnityEngine.Application.productName;");
            sb.AppendLine("#else");
            sb.AppendLine($"        public static string RootPath {{ get; set; }} = \"{PathProject.RootPath}\";");
            sb.AppendLine("        public static string DataPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;");
            sb.AppendLine("        public static string ProductName { get; set; } = \"App\";");
            sb.AppendLine("#endif");
            sb.AppendLine("");
            foreach (var def in _pathDefinitions)
            {
                sb.Append($"        public const string {def.Name} = ");
                if (def.ParentName == "None" || string.IsNullOrEmpty(def.ParentName)) sb.AppendLine($"\"{def.Suffix}\";");
                else sb.AppendLine($"{def.ParentName} + \"{def.Suffix}\";");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");
            try { File.WriteAllText(scriptPath, sb.ToString()); AssetDatabase.Refresh(); Debug.Log("PathProject.cs updated."); }
            catch (Exception e) { Debug.LogError($"Failed to write script: {e.Message}"); }
        }

        private string FindPathProjectScript()
        {
            string[] guids = AssetDatabase.FindAssets("PathProject t:MonoScript");
            return guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
        }
    }
}
#endif

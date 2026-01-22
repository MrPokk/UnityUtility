#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BitterECS.Extra;

namespace BitterECS.Integration.Editor
{
    public class PathProjectEditor : EditorWindow
    {
        [Serializable]
        class PathNode
        {
            public string path;
            [NonSerialized] public List<PathNode> children = new();
            public bool expanded;
            public PathNode(string path) => this.path = path;
        }

        [Serializable]
        class PathDefinition
        {
            public string Name;
            public string Suffix;
            public string ParentName; // The name of the const this depends on, or "None"

            public string GetFinalValue(List<PathDefinition> allDefs)
            {
                if (string.IsNullOrEmpty(ParentName) || ParentName == "None")
                    return Suffix;

                var parent = allDefs.FirstOrDefault(x => x.Name == ParentName);
                return parent != null ? parent.GetFinalValue(allDefs) + Suffix : Suffix;
            }
        }

        [SerializeField] PathNode _rootNode;
        [SerializeField] List<PathDefinition> _pathDefinitions = new();

        static Texture2D _folderIcon, _folderOpenedIcon;
        Vector2 _scrollPos;

        [MenuItem("BitterECS/Tools/Path Project Settings")]
        static void ShowWindow()
        {
            GetWindow<PathProjectEditor>("Path Project Settings").minSize = new Vector2(400, 600);
        }

        void OnEnable()
        {
            LoadIcons();
            LoadCurrentConstants();
            RefreshTree();
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

        /// <summary>
        /// Reflection magic to load existing constants and try to guess their hierarchy
        /// </summary>
        void LoadCurrentConstants()
        {
            _pathDefinitions.Clear();
            var rawFields = typeof(PathProject)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => new { Name = f.Name, Value = (string)f.GetValue(null) })
                .OrderBy(x => x.Value.Length) // Process shortest paths first
                .ToList();

            foreach (var field in rawFields)
            {
                var def = new PathDefinition { Name = field.Name };

                // Try to find the best matching parent (longest matching prefix)
                var possibleParent = _pathDefinitions
                    .Where(p => field.Value.StartsWith(p.GetFinalValue(_pathDefinitions)) && field.Value != p.GetFinalValue(_pathDefinitions))
                    .OrderByDescending(p => p.GetFinalValue(_pathDefinitions).Length)
                    .FirstOrDefault();

                if (possibleParent != null)
                {
                    def.ParentName = possibleParent.Name;
                    def.Suffix = field.Value.Substring(possibleParent.GetFinalValue(_pathDefinitions).Length);
                }
                else
                {
                    def.ParentName = "None";
                    def.Suffix = field.Value;
                }

                _pathDefinitions.Add(def);
            }
        }

        void RefreshTree()
        {
            // Build tree from the current Definitions list, not the class, so it updates live
            var paths = _pathDefinitions.Select(p => p.GetFinalValue(_pathDefinitions)).Where(p => !string.IsNullOrEmpty(p));
            _rootNode = new PathNode("ROOT");
            BuildTree(_rootNode, paths);
        }

        void BuildTree(PathNode parent, IEnumerable<string> paths)
        {
            var groups = paths
                .Select(p => p.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                .GroupBy(parts => parts[0]);

            foreach (var group in groups.OrderBy(g => g.Key))
            {
                var node = new PathNode(group.Key);
                parent.children.Add(node);

                var subPaths = group
                    .Where(parts => parts.Length > 1)
                    .Select(parts => string.Join("/", parts, 1, parts.Length - 1));

                if (subPaths.Any()) BuildTree(node, subPaths);
            }
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            DrawPathEditor();
            DrawActionButtons();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hierarchy Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (_rootNode != null) DrawNode(_rootNode, 0);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Path Configuration", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            // Root Path
            PathProject.RootPath = EditorGUILayout.TextField("Root Path", PathProject.RootPath);

            // Data Path with Selection Buttons
            EditorGUILayout.BeginHorizontal();
            PathProject.DataPath = EditorGUILayout.TextField("Data Path", PathProject.DataPath);

            // Button: Select Folder
            var folderIconContent = EditorGUIUtility.IconContent("Folder Icon");
            folderIconContent.tooltip = "Select Data Path Folder";
            if (GUILayout.Button(folderIconContent, GUILayout.Width(30), GUILayout.Height(19)))
            {
                string rawPath = EditorUtility.OpenFolderPanel("Select Data Path", PathProject.DataPath, "");
                if (!string.IsNullOrEmpty(rawPath))
                {
                    PathProject.DataPath = rawPath;
                    GUI.FocusControl(null); // Unfocus to refresh text field
                }
            }

            if (GUILayout.Button(new GUIContent("R", "Reset to Application.dataPath"), GUILayout.Width(25), GUILayout.Height(19)))
            {
                PathProject.DataPath = Application.dataPath;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            PathProject.ProductName = EditorGUILayout.TextField("Product Name", PathProject.ProductName);

            if (EditorGUI.EndChangeCheck())
            { }
        }

        void DrawPathEditor()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Constants Editor", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Define constants here. Use 'Parent' to chain paths (e.g., ENTITIES = DATA + \"Entities/\")", MessageType.Info);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header Row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name (Const)", EditorStyles.miniBoldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("Parent", EditorStyles.miniBoldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("Suffix / Value", EditorStyles.miniBoldLabel);
            GUILayout.Space(30);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < _pathDefinitions.Count; i++)
            {
                DrawDefinitionRow(i);
            }

            if (GUILayout.Button("+ Add New Path Constant"))
            {
                _pathDefinitions.Add(new PathDefinition { Name = "NEW_PATH", ParentName = "None", Suffix = "New/" });
            }

            EditorGUILayout.EndVertical();
        }

        void DrawDefinitionRow(int index)
        {
            var def = _pathDefinitions[index];

            EditorGUILayout.BeginHorizontal();

            // Name Field
            string newName = EditorGUILayout.TextField(def.Name, GUILayout.Width(120));
            // Simple validation to keep it uppercase/underscore
            def.Name = Regex.Replace(newName, @"[^a-zA-Z0-9_]", "").ToUpper();

            // Parent Dropdown
            var options = new List<string> { "None" };
            options.AddRange(_pathDefinitions.Where(x => x != def).Select(x => x.Name)); // Prevent self-reference

            int currentIndex = options.IndexOf(def.ParentName);
            if (currentIndex == -1) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(currentIndex, options.ToArray(), GUILayout.Width(100));
            def.ParentName = options[newIndex];

            // Suffix Field
            def.Suffix = EditorGUILayout.TextField(def.Suffix);

            // Remove Button
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                _pathDefinitions.RemoveAt(index);
                // Clean up children that pointed to this
                foreach (var d in _pathDefinitions.Where(d => d.ParentName == def.Name)) d.ParentName = "None";
                GUIUtility.ExitGUI(); // Prevent layout errors
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawActionButtons()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh Preview", GUILayout.Height(30)))
            {
                RefreshTree();
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("SAVE & GENERATE SCRIPT", GUILayout.Height(30)))
            {
                GenerateScript();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Directories on Disk"))
            {
                PathUtility.GenerationConstPath();
                AssetDatabase.Refresh();
                Debug.Log("Directories generated successfully!");
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawNode(PathNode node, int indent)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 15);

            var icon = node.expanded ? _folderOpenedIcon : _folderIcon;
            var content = new GUIContent(node.path, icon);

            if (node.children.Count > 0)
                node.expanded = EditorGUILayout.Foldout(node.expanded, content);
            else
                EditorGUILayout.LabelField(content);

            EditorGUILayout.EndHorizontal();

            if (!node.expanded || node.children.Count == 0) return;

            foreach (var child in node.children)
                DrawNode(child, indent + 1);
        }

        // --------------------------------------------------------------------------------
        // Code Generation Logic
        // --------------------------------------------------------------------------------

        void GenerateScript()
        {
            string scriptPath = FindPathProjectScript();
            if (string.IsNullOrEmpty(scriptPath))
            {
                Debug.LogError("Could not find PathProject.cs in the project.");
                return;
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("namespace BitterECS.Extra");
            sb.AppendLine("{");
            sb.AppendLine("    public static class PathProject");
            sb.AppendLine("    {");

            // Static Config Properties (Preserving the #if logic from original)
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

            // Constants
            foreach (var def in _pathDefinitions)
            {
                sb.Append($"        public const string {def.Name} = ");
                if (def.ParentName == "None" || string.IsNullOrEmpty(def.ParentName))
                {
                    sb.AppendLine($"\"{def.Suffix}\";");
                }
                else
                {
                    sb.AppendLine($"{def.ParentName} + \"{def.Suffix}\";");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            try
            {
                File.WriteAllText(scriptPath, sb.ToString());
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>PathProject.cs successfully updated at {scriptPath}</color>");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write script: {e.Message}");
            }
        }

        string FindPathProjectScript()
        {
            string[] guids = AssetDatabase.FindAssets("PathProject t:MonoScript");
            if (guids.Length == 0) return null;
            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }
    }
}
#endif

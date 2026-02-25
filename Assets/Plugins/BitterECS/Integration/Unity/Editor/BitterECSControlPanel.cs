#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BitterECS.Integration;
using BitterECS.Extra;
using BitterECS.Core;
using BitterECS.Extra.Editor;

namespace BitterECS.Editor
{
    public class EntitiesView
    {
        private BitterECSControlPanel _owner;
        private string TargetPath
        {
            get
            {
                try { return $"Assets/!{PathProject.ProductName}/{PathProject.RootPath}/{PathProject.ENTITIES}".Replace("//", "/").Replace("\\", "/"); }
                catch { return "Assets/"; }
            }
        }

        private enum SortMode { Name, ComponentCount, Subfolder }

        [Serializable]
        private class PrefabData
        {
            public string Name;
            public string Path;
            public string Subfolder;
            public int ComponentCount;
            public GameObject Asset;
        }

        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        private SortMode _currentSortMode = SortMode.Subfolder;
        private bool _sortAscending = true;

        private List<PrefabData> _allPrefabs = new();
        private List<PrefabData> _displayedPrefabs = new();
        private List<Type> _availableProviderTypes = new();

        private Dictionary<string, bool> _entityFoldouts = new();
        private Dictionary<string, bool> _componentFoldouts = new();
        private Dictionary<int, SerializedObject> _serializedObjects = new();

        public EntitiesView(BitterECSControlPanel owner) => _owner = owner;

        public void OnEnable()
        {
            _entityFoldouts.Clear();
            if (_owner.expandedEntityKeys != null)
                foreach (var key in _owner.expandedEntityKeys) _entityFoldouts[key] = true;

            _componentFoldouts.Clear();
            if (_owner.expandedComponentKeys != null)
                foreach (var key in _owner.expandedComponentKeys) _componentFoldouts[key] = true;

            RefreshData();
        }

        // Вызывается из главного окна перед сериализацией
        public void SaveState()
        {
            if (_owner == null) return;

            _owner.expandedEntityKeys.Clear();
            foreach (var kvp in _entityFoldouts)
                if (kvp.Value) _owner.expandedEntityKeys.Add(kvp.Key);

            _owner.expandedComponentKeys.Clear();
            foreach (var kvp in _componentFoldouts)
                if (kvp.Value) _owner.expandedComponentKeys.Add(kvp.Key);
        }

        public void Draw()
        {
            DrawToolbar();

            if (_displayedPrefabs.Count == 0 && _allPrefabs.Count == 0)
            {
                DrawEmptyState();
                return;
            }

            DrawPrefabList();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(BitterStyle.Toolbar))
            {
                if (GUILayout.Button(BitterStyle.IconRefresh, BitterStyle.ToolbarButton, GUILayout.Width(30)))
                    RefreshData();

                var newSearch = EditorGUILayout.TextField(_searchFilter, BitterStyle.SearchField, GUILayout.ExpandWidth(true));
                if (newSearch != _searchFilter)
                {
                    _searchFilter = newSearch;
                    ApplySortAndFilter();
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("FilterByLabel"), BitterStyle.ToolbarButton, GUILayout.Width(30)))
                    ShowSortMenu();

                GUILayout.Label($"{_displayedPrefabs.Count} Items", BitterStyle.SubHeaderLabel, GUILayout.MaxWidth(50));
            }
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("No entities found in:", style);
            GUILayout.Label(TargetPath, EditorStyles.centeredGreyMiniLabel);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Force Refresh", GUILayout.Width(120))) RefreshData();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
        }

        private void DrawPrefabList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.Space(5);

            string currentSubfolder = null;
            foreach (var entry in _displayedPrefabs)
            {
                if (_currentSortMode == SortMode.Subfolder && entry.Subfolder != currentSubfolder)
                {
                    currentSubfolder = entry.Subfolder;
                    GUILayout.Label(currentSubfolder.ToUpper(), BitterStyle.SubHeaderLabel);
                    EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
                    GUILayout.Space(3);
                }
                DrawEntityCard(entry);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntityCard(PrefabData entry)
        {
            if (!_entityFoldouts.ContainsKey(entry.Path)) _entityFoldouts[entry.Path] = false;
            var isExpanded = _entityFoldouts[entry.Path];

            using (new EditorGUILayout.VerticalScope(BitterStyle.Card))
            {
                var headerRect = EditorGUILayout.GetControlRect(false, 24);

                if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0) { _entityFoldouts[entry.Path] = !isExpanded; Event.current.Use(); }
                    else if (Event.current.button == 1)
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Ping Asset"), false, () => EditorGUIUtility.PingObject(entry.Asset));
                        menu.AddItem(new GUIContent("Select Asset"), false, () => Selection.activeObject = entry.Asset);
                        menu.ShowAsContext();
                        Event.current.Use();
                    }
                }

                if (Event.current.type == EventType.Repaint)
                {
                    var iconRect = new Rect(headerRect.x, headerRect.y + 4, 16, 16);
                    var icon = AssetDatabase.GetCachedIcon(entry.Path);
                    GUI.DrawTexture(iconRect, icon ? icon : BitterStyle.IconPrefab.image);

                    if (entry.ComponentCount == 0)
                    {
                        var warnRect = new Rect(headerRect.xMax - 20, headerRect.y + 4, 16, 16);
                        GUI.DrawTexture(warnRect, BitterStyle.IconWarn.image);
                    }

                    var arrowRect = new Rect(headerRect.xMax - 50, headerRect.y + 4, 16, 16);
                    EditorStyles.foldout.Draw(arrowRect, false, false, isExpanded, false);
                }

                var labelRect = new Rect(headerRect.x + 24, headerRect.y, headerRect.width - 80, headerRect.height);
                var displayName = _currentSortMode == SortMode.Subfolder ? entry.Name : $"{entry.Subfolder} / {entry.Name}";

                var orgColor = GUI.color;
                if (entry.ComponentCount == 0) GUI.color = new Color(1, 1, 1, 0.5f);
                GUI.Label(labelRect, displayName, EditorStyles.boldLabel);
                GUI.color = orgColor;

                if (entry.ComponentCount > 0)
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        var badgeRect = new Rect(headerRect.xMax - 30, headerRect.y + 4, 30, 16);
                        BitterStyle.MiniBadge.Draw(badgeRect, new GUIContent(entry.ComponentCount.ToString()), false, false, false, false);
                    }
                }

                if (isExpanded)
                {
                    GUILayout.Space(5);
                    EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0, 0, 0, 0.1f)); // Separator
                    GUILayout.Space(5);
                    DrawCardContent(entry.Asset, entry.Path);
                }
            }
        }

        private void DrawCardContent(GameObject prefab, string assetPath)
        {
            if (prefab == null) return;
            if (prefab.GetComponent<ProviderEcs>() == null)
            {
                EditorGUILayout.HelpBox("Missing Root ProviderEcs!", MessageType.Error);
                if (GUILayout.Button("Fix Now")) AddComponentSafe(prefab, typeof(ProviderEcs));
                EditorGUILayout.Space(5);
            }

            var providers = prefab.GetComponents<ITypedComponentProvider>();
            foreach (var provider in providers) DrawCleanInspector(provider as MonoBehaviour, prefab, assetPath);

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Asset", EditorStyles.miniButton))
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
                GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
                if (GUILayout.Button("+ Add Component", EditorStyles.miniButton)) ShowAddMenu(prefab);
                GUI.backgroundColor = Color.white;
            }
        }

        private void DrawCleanInspector(MonoBehaviour component, GameObject prefab, string assetPath)
        {
            if (component == null) return;

            var persistentKey = $"{assetPath}::{component.GetType().FullName}";
            var runtimeId = component.GetInstanceID();

            if (!_componentFoldouts.ContainsKey(persistentKey)) _componentFoldouts[persistentKey] = false;

            if (!_serializedObjects.TryGetValue(runtimeId, out var so) || so.targetObject == null)
            {
                so = new SerializedObject(component);
                _serializedObjects[runtimeId] = so;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                so.Update();
                EditorGUI.BeginChangeCheck();

                var isExpanded = _componentFoldouts[persistentKey];

                isExpanded = EditorGUILayout.InspectorTitlebar(isExpanded, component);
                _componentFoldouts[persistentKey] = isExpanded;

                if (isExpanded)
                {
                    using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = new RectOffset(12, 5, 5, 5) }))
                    {
                        var prop = so.GetIterator();
                        var enterChildren = true;
                        var hasProps = false;
                        while (prop.NextVisible(enterChildren))
                        {
                            if (prop.name != "m_Script")
                            {
                                EditorGUILayout.PropertyField(prop, true);
                                hasProps = true;
                            }
                            enterChildren = false;
                        }
                        if (!hasProps) EditorGUILayout.HelpBox("Empty Component", MessageType.None);
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(component);
                    EditorUtility.SetDirty(prefab);
                }
            }
            GUILayout.Space(2);
        }

        private void RefreshData()
        {
            _allPrefabs.Clear();
            _serializedObjects.Clear();
            var sysPath = TargetPath;
            if (Directory.Exists(sysPath) || AssetDatabase.IsValidFolder(sysPath))
            {
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { sysPath.TrimEnd('/') });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (asset != null)
                    {
                        var dir = Path.GetDirectoryName(path).Replace("\\", "/");
                        var folderName = dir.Replace(sysPath, "").Trim('/');
                        if (string.IsNullOrEmpty(folderName)) folderName = "Root";

                        _allPrefabs.Add(new PrefabData
                        {
                            Name = asset.name,
                            Path = path,
                            Subfolder = folderName,
                            ComponentCount = asset.GetComponents<ITypedComponentProvider>().Length,
                            Asset = asset
                        });
                    }
                }
            }
            _availableProviderTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ITypedComponentProvider).IsAssignableFrom(t))
                .ToList();
            ApplySortAndFilter();
        }

        private void ApplySortAndFilter()
        {
            var query = _allPrefabs.AsEnumerable();
            if (!string.IsNullOrEmpty(_searchFilter))
                query = query.Where(p => p.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase));

            var orderedQuery = query.OrderBy(p => p.ComponentCount == 0);
            switch (_currentSortMode)
            {
                case SortMode.Name: orderedQuery = _sortAscending ? orderedQuery.ThenBy(p => p.Name) : orderedQuery.ThenByDescending(p => p.Name); break;
                case SortMode.ComponentCount: orderedQuery = _sortAscending ? orderedQuery.ThenBy(p => p.ComponentCount) : orderedQuery.ThenByDescending(p => p.ComponentCount); break;
                case SortMode.Subfolder: orderedQuery = _sortAscending ? orderedQuery.ThenBy(p => p.Subfolder).ThenBy(p => p.Name) : orderedQuery.ThenByDescending(p => p.Subfolder).ThenByDescending(p => p.Name); break;
            }
            _displayedPrefabs = orderedQuery.ToList();
        }

        private void ShowSortMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Name"), _currentSortMode == SortMode.Name, () => SetSort(SortMode.Name));
            menu.AddItem(new GUIContent("Subfolder"), _currentSortMode == SortMode.Subfolder, () => SetSort(SortMode.Subfolder));
            menu.AddItem(new GUIContent("Count"), _currentSortMode == SortMode.ComponentCount, () => SetSort(SortMode.ComponentCount));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Ascending"), _sortAscending, () => { _sortAscending = true; ApplySortAndFilter(); });
            menu.AddItem(new GUIContent("Descending"), !_sortAscending, () => { _sortAscending = false; ApplySortAndFilter(); });
            menu.ShowAsContext();
        }

        private void SetSort(SortMode mode) { _currentSortMode = mode; ApplySortAndFilter(); }

        private void ShowAddMenu(GameObject prefab)
        {
            var menu = new GenericMenu();

            var collection = prefab.GetComponents<ITypedComponentProvider>().Select(c => c.GetType());
            var existing = new HashSet<Type>(collection);

            var sortedTypes = _availableProviderTypes.OrderBy(t => t.Name);

            foreach (var type in sortedTypes)
            {
                if (existing.Contains(type)) continue;

                if (type.IsAbstract) continue;

                if (type.IsGenericTypeDefinition) continue;

                menu.AddItem(new GUIContent(type.Name), false, () => AddComponentSafe(prefab, type));
            }

            if (menu.GetItemCount() == 0)
                menu.AddDisabledItem(new GUIContent("No available components"));

            menu.ShowAsContext();
        }

        private void AddComponentSafe(GameObject prefabAsset, Type type)
        {
            Undo.AddComponent(prefabAsset, type);
            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();
            RefreshData();
            if (!_entityFoldouts.ContainsKey(AssetDatabase.GetAssetPath(prefabAsset)))
                _entityFoldouts[AssetDatabase.GetAssetPath(prefabAsset)] = true;
        }
    }

    public class PathsView
    {
        private BitterECSControlPanel _owner;

        [Serializable]
        class PathNode
        {
            public string path;
            public string fullPath;
            public List<PathNode> children = new();
            public bool expanded;
            public PathNode(string path, string parentPath)
            {
                this.path = path;
                this.fullPath = string.IsNullOrEmpty(parentPath) ? path : parentPath + "/" + path;
            }
        }

        [Serializable]
        class PathDefinition
        {
            public string Name;
            public string Suffix;
            public string ParentName;

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

        public PathsView(BitterECSControlPanel owner) => _owner = owner;

        public void OnEnable()
        {
            LoadCurrentConstants();
            RefreshTree();
        }

        public void SaveState()
        {
            if (_owner == null) return;
            _owner.expandedPathKeys.Clear();
            if (_rootNode != null)
                CollectExpandedNodes(_rootNode, _owner.expandedPathKeys);
        }

        private void CollectExpandedNodes(PathNode node, List<string> expandedList)
        {
            if (node.expanded) expandedList.Add(node.fullPath);
            foreach (var child in node.children) CollectExpandedNodes(child, expandedList);
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
                    var rawPath = EditorUtility.OpenFolderPanel("Select Data Path", PathProject.DataPath, "");
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

            using (new EditorGUILayout.VerticalScope(BitterStyle.Card))
            {
                GUILayout.Label("Constants Definition", BitterStyle.HeaderLabel);
                GUILayout.Space(5);

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("Const Name", GUILayout.Width(140));
                    GUILayout.Label("Parent", GUILayout.Width(100));
                    GUILayout.Label("Value / Suffix");
                    GUILayout.Space(30);
                }

                for (var i = 0; i < _pathDefinitions.Count; i++) DrawDefinitionRow(i);

                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Add New", GUILayout.Height(24)))
                {
                    _pathDefinitions.Add(new PathDefinition { Name = "NEW_PATH", ParentName = "None", Suffix = "New/" });
                    SortDefinitions();
                }
                if (GUILayout.Button("Scan & Import Missing Folders", GUILayout.Height(24)))
                {
                    ScanAndAddMissingFolders();
                }
                EditorGUILayout.EndHorizontal();
            }

            using (new EditorGUILayout.VerticalScope(BitterStyle.Card))
            {
                GUILayout.Label("Generated Constants Prefabs", BitterStyle.HeaderLabel);
                GUILayout.Space(5);
                if (GUILayout.Button("Create Paths Prefab", GUILayout.Height(24)))
                {
                    BitterAutoPathConstantsGenerator.GenerateAll();
                    AssetDatabase.Refresh();
                }
            }

            GUILayout.Space(10);

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
                    if (GUILayout.Button("Refresh Preview", GUILayout.Height(30)))
                    {
                        SortDefinitions();
                        RefreshTree();
                    }
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

        private void ScanAndAddMissingFolders()
        {
            string baseSearchPath = Application.dataPath;
            if (!string.IsNullOrEmpty(PathProject.RootPath))
            {
                baseSearchPath = Path.Combine(Application.dataPath, PathProject.RootPath).Replace("\\", "/");
            }

            if (!Directory.Exists(baseSearchPath))
            {
                Debug.LogWarning($"[PathsView] Root directory does not exist: {baseSearchPath}");
                return;
            }

            var existingPaths = _pathDefinitions
                .Select(p => p.GetFinalValue(_pathDefinitions).Trim('/'))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToHashSet();

            string[] allDirectories = Directory.GetDirectories(baseSearchPath, "*", SearchOption.AllDirectories);
            int addedCount = 0;

            foreach (var realDir in allDirectories)
            {
                string normalizedRealDir = realDir.Replace("\\", "/");

                string relativeToRoot = normalizedRealDir.Substring(baseSearchPath.Length).Trim('/');

                if (string.IsNullOrEmpty(relativeToRoot)) continue;

                if (!existingPaths.Contains(relativeToRoot))
                {
                    AddNewPathDefinitionFromPath(relativeToRoot);
                    addedCount++;

                    existingPaths.Add(relativeToRoot);
                }
            }

            if (addedCount > 0)
            {
                SortDefinitions();
                RefreshTree();
                Debug.Log($"[PathsView] Added {addedCount} new folder definitions from Root.");
            }
        }

        private void AddNewPathDefinitionFromPath(string relativeToRootPath)
        {
            string parentName = "None";
            string suffix = relativeToRootPath + "/";

            var potentialParents = _pathDefinitions
                .Select(p => new { Name = p.Name, FullVal = p.GetFinalValue(_pathDefinitions) })
                .Where(p => !string.IsNullOrEmpty(p.FullVal))
                .OrderByDescending(p => p.FullVal.Length);

            foreach (var parent in potentialParents)
            {
                if (suffix.StartsWith(parent.FullVal) && suffix != parent.FullVal)
                {
                    parentName = parent.Name;
                    suffix = suffix.Substring(parent.FullVal.Length);
                    break;
                }
            }

            string folderName = Path.GetFileName(relativeToRootPath.TrimEnd('/'));
            string constName = Regex.Replace(folderName, @"[^a-zA-Z0-9_]", "").ToUpper() + "_PATH";

            int counter = 1;
            string originalName = constName;
            while (_pathDefinitions.Any(p => p.Name == constName))
            {
                constName = originalName + "_" + counter;
                counter++;
            }

            _pathDefinitions.Add(new PathDefinition
            {
                Name = constName,
                ParentName = parentName,
                Suffix = suffix
            });
        }

        private void LoadCurrentConstants()
        {
            _pathDefinitions.Clear();
            var rawFields = typeof(PathProject).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => new { f.Name, Value = (string)f.GetValue(null) })
                .OrderBy(x => x.Value.Length).ToList();

            foreach (var field in rawFields)
            {
                var normalizedName = Regex.Replace(field.Name, @"[^a-zA-Z0-9_]", "").ToUpper();
                var def = new PathDefinition { Name = normalizedName };

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
            SortDefinitions();
        }

        private void SortDefinitions()
        {
            _pathDefinitions = _pathDefinitions
                .OrderBy(x => x.ParentName == "None" ? 0 : 1)
                .ThenBy(x => x.ParentName)
                .ThenBy(x => x.Name)
                .ToList();
        }

        private void RefreshTree()
        {
            var paths = _pathDefinitions.Select(p => p.GetFinalValue(_pathDefinitions)).Where(p => !string.IsNullOrEmpty(p));
            _rootNode = new PathNode("ROOT", "");
            if (_owner.expandedPathKeys.Contains(_rootNode.fullPath)) _rootNode.expanded = true;
            BuildTree(_rootNode, paths);
        }

        private void BuildTree(PathNode parent, IEnumerable<string> paths)
        {
            var groups = paths.Select(p => p.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)).GroupBy(parts => parts[0]);
            foreach (var group in groups.OrderBy(g => g.Key))
            {
                var node = new PathNode(group.Key, parent.fullPath);
                if (_owner.expandedPathKeys.Contains(node.fullPath)) node.expanded = true;
                parent.children.Add(node);
                var subPaths = group.Where(parts => parts.Length > 1).Select(parts => string.Join("/", parts, 1, parts.Length - 1));
                if (subPaths.Any()) BuildTree(node, subPaths);
            }
        }

        private void DrawDefinitionRow(int index)
        {
            if (index >= _pathDefinitions.Count) return;
            var def = _pathDefinitions[index];
            using (new EditorGUILayout.HorizontalScope())
            {
                var newName = EditorGUILayout.TextField(def.Name, GUILayout.Width(140));
                def.Name = Regex.Replace(newName, @"[^a-zA-Z0-9_]", "").ToUpper();

                var options = new List<string> { "None" };
                options.AddRange(_pathDefinitions.Where(x => x != def).Select(x => x.Name).OrderBy(n => n));
                var currentIndex = options.IndexOf(def.ParentName);
                if (currentIndex == -1) currentIndex = 0;

                EditorGUI.BeginChangeCheck();
                var newIndex = EditorGUILayout.Popup(currentIndex, options.ToArray(), GUILayout.Width(100));
                if (EditorGUI.EndChangeCheck()) def.ParentName = options[newIndex];

                def.Suffix = EditorGUILayout.TextField(def.Suffix);

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    _pathDefinitions.RemoveAt(index);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawNode(PathNode node, int indent)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(indent * 15);
                var icon = node.expanded ? BitterStyle.IconFolderOpen : BitterStyle.IconFolder;
                var content = new GUIContent($" {node.path}", icon.image);
                if (node.children.Count > 0) node.expanded = EditorGUILayout.Foldout(node.expanded, content, true);
                else GUILayout.Label(content, GUILayout.Height(20));
            }
            if (node.expanded && node.children.Count > 0)
            {
                foreach (var child in node.children) DrawNode(child, indent + 1);
            }
        }

        private void GenerateScript()
        {
            var scriptPath = FindPathProjectScript();
            if (string.IsNullOrEmpty(scriptPath)) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
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

            SortDefinitions();
            foreach (var def in _pathDefinitions)
            {
                sb.Append($"        public const string {def.Name} = ");
                if (def.ParentName == "None" || string.IsNullOrEmpty(def.ParentName))
                    sb.AppendLine($"\"{def.Suffix}\";");
                else
                    sb.AppendLine($"{def.ParentName} + \"{def.Suffix}\";");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");
            File.WriteAllText(scriptPath, sb.ToString());
            AssetDatabase.Refresh();
        }

        private string FindPathProjectScript()
        {
            var guids = AssetDatabase.FindAssets("PathProject t:MonoScript");
            return guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
        }
    }

    public class SystemsView
    {
        private readonly BitterECSControlPanel _owner;
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _lastRefreshTime;
        private const float REFRESH_RATE = 0.5f;

        private string _searchFilter = "";
        private bool _showAllPriorities = true;
        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

        public SystemsView(BitterECSControlPanel owner) => _owner = owner;

        public void OnEnable()
        {
            if (_owner.expandedSystemKeys != null && _owner.expandedSystemKeys.Count > 0)
            {
                foreach (var key in _owner.expandedSystemKeys) _foldouts[key] = true;
            }
            else
            {
                foreach (Priority p in Enum.GetValues(typeof(Priority)))
                    _foldouts[p.ToString()] = true;
            }
        }

        public void SaveState()
        {
            if (_owner == null) return;
            _owner.expandedSystemKeys.Clear();
            foreach (var kvp in _foldouts)
            {
                if (kvp.Value) _owner.expandedSystemKeys.Add(kvp.Key);
            }
        }

        public void Update()
        {
            if (Application.isPlaying && _autoRefresh &&
                Time.realtimeSinceStartup - _lastRefreshTime > REFRESH_RATE)
            {
                _owner.Repaint();
                _lastRefreshTime = Time.realtimeSinceStartup;
            }
        }

        public void Draw()
        {
            DrawHeader();
            DrawSystemList();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(BitterStyle.Toolbar))
            {
                var systems = EcsReflectionHelper.GetSystems();
                GUILayout.Label($"Active: {systems.Count}", BitterStyle.SubHeaderLabel);

                GUILayout.FlexibleSpace();

                _autoRefresh = GUILayout.Toggle(_autoRefresh, BitterStyle.IconAutoRefresh, BitterStyle.ToolbarButton, GUILayout.Width(30));
                if (GUILayout.Button(BitterStyle.IconRefresh, BitterStyle.ToolbarButton, GUILayout.Width(30)))
                    _lastRefreshTime = Time.realtimeSinceStartup;

                GUILayout.Space(10);

                // Search Bar
                _searchFilter = EditorGUILayout.TextField(_searchFilter, BitterStyle.SearchField, GUILayout.Width(200));
            }

            // Sub-toolbar for filters
            using (new EditorGUILayout.HorizontalScope())
            {
                _showAllPriorities = EditorGUILayout.ToggleLeft("Show Empty Priorities", _showAllPriorities, GUILayout.Width(150));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Expand All", EditorStyles.miniButtonLeft)) ExpandCollapseAll(true);
                if (GUILayout.Button("Collapse All", EditorStyles.miniButtonRight)) ExpandCollapseAll(false);
            }
        }

        private void DrawSystemList()
        {
            if (!Application.isPlaying)
            {
                DrawEmptyState("Enter Play Mode to inspect systems.");
                return;
            }

            var allSystems = EcsReflectionHelper.GetSystems();
            if (allSystems.Count == 0)
            {
                DrawEmptyState("No active systems found.");
                return;
            }

            var filtered = allSystems
                .Where(s => string.IsNullOrEmpty(_searchFilter) ||
                           s.GetType().Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                .GroupBy(s => s.Priority)
                .OrderBy(g => (int)g.Key);

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;
                EditorGUILayout.Space(5);
                foreach (var group in filtered)
                {
                    if (!_showAllPriorities && (group.Key == Priority.FIRST_TASK || group.Key == Priority.LAST_TASK)) continue;
                    DrawPriorityGroup(group.Key, group.ToList());
                }
            }
        }

        private void DrawEmptyState(string msg)
        {
            GUILayout.FlexibleSpace();
            var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, normal = { textColor = Color.gray } };
            GUILayout.Label(msg, style);
            GUILayout.FlexibleSpace();
        }

        private void DrawPriorityGroup(Priority priority, List<IEcsSystem> systems)
        {
            var key = priority.ToString();
            if (!_foldouts.ContainsKey(key)) _foldouts[key] = true;

            // Header for Priority
            Rect headerRect = EditorGUILayout.GetControlRect(false, 24);
            EditorGUI.DrawRect(headerRect, GetPriorityColor(priority) * 0.3f); // Subtle BG

            // Triangle & Label
            var foldoutRect = new Rect(headerRect.x + 5, headerRect.y, headerRect.width - 5, headerRect.height);
            _foldouts[key] = EditorGUI.Foldout(foldoutRect, _foldouts[key], $"{priority} ({systems.Count})", true, EditorStyles.boldLabel);

            if (_foldouts[key])
            {
                foreach (var system in systems) DrawSystemItem(system);
                GUILayout.Space(4); // Gap between groups
            }
        }

        private void DrawSystemItem(IEcsSystem system)
        {
            var type = system.GetType();

            using (new EditorGUILayout.VerticalScope(BitterStyle.Card))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var interfaceType = type.GetInterfaces().FirstOrDefault();
                    var icon = GetSystemIcon(interfaceType);

                    if (icon != null) GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                    else GUILayout.Space(20);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Label(type.Name, EditorStyles.boldLabel);
                        GUILayout.Label(type.Namespace ?? "No Namespace", EditorStyles.miniLabel);
                    }

                    GUILayout.FlexibleSpace();

                    if (interfaceType != null) DrawInterfaceBadge(interfaceType);
                }
            }
        }

        private void DrawInterfaceBadge(Type type)
        {
            var color = GetInterfaceColor(type);
            var name = type.Name.Replace("IEcs", "");
            BitterStyle.DrawBadge(name, color);
        }

        private void ExpandCollapseAll(bool expand)
        {
            var keys = _foldouts.Keys.ToList();
            foreach (var key in keys) _foldouts[key] = expand;
        }

        private Color GetPriorityColor(Priority p) => p switch
        {
            Priority.High => new Color(1f, 0.4f, 0.2f),
            Priority.Medium => new Color(1f, 0.9f, 0.3f),
            Priority.Low => new Color(0.4f, 0.9f, 0.4f),
            Priority.FIRST_TASK => new Color(1f, 0.3f, 0.3f),
            Priority.LAST_TASK => new Color(0.3f, 0.5f, 1f),
            _ => Color.white
        };

        private Color GetInterfaceColor(Type t) => t?.Name switch
        {
            "IEcsInitSystem" => new Color(0.5f, 1f, 0.6f),
            "IEcsRunSystem" => new Color(0.6f, 0.8f, 1f),
            "IEcsDestroySystem" => new Color(1f, 0.5f, 0.5f),
            "IEcsFixedRunSystem" => new Color(1f, 0.8f, 0.4f),
            _ => Color.white
        };

        private Texture2D GetSystemIcon(Type t)
        {
            var name = t?.Name switch
            {
                "IEcsInitSystem" => "d_PlayButton",
                "IEcsRunSystem" => "d_Animation.Play",
                "IEcsDestroySystem" => "d_winbtn_win_close",
                "IEcsFixedRunSystem" => "d_Animation.FixedFrame",
                _ => "cs Script Icon"
            };
            return EditorGUIUtility.IconContent(name).image as Texture2D;
        }
    }

    public class BitterECSControlPanel : EditorWindow, ISerializationCallbackReceiver
    {
        private enum Tab { Entities, Systems, Settings }

        [SerializeField] private Tab _currentTab = Tab.Systems;

        public List<string> expandedEntityKeys = new();
        public List<string> expandedComponentKeys = new();
        public List<string> expandedSystemKeys = new();
        public List<string> expandedPathKeys = new();

        private EntitiesView _entitiesView;
        private SystemsView _systemsView;
        private PathsView _pathsView;

        [MenuItem("BitterECS/Control Panel", priority = 0)]
        public static void OpenWindow()
        {
            var window = GetWindow<BitterECSControlPanel>("BitterECS");
            window.minSize = new Vector2(500, 600);
        }

        private void OnEnable()
        {
            _entitiesView = new EntitiesView(this);
            _systemsView = new SystemsView(this);
            _pathsView = new PathsView(this);

            _entitiesView.OnEnable();
            _systemsView.OnEnable();
            _pathsView.OnEnable();
        }

        public void OnBeforeSerialize()
        {
            _entitiesView?.SaveState();
            _systemsView?.SaveState();
            _pathsView?.SaveState();
        }

        public void OnAfterDeserialize()
        {
        }

        private void OnGUI()
        {
            BitterStyle.Init();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BitterStyle.MainBgColor);
            DrawToolbar();
            GUILayout.Space(5);

            using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = new RectOffset(5, 5, 0, 5) }))
            {
                DrawContent();
            }
        }

        private void Update()
        {
            if (_currentTab == Tab.Systems) _systemsView.Update();
        }

        private void DrawToolbar()
        {
            GUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                string[] tabNames = { " Entities Manager ", " Systems Diagnostic ", " Path Settings " };
                _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, tabNames, GUILayout.Height(30), GUILayout.MinWidth(300));
                GUILayout.FlexibleSpace();
            }

            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0, 0, 0, 0.3f));
        }

        private void DrawContent()
        {
            switch (_currentTab)
            {
                case Tab.Systems: _systemsView.Draw(); break;
                case Tab.Entities: _entitiesView.Draw(); break;
                case Tab.Settings: _pathsView.Draw(); break;
            }
        }
    }

    public static class EcsReflectionHelper
    {
        private static FieldInfo _systemsField;
        public static IReadOnlyCollection<IEcsSystem> GetSystems()
        {
            if (!Application.isPlaying || EcsSystems.Instance == null) return Array.Empty<IEcsSystem>();
            if (_systemsField == null) _systemsField = typeof(EcsSystems).GetField("_systems", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_systemsField == null) return Array.Empty<IEcsSystem>();
            var set = _systemsField.GetValue(EcsSystems.Instance) as SortedSet<IEcsSystem>;
            return set ?? (IReadOnlyCollection<IEcsSystem>)Array.Empty<IEcsSystem>();
        }
        public static int GetSystemsCount() => GetSystems().Count;
    }

    [InitializeOnLoad]
    public static class EcsHierarchyOverlay
    {
        private static bool _isListening;
        private static GUIStyle _badgeStyle;

        static EcsHierarchyOverlay()
        {
            EditorApplication.playModeStateChanged -= OnPlayStateChanged;
            EditorApplication.playModeStateChanged += OnPlayStateChanged;
            if (Application.isPlaying) StartListening();
        }

        private static void OnPlayStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) StartListening();
            else if (state == PlayModeStateChange.ExitingPlayMode) StopListening();
        }

        private static void StartListening()
        {
            if (_isListening) return;
            EditorApplication.hierarchyWindowItemOnGUI += DrawOverlay;
            _isListening = true;
        }

        private static void StopListening()
        {
            if (!_isListening) return;
            EditorApplication.hierarchyWindowItemOnGUI -= DrawOverlay;
            _isListening = false;
        }

        private static void DrawOverlay(int instanceID, Rect rect)
        {
#if UNITY_6000_3_OR_NEWER
            var gameObject = EditorUtility.EntityIdToObject(instanceID) as GameObject;
#else
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
#endif
            if (gameObject == null || gameObject.name != "ECS Systems") return;
            var count = EcsReflectionHelper.GetSystemsCount();
            if (count <= 0) return;
            if (_badgeStyle == null) _badgeStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } };
            var labelRect = new Rect(rect);
            labelRect.xMax -= 20;
            GUI.Label(labelRect, $"{count} systems", _badgeStyle);
        }
    }
}
#endif

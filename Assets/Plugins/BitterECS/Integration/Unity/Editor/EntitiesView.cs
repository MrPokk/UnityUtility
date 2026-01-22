#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BitterECS.Integration;
using BitterECS.Extra;

namespace BitterECS.Editor
{
    public class EntitiesView
    {
        private BitterECSControlPanel _owner;
        private string TargetPath => $"Assets/!{PathProject.ProductName}/{PathProject.RootPath}/{PathProject.ENTITIES}".Replace("//", "/").Replace("\\", "/");

        private enum SortMode { Name, ComponentCount, Subfolder }
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

        public void OnBeforeSerialize()
        {
            if (_owner == null) return;
            _owner.expandedEntityKeys.Clear();
            foreach (var kvp in _entityFoldouts) if (kvp.Value) _owner.expandedEntityKeys.Add(kvp.Key);
            _owner.expandedComponentKeys.Clear();
            foreach (var kvp in _componentFoldouts) if (kvp.Value) _owner.expandedComponentKeys.Add(kvp.Key);
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
            if (GUILayout.Button("Force Refresh", GUILayout.Width(120))) RefreshData();
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
                    Rect iconRect = new Rect(headerRect.x, headerRect.y + 4, 16, 16);
                    var icon = AssetDatabase.GetCachedIcon(entry.Path);
                    GUI.DrawTexture(iconRect, icon ? icon : BitterStyle.IconPrefab.image);

                    if (entry.ComponentCount == 0)
                    {
                        Rect warnRect = new Rect(headerRect.xMax - 20, headerRect.y + 4, 16, 16);
                        GUI.DrawTexture(warnRect, BitterStyle.IconWarn.image);
                    }

                    Rect arrowRect = new Rect(headerRect.xMax - 50, headerRect.y + 4, 16, 16);
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
                        Rect badgeRect = new Rect(headerRect.xMax - 30, headerRect.y + 4, 30, 16);
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
            if (prefab.GetComponent<MonoProvider>() == null)
            {
                EditorGUILayout.HelpBox("Missing Root MonoProvider!", MessageType.Error);
                if (GUILayout.Button("Fix Now")) AddComponentSafe(prefab, typeof(MonoProvider));
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

                bool isExpanded = _componentFoldouts[persistentKey];

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
            _availableProviderTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ITypedComponentProvider).IsAssignableFrom(t)).ToList();
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
            GenericMenu menu = new GenericMenu();
            var existing = new HashSet<Type>(prefab.GetComponents<ITypedComponentProvider>().Select(c => c.GetType()));
            var sortedTypes = _availableProviderTypes.OrderBy(t => t.Name);
            foreach (var type in sortedTypes)
            {
                if (existing.Contains(type)) continue;
                menu.AddItem(new GUIContent(type.Name), false, () => AddComponentSafe(prefab, type));
            }
            if (menu.GetItemCount() == 0) menu.AddDisabledItem(new GUIContent("No available components"));
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
}
#endif

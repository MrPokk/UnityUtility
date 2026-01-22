using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BitterECS.Core;
using BitterECS.Integration;
using BitterECS.Extra;

namespace BitterECS.Editor
{
    // 1. Добавляем интерфейс ISerializationCallbackReceiver
    public class BitterECSEntitiesEditor : EditorWindow, ISerializationCallbackReceiver
    {
        // --- Configuration ---
        private string TargetPath => $"Assets/!{PathProject.ProductName}/{PathProject.RootPath}/{PathProject.ENTITIES}"
                                     .Replace("//", "/").Replace("\\", "/");

        // --- Data Types ---
        private enum SortMode { Name, ComponentCount, Subfolder }

        private class PrefabData
        {
            public string Name;
            public string Path;
            public string Subfolder;
            public int ComponentCount;
            public GameObject Asset;
        }

        // --- Visual Styles & Constants ---
        private static class UI
        {
            public static Color HeaderBackground => EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.85f, 0.85f, 0.85f);
            public static Color CardBackground => EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.9f, 0.9f, 0.9f);
            public static Color WindowBackground => EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.78f, 0.78f, 0.78f);
            public static Color AccentColor => new Color(0.23f, 0.58f, 0.95f); // Unity Blue
            public static Color WarningColor => new Color(0.95f, 0.7f, 0.2f);

            public static GUIStyle CardStyle;
            public static GUIStyle FolderHeaderStyle;
            public static GUIStyle EntityTitleStyle;
            public static GUIStyle ComponentBadgeStyle;

            public static void Init()
            {
                if (CardStyle != null) return;

                CardStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(4, 4, 4, 8),
                    normal = { background = MakeTex(1, 1, CardBackground) }
                };

                FolderHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 11,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.6f, 0.6f, 0.6f) : Color.gray },
                    margin = new RectOffset(8, 0, 10, 4),
                    alignment = TextAnchor.LowerLeft
                };

                EntityTitleStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : Color.black }
                };

                ComponentBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                    padding = new RectOffset(4, 4, 0, 0)
                };
            }

            private static Texture2D MakeTex(int width, int height, Color col)
            {
                var pix = new Color[width * height];
                for (int i = 0; i < pix.Length; i++) pix[i] = col;
                var result = new Texture2D(width, height);
                result.SetPixels(pix);
                result.Apply();
                return result;
            }
        }

        // --- State ---
        private Vector2 _scrollPosition;
        private string _searchFilter = "";

        private SortMode _currentSortMode = SortMode.Name;
        private bool _sortAscending = true;

        private List<PrefabData> _allPrefabs = new List<PrefabData>();
        private List<PrefabData> _displayedPrefabs = new List<PrefabData>();
        private List<Type> _availableProviderTypes = new List<Type>();

        // --- Serialization State ---
        // Словари для быстрого доступа в Runtime (OnGUI)
        private Dictionary<string, bool> _entityFoldouts = new Dictionary<string, bool>();
        // Изменили ключ компонентов с int на string, чтобы он был стабильным
        private Dictionary<string, bool> _componentFoldouts = new Dictionary<string, bool>();
        private Dictionary<int, SerializedObject> _serializedObjects = new Dictionary<int, SerializedObject>();

        // Списки для сохранения данных Unity (Unity умеет сериализовать List, но не Dictionary)
        [SerializeField] private List<string> _expandedEntityKeys = new List<string>();
        [SerializeField] private List<string> _expandedComponentKeys = new List<string>();

        // --- Serialization Implementation ---

        // Вызывается перед тем, как Unity сохранит состояние окна (компиляция, ctrl+s и т.д.)
        public void OnBeforeSerialize()
        {
            _expandedEntityKeys.Clear();
            foreach (var kvp in _entityFoldouts)
            {
                if (kvp.Value) _expandedEntityKeys.Add(kvp.Key);
            }

            _expandedComponentKeys.Clear();
            foreach (var kvp in _componentFoldouts)
            {
                if (kvp.Value) _expandedComponentKeys.Add(kvp.Key);
            }
        }

        // Вызывается после того, как Unity восстановит состояние окна
        public void OnAfterDeserialize()
        {
            _entityFoldouts = new Dictionary<string, bool>();
            foreach (var key in _expandedEntityKeys) _entityFoldouts[key] = true;

            _componentFoldouts = new Dictionary<string, bool>();
            foreach (var key in _expandedComponentKeys) _componentFoldouts[key] = true;
        }

        [MenuItem("BitterECS/Entities Manager")]
        public static void ShowWindow()
        {
            var w = GetWindow<BitterECSEntitiesEditor>("Entities");
            w.titleContent = new GUIContent(" Entities", EditorGUIUtility.IconContent("d_Prefab Icon").image);
            w.minSize = new Vector2(350, 500);
        }

        private void OnEnable() => RefreshData();

        private void OnGUI()
        {
            UI.Init();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), UI.WindowBackground);
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
            Rect toolbarRect = EditorGUILayout.GetControlRect(false, 32);
            GUILayout.Space(-32);

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(toolbarRect, UI.HeaderBackground);
                EditorGUI.DrawRect(new Rect(0, toolbarRect.height - 1, toolbarRect.width, 1), new Color(0, 0, 0, 0.2f));
            }

            EditorGUILayout.BeginHorizontal(new GUIStyle { padding = new RectOffset(8, 8, 6, 6) });

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_TreeEditor.Refresh"), EditorStyles.iconButton, GUILayout.Width(24), GUILayout.Height(20)))
            {
                RefreshData();
            }

            GUILayout.Space(6);

            var searchRect = EditorGUILayout.GetControlRect(false, 20, GUILayout.MinWidth(100), GUILayout.MaxWidth(400));
            string newSearch = EditorGUI.TextField(searchRect, _searchFilter, EditorStyles.toolbarSearchField);
            if (newSearch != _searchFilter)
            {
                _searchFilter = newSearch;
                ApplySortAndFilter();
            }

            if (!string.IsNullOrEmpty(_searchFilter))
            {
                Rect clearBtnRect = new Rect(searchRect.xMax - 20, searchRect.y, 20, 20);
                GUIStyle cancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.miniButton;
                if (GUI.Button(clearBtnRect, GUIContent.none, cancelStyle))
                {
                    _searchFilter = "";
                    GUI.FocusControl(null);
                    ApplySortAndFilter();
                }
            }

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            GUILayout.Space(2);
            GUILayout.Label($"{_displayedPrefabs.Count} Items", EditorStyles.miniLabel);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            if (GUILayout.Button(EditorGUIUtility.IconContent("FilterByLabel"), EditorStyles.iconButton, GUILayout.Width(24), GUILayout.Height(20)))
            {
                ShowSortMenu();
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 14 };
            GUILayout.Label($"No entities found.", style);
            GUILayout.Label(TargetPath, EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Force Refresh", GUILayout.Width(120), GUILayout.Height(30))) RefreshData();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void DrawPrefabList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });

            string currentSubfolder = null;

            foreach (var entry in _displayedPrefabs)
            {
                if (_currentSortMode == SortMode.Subfolder && entry.Subfolder != currentSubfolder)
                {
                    currentSubfolder = entry.Subfolder;
                    EditorGUILayout.LabelField(currentSubfolder.ToUpper(), UI.FolderHeaderStyle);
                }

                DrawEntityCard(entry);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawEntityCard(PrefabData entry)
        {
            if (!_entityFoldouts.ContainsKey(entry.Path)) _entityFoldouts[entry.Path] = false;
            bool isExpanded = _entityFoldouts[entry.Path];

            EditorGUILayout.BeginVertical(UI.CardStyle);

            Rect headerRect = EditorGUILayout.GetControlRect(false, 32);
            bool isHover = headerRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.MouseDown && isHover)
            {
                if (Event.current.button == 0)
                {
                    _entityFoldouts[entry.Path] = !isExpanded;
                    Event.current.Use();
                }
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
                if (isHover)
                    EditorGUI.DrawRect(headerRect, EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.05f) : new Color(0, 0, 0, 0.05f));

                Rect stripeRect = new Rect(headerRect.x, headerRect.y, 4, headerRect.height);
                EditorGUI.DrawRect(stripeRect, entry.ComponentCount > 0 ? UI.AccentColor : Color.gray);

                Rect contentRect = new Rect(headerRect.x + 12, headerRect.y, headerRect.width - 12, headerRect.height);
                Rect arrowRect = new Rect(contentRect.x, contentRect.y + 8, 16, 16);
                Rect iconRect = new Rect(contentRect.x + 16, contentRect.y + 8, 16, 16);

                EditorStyles.foldout.Draw(arrowRect, false, false, isExpanded, false);

                var icon = AssetDatabase.GetCachedIcon(entry.Path);
                GUI.DrawTexture(iconRect, icon ? icon : EditorGUIUtility.IconContent("d_Prefab Icon").image);
            }

            Rect textContentRect = new Rect(headerRect.x + 12, headerRect.y, headerRect.width - 12, headerRect.height);
            Rect labelRect = new Rect(textContentRect.x + 40, textContentRect.y, textContentRect.width - 90, textContentRect.height);
            string displayName = _currentSortMode == SortMode.Subfolder ? entry.Name : $"{entry.Subfolder} / {entry.Name}";

            Color orgColor = GUI.color;
            if (entry.ComponentCount == 0) GUI.color = new Color(1, 1, 1, 0.5f);

            GUI.Label(labelRect, displayName, UI.EntityTitleStyle);
            GUI.color = orgColor;

            if (Event.current.type == EventType.Repaint)
            {
                if (entry.ComponentCount > 0)
                {
                    Rect badgeRect = new Rect(headerRect.xMax - 30, headerRect.y + 8, 24, 16);
                    UI.ComponentBadgeStyle.Draw(badgeRect, entry.ComponentCount.ToString(), false, false, false, false);
                }
                else
                {
                    Rect warnRect = new Rect(headerRect.xMax - 25, headerRect.y + 8, 16, 16);
                    GUI.DrawTexture(warnRect, EditorGUIUtility.IconContent("console.warnicon.sml").image);
                }
            }

            if (isExpanded)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1, headerRect.width, 1), new Color(0, 0, 0, 0.1f));

                EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });
                // Передаем путь для генерации стабильных ключей
                DrawCardContent(entry.Asset, entry.Path);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
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

            foreach (var provider in providers)
            {
                DrawCleanInspector(provider as MonoBehaviour, prefab, assetPath);
            }

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select Asset", EditorStyles.miniButton, GUILayout.Height(24)))
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            GUILayout.Space(5);

            GUI.backgroundColor = new Color(0.9f, 1f, 0.9f);
            if (GUILayout.Button("+ Add Component", EditorStyles.miniButton, GUILayout.Height(24)))
            {
                ShowAddMenu(prefab);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCleanInspector(MonoBehaviour component, GameObject prefab, string assetPath)
        {
            if (component == null) return;

            // Генерируем стабильный ID на основе пути ассета и типа компонента
            // GetInstanceID() не подходит, так как меняется при перезагрузке
            string persistentKey = $"{assetPath}::{component.GetType().FullName}";
            int runtimeId = component.GetInstanceID();

            if (!_componentFoldouts.ContainsKey(persistentKey)) _componentFoldouts[persistentKey] = false;

            if (!_serializedObjects.TryGetValue(runtimeId, out SerializedObject so) || so.targetObject == null)
            {
                so = new SerializedObject(component);
                _serializedObjects[runtimeId] = so;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            so.Update();
            EditorGUI.BeginChangeCheck();

            bool isExpanded = _componentFoldouts[persistentKey];

            // InspectorTitlebar возвращает новое состояние
            isExpanded = EditorGUILayout.InspectorTitlebar(isExpanded, component);
            _componentFoldouts[persistentKey] = isExpanded;

            if (isExpanded)
            {
                EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(12, 5, 5, 5) });

                SerializedProperty prop = so.GetIterator();
                bool enterChildren = true;
                bool hasProps = false;

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

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(component);
                EditorUtility.SetDirty(prefab);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        // --- Logic Helpers ---

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

        private void SetSort(SortMode mode)
        {
            _currentSortMode = mode;
            ApplySortAndFilter();
        }

        private void RefreshData()
        {
            _allPrefabs.Clear();
            _serializedObjects.Clear();

            string sysPath = TargetPath;
            if (Directory.Exists(sysPath) || AssetDatabase.IsValidFolder(sysPath))
            {
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { sysPath.TrimEnd('/') });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (asset != null)
                    {
                        string dir = Path.GetDirectoryName(path).Replace("\\", "/");
                        string folderName = dir.Replace(sysPath, "").Trim('/');
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
                case SortMode.Name:
                    orderedQuery = _sortAscending ? orderedQuery.ThenBy(p => p.Name) : orderedQuery.ThenByDescending(p => p.Name);
                    break;
                case SortMode.ComponentCount:
                    orderedQuery = _sortAscending ? orderedQuery.ThenBy(p => p.ComponentCount) : orderedQuery.ThenByDescending(p => p.ComponentCount);
                    break;
                case SortMode.Subfolder:
                    orderedQuery = _sortAscending
                        ? orderedQuery.ThenBy(p => p.Subfolder).ThenBy(p => p.Name)
                        : orderedQuery.ThenByDescending(p => p.Subfolder).ThenByDescending(p => p.Name);
                    break;
            }

            _displayedPrefabs = orderedQuery.ToList();
            Repaint();
        }

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

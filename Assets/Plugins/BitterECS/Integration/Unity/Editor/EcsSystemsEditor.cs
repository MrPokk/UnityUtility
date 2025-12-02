#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BitterECS.Core;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitterECS.Integration.Editor
{
    public class EcsSystemsEditor : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _lastRefreshTime;
        private const float REFRESH_INTERVAL = 1.0f;

        // Foldout states
        private Dictionary<Priority, bool> _priorityFoldouts = new Dictionary<Priority, bool>();
        private Dictionary<string, bool> _systemFoldouts = new Dictionary<string, bool>();

        // Search and filter
        private string _searchString = "";
        private bool _showSearch = false;
        private bool _showAllPriorities = true;

        [MenuItem("Tools/BitterECS/Systems Diagnostic")]
        public static void ShowWindow()
        {
            var window = GetWindow<EcsSystemsEditor>("ECS Systems Diagnostic");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            // Initialize foldout states for all priorities
            foreach (Priority priority in System.Enum.GetValues(typeof(Priority)))
            {
                _priorityFoldouts[priority] = true;
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            DrawSystemsList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var systemsCount = GetSystemsCount();
            var activeSystems = GetActiveSystemsCount();

            GUILayout.Label($"Systems: {activeSystems}/{systemsCount}", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(_showSearch ? "▼ Search" : "▲ Search", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                _showSearch = !_showSearch;
            }

            EditorGUILayout.EndHorizontal();

            if (_showSearch)
            {
                DrawSearchPanel();
            }
        }

        private void DrawSearchPanel()
        {
            EditorGUILayout.BeginVertical("HelpBox");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(60));
            _searchString = EditorGUILayout.TextField(_searchString);
            if (GUILayout.Button("X", GUILayout.Width(20)) && !string.IsNullOrEmpty(_searchString))
            {
                _searchString = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Show All:", GUILayout.Width(60));
            _showAllPriorities = EditorGUILayout.Toggle(_showAllPriorities);
            if (!_showAllPriorities)
            {
                GUILayout.Label("(Showing only High/Medium/Low)", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Auto refresh toggle with icon
            var autoRefreshContent = new GUIContent(" Auto Refresh", EditorGUIUtility.IconContent("d_RotateTool On").image);
            _autoRefresh = GUILayout.Toggle(_autoRefresh, autoRefreshContent, EditorStyles.toolbarButton, GUILayout.Width(120));
            // Refresh button with icon
            var refreshContent = new GUIContent(" Refresh", EditorGUIUtility.IconContent("d_Refresh").image);
            if (GUILayout.Button(refreshContent, EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                RefreshSystems();
            }

            // Expand/Collapse all buttons
            if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ExpandAll();
            }

            if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                CollapseAll();
            }

            GUILayout.FlexibleSpace();

            // Stats
            var memory = System.GC.GetTotalMemory(false) / 1024f / 1024f;
            GUILayout.Label($"Memory: {memory:F2} MB", EditorStyles.miniLabel, GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSystemsList()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to view ECS systems hierarchy", MessageType.Info);
                return;
            }

            var systems = GetAllSystems();

            if (systems.Count == 0)
            {
                EditorGUILayout.HelpBox("No ECS systems found. Systems will appear here when they are registered.", MessageType.Warning);
                return;
            }

            // Apply search filter
            var filteredSystems = systems
                .Where(s => string.IsNullOrEmpty(_searchString) ||
                           s.GetType().Name.IndexOf(_searchString, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space();

            // Group by priority with custom ordering
            var groupedSystems = filteredSystems
                .GroupBy(s => s.PrioritySystem)
                .OrderBy(g => GetPriorityOrder(g.Key));

            foreach (var priorityGroup in groupedSystems)
            {
                // Skip FIRST_TASK and LAST_TASK if not showing all priorities
                if (!_showAllPriorities &&
                    (priorityGroup.Key == Priority.FIRST_TASK || priorityGroup.Key == Priority.LAST_TASK))
                {
                    continue;
                }

                DrawPriorityGroup(priorityGroup.Key, priorityGroup.ToList());
            }

            EditorGUILayout.EndScrollView();
        }

        private int GetPriorityOrder(Priority priority)
        {
            // Custom ordering to match the enum values
            return (int)priority;
        }

        private string GetPriorityDisplayName(Priority priority)
        {
            return priority switch
            {
                Priority.FIRST_TASK => "FIRST TASK",
                Priority.LAST_TASK => "LAST TASK",
                _ => priority.ToString()
            };
        }

        private Color GetPriorityColor(Priority priority)
        {
            return priority switch
            {
                Priority.FIRST_TASK => new Color(0.8f, 0.2f, 0.2f, 0.8f),
                Priority.High => new Color(0.8f, 0.4f, 0.2f, 0.8f),
                Priority.Medium => new Color(0.8f, 0.8f, 0.2f, 0.8f),
                Priority.Low => new Color(0.4f, 0.8f, 0.2f, 0.8f),
                Priority.LAST_TASK => new Color(0.2f, 0.2f, 0.8f, 0.8f),
                _ => new Color(0.6f, 0.6f, 0.6f, 0.8f)
            };
        }

        private void DrawPriorityGroup(Priority priority, List<IEcsSystem> systems)
        {
            if (!_priorityFoldouts.ContainsKey(priority))
            {
                _priorityFoldouts[priority] = true;
            }

            EditorGUILayout.BeginVertical("HelpBox");

            // Priority header with foldout and color coding
            EditorGUILayout.BeginHorizontal();

            var priorityColor = GetPriorityColor(priority);
            var originalColor = GUI.color;
            GUI.color = priorityColor;

            var foldoutContent = new GUIContent($" {GetPriorityDisplayName(priority)} ({systems.Count} systems)");
            _priorityFoldouts[priority] = EditorGUILayout.Foldout(_priorityFoldouts[priority], foldoutContent, true, EditorStyles.foldoutHeader);

            GUI.color = originalColor;

            GUILayout.FlexibleSpace();

            // Priority value badge
            var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.gray },
                alignment = TextAnchor.MiddleRight,
                padding = new RectOffset(4, 4, 0, 0)
            };
            GUILayout.Label($"Value: {(int)priority}", badgeStyle);

            EditorGUILayout.EndHorizontal();

            if (_priorityFoldouts[priority])
            {
                EditorGUI.indentLevel++;

                foreach (var system in systems.OrderBy(s => s.GetType().Name))
                {
                    DrawSystem(system);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawSystem(IEcsSystem system)
        {
            var systemType = system.GetType();
            var systemKey = systemType.FullName;

            if (!_systemFoldouts.ContainsKey(systemKey))
            {
                _systemFoldouts[systemKey] = false;
            }

            EditorGUILayout.BeginVertical("Box");

            // System header
            EditorGUILayout.BeginHorizontal();

            // System icon based on interface type
            var interfaces = systemType.GetInterfaces();
            var primaryInterface = interfaces.FirstOrDefault();
            Texture2D icon = GetSystemIcon(primaryInterface);

            if (icon != null)
            {
                GUILayout.Label(new GUIContent(icon), GUILayout.Width(28), GUILayout.Height(28));
            }

            // System name with foldout
            var systemName = systemType.Name;
            _systemFoldouts[systemKey] = EditorGUILayout.Foldout(_systemFoldouts[systemKey], systemName, true);

            GUILayout.FlexibleSpace();

            // Interface badge
            if (primaryInterface != null)
            {
                var interfaceName = primaryInterface.Name.Replace("IEcs", "");
                var badgeContent = new GUIContent(interfaceName);
                var badgeStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    margin = new RectOffset(2, 2, 2, 2)
                };

                GUI.color = GetInterfaceColor(primaryInterface);
                if (GUILayout.Button(badgeContent, badgeStyle, GUILayout.Width(80)))
                {
                    _systemFoldouts[systemKey] = !_systemFoldouts[systemKey];
                }
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            // System details when expanded
            if (_systemFoldouts[systemKey])
            {
                EditorGUI.indentLevel++;

                DrawSystemDetails(systemType, interfaces);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawSystemDetails(System.Type systemType, System.Type[] interfaces)
        {
            // Full type name
            EditorGUILayout.LabelField($"Type: {systemType.FullName}", EditorStyles.miniBoldLabel);

            // Assembly info
            EditorGUILayout.LabelField($"Assembly: {systemType.Assembly.GetName().Name}", EditorStyles.miniLabel);

            // Interfaces
            if (interfaces.Length > 0)
            {
                var interfaceNames = string.Join(", ", interfaces.Select(i => i.Name));
                EditorGUILayout.LabelField($"Interfaces: {interfaceNames}", EditorStyles.miniLabel);
            }
        }

        private Texture2D GetSystemIcon(System.Type interfaceType)
        {
            if (interfaceType == null) return null;

            string iconName = interfaceType.Name switch
            {
                "IEcsInitSystem" => "d_PlayButton",
                "IEcsRunSystem" => "d_Animation.Play",
                "IEcsDestroySystem" => "d_winbtn_win_close",
                "IEcsFixedRunSystem" => "d_Animation.FixedFrame",
                _ => "d_ScriptableObject Icon"
            };

            return EditorGUIUtility.IconContent(iconName).image as Texture2D;
        }

        private Color GetInterfaceColor(System.Type interfaceType)
        {
            return interfaceType.Name switch
            {
                "IEcsInitSystem" => new Color(0.2f, 0.8f, 0.2f, 0.8f),
                "IEcsRunSystem" => new Color(0.2f, 0.5f, 0.8f, 0.8f),
                "IEcsDestroySystem" => new Color(0.8f, 0.2f, 0.2f, 0.8f),
                "IEcsFixedRunSystem" => new Color(0.8f, 0.6f, 0.2f, 0.8f),
                _ => new Color(0.6f, 0.6f, 0.6f, 0.8f)
            };
        }

        private List<IEcsSystem> GetAllSystems()
        {
            if (!Application.isPlaying)
                return new List<IEcsSystem>();

            try
            {
                // Получаем экземпляр синглтона
                var instance = EcsSystems.Instance;
                if (instance == null)
                    return new List<IEcsSystem>();

                // Получаем приватное поле _systems через рефлексию
                var systemsField = typeof(EcsSystems).GetField("_systems",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (systemsField == null)
                    return new List<IEcsSystem>();

                // Теперь _systems это SortedSet<IEcsSystem>, а не List<IEcsSystem>
                var systemsSet = systemsField.GetValue(instance) as System.Collections.Generic.SortedSet<IEcsSystem>;
                if (systemsSet == null)
                    return new List<IEcsSystem>();

                return systemsSet.ToList();
            }
            catch
            {
                return new List<IEcsSystem>();
            }
        }

        private int GetSystemsCount()
        {
            if (!Application.isPlaying)
                return 0;

            try
            {
                // Получаем экземпляр синглтона
                var instance = EcsSystems.Instance;
                if (instance == null)
                    return 0;

                // Получаем приватное поле _systems через рефлексию
                var systemsField = typeof(EcsSystems).GetField("_systems",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (systemsField == null)
                    return 0;

                // Теперь _systems это SortedSet<IEcsSystem>
                var systemsSet = systemsField.GetValue(instance) as System.Collections.Generic.SortedSet<IEcsSystem>;
                return systemsSet?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private int GetActiveSystemsCount()
        {
            return GetAllSystems().Count(s => s != null);
        }

        private void RefreshSystems()
        {
            Repaint();
            _lastRefreshTime = Time.realtimeSinceStartup;
        }

        private void ExpandAll()
        {
            foreach (var key in _priorityFoldouts.Keys.ToList())
            {
                _priorityFoldouts[key] = true;
            }
            foreach (var key in _systemFoldouts.Keys.ToList())
            {
                _systemFoldouts[key] = true;
            }
        }

        private void CollapseAll()
        {
            foreach (var key in _priorityFoldouts.Keys.ToList())
            {
                _priorityFoldouts[key] = false;
            }
            foreach (var key in _systemFoldouts.Keys.ToList())
            {
                _systemFoldouts[key] = false;
            }
        }

        private void Update()
        {
            if (Application.isPlaying && _autoRefresh &&
                Time.realtimeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                RefreshSystems();
            }
        }
    }

    [InitializeOnLoad]
    public static class EcsSystemsHierarchyViewer
    {
        private static bool _isSubscribed = false;

        static EcsSystemsHierarchyViewer()
        {
            Subscribe();
        }

        private static void Subscribe()
        {
            if (!_isSubscribed)
            {
                EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
                _isSubscribed = true;

                // Отпишемся при перекомпиляции или выходе из play mode
                AssemblyReloadEvents.beforeAssemblyReload += Unsubscribe;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }
        }

        private static void Unsubscribe()
        {
            if (_isSubscribed)
            {
                EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItemGUI;
                _isSubscribed = false;

                // Убираем обработчики отписки
                AssemblyReloadEvents.beforeAssemblyReload -= Unsubscribe;
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Отписываемся при выходе из play mode
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Unsubscribe();
            }
            // Подписываемся при входе в play mode
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Subscribe();
            }
        }

        private static void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
        {
            if (!Application.isPlaying)
            {
                // Если не в play mode, отписываемся
                Unsubscribe();
                return;
            }

            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject != null && gameObject.name == "ECS Systems")
            {
                var systemsCount = GetSystemsCount();
                if (systemsCount > 0)
                {
                    // Create style here to avoid static constructor issues
                    var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                        alignment = TextAnchor.MiddleRight,
                        padding = new RectOffset(4, 4, 0, 0),
                        margin = new RectOffset(0, 0, 1, 1)
                    };

                    var rect = new Rect(selectionRect);
                    rect.x = rect.width - 100;
                    rect.width = 90;

                    // Draw badge background
                    EditorGUI.DrawRect(new Rect(rect.x - 2, rect.y + 1, rect.width + 4, rect.height - 2),
                        new Color(0.1f, 0.1f, 0.1f, 0.8f));

                    // Draw systems count
                    GUI.Label(rect, $"{systemsCount} systems", badgeStyle);
                }
            }
        }

        private static int GetSystemsCount()
        {
            if (!Application.isPlaying)
                return 0;

            try
            {
                // Получаем экземпляр синглтона
                var instance = EcsSystems.Instance;
                if (instance == null)
                    return 0;

                // Получаем приватное поле _systems через рефлексию
                var systemsField = typeof(EcsSystems).GetField("_systems",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (systemsField == null)
                    return 0;

                // Теперь _systems это SortedSet<IEcsSystem>
                var systemsSet = systemsField.GetValue(instance) as System.Collections.Generic.SortedSet<IEcsSystem>;
                return systemsSet?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BitterECS.Core;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitterECS.Integration.Editor
{
    public static class EcsReflectionHelper
    {
        private static FieldInfo _systemsField;

        public static IReadOnlyCollection<IEcsSystem> GetSystems()
        {
            if (!Application.isPlaying || EcsSystems.Instance == null)
                return System.Array.Empty<IEcsSystem>();

            if (_systemsField == null)
            {
                _systemsField = typeof(EcsSystems).GetField("_systems",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (_systemsField == null)
                return System.Array.Empty<IEcsSystem>();

            var set = _systemsField.GetValue(EcsSystems.Instance) as SortedSet<IEcsSystem>;
            return set ?? (IReadOnlyCollection<IEcsSystem>)System.Array.Empty<IEcsSystem>();
        }

        public static int GetSystemsCount()
        {
            return GetSystems().Count;
        }
    }

    public class EcsSystemsEditor : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _lastRefreshTime;
        private const float REFRESH_RATE = 0.5f;

        private string _searchFilter = "";
        private bool _isSearchExpanded = false;
        private bool _showAllPriorities = true;

        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

        [MenuItem("BitterECS/Tools/Systems Diagnostic")]
        public static void OpenWindow()
        {
            var window = GetWindow<EcsSystemsEditor>("ECS Systems");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            foreach (Priority p in System.Enum.GetValues(typeof(Priority)))
            {
                _foldouts[p.ToString()] = true;
            }
        }

        private void OnGUI()
        {
            Styles.Initialize();

            DrawHeader();
            DrawToolbar();
            DrawContent();
        }

        private void Update()
        {
            if (Application.isPlaying && _autoRefresh &&
                Time.realtimeSinceStartup - _lastRefreshTime > REFRESH_RATE)
            {
                Repaint();
                _lastRefreshTime = Time.realtimeSinceStartup;
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var systems = EcsReflectionHelper.GetSystems();
                GUILayout.Label($"Active Systems: {systems.Count}", EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(_isSearchExpanded ? "▼ Search" : "▲ Search", EditorStyles.toolbarDropDown, GUILayout.Width(70)))
                {
                    _isSearchExpanded = !_isSearchExpanded;
                }
            }

            if (_isSearchExpanded)
            {
                DrawSearchSettings();
            }
        }

        private void DrawSearchSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Filter:", GUILayout.Width(50));
                    _searchFilter = EditorGUILayout.TextField(_searchFilter);

                    if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(45)))
                    {
                        _searchFilter = "";
                        GUI.FocusControl(null);
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _showAllPriorities = EditorGUILayout.ToggleLeft("Show All Priorities", _showAllPriorities);
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _autoRefresh = GUILayout.Toggle(_autoRefresh, Styles.IconAutoRefresh, EditorStyles.toolbarButton, GUILayout.Width(40));

                if (GUILayout.Button(Styles.IconRefresh, EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    _lastRefreshTime = Time.realtimeSinceStartup;
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Expand All", EditorStyles.toolbarButton)) ExpandCollapseAll(true);
                if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton)) ExpandCollapseAll(false);

                var memory = System.GC.GetTotalMemory(false) / 1048576f;
                GUILayout.Label($"{memory:F2} MB", EditorStyles.miniLabel, GUILayout.Width(60));
            }
        }

        private void DrawContent()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect systems.", MessageType.Info);
                return;
            }

            var allSystems = EcsReflectionHelper.GetSystems();
            if (allSystems.Count == 0)
            {
                EditorGUILayout.HelpBox("No active systems found.", MessageType.Warning);
                return;
            }

            var filtered = allSystems
                .Where(s => string.IsNullOrEmpty(_searchFilter) ||
                           s.GetType().Name.IndexOf(_searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .GroupBy(s => s.Priority)
                .OrderBy(g => (int)g.Key);

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;
                EditorGUILayout.Space(5);

                foreach (var group in filtered)
                {
                    if (!_showAllPriorities && IsEdgePriority(group.Key)) continue;
                    DrawPriorityGroup(group.Key, group.ToList());
                }
            }
        }

        private void DrawPriorityGroup(Priority priority, List<IEcsSystem> systems)
        {
            var key = priority.ToString();
            if (!_foldouts.ContainsKey(key)) _foldouts[key] = true;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.backgroundColor = GetPriorityColor(priority);

                    var title = $"{priority} ({systems.Count})";
                    _foldouts[key] = EditorGUILayout.Foldout(_foldouts[key], title, true, Styles.BoldFoldout);

                    GUI.backgroundColor = Color.white;
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"Order: {(int)priority}", EditorStyles.miniLabel);
                }

                if (_foldouts[key])
                {
                    EditorGUI.indentLevel++;
                    foreach (var system in systems)
                    {
                        DrawSystemItem(system);
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawSystemItem(IEcsSystem system)
        {
            var type = system.GetType();
            var key = type.FullName;
            if (!_foldouts.ContainsKey(key)) _foldouts[key] = false;

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var interfaceType = type.GetInterfaces().FirstOrDefault();
                    var icon = GetSystemIcon(interfaceType);

                    if (icon != null)
                    {
                        GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                    }

                    _foldouts[key] = EditorGUILayout.Foldout(_foldouts[key], type.Name, true);

                    GUILayout.FlexibleSpace();

                    if (interfaceType != null)
                    {
                        DrawInterfaceBadge(interfaceType);
                    }
                }

                if (_foldouts[key])
                {
                    DrawSystemInfo(type);
                }
            }
        }

        private void DrawSystemInfo(System.Type type)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Namespace", type.Namespace, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Assembly", type.Assembly.GetName().Name, EditorStyles.miniLabel);

            var interfaces = type.GetInterfaces();
            if (interfaces.Length > 0)
            {
                var list = string.Join(", ", interfaces.Select(i => i.Name));
                EditorGUILayout.LabelField("Implements", list, EditorStyles.miniLabel);
            }
            EditorGUI.indentLevel--;
        }

        private void DrawInterfaceBadge(System.Type type)
        {
            var color = GetInterfaceColor(type);
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color;

            var name = type.Name.Replace("IEcs", "");
            GUILayout.Label(name, EditorStyles.miniButton, GUILayout.Width(80));

            GUI.backgroundColor = oldColor;
        }

        private void ExpandCollapseAll(bool expand)
        {
            var keys = _foldouts.Keys.ToList();
            foreach (var key in keys) _foldouts[key] = expand;
        }

        private bool IsEdgePriority(Priority p) => p == Priority.FIRST_TASK || p == Priority.LAST_TASK;

        private Color GetPriorityColor(Priority p) => p switch
        {
            Priority.High => new Color(1f, 0.6f, 0.2f),
            Priority.Medium => new Color(1f, 1f, 0.4f),
            Priority.Low => new Color(0.4f, 1f, 0.4f),
            Priority.FIRST_TASK => new Color(1f, 0.4f, 0.4f),
            Priority.LAST_TASK => new Color(0.4f, 0.6f, 1f),
            _ => Color.white
        };

        private Color GetInterfaceColor(System.Type t) => t?.Name switch
        {
            "IEcsInitSystem" => new Color(0.5f, 1f, 0.5f),
            "IEcsRunSystem" => new Color(0.5f, 0.8f, 1f),
            "IEcsDestroySystem" => new Color(1f, 0.5f, 0.5f),
            "IEcsFixedRunSystem" => new Color(1f, 0.8f, 0.4f),
            _ => Color.white
        };

        private Texture2D GetSystemIcon(System.Type t)
        {
            var name = t?.Name switch
            {
                "IEcsInitSystem" => "d_PlayButton",
                "IEcsRunSystem" => "d_Animation.Play",
                "IEcsDestroySystem" => "d_winbtn_win_close",
                "IEcsFixedRunSystem" => "d_Animation.FixedFrame",
                _ => "d_ScriptableObject Icon"
            };
            return EditorGUIUtility.IconContent(name).image as Texture2D;
        }

        private static class Styles
        {
            public static GUIContent IconRefresh;
            public static GUIContent IconAutoRefresh;
            public static GUIStyle BoldFoldout;

            public static void Initialize()
            {
                if (IconRefresh != null) return;

                IconRefresh = EditorGUIUtility.IconContent("d_Refresh");
                IconAutoRefresh = EditorGUIUtility.IconContent("d_RotateTool On");

                BoldFoldout = new GUIStyle(EditorStyles.foldoutHeader)
                {
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 22
                };
            }
        }
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
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null || gameObject.name != "ECS Systems") return;

            var count = EcsReflectionHelper.GetSystemsCount();
            if (count <= 0) return;

            if (_badgeStyle == null)
            {
                _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
                };
            }

            var labelRect = new Rect(rect);
            labelRect.xMax -= 20;

            GUI.Label(labelRect, $"{count} systems", _badgeStyle);
        }
    }
}
#endif

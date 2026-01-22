#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using BitterECS.Core;

namespace BitterECS.Editor
{
    public class SystemsView
    {
        private readonly EditorWindow _owner;
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _lastRefreshTime;
        private const float REFRESH_RATE = 0.5f;

        private string _searchFilter = "";
        private bool _showAllPriorities = true;
        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

        public SystemsView(EditorWindow owner) => _owner = owner;

        public void OnEnable()
        {
            foreach (Priority p in Enum.GetValues(typeof(Priority)))
                _foldouts[p.ToString()] = true;
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
            Rect foldoutRect = new Rect(headerRect.x + 5, headerRect.y, headerRect.width - 5, headerRect.height);
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
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using BitterECS.Core;

namespace BitterECS.Editor
{
    public class BitterECSControlPanel : EditorWindow
    {
        private enum Tab { Entities, Systems, Settings }

        [SerializeField] private Tab _currentTab = Tab.Systems;
        [SerializeField] public List<string> expandedEntityKeys = new();
        [SerializeField] public List<string> expandedComponentKeys = new();

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
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
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

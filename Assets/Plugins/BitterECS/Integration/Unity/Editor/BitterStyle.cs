using UnityEditor;
using UnityEngine;
using System;

namespace BitterECS.Integration.Unity.Editor
{
    public static class BitterStyle
    {
        private static bool _initialized;

        // Colors
        public static Color MainBgColor => EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f);
        public static Color CardBgColor => EditorGUIUtility.isProSkin ? new Color(0.28f, 0.28f, 0.28f) : new Color(0.9f, 0.9f, 0.9f);
        public static Color HeaderBgColor => EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.85f, 0.85f, 0.85f);
        public static Color AccentColor => new Color(0.2f, 0.6f, 0.85f);
        public static Color SelectionColor => new Color(0.2f, 0.6f, 0.85f, 0.2f);
        public static Color WarningColor => new Color(0.95f, 0.7f, 0.2f);

        // GUI Styles
        public static GUIStyle Container { get; private set; }
        public static GUIStyle Card { get; private set; }
        public static GUIStyle CardStyle { get; private set; }
        public static GUIStyle HeaderLabel { get; private set; }
        public static GUIStyle SubHeaderLabel { get; private set; }
        public static GUIStyle FolderHeaderStyle { get; private set; }
        public static GUIStyle EntityTitleStyle { get; private set; }
        public static GUIStyle ComponentBadgeStyle { get; private set; }
        public static GUIStyle MiniBadge { get; private set; }
        public static GUIStyle SearchField { get; private set; }
        public static GUIStyle Toolbar { get; private set; }
        public static GUIStyle ToolbarButton { get; private set; }

        // Icons
        public static GUIContent IconRefresh { get; private set; }
        public static GUIContent IconAutoRefresh { get; private set; }
        public static GUIContent IconFolder { get; private set; }
        public static GUIContent IconFolderOpen { get; private set; }
        public static GUIContent IconScript { get; private set; }
        public static GUIContent IconPrefab { get; private set; }
        public static GUIContent IconWarn { get; private set; }

        public static void Init()
        {
            if (_initialized) return;

            InitStyles();
            InitIcons();

            _initialized = true;
        }

        private static void InitStyles()
        {
            CardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(4, 4, 4, 8),
                normal = { background = MakeTexture(1, 1, CardBgColor) }
            };

            Container = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 5, 5)
            };

            Card = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(4, 4, 4, 4),
                normal = { background = MakeTexture(2, 2, CardBgColor) }
            };

            HeaderLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };

            SubHeaderLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.darkGray }
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

            MiniBadge = new GUIStyle(EditorStyles.miniButton)
            {
                fixedHeight = 16,
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(2, 2, 2, 2)
            };

            SearchField = new GUIStyle(EditorStyles.toolbarSearchField);
            Toolbar = new GUIStyle(EditorStyles.toolbar);
            ToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
        }

        private static void InitIcons()
        {
            IconRefresh = EditorGUIUtility.IconContent("d_Refresh");
            IconAutoRefresh = EditorGUIUtility.IconContent("d_RotateTool On");
            IconFolder = EditorGUIUtility.IconContent("Folder Icon");
            IconFolderOpen = EditorGUIUtility.IconContent("FolderOpened Icon");
            IconScript = EditorGUIUtility.IconContent("cs Script Icon");
            IconPrefab = EditorGUIUtility.IconContent("d_Prefab Icon");
            IconWarn = EditorGUIUtility.IconContent("console.warnicon.sml");
        }

        public static Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            var result = new Texture2D(width, height) { hideFlags = HideFlags.HideAndDontSave };
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public static void DrawHeader(string title, Action drawRightSide = null)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(title, HeaderLabel, GUILayout.Height(20));
                GUILayout.FlexibleSpace();
                drawRightSide?.Invoke();
            }
        }

        public static void DrawBadge(string text, Color color)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Label(text, MiniBadge, GUILayout.MinWidth(text.Length * 7 + 10));
            GUI.backgroundColor = oldColor;
        }
    }
}

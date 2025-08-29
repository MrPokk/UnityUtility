#if UNITY_EDITOR
using BitterCMS.CMSSystem;
using BitterCMS.System.Serialization;
using BitterCMS.Utility;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BitterCMS.UnityIntegration.Editor
{
    public class CMSDatabaseEditor : CMSEditorTabCore
    {
        private bool _showProvidersSection = true;
        private bool _showEntitiesSection = true;
        private string _searchString = "";
        private int _sortOption = 0;

        public override void Draw()
        {
            DrawSearchAndSortOptions();

            _showProvidersSection = EditorGUILayout.Foldout(_showProvidersSection, "Providers");
            if (_showProvidersSection)
                DrawProvider();

            EditorGUILayout.Space(10);

            _showEntitiesSection = EditorGUILayout.Foldout(_showEntitiesSection, "Entities");
            if (_showEntitiesSection)
                DrawEntity();
        }

        private void DrawSearchAndSortOptions()
        {
            var searchContent = new GUIContent("", "Search database items");
            var textFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontStyle = string.IsNullOrEmpty(_searchString) ? FontStyle.Italic : FontStyle.Normal
            };

            EditorGUILayout.BeginHorizontal();
            {

                _searchString = EditorGUILayout.TextField(searchContent, _searchString,
                    textFieldStyle, GUILayout.ExpandWidth(true));

                if (string.IsNullOrEmpty(_searchString))
                {
                    var textFieldRect = GUILayoutUtility.GetLastRect();
                    EditorGUI.LabelField(textFieldRect, "Search", new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = EditorStyles.centeredGreyMiniLabel.normal.textColor },
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(5, 0, 0, 0)
                    });
                }

                _sortOption = EditorGUILayout.Popup(_sortOption, new[] { "A-Z", "Z-A" }, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }

        private void DrawProvider()
        {
            var allProvider = ProviderDatabase.GetAll();

            var filteredProviders = allProvider.Where(v => v &&
                                                   v.GetType().Name.ToLower().Contains(_searchString.ToLower())).ToList();

            EditorGUILayout.LabelField($"Loaded Providers ({filteredProviders.Count})", EditorStyles.boldLabel);
            if (!filteredProviders.Any())
            {
                EditorGUILayout.HelpBox("No Providers found matching search criteria", MessageType.Info);
                return;
            }

            var sortedProviders = _sortOption == 0
                ? filteredProviders.OrderBy(v => v.GetType().Name)
                : filteredProviders.OrderByDescending(v => v.GetType().Name);

            foreach (var Provider in sortedProviders)
            {
                if (!Provider)
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(Provider.GetType().Name, EditorStyles.boldLabel);
                EditorGUILayout.ObjectField(Provider.gameObject, typeof(GameObject), false);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }

        private void DrawEntity()
        {
            var allEntity = EntityDatabase.GetAll();
            var isRecreate = false;

            var filteredEntities = allEntity.Where(e =>
                e.Key.Name.ToLower().Contains(_searchString.ToLower())).ToList();

            EditorGUILayout.LabelField($"Loaded Entities ({filteredEntities.Count})", EditorStyles.boldLabel);

            if (!filteredEntities.Any())
            {
                EditorGUILayout.HelpBox("No entities found matching search criteria", MessageType.Info);
                return;
            }

            var sortedEntities = _sortOption == 0
                ? filteredEntities.OrderBy(e => e.Key.Name)
                : filteredEntities.OrderByDescending(e => e.Key.Name);

            foreach (var entity in sortedEntities)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(entity.Key.Name, EditorStyles.boldLabel);

                if (GUILayout.Button("Create", GUILayout.Height(20)))
                {
                    EntityDatabase.SaveEntity(entity.Key);
                    isRecreate = true;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            if (isRecreate)
                AssetDatabase.Refresh();
        }
    }
}
#endif

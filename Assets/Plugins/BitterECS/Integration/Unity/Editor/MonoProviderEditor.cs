#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Linq;
using BitterECS.Core;
using BitterECS.Utility;

namespace BitterECS.Integration.Editor
{
    [CustomEditor(typeof(MonoProvider))]
    public class MonoProviderEditor : UnityEditor.Editor
    {
        private MonoProvider _provider;
        private SerializedProperty _entityTypeProperty;

        private string[] _typeNames;
        private Type[] _types;
        private int _selectedIndex = -1;

        private void OnEnable()
        {
            _types = ReflectionUtility.FindAllImplement<EcsEntity>();
            _typeNames = _types.Select(t => t.FullName).ToArray();

            _provider = (MonoProvider)target;
            _entityTypeProperty = serializedObject.FindProperty("entityType");

            if (_provider.entityType != null && _provider.entityType.Type != null)
            {
                _selectedIndex = Array.IndexOf(_types, _provider.entityType.Type);
            }

            if (_selectedIndex == -1 && _types.Length > 0)
            {
                _selectedIndex = 0;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            _selectedIndex = EditorGUILayout.Popup("Entity Type", _selectedIndex, _typeNames);

            if (EditorGUI.EndChangeCheck() && _selectedIndex >= 0 && _selectedIndex < _types.Length)
            {
                var selectedType = _types[_selectedIndex];

                _entityTypeProperty.FindPropertyRelative("_typeName").stringValue = selectedType.FullName;
                _entityTypeProperty.FindPropertyRelative("_assemblyName").stringValue = selectedType.Assembly.FullName;

                _provider.entityType.Type = selectedType;

                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.LabelField("Current Type", _provider.EntityType?.FullName ?? "None");

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Select an EcsEntity type that this provider will create", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif

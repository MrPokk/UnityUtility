#if UNITY_EDITOR
using UnityEditor;
using System;
using BitterECS.Core;

namespace BitterECS.Integration.Editor
{
    [CustomEditor(typeof(MonoProvider))]
    public class MonoProviderEditor : UnityEditor.Editor
    {
        private MonoProvider _provider;
        private string[] _typeNames;
        private Type[] _types;
        private int _selectedIndex = -1;

        private void OnEnable()
        {
            _provider = (MonoProvider)target;

            _types = ReflectionUtility.FindAllImplement<EcsPresenter>();
            _typeNames = new string[_types.Length + 1];
            _typeNames[0] = "None";
            for (var i = 0; i < _types.Length; i++)
            {
                _typeNames[i + 1] = _types[i].FullName;
            }

            UpdateSelectedIndexFromProvider();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();

            var newSelectedIndex = EditorGUILayout.Popup("Presenter Type", _selectedIndex, _typeNames);

            if (EditorGUI.EndChangeCheck())
            {
                _selectedIndex = newSelectedIndex;

                if (_selectedIndex == 0)
                {
                    Select(null);
                }
                else
                {
                    var selectIndex = _selectedIndex - 1;
                    Select(_types[selectIndex]);
                }
            }

            EditorGUILayout.LabelField("Current:", _provider.PresenterType?.FullName ?? "None");
            EditorGUILayout.Space();
        }

        private void UpdateSelectedIndexFromProvider()
        {
            if (_provider.PresenterType == null)
            {
                _selectedIndex = 0;
                return;
            }

            for (var i = 0; i < _types.Length; i++)
            {
                if (_types[i] == _provider.PresenterType)
                {
                    _selectedIndex = i + 1;
                    return;
                }
            }

            _selectedIndex = 0;
        }

        private void Select(Type type)
        {
            _provider._presenterType = type;
            EditorUtility.SetDirty(_provider);
        }
    }
}
#endif

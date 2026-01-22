using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SelectImplementationAttribute))]
public class SelectImplementationDrawer : PropertyDrawer
{
    private static readonly Dictionary<Type, List<Type>> s_cachedDerivedTypes = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var selectorAttribute = (SelectImplementationAttribute)attribute;
        var derivedTypes = GetDerivedTypes(selectorAttribute.FieldType);
        var typeOptions = GetTypeOptions(derivedTypes);
        var currentSelectionIndex = GetCurrentSelectionIndex(property, derivedTypes);

        var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var labelRect = new Rect(headerRect.x, headerRect.y, EditorGUIUtility.labelWidth, headerRect.height);
        var popupRect = new Rect(headerRect.x + EditorGUIUtility.labelWidth, headerRect.y, headerRect.width - EditorGUIUtility.labelWidth, headerRect.height);

        property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);

        EditorGUI.BeginChangeCheck();
        var newSelectionIndex = EditorGUI.Popup(popupRect, currentSelectionIndex, typeOptions);

        if (EditorGUI.EndChangeCheck())
        {
            UpdateManagedReference(property, derivedTypes, newSelectionIndex);
        }

        if (property.isExpanded && property.managedReferenceValue != null)
        {
            DrawChildProperties(position, property);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = EditorGUIUtility.singleLineHeight;

        if (property.isExpanded && property.managedReferenceValue != null)
        {
            height += GetChildrenHeight(property);
        }

        return height;
    }

    private void DrawChildProperties(Rect position, SerializedProperty property)
    {
        EditorGUI.indentLevel++;

        var currentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        foreach (var child in EnumerateChildren(property))
        {
            var childHeight = EditorGUI.GetPropertyHeight(child, true);
            var childRect = new Rect(position.x, currentY, position.width, childHeight);

            EditorGUI.PropertyField(childRect, child, true);

            currentY += childHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        EditorGUI.indentLevel--;
    }

    private float GetChildrenHeight(SerializedProperty property)
    {
        var height = 0f;

        foreach (var child in EnumerateChildren(property))
        {
            height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
        }

        return height;
    }

    private IEnumerable<SerializedProperty> EnumerateChildren(SerializedProperty property)
    {
        var iterator = property.Copy();
        var endPropertyPath = iterator.propertyPath;

        if (iterator.NextVisible(true))
        {
            do
            {
                if (!iterator.propertyPath.StartsWith(endPropertyPath + "."))
                    break;

                yield return iterator;
            }
            while (iterator.NextVisible(false));
        }
    }

    private List<Type> GetDerivedTypes(Type baseType)
    {
        if (!s_cachedDerivedTypes.TryGetValue(baseType, out var types))
        {
            types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => baseType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .ToList();

            s_cachedDerivedTypes[baseType] = types;
        }
        return types;
    }

    private string[] GetTypeOptions(List<Type> types)
    {
        return types.Select(t => t.Name).Prepend("None (Null)").ToArray();
    }

    private int GetCurrentSelectionIndex(SerializedProperty property, List<Type> types)
    {
        if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
            return 0;

        var fullTypeName = property.managedReferenceFullTypename.Split(' ').Last();
        var index = types.FindIndex(t => t.FullName == fullTypeName);

        return index + 1;
    }

    private void UpdateManagedReference(SerializedProperty property, List<Type> types, int index)
    {
        if (index == 0)
        {
            property.managedReferenceValue = null;
        }
        else
        {
            var selectedType = types[index - 1];
            property.managedReferenceValue = Activator.CreateInstance(selectedType);
        }
    }
}
#endif

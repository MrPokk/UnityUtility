using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public static class GraphUIFactory
{
    public static void DrawComponentFields(VisualElement container, object component, Action onValueChanged)
    {
        var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.Name is "id" or "key" or "idComponent" or "position" or "components") continue;
            if (field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null) continue;

            var element = CreateReflectedField(component, field, onValueChanged);
            if (element != null) container.Add(element);
        }
    }

    public static VisualElement CreateCustomToolbar()
    {
        return new VisualElement
        {
            style = {
                flexDirection = FlexDirection.Row,
                backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f),
                borderBottomWidth = 1,
                borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 1f),
                paddingTop = 6, paddingBottom = 6, paddingLeft = 10, paddingRight = 10,
                alignItems = Align.Center
            }
        };
    }

    public static Button CreateButton(string text, Action onClick)
    {
        return GraphUIStyle.CreateActionButton(text, onClick);
    }

    public static VisualElement CreateComponentGroup(string name, Action onDelete, Action<VisualElement> contentBuilder)
    {
        var box = GraphUIStyle.CreateBox();

        var header = GraphUIStyle.CreateRow();
        header.Add(GraphUIStyle.CreateHeaderLabel(name));
        header.Add(GraphUIStyle.CreateDeleteButton(onDelete));

        box.Add(header);

        contentBuilder?.Invoke(box);

        return box;
    }

    public static VisualElement CreateReflectedField(object target, FieldInfo field, Action onValueChanged)
    {
        var value = field.GetValue(target);
        var type = field.FieldType;
        var name = ObjectNames.NicifyVariableName(field.Name);
        var isReadOnly = field.GetCustomAttribute<BitterECS.Integration.Unity.ReadOnlyAttribute>() != null;

        VisualElement Setup<T, V>(T uiField, V val) where T : BaseField<V>
        {
            uiField.value = val;
            if (isReadOnly) uiField.SetEnabled(false);
            else uiField.RegisterValueChangedCallback(e => { field.SetValue(target, e.newValue); onValueChanged?.Invoke(); });
            return uiField;
        }

        return type switch
        {
            _ when type == typeof(int) => Setup(new IntegerField(name), (int)value),
            _ when type == typeof(float) => Setup(new FloatField(name), (float)value),
            _ when type == typeof(string) => Setup(new TextField(name), (string)value),
            _ when type == typeof(bool) => Setup(new Toggle(name), (bool)value),
            _ when type == typeof(Vector2) => Setup(new Vector2Field(name), (Vector2)value),
            _ when type == typeof(Vector3) => Setup(new Vector3Field(name), (Vector3)value),
            _ when type.IsEnum => Setup(new EnumField(name, (Enum)value), (Enum)value),
            _ when typeof(UnityEngine.Object).IsAssignableFrom(type) => Setup(new ObjectField(name) { objectType = type }, (UnityEngine.Object)value),
            _ => new Label($"{name}: {type.Name} not supported") { style = { color = Color.red } }
        };
    }
}

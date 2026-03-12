using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

public static class GraphUIStyle
{
    public static Color NodeBackground => new(0.18f, 0.18f, 0.18f);
    public static Color ContainerBackground => new(0.25f, 0.25f, 0.25f);
    public static Color BorderColor => Color.black;
    public static Color DeleteBtnText => new(1f, 0.4f, 0.4f);
    public static Color DeleteBtnBg => new(0.2f, 0.2f, 0.2f);

    public static VisualElement CreateNodeContainer() => new()
    {
        style =
        {
            paddingBottom = 5,
            borderBottomWidth = 1,
            borderBottomColor = BorderColor
        }
    };

    public static VisualElement CreateBox() => new()
    {
        style =
        {
            marginTop = 5,
            paddingTop = 5,
            paddingBottom = 5,
            paddingLeft = 5,
            paddingRight = 5,
            backgroundColor = ContainerBackground,
            borderTopLeftRadius = 4,
            borderTopRightRadius = 4,
            borderBottomLeftRadius = 4,
            borderBottomRightRadius = 4
        }
    };

    public static Label CreateHeaderLabel(string text) => new(ObjectNames.NicifyVariableName(text))
    {
        style = { unityFontStyleAndWeight = FontStyle.Bold }
    };

    public static Label CreateLabel(string text, bool bold = false) => new(text)
    {
        style = { unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal }
    };

    public static Button CreateDeleteButton(System.Action onClick) => new(onClick)
    {
        text = "X",
        style =
        {
            color = DeleteBtnText,
            backgroundColor = DeleteBtnBg,
            width = 20
        }
    };

    public static VisualElement CreateRow() => new()
    {
        style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 2 }
    };

    public static Button CreateActionButton(string text, System.Action onClick) => new(onClick)
    {
        text = text,
        style =
        {
            marginTop = 5,
            paddingTop = 4,
            paddingBottom = 4
        }
    };

    public static Button CreateSmallButton(string text, System.Action onClick) => new(onClick)
    {
        text = text,
        style =
        {
            width = 20,
            marginLeft = 2,
            fontSize = 10
        }
    };
}

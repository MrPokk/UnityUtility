using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace GraphTree
{
    public static class GraphUIStyle
    {
        public static Color NodeBackground => new(0.18f, 0.18f, 0.18f);
        public static Color ContainerBackground => new(0.25f, 0.25f, 0.25f);
        public static Color BorderColor => Color.black;
        public static Color DeleteBtnText => new(1f, 0.4f, 0.4f);
        public static Color DeleteBtnBg => new(0.2f, 0.2f, 0.2f);
        public static Color TypeLabelColor => new(0.7f, 0.7f, 1f);

        public static void ApplyFlexGrow(VisualElement element) => element.style.flexGrow = 1;

        public static void ApplyNodeStyle(GraphNodeVisual node)
        {
            node.extensionContainer.style.backgroundColor = NodeBackground;
            node.style.minWidth = 250;
            node.extensionContainer.style.flexGrow = 1;
            node.mainContainer.style.flexGrow = 1;
        }

        public static void ApplyCustomFieldsBoxStyle(VisualElement box)
        {
            box.style.paddingTop = 5;
            box.style.paddingBottom = 5;
            box.style.borderBottomWidth = 1;
            box.style.borderBottomColor = BorderColor;
        }

        public static void ApplyToolbarStyle(VisualElement toolbar)
        {
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            toolbar.style.paddingTop = 6;
            toolbar.style.paddingBottom = 6;
            toolbar.style.paddingLeft = 10;
            toolbar.style.paddingRight = 10;
            toolbar.style.alignItems = Align.Center;
        }

        public static void ApplyAssetFieldStyle(VisualElement field, VisualElement label)
        {
            field.style.width = 250;
            label.style.minWidth = 45;
        }

        public static void ApplyGridSizeFieldStyle(VisualElement field, VisualElement label)
        {
            field.style.width = 110;
            field.style.marginLeft = 15;
            label.style.minWidth = 35;
        }

        public static void ApplyScaleFieldStyle(VisualElement field, VisualElement label)
        {
            field.style.width = 120;
            field.style.marginLeft = 15;
            label.style.minWidth = 45;
        }

        public static void ApplyRowFlexDirection(VisualElement container)
        {
            container.style.flexDirection = FlexDirection.Row;
        }

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

        public static Label CreateLabel(string text, bool bold = false, Color? color = null)
        {
            var label = new Label(text)
            {
                style = { unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal }
            };
            if (color.HasValue) label.style.color = color.Value;
            return label;
        }

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
}

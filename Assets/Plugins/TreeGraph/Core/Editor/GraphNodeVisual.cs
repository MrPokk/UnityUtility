using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphTree
{
    public class GraphNodeVisual : Node
    {
        public GraphNodeData Data { get; private set; }
        public ref int Id => ref Data.idComponent.id;
        public ref string Key => ref Data.idComponent.key;
        public ref Vector2 Position => ref Data.position.position;
        public ref Vector2 Size => ref Data.position.size;

        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        private Vector2Field _posField;
        private VisualElement _componentsContainer;
        private readonly GraphTreeView _graph;

        public override bool expanded
        {
            get => base.expanded;
            set
            {
                base.expanded = value;
                if (!value)
                {
                    style.width = StyleKeyword.Null;
                    style.height = StyleKeyword.Null;
                }
                else if (Size.x > 50 && Size.y > 50)
                {
                    style.width = Size.x;
                    style.height = Size.y;
                }
            }
        }

        public GraphNodeVisual(GraphNodeData data, GraphTreeView graph)
        {
            Data = data;
            _graph = graph;

            ConfigureNode();
            CreatePorts();
            BuildUI();
            RefreshExpandedState();
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Position = newPos.position / _graph.WorldScale;

            if (expanded && newPos.width > 50 && newPos.height > 50)
            {
                Size = newPos.size;
            }

            _posField?.SetValueWithoutNotify(Position);
        }

        public void RefreshPositionFromData()
        {
            var screenPos = Position * _graph.WorldScale;

            if (expanded && Size.x > 50 && Size.y > 50)
            {
                style.width = Size.x;
                style.height = Size.y;
                base.SetPosition(new Rect(screenPos, Size));
            }
            else
            {
                style.width = StyleKeyword.Null;
                style.height = StyleKeyword.Null;
                base.SetPosition(new Rect(screenPos, Vector2.zero));
            }

            _posField?.SetValueWithoutNotify(Position);
        }

        private void ConfigureNode()
        {
            title = string.IsNullOrEmpty(Key) ? $"Graph {Id}" : Key;
            GraphUIStyle.ApplyNodeStyle(this);

            capabilities |= Capabilities.Resizable;
            mainContainer.Add(new Resizer());
        }

        private void CreatePorts()
        {
            InputPort = AddPort(Direction.Input, "In");
            OutputPort = AddPort(Direction.Output, "Out");
        }

        private Port AddPort(Direction dir, string name)
        {
            var port = InstantiatePort(Orientation.Horizontal, dir, Port.Capacity.Multi, typeof(float));
            port.portName = name;
            (dir == Direction.Input ? inputContainer : outputContainer).Add(port);
            return port;
        }

        private void BuildUI()
        {
            var infoBox = GraphUIStyle.CreateNodeContainer();

            var typeLabel = GraphUIStyle.CreateLabel($"[{Data.GetType().Name}]", true, GraphUIStyle.TypeLabelColor);
            infoBox.Add(typeLabel);

            infoBox.Add(GraphUIStyle.CreateLabel($"ID: {Id}", bold: true));

            var keyField = new TextField("Key") { value = Key };
            keyField.RegisterValueChangedCallback(e => { Key = e.newValue; title = e.newValue; });
            infoBox.Add(keyField);

            _posField = new Vector2Field("Position") { value = Position };
            _posField.RegisterValueChangedCallback(e =>
            {
                var currentSize = Size.x > 50 && Size.y > 50 ? Size : new Vector2(250, 150);
                SetPosition(new Rect(e.newValue * _graph.WorldScale, currentSize));
            });
            infoBox.Add(_posField);

            extensionContainer.Add(infoBox);

            var customFieldsBox = new VisualElement();
            GraphUIFactory.DrawComponentFields(customFieldsBox, Data, () => { });
            if (customFieldsBox.childCount > 0)
            {
                GraphUIStyle.ApplyCustomFieldsBoxStyle(customFieldsBox);
                extensionContainer.Add(customFieldsBox);
            }

            _componentsContainer = new VisualElement { style = { flexGrow = 1 } };
            extensionContainer.Add(_componentsContainer);
            RefreshComponents();

            extensionContainer.Add(GraphUIFactory.CreateButton("Add Component", ShowAddComponentMenu));
        }

        private void RefreshComponents()
        {
            _componentsContainer.Clear();
            for (var i = 0; i < Data.components.Count; i++)
            {
                var idx = i;
                var comp = Data.components[i];
                var typeName = comp.GetType().Name.Replace("Component", "");

                var group = GraphUIStyle.CreateBox();

                var header = GraphUIStyle.CreateRow();
                header.Add(GraphUIStyle.CreateHeaderLabel(typeName));

                var buttonsRow = new VisualElement();
                GraphUIStyle.ApplyRowFlexDirection(buttonsRow);

                if (idx > 0)
                {
                    var upBtn = GraphUIStyle.CreateSmallButton("▲", () =>
                    {
                        (Data.components[idx], Data.components[idx - 1]) = (Data.components[idx - 1], Data.components[idx]);
                        RefreshComponents();
                    });
                    buttonsRow.Add(upBtn);
                }

                if (idx < Data.components.Count - 1)
                {
                    var downBtn = GraphUIStyle.CreateSmallButton("▼", () =>
                    {
                        (Data.components[idx], Data.components[idx + 1]) = (Data.components[idx + 1], Data.components[idx]);
                        RefreshComponents();
                    });
                    buttonsRow.Add(downBtn);
                }

                var deleteBtn = GraphUIStyle.CreateDeleteButton(() =>
                {
                    Data.components.RemoveAt(idx);
                    RefreshComponents();
                });
                buttonsRow.Add(deleteBtn);

                header.Add(buttonsRow);
                group.Add(header);

                GraphUIFactory.DrawComponentFields(group, comp, () => { });

                _componentsContainer.Add(group);
            }
        }

        private void ShowAddComponentMenu()
        {
            var menu = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom<IGraphComponent>().Where(t => !t.IsAbstract && t.IsValueType);

            foreach (var type in types)
            {
                if (Data.components.Any(c => c.GetType() == type)) continue;
                menu.AddItem(new GUIContent(type.Name.Replace("Component", "")), false, () =>
                {
                    Data.components.Add((IGraphComponent)Activator.CreateInstance(type));
                    RefreshComponents();
                });
            }
            menu.ShowAsContext();
        }
    }
}

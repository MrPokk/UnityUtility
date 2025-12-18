using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using BitterECS.Core;

[CustomEditor(typeof(AIBehaviourTreeConfig))]
public sealed class AIBehaviourTreeEditor : Editor
{
    private Type[] _conditionTypes;
    private Type[] _actionTypes;

    private AIBehaviourTreeConfig _config;

    private void OnEnable()
    {
        _config = (AIBehaviourTreeConfig)target;
        _conditionTypes = ReflectionUtility.FindAllImplement<AIAbstractCondition>();
        _actionTypes = ReflectionUtility.FindAllImplement<AIAbstractAction>();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "BehaviourTrees");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("AI Behaviors", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Behaviors are executed from top to bottom. Use buttons to reorder.", MessageType.Info);

        for (int i = 0; i < _config.behaviourTrees.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Behavior {i + 1}", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));

            GUI.enabled = i > 0;
            if (GUILayout.Button("↑", GUILayout.Width(20)))
            {
                MoveBehaviorUp(i);
                GUI.FocusControl(null);
            }

            GUI.enabled = i < _config.behaviourTrees.Count - 1;
            if (GUILayout.Button("↓", GUILayout.Width(20)))
            {
                MoveBehaviorDown(i);
                GUI.FocusControl(null);
            }
            GUI.enabled = true;

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                RemoveBehavior(i);
                break;
            }

            EditorGUILayout.EndHorizontal();

            DrawBehavior(_config.behaviourTrees[i], i);

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add New Behavior", GUILayout.Width(150)))
        {
            AddNewEmptyBehavior();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_config);
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void DrawBehavior(AIBehaviourTree behavior, int index)
    {
        if (behavior == null)
        {
            EditorGUILayout.HelpBox("Behavior is null!", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
        DrawConditionActionsList(behavior.conditions, index, "Add Condition", AddConditionMenu);

        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        DrawConditionActionsList(behavior.actions, index, "Add Action", AddActionMenu);

    }

    private void DrawConditionActionsList<T>(List<T> list, int behaviorIndex, string addButtonLabel, Action<int> addMenuAction) where T : class
    {
        EditorGUI.indentLevel++;

        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(list[i]?.GetType().Name ?? "Null", EditorStyles.wordWrappedLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                RemoveListItem(list, i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button(addButtonLabel, GUILayout.Width(150)))
        {
            addMenuAction(behaviorIndex);
        }

        EditorGUI.indentLevel--;
    }

    private void AddNewEmptyBehavior()
    {
        Undo.RecordObject(_config, "Add Behavior");
        _config.behaviourTrees.Add(new AIBehaviourTree());
        EditorUtility.SetDirty(_config);
    }
    private void MoveBehaviorUp(int index)
    {
        Undo.RecordObject(_config, "Move Behavior Up");
        _config.behaviourTrees.Swap(index, index - 1);
        EditorUtility.SetDirty(_config);
    }

    private void MoveBehaviorDown(int index)
    {
        Undo.RecordObject(_config, "Move Behavior Down");
        _config.behaviourTrees.Swap(index, index + 1);
        EditorUtility.SetDirty(_config);
    }

    private void RemoveBehavior(int index)
    {
        Undo.RecordObject(_config, "Remove Behavior");
        _config.behaviourTrees.RemoveAt(index);
        EditorUtility.SetDirty(_config);
    }

    private void RemoveListItem<T>(List<T> list, int index)
    {
        Undo.RecordObject(_config, "Remove List Item");
        list.RemoveAt(index);
        EditorUtility.SetDirty(_config);
    }

    private void AddConditionMenu(int behaviorIndex)
    {
        var menu = new GenericMenu();

        foreach (var condition in _conditionTypes)
        {
            menu.AddItem(new GUIContent(condition.Name), false, () => AddCondition(condition, behaviorIndex));
        }

        menu.ShowAsContext();
    }

    private void AddActionMenu(int behaviorIndex)
    {
        var menu = new GenericMenu();

        foreach (var condition in _actionTypes)
        {
            menu.AddItem(new GUIContent(condition.Name), false, () => AddCondition(condition, behaviorIndex));
        }

        menu.ShowAsContext();
    }

    private void AddCondition(Type type, int behaviorIndex)
    {
        if (behaviorIndex < 0 || behaviorIndex >= _config.behaviourTrees.Count) return;

        Undo.RecordObject(_config, "Add Condition");
        _config.behaviourTrees[behaviorIndex].conditions.Add((AIAbstractCondition)Activator.CreateInstance(type));
        EditorUtility.SetDirty(_config);
    }

    private void AddAction(Type action, int behaviorIndex)
    {
        if (behaviorIndex < 0 || behaviorIndex >= _config.behaviourTrees.Count) return;

        Undo.RecordObject(_config, "Add Action");
        _config.behaviourTrees[behaviorIndex].actions.Add((AIAbstractAction)Activator.CreateInstance(action));
        EditorUtility.SetDirty(_config);
    }
}

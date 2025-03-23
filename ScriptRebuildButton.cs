#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ScriptRebuildButton : EditorWindow
{
    [SerializeField] private bool _clearConsole = true; // Option to clear console before rebuild

    [MenuItem("Tools/Rebuild Scripts")]
    public static void ShowWindow()
    {
        GetWindow<ScriptRebuildButton>("Script Rebuild");
    }

    void OnGUI()
    {
        _clearConsole = EditorGUILayout.Toggle("Clear Console Before Rebuild", _clearConsole);

        if (GUILayout.Button("Rebuild Scripts"))
        {
            if (_clearConsole)
                ClearConsole();

            RebuildScripts();
        }
    }

    private static void RebuildScripts()
    {
        AssetDatabase.Refresh();
        Debug.Log("Scripts Rebuilt");
    }

    [MenuItem("Tools/Clear Console")] // Create menu entry to clear console (for convenience)
    private static void ClearConsole()
    {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        clearMethod.Invoke(null, null);
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

[InitializeOnLoad]
public static class VContainerDefines
{
    static VContainerDefines()
    {
        EditorApplication.delayCall += UpdateVContainerDefines;
    }

    [MenuItem("Tools/Update VContainer Defines")]
    public static void UpdateVContainerDefines()
    {
        var buildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        if (buildTarget == NamedBuildTarget.Unknown)
        {
            Debug.Log("❌ Unknown build target");
            return;
        }

        PlayerSettings.GetScriptingDefineSymbols(buildTarget, out var currentDefines);
        var defines = currentDefines.ToList();
        var hasDefine = defines.Contains("VCONTAINER_AVAILABLE");
        var hasVContainer = AppDomain.CurrentDomain.GetAssemblies()
            .Any(asm => asm.GetType("VContainer.ContainerBuilder") != null);

        if (hasVContainer) defines.Add("VCONTAINER_AVAILABLE");
        else defines.Remove("VCONTAINER_AVAILABLE");

        PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines.ToArray());
        Debug.Log(hasVContainer ?
            "✅ VCONTAINER_AVAILABLE added - VContainer found" :
            "❌ VCONTAINER_AVAILABLE removed - VContainer not found");
    }
}
#endif

#if UNITY_6000_0_OR_NEWER && UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

public class ScriptBuildProfiles : EditorWindow
{
    [MenuItem("File/Build All Platforms ^#&P", priority = 212)]
    private static void BuildPlatforms()
    {

        if (!Directory.Exists("Assets/Settings/Build Profiles/"))
            Directory.CreateDirectory("Assets/Settings/Build Profiles/");

        string[] Files = Directory.GetFiles("Assets/Settings/Build Profiles/");

        if (Files.Length == 0)
            throw new NullReferenceException("ERROR: No Build Profiles found");

        foreach (string Element in Files)
        {
            if (Path.GetExtension(Element) != ".asset") continue;
            BuildProfile BuildProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(Element);

            if (BuildProfile == null)
                continue;

            SimpleBuild(BuildProfile, BuildProfile.name);
            Wait(() => BuildPipeline.isBuildingPlayer);
        }
        Debug.Log("Build All Platforms");
    }

    private static void SimpleBuild(BuildProfile buildProfile, string Name)
    {
        BuildPlayerWithProfileOptions options = new BuildPlayerWithProfileOptions();

        options.buildProfile = buildProfile;
        options.locationPathName = $"Build/{Name}/{Name}.exe";
        options.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(options);
    }

    private static IEnumerator Wait(Func<bool> op)
    {
        while (!op())
        {
            yield return new WaitForEndOfFrame();
        }
    }
}
#endif

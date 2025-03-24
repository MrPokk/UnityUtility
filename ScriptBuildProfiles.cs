#if UNITY_6000_0_OR_NEWER && UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class ScriptBuildProfiles : EditorWindow
{

    private const string _directoryBuild = "Assets/Settings/Build Profiles/";

    [MenuItem("File/Build All Platforms ^#&P", priority = 212)]
    private static void BuildPlatforms()
    {
        t:
        if (!Directory.Exists(_directoryBuild))
            Directory.CreateDirectory(_directoryBuild);

        var Files = Directory.GetFiles(_directoryBuild);

        if (Files.Length == 0)
            throw new NullReferenceException("ERROR: No Build Profiles found");

        foreach (var Element in FindAllBuildProfiles())
        {
            if (Element == null)
                continue;

            SimpleBuild(Element);
            
            Wait(() => BuildPipeline.isBuildingPlayer);
        }
    }

    private static void SimpleBuild(BuildProfile buildProfile)
    {

        BuildReport Report = BuildPipeline.BuildPlayer(GetBuildOptions(buildProfile));

        OnValidate(Report, buildProfile.name);
    }

    private static List<BuildProfile> FindAllBuildProfiles()
    {
        List<BuildProfile> profiles = new List<BuildProfile>();
        string[] guids = AssetDatabase.FindAssets("t:BuildProfile");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);

            if (profile != null)
                profiles.Add(profile);
        }

        return profiles;
    }


    private static BuildPlayerWithProfileOptions GetBuildOptions(BuildProfile buildProfile)
    {
        BuildPlayerWithProfileOptions PlayerOptions = new BuildPlayerWithProfileOptions();

        var NameProfile = buildProfile.name;

        PlayerOptions.buildProfile = buildProfile;
        PlayerOptions.locationPathName = $"Build/{NameProfile}/{NameProfile}.exe";

        return PlayerOptions;
    }

    private static void OnValidate(BuildReport report, string nameProfile)
    {
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"<color=green>Build succeeded:</color> {nameProfile}");
        else
            Debug.LogError($"Build failed for profile: {nameProfile}");
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

using BitterECS.Extra;
using BitterECS.Extra.Editor;
using UnityEditor;

public class PlayableGen : AbstractConstantsGenerator
{
    [MenuItem(PathTool + "/Playable Paths")]
    public static void Generate()
    {
        var path = PathProject.ENTITIES;

        GenerateConstants(
            resourcesPath: path,
            className: "PlayablePathPrefab",
            generatorScriptFileName: nameof(PlayableGen),
            requiredComponent: typeof(TestProvider)
        );
    }
}

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OctopusS1SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/OctopusS1Prototype.unity";

    [MenuItem("Octopus S1/Build Prototype Scene")]
    public static void BuildPrototypeScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Octopus S1 — prototype runtime");
        root.AddComponent<OctopusS1Prototype>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.companyName = "EVENTO";
        PlayerSettings.productName = "Octopus S1";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.evento.octopuss1");
        AssetDatabase.SaveAssets();
        Debug.Log("Octopus S1 prototype scene created at " + ScenePath + ". All geometry is placeholder blockout.");
    }
}
#endif

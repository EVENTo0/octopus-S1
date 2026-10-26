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
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new System.IO.IOException("Could not save prototype scene.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.companyName = "EVENTO";
        PlayerSettings.productName = "Octopus S1";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.evento.octopuss1");
        var shader = Shader.Find("Standard");
        if (shader == null) throw new System.InvalidOperationException("Standard shader missing.");
        Directory.CreateDirectory("Assets/Resources");
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Blockout.mat") == null)
            AssetDatabase.CreateAsset(new Material(shader), "Assets/Resources/Blockout.mat");
        AssetDatabase.SaveAssets();
        Debug.Log("Octopus S1 prototype scene created at " + ScenePath + ". All geometry is placeholder blockout.");
    }
}
#endif

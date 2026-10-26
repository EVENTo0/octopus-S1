using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MissionTests
{
    private OctopusS1Prototype game;
    private T Get<T>(string name) => (T)typeof(OctopusS1Prototype)
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
    private void Call(string name) => typeof(OctopusS1Prototype)
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);

    [UnityTest]
    public IEnumerator MissionRequiresCellsRelayAndExtraction()
    {
        SceneManager.SetActiveScene(SceneManager.CreateScene("MissionTest"));
        game = new GameObject("Mission").AddComponent<OctopusS1Prototype>();
        yield return null;
        game.enabled = false;
        var player = Get<Transform>("playerRoot");
        player.position = Get<Transform>("extraction").position;
        Call("TryInteract");
        Assert.IsFalse(Get<bool>("missionComplete"));
        player.position = Get<Transform>("relay").position;
        Call("TryInteract");
        Assert.IsFalse(Get<bool>("relayActive"));
        foreach (var cell in Get<Transform[]>("resources"))
        {
            player.position = cell.position;
            Call("CollectNearbyResources");
            yield return null;
        }
        Assert.AreEqual(3, Get<int>("collected"));
        player.position = Get<Transform>("extraction").position;
        Call("TryInteract");
        Assert.IsFalse(Get<bool>("missionComplete"));
        player.position = Get<Transform>("relay").position;
        Call("TryInteract");
        Assert.IsTrue(Get<bool>("relayActive"));
        player.position = Get<Transform>("extraction").position;
        Call("TryInteract");
        Assert.IsTrue(Get<bool>("missionComplete"));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        // The blockout creates root objects in the test's own scene. Unload only that scene:
        // the runner's InitTestScene hosts the test controller and must stay loaded.
        var scene = SceneManager.GetSceneByName("MissionTest");
        if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
    }
}

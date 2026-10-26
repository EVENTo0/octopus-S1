using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MissionTests
{
    private OctopusS1Prototype game;
    private string savedPrefs;

    private T Get<T>(string name) => (T)typeof(OctopusS1Prototype)
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
    private object Call(string name, params object[] args) => typeof(OctopusS1Prototype)
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, args);
    private string State => Get<object>("state").ToString();

    [SetUp]
    public void KeepOwnerSave()
    {
        // Tests write progress; restore whatever the editor had before.
        savedPrefs = PlayerPrefs.GetString(OctopusS1SaveStore.Key, null);
        PlayerPrefs.DeleteKey(OctopusS1SaveStore.Key);
    }

    private IEnumerator StartGame()
    {
        SceneManager.SetActiveScene(SceneManager.CreateScene("MissionTest"));
        game = new GameObject("Mission").AddComponent<OctopusS1Prototype>();
        yield return null;
        game.enabled = false;
    }

    private IEnumerator CollectAllCells()
    {
        var player = Get<Transform>("playerRoot");
        foreach (var cell in (Transform[])Get<Transform[]>("resources").Clone())
        {
            player.position = cell.position;
            Call("CollectNearbyResources");
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator MissionRequiresCellsRelayAndExtraction()
    {
        yield return StartGame();
        Assert.AreEqual("Menu", State);
        var player = Get<Transform>("playerRoot");
        player.position = Get<Transform>("extraction").position;
        Call("TryInteract");
        Assert.IsFalse(Get<bool>("missionComplete"));
        player.position = Get<Transform>("relay").position;
        Call("TryInteract");
        Assert.IsFalse(Get<bool>("relayActive"));
        yield return CollectAllCells();
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
        Assert.AreEqual("Result", State);
        Assert.AreEqual(1, OctopusS1SaveStore.Load().missionsCompleted, "Completed mission is persisted.");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator CellCountsOnceEvenWhenCheckedTwiceInOneFrame()
    {
        yield return StartGame();
        var player = Get<Transform>("playerRoot");
        player.position = Get<Transform[]>("resources")[0].position;
        Call("CollectNearbyResources");
        Call("CollectNearbyResources");
        Assert.AreEqual(1, Get<int>("collected"));
        yield return null;
        Call("CollectNearbyResources");
        Assert.AreEqual(1, Get<int>("collected"));
    }

    [UnityTest]
    public IEnumerator RestartGivesAFreshMissionAfterSuccess()
    {
        yield return StartGame();
        yield return CollectAllCells();
        var player = Get<Transform>("playerRoot");
        player.position = Get<Transform>("relay").position;
        Call("TryInteract");
        player.position = Get<Transform>("extraction").position;
        Call("TryInteract");
        Assert.IsTrue(Get<bool>("missionComplete"));

        var oldWorld = Get<Transform>("worldRoot");
        Call("RestartMission");
        yield return null;
        Assert.IsTrue(oldWorld == null, "Old world is removed.");
        Assert.IsFalse(Get<bool>("missionComplete"));
        Assert.IsFalse(Get<bool>("relayActive"));
        Assert.AreEqual(0, Get<int>("collected"));
        foreach (var cell in Get<Transform[]>("resources")) Assert.IsNotNull(cell);
        yield return CollectAllCells();
        Assert.AreEqual(3, Get<int>("collected"), "Second run collects three new cells.");
    }

    [UnityTest]
    public IEnumerator FallingOutOfTheWorldReturnsToStart()
    {
        yield return StartGame();
        var player = Get<Transform>("playerRoot");
        player.position = new Vector3(3f, -20f, 3f);
        Call("RecoverIfOutOfBounds");
        Assert.Less(Vector3.Distance(player.position, new Vector3(0f, 0f, -9f)), 0.01f);
    }

    [UnityTest]
    public IEnumerator TouchJoystickAndButtons()
    {
        yield return StartGame();
        if (Screen.width < 100 || Screen.height < 100) Assert.Ignore("No usable screen size in this test player.");
        Call("SetState", Get<object>("state").GetType().GetField("Playing").GetValue(null));
        var start = new Vector2(Screen.width * 0.2f, Screen.height * 0.3f);
        Call("HandlePointer", 7, start, TouchPhase.Began);
        Call("HandlePointer", 7, start + new Vector2(0f, Screen.height), TouchPhase.Moved);
        Assert.AreEqual(1f, Get<Vector2>("touchMoveInput").y, 0.001f, "Dragging up on the left moves forward.");

        var yaw = Get<float>("cameraYaw");
        var look = new Vector2(Screen.width * 0.7f, Screen.height * 0.5f);
        Call("HandlePointer", 8, look, TouchPhase.Began);
        Call("HandlePointer", 8, look + new Vector2(Screen.width * 0.1f, 0f), TouchPhase.Moved);
        Assert.Greater(Get<float>("cameraYaw"), yaw, "A second finger turns the camera while moving.");

        Call("HandlePointer", 7, start, TouchPhase.Ended);
        Assert.AreEqual(Vector2.zero, Get<Vector2>("touchMoveInput"));

        var interact = (Rect)Call("InteractRect");
        Call("HandlePointer", 9, new Vector2(interact.center.x, Screen.height - interact.center.y), TouchPhase.Began);
        Assert.IsTrue(Get<bool>("interactRequested"));
    }

    [Test]
    public void SaveDataRecoversFromCorruptionAndBadValues()
    {
        bool recovered;
        var corrupt = OctopusS1SaveData.Parse("{not json", out recovered);
        Assert.IsTrue(recovered);
        Assert.AreEqual(0, corrupt.missionsCompleted);

        var future = OctopusS1SaveData.Parse("{\"schema\":99,\"missionsCompleted\":5}", out recovered);
        Assert.IsTrue(recovered);
        Assert.AreEqual(0, future.missionsCompleted);

        var repaired = OctopusS1SaveData.Parse("{\"schema\":1,\"language\":\"xx\",\"volume\":7,\"missionsCompleted\":-3}", out recovered);
        Assert.IsTrue(recovered);
        Assert.AreEqual("", repaired.language);
        Assert.AreEqual(1f, repaired.volume);
        Assert.AreEqual(0, repaired.missionsCompleted);

        var data = new OctopusS1SaveData { language = "ar", soundOn = false, volume = 0.3f, missionsCompleted = 4, bestTimeSeconds = 41.5f };
        var roundTrip = OctopusS1SaveData.Parse(data.ToJson(), out recovered);
        Assert.IsFalse(recovered);
        Assert.AreEqual("ar", roundTrip.language);
        Assert.IsFalse(roundTrip.soundOn);
        Assert.AreEqual(4, roundTrip.missionsCompleted);
        Assert.AreEqual(41.5f, roundTrip.bestTimeSeconds);
    }

    [Test]
    public void ArabicIsShapedAndOrderedForDisplay()
    {
        // سلام: initial seen, lam-alef final ligature, isolated meem; drawn right to left.
        Assert.AreEqual("ﻡﻼﺳ", ArabicText.Visual("سلام"));
        // Numbers keep their reading order inside an RTL line.
        Assert.AreEqual("12 ﺏ", ArabicText.Visual("ب 12"));
        Assert.AreEqual("1 / 3 :ﺭ", ArabicText.Visual("ر: 1 / 3"));
        Assert.AreEqual("Scan", ArabicText.Visual("Scan"), "Latin-only text is untouched.");
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        // The blockout creates root objects in the test's own scene. Unload only that scene:
        // the runner's InitTestScene hosts the test controller and must stay loaded.
        var scene = SceneManager.GetSceneByName("MissionTest");
        if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        Time.timeScale = 1f;
        if (savedPrefs != null) PlayerPrefs.SetString(OctopusS1SaveStore.Key, savedPrefs);
        else PlayerPrefs.DeleteKey(OctopusS1SaveStore.Key);
    }
}

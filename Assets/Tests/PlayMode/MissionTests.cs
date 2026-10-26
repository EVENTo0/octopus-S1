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

    // ---------- Real frame path with injected touches (review R2) ----------

    private void Play() => Call("SetState", Get<object>("state").GetType().GetField("Playing").GetValue(null));

    private static Vector2 ScreenPoint(Rect guiRect) => new Vector2(guiRect.center.x, Screen.height - guiRect.center.y);
    private static OctopusS1Prototype.PointerSample Touch(int finger, Vector2 position, TouchPhase phase) =>
        new OctopusS1Prototype.PointerSample(finger, position, phase);

    // Runs one gameplay frame exactly as Update does, with these touches reported this frame.
    private void Frame(params OctopusS1Prototype.PointerSample[] touches)
    {
        typeof(OctopusS1Prototype).GetField("injectedPointers", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(game, new System.Collections.Generic.List<OctopusS1Prototype.PointerSample>(touches));
        Call("Step", 0.1f);
    }

    // CharacterController keeps its own position; disable it to teleport so the next Move starts there.
    private void Place(Vector3 position)
    {
        var controller = Get<CharacterController>("player");
        controller.enabled = false;
        Get<Transform>("playerRoot").position = position;
        controller.enabled = true;
    }

    private Vector2 PauseButton => ScreenPoint((Rect)Call("PauseRect"));
    private Vector2 InteractButton => ScreenPoint((Rect)Call("InteractRect"));
    private Vector2 LeftSide => new Vector2(Screen.width * 0.2f, Screen.height * 0.3f);

    [UnityTest]
    public IEnumerator PauseTouchStopsCollectionInTheSameFrame()
    {
        yield return StartGame();
        if (Screen.width < 100 || Screen.height < 100) Assert.Ignore("No usable screen size in this test player.");
        Play();
        Place(Get<Transform[]>("resources")[0].position);
        Frame(Touch(1, PauseButton, TouchPhase.Began));
        Assert.AreEqual("Paused", State);
        Assert.AreEqual(0, Get<int>("collected"), "Nothing is collected in the frame Pause was pressed.");
        Frame();
        Assert.AreEqual(0, Get<int>("collected"), "Nothing is collected while paused.");
    }

    [UnityTest]
    public IEnumerator InteractTouchesAroundPauseDoNotActivateTheRelay()
    {
        yield return StartGame();
        if (Screen.width < 100 || Screen.height < 100) Assert.Ignore("No usable screen size in this test player.");
        yield return CollectAllCells();
        Play();
        Place(Get<Transform>("relay").position + new Vector3(0f, -1.1f, -1.5f));
        Frame(Touch(1, InteractButton, TouchPhase.Began), Touch(2, PauseButton, TouchPhase.Began), Touch(3, InteractButton, TouchPhase.Began));
        Assert.AreEqual("Paused", State);
        Assert.IsFalse(Get<bool>("relayActive"), "Interact before or after Pause in the same frame is dropped.");
        Assert.IsFalse(Get<bool>("interactRequested"));
        Play();
        Frame();
        Assert.IsFalse(Get<bool>("relayActive"), "No stale interaction fires on resume.");
        Frame(Touch(4, InteractButton, TouchPhase.Began));
        Assert.IsTrue(Get<bool>("relayActive"), "A new Interact after resume works.");
    }

    [UnityTest]
    public IEnumerator JoystickMovesInTheSameFrameAndNeverSticksAfterPauseOrCancel()
    {
        yield return StartGame();
        if (Screen.width < 100 || Screen.height < 100) Assert.Ignore("No usable screen size in this test player.");
        Play();
        var player = Get<Transform>("playerRoot");
        var up = LeftSide + new Vector2(0f, Screen.height * 0.3f);
        var z = player.position.z;
        Frame(Touch(1, LeftSide, TouchPhase.Began), Touch(1, up, TouchPhase.Moved));
        Assert.Greater(player.position.z, z + 0.1f, "Touch movement applies in the frame it is read.");

        Frame(Touch(1, up, TouchPhase.Stationary), Touch(2, PauseButton, TouchPhase.Began));
        Play();
        var resumed = player.position;
        Frame(Touch(1, up, TouchPhase.Stationary));
        Assert.Less(Vector3.Distance(resumed, player.position), 0.01f, "A finger held through Pause does not keep moving the player.");

        Frame(Touch(5, LeftSide, TouchPhase.Began), Touch(5, up, TouchPhase.Moved));
        Frame(Touch(5, up, TouchPhase.Canceled));
        var cancelled = player.position;
        Frame();
        Assert.Less(Vector3.Distance(cancelled, player.position), 0.01f, "A cancelled touch releases the joystick.");
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

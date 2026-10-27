using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime blockout for the first Octopus S1 mission.
/// All generated geometry is temporary and must be replaced by approved original assets.
/// </summary>
public sealed class OctopusS1Prototype : MonoBehaviour
{
    private enum GameState { Menu, Help, Settings, Playing, Paused, Result }

    /// <summary>A touch or mouse sample in screen pixels (origin bottom left), as reported by Input.</summary>
    public struct PointerSample
    {
        public int finger;
        public Vector2 position;
        public TouchPhase phase;
        public PointerSample(int finger, Vector2 position, TouchPhase phase)
        {
            this.finger = finger;
            this.position = position;
            this.phase = phase;
        }
    }

    private const float WalkSpeed = 4.4f;
    private const float WorldHalfSize = 24f;
    private const int NoFinger = int.MinValue;
    private const int MouseFinger = -1;
    private static readonly Vector3 SpawnPoint = new Vector3(0f, 0f, -9f);

    private Transform worldRoot;
    private CharacterController player;
    private Transform playerRoot;
    private Transform cameraTransform;
    private Transform companion;
    private readonly Transform[] resources = new Transform[3];
    private Transform relay;
    private Transform extraction;
    private int collected;
    private bool relayActive;
    private bool missionComplete;
    private bool scanPulse;
    private float scanUntil;
    private float cameraYaw;
    private float missionTime;
    private Vector2 moveInput;
    private Vector2 touchMoveInput;
    private bool interactRequested;
    private bool scanRequested;
    // When set (tests only), pointer samples come from this list instead of Input; it is drained each frame.
    private List<PointerSample> injectedPointers;

    private GameState state = GameState.Menu;
    private GameState settingsReturnState = GameState.Menu;
    private OctopusS1SaveData save = new OctopusS1SaveData();
    private string toastKey;
    private float toastUntil;

    private int moveFinger = NoFinger;
    private int lookFinger = NoFinger;
    private Vector2 moveOrigin;
    private Vector2 lookLast;

    private AudioSource audioSource;
    private AudioClip collectClip;
    private AudioClip relayClip;
    private AudioClip successClip;
    private AudioClip denyClip;

    private readonly Dictionary<string, string> visualText = new Dictionary<string, string>();
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle panelStyle;
    private float styleScale;
    private string styleLanguage;

    private void Start()
    {
        Application.targetFrameRate = 60;
        save = OctopusS1SaveStore.Load();
        CreateAudio();
        ApplyAudioSettings();
        BuildWorld();
        SetState(GameState.Menu);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && state == GameState.Playing) SetState(GameState.Paused);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && state == GameState.Playing) SetState(GameState.Paused);
    }

    private void Update()
    {
        if (player == null) return;
        if (Input.GetKeyDown(KeyCode.Escape)) HandleBack();
        Step(Time.deltaTime);
    }

    private void Step(float deltaTime)
    {
        if (state != GameState.Playing) return;
        Simulate(deltaTime);
    }

    // One gameplay frame. Tests drive this directly with injected pointer samples.
    private void Simulate(float deltaTime)
    {
        // Touches first, so movement uses this frame's joystick; a Pause touch ends the frame here.
        ReadPointers();
        if (state != GameState.Playing) return;
        ReadKeyboard();
        missionTime += deltaTime;

        var direction = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
        if (direction.sqrMagnitude > 1f) direction.Normalize();
        player.Move(direction * WalkSpeed * deltaTime);
        player.Move(Vector3.down * 9.81f * deltaTime);
        if (direction.sqrMagnitude > 0.01f)
            playerRoot.rotation = Quaternion.Slerp(playerRoot.rotation, Quaternion.LookRotation(direction), deltaTime * 10f);

        UpdateCamera();
        UpdateCompanion();

        if (scanRequested)
        {
            scanRequested = false;
            scanPulse = true;
            scanUntil = Time.time + 1.6f;
            SetResourceScanGlow(true);
            ShowToast("scanning");
        }
        if (scanPulse && Time.time > scanUntil)
        {
            scanPulse = false;
            SetResourceScanGlow(false);
        }

        CollectNearbyResources();
        if (interactRequested)
        {
            interactRequested = false;
            TryInteract();
        }
        RecoverIfOutOfBounds();
    }

    // ---------- State ----------

    private void SetState(GameState next)
    {
        state = next;
        Time.timeScale = next == GameState.Playing ? 1f : 0f;
        moveFinger = NoFinger;
        lookFinger = NoFinger;
        touchMoveInput = Vector2.zero;
        moveInput = Vector2.zero;
        interactRequested = false;
        scanRequested = false;
    }

    private void HandleBack()
    {
        switch (state)
        {
            case GameState.Playing: SetState(GameState.Paused); break;
            case GameState.Paused: SetState(GameState.Playing); break;
            case GameState.Help: SetState(GameState.Menu); break;
            case GameState.Settings: SetState(settingsReturnState); break;
        }
    }

    private void StartOrContinue()
    {
        if (missionComplete) RestartMission();
        SetState(GameState.Playing);
    }

    private void RestartMission()
    {
        if (worldRoot != null) Destroy(worldRoot.gameObject);
        for (var i = 0; i < resources.Length; i++) resources[i] = null;
        collected = 0;
        relayActive = false;
        missionComplete = false;
        scanPulse = false;
        missionTime = 0f;
        cameraYaw = 0f;
        toastKey = null;
        BuildWorld();
    }

    private void OnMissionComplete()
    {
        missionComplete = true;
        save.missionsCompleted++;
        if (save.bestTimeSeconds <= 0f || missionTime < save.bestTimeSeconds) save.bestTimeSeconds = missionTime;
        OctopusS1SaveStore.Save(save);
        Play(successClip);
        SetState(GameState.Result);
    }

    // ---------- Input ----------

    private void ReadKeyboard()
    {
        var keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveInput = keyboard.sqrMagnitude > 0.04f
            ? Vector2.ClampMagnitude(keyboard, 1f)
            : Vector2.ClampMagnitude(touchMoveInput, 1f);
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) interactRequested = true;
        if (Input.GetKeyDown(KeyCode.Q)) scanRequested = true;
    }

    private void ReadPointers()
    {
        if (injectedPointers != null)
        {
            var samples = injectedPointers.ToArray();
            injectedPointers.Clear();
            // Touches after a Pause in the same frame are ignored.
            for (var i = 0; i < samples.Length && state == GameState.Playing; i++)
                HandlePointer(samples[i].finger, samples[i].position, samples[i].phase);
            return;
        }
        if (Input.touchCount > 0)
        {
            for (var i = 0; i < Input.touchCount && state == GameState.Playing; i++)
            {
                var touch = Input.GetTouch(i);
                HandlePointer(touch.fingerId, touch.position, touch.phase);
            }
            return;
        }
        if (Input.GetMouseButtonDown(0)) HandlePointer(MouseFinger, Input.mousePosition, TouchPhase.Began);
        else if (Input.GetMouseButton(0)) HandlePointer(MouseFinger, Input.mousePosition, TouchPhase.Moved);
        else if (Input.GetMouseButtonUp(0)) HandlePointer(MouseFinger, Input.mousePosition, TouchPhase.Ended);
    }

    // Left side: virtual joystick. Right side: camera drag. On-screen buttons take priority.
    // Position is in screen pixels with the origin at the bottom left, as reported by Input.
    private void HandlePointer(int finger, Vector2 position, TouchPhase phase)
    {
        var gui = new Vector2(position.x, Screen.height - position.y);
        switch (phase)
        {
            case TouchPhase.Began:
                if (InteractRect().Contains(gui)) { interactRequested = true; return; }
                if (ScanRect().Contains(gui)) { scanRequested = true; return; }
                if (PauseRect().Contains(gui)) { SetState(GameState.Paused); return; }
                if (position.x < Screen.width * 0.45f && moveFinger == NoFinger)
                {
                    moveFinger = finger;
                    moveOrigin = position;
                }
                else if (lookFinger == NoFinger)
                {
                    lookFinger = finger;
                    lookLast = position;
                }
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (finger == moveFinger)
                {
                    touchMoveInput = Vector2.ClampMagnitude((position - moveOrigin) / JoystickRadius(), 1f);
                }
                else if (finger == lookFinger)
                {
                    cameraYaw += (position.x - lookLast.x) / Mathf.Max(1f, Screen.width) * 220f;
                    lookLast = position;
                }
                break;
            default:
                if (finger == moveFinger) { moveFinger = NoFinger; touchMoveInput = Vector2.zero; }
                if (finger == lookFinger) lookFinger = NoFinger;
                break;
        }
    }

    // ---------- World ----------

    private void BuildWorld()
    {
        worldRoot = new GameObject("Octopus S1 world - placeholder").transform;
        RenderSettings.ambientLight = new Color(0.58f, 0.53f, 0.43f);
        var ground = MakePrimitive(PrimitiveType.Plane, "PLACEHOLDER - desert ground", new Vector3(0f, -0.15f, 0f), new Vector3(5f, 1f, 5f), new Color(0.67f, 0.48f, 0.29f));
        ground.GetComponent<Collider>().name = "Placeholder ground collider";
        BuildBoundary();

        CreateRock(new Vector3(-9f, 1.1f, 6f), new Vector3(7f, 2.2f, 5f), new Color(0.48f, 0.28f, 0.20f));
        CreateRock(new Vector3(10f, 1.5f, 10f), new Vector3(5f, 3f, 7f), new Color(0.56f, 0.32f, 0.22f));
        CreateRock(new Vector3(-11f, 1.6f, -9f), new Vector3(6f, 3.2f, 5f), new Color(0.51f, 0.31f, 0.23f));

        var sun = new GameObject("Placeholder sun");
        sun.transform.SetParent(worldRoot, false);
        sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = new Color(1f, 0.79f, 0.57f);

        playerRoot = new GameObject("Player - blockout").transform;
        playerRoot.SetParent(worldRoot, false);
        playerRoot.position = SpawnPoint;
        var body = MakePrimitive(PrimitiveType.Capsule, "Player body placeholder", playerRoot.position + Vector3.up, new Vector3(0.7f, 1f, 0.7f), new Color(0.21f, 0.29f, 0.29f), false);
        body.transform.SetParent(playerRoot, true);
        player = playerRoot.gameObject.AddComponent<CharacterController>();
        player.height = 1.8f;
        player.radius = 0.36f;
        player.center = Vector3.up * 0.9f;

        var cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(worldRoot, false);
        cameraObject.tag = "MainCamera";
        cameraTransform = cameraObject.transform;
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 62f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.86f, 0.74f, 0.58f);
        cameraObject.AddComponent<AudioListener>();

        BuildCompanion();
        BuildMissionObjects();

        var marker = MakePrimitive(PrimitiveType.Cylinder, "Relay marker placeholder", new Vector3(0f, 0.25f, 5f), new Vector3(0.8f, 0.25f, 0.8f), new Color(0.30f, 0.72f, 0.76f), false);
        marker.transform.localScale = new Vector3(0.55f, 0.15f, 0.55f);
        UpdateCamera();
        // Colliders were just placed and scaled. Physics only syncs transforms on a simulation step,
        // which does not run while paused (timeScale 0); without this the first Move after a
        // restart can see the unscaled ground and fall through it.
        Physics.SyncTransforms();
    }

    // Invisible walls keep the player on the blockout ground.
    private void BuildBoundary()
    {
        var size = WorldHalfSize + 1f;
        var walls = new[] {
            new KeyValuePair<Vector3, Vector3>(new Vector3(0f, 2f, size), new Vector3(size * 2f, 6f, 1f)),
            new KeyValuePair<Vector3, Vector3>(new Vector3(0f, 2f, -size), new Vector3(size * 2f, 6f, 1f)),
            new KeyValuePair<Vector3, Vector3>(new Vector3(size, 2f, 0f), new Vector3(1f, 6f, size * 2f)),
            new KeyValuePair<Vector3, Vector3>(new Vector3(-size, 2f, 0f), new Vector3(1f, 6f, size * 2f)) };
        foreach (var wall in walls)
        {
            var go = new GameObject("World boundary");
            go.transform.SetParent(worldRoot, false);
            go.transform.position = wall.Key;
            go.AddComponent<BoxCollider>().size = wall.Value;
        }
    }

    private void BuildCompanion()
    {
        companion = new GameObject("Octopus companion - placeholder").transform;
        companion.SetParent(worldRoot, false);
        companion.position = playerRoot.position + new Vector3(1.1f, 0.5f, -0.8f);
        // The companion has no colliders, so it can never block or push the player.
        var body = MakePrimitive(PrimitiveType.Sphere, "Companion body", companion.position, Vector3.one * 0.95f, new Color(0.82f, 0.72f, 0.55f), false);
        body.transform.SetParent(companion, true);
        body.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
        body.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0.02f, 0.32f, 0.42f));
        for (var i = 0; i < 6; i++)
        {
            var angle = i * Mathf.PI * 2f / 6f;
            var offset = new Vector3(Mathf.Cos(angle), -0.25f, Mathf.Sin(angle)) * 0.63f;
            var tentacle = MakePrimitive(PrimitiveType.Capsule, "Tentacle placeholder", companion.position + offset, new Vector3(0.18f, 0.36f, 0.18f), new Color(0.67f, 0.50f, 0.36f), false);
            tentacle.transform.SetParent(companion, true);
        }
        var eye = MakePrimitive(PrimitiveType.Sphere, "Companion scan eye", companion.position + new Vector3(0f, 0.06f, 0.44f), new Vector3(0.26f, 0.26f, 0.12f), new Color(0.08f, 0.85f, 0.95f), false);
        eye.transform.SetParent(companion, true);
    }

    private void BuildMissionObjects()
    {
        var positions = new[] { new Vector3(-4f, 0.35f, -1f), new Vector3(3f, 0.35f, 2f), new Vector3(-2f, 0.35f, 5f) };
        for (var i = 0; i < resources.Length; i++)
            resources[i] = MakePrimitive(PrimitiveType.Cube, "Relay cell " + (i + 1) + " - placeholder", positions[i], Vector3.one * 0.55f, new Color(0.26f, 0.78f, 0.72f), false).transform;

        relay = MakePrimitive(PrimitiveType.Cylinder, "Signal relay - placeholder", new Vector3(0f, 1.1f, 8f), new Vector3(1.1f, 2.2f, 1.1f), new Color(0.48f, 0.55f, 0.48f)).transform;
        extraction = MakePrimitive(PrimitiveType.Cylinder, "Extraction point - placeholder", new Vector3(0f, 0.1f, 14f), new Vector3(2f, 0.2f, 2f), new Color(0.86f, 0.63f, 0.28f)).transform;
    }

    private void CreateRock(Vector3 position, Vector3 scale, Color color)
    {
        var rock = MakePrimitive(PrimitiveType.Cube, "Rock formation - placeholder", position, scale, color);
        rock.transform.rotation = Quaternion.Euler(0f, Random.Range(-14f, 14f), Random.Range(-4f, 4f));
    }

    private GameObject MakePrimitive(PrimitiveType type, string objectName, Vector3 position, Vector3 scale, Color color, bool solid = true)
    {
        var item = GameObject.CreatePrimitive(type);
        item.name = objectName;
        item.transform.SetParent(worldRoot, false);
        item.transform.position = position;
        item.transform.localScale = scale;
        if (!solid) Destroy(item.GetComponent<Collider>());
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        item.GetComponent<Renderer>().material = material;
        return item;
    }

    private void UpdateCamera()
    {
        cameraTransform.rotation = Quaternion.Euler(18f, cameraYaw, 0f);
        cameraTransform.position = playerRoot.position - cameraTransform.forward * 7.5f + Vector3.up * 3.2f;
    }

    private void UpdateCompanion()
    {
        var follow = playerRoot.position + playerRoot.right * 1.25f - playerRoot.forward * 1.05f + Vector3.up * 0.65f;
        companion.position = Vector3.Lerp(companion.position, follow, Time.deltaTime * 3.2f);
        companion.Rotate(Vector3.up, 36f * Time.deltaTime, Space.World);
        if (scanPulse)
        {
            var pulse = Mathf.PingPong(Time.time * 1.8f, 1f);
            companion.localScale = Vector3.one * (1f + pulse * 0.12f);
        }
        else companion.localScale = Vector3.one;
    }

    private void SetResourceScanGlow(bool enabled)
    {
        foreach (var resource in resources)
        {
            if (resource == null) continue;
            var material = resource.GetComponent<Renderer>().material;
            if (enabled)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.05f, 0.65f, 0.72f));
            }
            else
            {
                material.SetColor("_EmissionColor", Color.black);
                material.DisableKeyword("_EMISSION");
            }
        }
    }

    // ---------- Mission ----------

    private void CollectNearbyResources()
    {
        for (var i = 0; i < resources.Length; i++)
        {
            var resource = resources[i];
            if (resource == null) continue;
            resource.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);
            if (Vector3.Distance(playerRoot.position, resource.position) < 1.25f)
            {
                // Clear the slot first: Destroy is deferred, so the cell must not be counted again this frame.
                resources[i] = null;
                collected++;
                Destroy(resource.gameObject);
                Play(collectClip);
                ShowToast("cell_got");
                break;
            }
        }
    }

    private void TryInteract()
    {
        if (missionComplete) return;
        if (!relayActive && Vector3.Distance(playerRoot.position, relay.position) < 2.3f)
        {
            if (collected < 3)
            {
                Play(denyClip);
                ShowToast("need_cells");
                return;
            }
            relayActive = true;
            relay.GetComponent<Renderer>().material.color = new Color(0.18f, 0.76f, 0.76f);
            Play(relayClip);
            ShowToast("relay_on");
            return;
        }
        if (Vector3.Distance(playerRoot.position, extraction.position) < 2.5f)
        {
            if (relayActive) OnMissionComplete();
            else
            {
                Play(denyClip);
                ShowToast("need_relay");
            }
            return;
        }
        ShowToast("nothing");
    }

    private void RecoverIfOutOfBounds()
    {
        var p = playerRoot.position;
        if (p.y < -4f || Mathf.Abs(p.x) > WorldHalfSize + 2f || Mathf.Abs(p.z) > WorldHalfSize + 2f)
            ReturnToStart();
    }

    private void ReturnToStart()
    {
        player.enabled = false;
        playerRoot.position = SpawnPoint;
        playerRoot.rotation = Quaternion.identity;
        player.enabled = true;
        Physics.SyncTransforms();
        companion.position = SpawnPoint + new Vector3(1.1f, 0.5f, -0.8f);
        cameraYaw = 0f;
        UpdateCamera();
        ShowToast("recovered");
    }

    // ---------- Audio ----------

    private void CreateAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.ignoreListenerPause = true;
        collectClip = Tone("collect", 880f, 0.12f);
        relayClip = Tone("relay", 523f, 0.35f);
        successClip = Tone("success", 659f, 0.6f);
        denyClip = Tone("deny", 196f, 0.18f);
    }

    private static AudioClip Tone(string clipName, float frequency, float seconds)
    {
        const int rate = 22050;
        var samples = new float[Mathf.CeilToInt(rate * seconds)];
        for (var i = 0; i < samples.Length; i++)
        {
            var envelope = 1f - (float)i / samples.Length;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / rate) * 0.35f * envelope;
        }
        var clip = AudioClip.Create(clipName, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null && save.soundOn) audioSource.PlayOneShot(clip);
    }

    private void ApplyAudioSettings()
    {
        AudioListener.volume = save.soundOn ? save.volume : 0f;
    }

    // ---------- UI ----------

    private string Language => save.language != "" ? save.language :
        (Application.systemLanguage == SystemLanguage.Arabic ? "ar" : "en");

    private string T(string key, params object[] args)
    {
        var text = OctopusS1Text.Get(Language, key);
        if (args.Length > 0) text = string.Format(text, args);
        string visual;
        if (!visualText.TryGetValue(text, out visual))
        {
            visual = ArabicText.Visual(text);
            if (visualText.Count > 256) visualText.Clear();
            visualText[text] = visual;
        }
        return visual;
    }

    private void ShowToast(string key)
    {
        toastKey = key;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private float UiScale => Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) / 720f, 0.5f, 3f);
    private float JoystickRadius() => Mathf.Max(40f, Mathf.Min(Screen.width, Screen.height) * 0.12f);

    // Safe area in GUI coordinates (origin top left).
    private Rect SafeGuiRect()
    {
        var safe = Screen.safeArea;
        return new Rect(safe.xMin, Screen.height - safe.yMax, safe.width, safe.height);
    }

    private Rect InteractRect()
    {
        var u = UiScale; var s = SafeGuiRect();
        return new Rect(s.xMax - 214f * u, s.yMax - 154f * u, 190f * u, 130f * u);
    }

    private Rect ScanRect()
    {
        var u = UiScale; var interact = InteractRect();
        return new Rect(interact.x, interact.y - 126f * u, interact.width, 110f * u);
    }

    private Rect PauseRect()
    {
        var u = UiScale; var s = SafeGuiRect();
        return new Rect(s.xMax - 120f * u, s.yMin + 20f * u, 100f * u, 80f * u);
    }

    private void EnsureStyles()
    {
        var u = UiScale;
        if (titleStyle != null && Mathf.Approximately(styleScale, u) && styleLanguage == Language) return;
        styleScale = u;
        styleLanguage = Language;
        var textAnchor = Language == "ar" ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(44 * u), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(28 * u), alignment = textAnchor, wordWrap = true };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(30 * u), wordWrap = true };
        panelStyle = new GUIStyle(GUI.skin.box);
        titleStyle.normal.textColor = Color.white;
        labelStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (player == null) return;
        EnsureStyles();
        GUI.matrix = Matrix4x4.identity;
        switch (state)
        {
            case GameState.Menu: DrawMenu(); break;
            case GameState.Help: DrawHelp(); break;
            case GameState.Settings: DrawSettings(); break;
            case GameState.Playing: DrawHud(); break;
            case GameState.Paused: DrawHud(); DrawPause(); break;
            case GameState.Result: DrawResult(); break;
        }
    }

    private Rect CenterPanel(float width, float height)
    {
        var u = UiScale; var s = SafeGuiRect();
        var w = Mathf.Min(width * u, s.width - 20f);
        var h = Mathf.Min(height * u, s.height - 20f);
        return new Rect(s.center.x - w * 0.5f, s.center.y - h * 0.5f, w, h);
    }

    private bool PanelButton(Rect panel, int row, string text)
    {
        var u = UiScale;
        var rect = new Rect(panel.x + 30f * u, panel.y + (150f + row * 92f) * u, panel.width - 60f * u, 78f * u);
        return GUI.Button(rect, text, buttonStyle);
    }

    private void DrawPanel(Rect panel, string title)
    {
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x, panel.y + 20f * UiScale, panel.width, 70f * UiScale), title, titleStyle);
    }

    private void DrawMenu()
    {
        var panel = CenterPanel(680f, 620f);
        DrawPanel(panel, T("title"));
        var u = UiScale;
        GUI.Label(new Rect(panel.x + 30f * u, panel.y + 88f * u, panel.width - 60f * u, 50f * u), T("subtitle"), labelStyle);
        var inProgress = missionTime > 0f && !missionComplete;
        if (PanelButton(panel, 0, T(inProgress ? "continue" : "start"))) StartOrContinue();
        if (inProgress && PanelButton(panel, 1, T("restart"))) { RestartMission(); SetState(GameState.Playing); }
        if (PanelButton(panel, inProgress ? 2 : 1, T("help"))) SetState(GameState.Help);
        if (PanelButton(panel, inProgress ? 3 : 2, T("settings"))) { settingsReturnState = GameState.Menu; SetState(GameState.Settings); }
        if (!inProgress && PanelButton(panel, 3, T("language")))
        {
            save.language = Language == "ar" ? "en" : "ar";
            OctopusS1SaveStore.Save(save);
        }
    }

    private void DrawHelp()
    {
        var panel = CenterPanel(900f, 620f);
        DrawPanel(panel, T("help"));
        var u = UiScale;
        GUI.Label(new Rect(panel.x + 30f * u, panel.y + 110f * u, panel.width - 60f * u, panel.height - 230f * u), T("help_body"), labelStyle);
        if (GUI.Button(new Rect(panel.x + 30f * u, panel.yMax - 100f * u, panel.width - 60f * u, 78f * u), T("back"), buttonStyle))
            SetState(GameState.Menu);
    }

    private void DrawSettings()
    {
        var panel = CenterPanel(680f, 620f);
        DrawPanel(panel, T("settings"));
        var u = UiScale;
        if (PanelButton(panel, 0, T("language")))
        {
            save.language = Language == "ar" ? "en" : "ar";
            OctopusS1SaveStore.Save(save);
        }
        if (PanelButton(panel, 1, T(save.soundOn ? "sound_on" : "sound_off")))
        {
            save.soundOn = !save.soundOn;
            ApplyAudioSettings();
            OctopusS1SaveStore.Save(save);
        }
        var row = new Rect(panel.x + 30f * u, panel.y + (150f + 2 * 92f) * u, panel.width - 60f * u, 78f * u);
        GUI.Label(new Rect(row.x, row.y, row.width, 36f * u), T("volume") + "  " + Mathf.RoundToInt(save.volume * 100f) + "%", labelStyle);
        var volume = GUI.HorizontalSlider(new Rect(row.x, row.y + 46f * u, row.width, 30f * u), save.volume, 0f, 1f);
        if (!Mathf.Approximately(volume, save.volume))
        {
            save.volume = volume;
            ApplyAudioSettings();
        }
        if (PanelButton(panel, 3, T("back")))
        {
            OctopusS1SaveStore.Save(save);
            SetState(settingsReturnState);
        }
    }

    private void DrawPause()
    {
        var panel = CenterPanel(680f, 640f);
        DrawPanel(panel, T("paused"));
        if (PanelButton(panel, 0, T("resume"))) SetState(GameState.Playing);
        if (PanelButton(panel, 1, T("restart"))) { RestartMission(); SetState(GameState.Playing); }
        if (PanelButton(panel, 2, T("recover"))) { ReturnToStart(); SetState(GameState.Playing); }
        if (PanelButton(panel, 3, T("settings"))) { settingsReturnState = GameState.Paused; SetState(GameState.Settings); }
        if (PanelButton(panel, 4, T("menu"))) SetState(GameState.Menu);
    }

    private void DrawResult()
    {
        var panel = CenterPanel(820f, 520f);
        DrawPanel(panel, T("result_title"));
        var u = UiScale;
        GUI.Label(new Rect(panel.x + 30f * u, panel.y + 100f * u, panel.width - 60f * u, 50f * u),
            T("result_stats", missionTime.ToString("0.0"), save.bestTimeSeconds.ToString("0.0"), save.missionsCompleted), labelStyle);
        if (PanelButton(panel, 1, T("play_again"))) { RestartMission(); SetState(GameState.Playing); }
        if (PanelButton(panel, 2, T("menu"))) SetState(GameState.Menu);
    }

    private void DrawHud()
    {
        var u = UiScale; var s = SafeGuiRect();
        var hud = new Rect(s.xMin + 20f * u, s.yMin + 20f * u, Mathf.Min(620f * u, s.width * 0.6f), 120f * u);
        GUI.Box(hud, GUIContent.none, panelStyle);
        var objective = missionComplete ? T("obj_done") : relayActive ? T("obj_extract") :
            collected >= 3 ? T("obj_relay") : T("obj_collect", collected);
        GUI.Label(new Rect(hud.x + 16f * u, hud.y + 8f * u, hud.width - 32f * u, 56f * u), objective, labelStyle);
        if (toastKey != null && Time.unscaledTime < toastUntil)
            GUI.Label(new Rect(hud.x + 16f * u, hud.y + 60f * u, hud.width - 32f * u, 52f * u), T(toastKey), labelStyle);

        GUI.Box(InteractRect(), T("interact"), buttonStyle);
        GUI.Box(ScanRect(), T("scan"), buttonStyle);
        GUI.Box(PauseRect(), "II", buttonStyle);

        if (moveFinger != NoFinger)
        {
            var r = JoystickRadius();
            var origin = new Vector2(moveOrigin.x, Screen.height - moveOrigin.y);
            var knob = origin + new Vector2(touchMoveInput.x, -touchMoveInput.y) * r;
            GUI.Box(new Rect(origin.x - r, origin.y - r, r * 2f, r * 2f), GUIContent.none, panelStyle);
            GUI.Box(new Rect(knob.x - r * 0.35f, knob.y - r * 0.35f, r * 0.7f, r * 0.7f), GUIContent.none, buttonStyle);
        }
    }
}

using UnityEngine;

/// <summary>
/// Runtime blockout for the first Octopus S1 mission.
/// All generated geometry is temporary and must be replaced by approved original assets.
/// </summary>
public sealed class OctopusS1Prototype : MonoBehaviour
{
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
    private float cameraYaw = 0f;
    private Vector2 moveInput;
    private bool interactRequested;
    private bool scanRequested;
    private const float WalkSpeed = 4.4f;

    private void Start()
    {
        Application.targetFrameRate = 60;
        BuildWorld();
    }

    private void Update()
    {
        if (player == null || missionComplete) return;

        ReadKeyboard();
        var direction = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
        if (direction.sqrMagnitude > 1f) direction.Normalize();
        player.Move(direction * WalkSpeed * Time.deltaTime);
        player.Move(Vector3.down * 9.81f * Time.deltaTime);

        cameraTransform.rotation = Quaternion.Euler(18f, cameraYaw, 0f);
        cameraTransform.position = playerRoot.position - cameraTransform.forward * 7.5f + Vector3.up * 3.2f;
        UpdateCompanion();

        if (scanRequested)
        {
            scanRequested = false;
            scanPulse = true;
            scanUntil = Time.time + 1.6f;
        }
        if (Time.time > scanUntil) scanPulse = false;

        CollectNearbyResources();
        if (interactRequested)
        {
            interactRequested = false;
            TryInteract();
        }
    }

    private void ReadKeyboard()
    {
        var keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (keyboard.sqrMagnitude > 0.04f) moveInput = Vector2.ClampMagnitude(keyboard, 1f);
        else moveInput = Vector2.zero;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) interactRequested = true;
        if (Input.GetKeyDown(KeyCode.Q)) scanRequested = true;
        if (Input.GetKeyDown(KeyCode.Escape)) moveInput = Vector2.zero;
    }

    private void BuildWorld()
    {
        RenderSettings.ambientLight = new Color(0.58f, 0.53f, 0.43f);
        var ground = MakePrimitive(PrimitiveType.Plane, "PLACEHOLDER — desert ground", new Vector3(0f, -0.15f, 0f), new Vector3(5f, 1f, 5f), new Color(0.67f, 0.48f, 0.29f));
        ground.GetComponent<Collider>().name = "Placeholder ground collider";

        CreateRock( new Vector3(-9f, 1.1f, 6f), new Vector3(7f, 2.2f, 5f), new Color(0.48f, 0.28f, 0.20f));
        CreateRock( new Vector3(10f, 1.5f, 10f), new Vector3(5f, 3f, 7f), new Color(0.56f, 0.32f, 0.22f));
        CreateRock( new Vector3(-11f, 1.6f, -9f), new Vector3(6f, 3.2f, 5f), new Color(0.51f, 0.31f, 0.23f));

        var sun = new GameObject("Placeholder sun");
        sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = new Color(1f, 0.79f, 0.57f);

        playerRoot = new GameObject("Player — blockout").transform;
        playerRoot.position = new Vector3(0f, 0f, -9f);
        var body = MakePrimitive(PrimitiveType.Capsule, "Player body placeholder", playerRoot.position + Vector3.up, new Vector3(0.7f, 1f, 0.7f), new Color(0.21f, 0.29f, 0.29f));
        body.transform.SetParent(playerRoot, true);
        Destroy(body.GetComponent<Collider>());
        player = playerRoot.gameObject.AddComponent<CharacterController>();
        player.height = 1.8f;
        player.radius = 0.36f;
        player.center = Vector3.up * 0.9f;

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraTransform = cameraObject.transform;
        cameraObject.AddComponent<Camera>().fieldOfView = 62f;
        cameraObject.AddComponent<AudioListener>();

        BuildCompanion();
        BuildMissionObjects();

        var title = new GameObject("Mission direction marker");
        title.transform.position = new Vector3(0f, 2.5f, 5f);
        var marker = MakePrimitive(PrimitiveType.Cylinder, "Relay marker placeholder", new Vector3(0f, 0.25f, 5f), new Vector3(0.8f, 0.25f, 0.8f), new Color(0.30f, 0.72f, 0.76f));
        marker.transform.localScale = new Vector3(0.55f, 0.15f, 0.55f);
    }

    private void BuildCompanion()
    {
        companion = new GameObject("Octopus companion — placeholder").transform;
        companion.position = playerRoot.position + new Vector3(1.1f, 0.5f, -0.8f);
        var body = MakePrimitive(PrimitiveType.Sphere, "Companion body", companion.position, Vector3.one * 0.95f, new Color(0.82f, 0.72f, 0.55f));
        body.transform.SetParent(companion, true);
        body.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
        body.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0.02f, 0.32f, 0.42f));
        for (var i = 0; i < 6; i++)
        {
            var angle = i * Mathf.PI * 2f / 6f;
            var offset = new Vector3(Mathf.Cos(angle), -0.25f, Mathf.Sin(angle)) * 0.63f;
            var tentacle = MakePrimitive(PrimitiveType.Capsule, "Tentacle placeholder", companion.position + offset, new Vector3(0.18f, 0.36f, 0.18f), new Color(0.67f, 0.50f, 0.36f));
            tentacle.transform.SetParent(companion, true);
        }
        var eye = MakePrimitive(PrimitiveType.Sphere, "Companion scan eye", companion.position + new Vector3(0f, 0.06f, 0.44f), new Vector3(0.26f, 0.26f, 0.12f), new Color(0.08f, 0.85f, 0.95f));
        eye.transform.SetParent(companion, true);
    }

    private void BuildMissionObjects()
    {
        var positions = new[] { new Vector3(-4f, 0.35f, -1f), new Vector3(3f, 0.35f, 2f), new Vector3(-2f, 0.35f, 5f) };
        for (var i = 0; i < resources.Length; i++)
            resources[i] = MakePrimitive(PrimitiveType.Cube, "Relay cell " + (i + 1) + " — placeholder", positions[i], Vector3.one * 0.55f, new Color(0.26f, 0.78f, 0.72f)).transform;

        relay = MakePrimitive(PrimitiveType.Cylinder, "Signal relay — placeholder", new Vector3(0f, 1.1f, 8f), new Vector3(1.1f, 2.2f, 1.1f), new Color(0.48f, 0.55f, 0.48f)).transform;
        extraction = MakePrimitive(PrimitiveType.Cylinder, "Extraction point — placeholder", new Vector3(0f, 0.1f, 14f), new Vector3(2f, 0.2f, 2f), new Color(0.86f, 0.63f, 0.28f)).transform;
    }

    private void CreateRock(Vector3 position, Vector3 scale, Color color)
    {
        var rock = MakePrimitive(PrimitiveType.Cube, "Rock formation — placeholder", position, scale, color);
        rock.transform.rotation = Quaternion.Euler(0f, Random.Range(-14f, 14f), Random.Range(-4f, 4f));
    }

    private static GameObject MakePrimitive(PrimitiveType type, string objectName, Vector3 position, Vector3 scale, Color color)
    {
        var item = GameObject.CreatePrimitive(type);
        item.name = objectName;
        item.transform.position = position;
        item.transform.localScale = scale;
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        item.GetComponent<Renderer>().material = material;
        return item;
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

    private void CollectNearbyResources()
    {
        foreach (var resource in resources)
        {
            if (resource == null) continue;
            resource.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);
            if (Vector3.Distance(playerRoot.position, resource.position) < 1.25f)
            {
                collected++;
                Destroy(resource.gameObject);
                break;
            }
        }
    }

    private void TryInteract()
    {
        if (!relayActive && Vector3.Distance(playerRoot.position, relay.position) < 2.3f && collected >= 3)
        {
            relayActive = true;
            relay.GetComponent<Renderer>().material.color = new Color(0.18f, 0.76f, 0.76f);
            return;
        }
        if (relayActive && Vector3.Distance(playerRoot.position, extraction.position) < 2.5f)
            missionComplete = true;
    }

    private void OnGUI()
    {
        var scale = Mathf.Clamp(Screen.width / 900f, 0.75f, 1.35f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        var width = Screen.width / scale;
        GUI.Box(new Rect(18f, 18f, 360f, 96f), "OCTOPUS S1  •  BLOCKOUT");
        var objective = missionComplete ? "اكتملت المهمة — عادت الإشارة" :
            relayActive ? "توجه إلى نقطة الإخلاء" : collected >= 3 ? "فعّل برج الإرسال (E)" : "اجمع خلايا الإرسال: " + collected + " / 3";
        GUI.Label(new Rect(36f, 55f, 330f, 34f), objective);
        GUI.Label(new Rect(20f, 116f, 360f, 28f), "WASD / الأسهم للحركة  •  Q للكشف  •  E للتفاعل");

        if (GUI.RepeatButton(new Rect(22f, 145f, 64f, 56f), "◀")) moveInput.x = -1f;
        if (GUI.RepeatButton(new Rect(156f, 145f, 64f, 56f), "▶")) moveInput.x = 1f;
        if (GUI.RepeatButton(new Rect(88f, 112f, 64f, 56f), "▲")) moveInput.y = 1f;
        if (GUI.RepeatButton(new Rect(88f, 178f, 64f, 56f), "▼")) moveInput.y = -1f;
        if (GUI.Button(new Rect(width - 150f, 146f, 124f, 54f), "تفاعل")) interactRequested = true;
        if (GUI.Button(new Rect(width - 150f, 208f, 124f, 54f), "كشف")) scanRequested = true;

        if (missionComplete)
        {
            GUI.Box(new Rect(width * 0.5f - 210f, 100f, 420f, 96f), "اكتملت مهمة Octopus S1");
            GUI.Label(new Rect(width * 0.5f - 170f, 145f, 350f, 32f), "تم تشغيل الإرسال والوصول إلى الإخلاء.");
        }
    }
}

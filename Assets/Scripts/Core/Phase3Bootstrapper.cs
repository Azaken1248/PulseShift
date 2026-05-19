using UnityEngine;

[DefaultExecutionOrder(-400)] // Forced even lower to beat everything
public class Phase3Bootstrapper : MonoBehaviour
{
    [Header("Engine Dependencies")]
    [SerializeField] private NoteManager _noteManager;
    [SerializeField] private NotePool _notePool;
    [SerializeField] private SliderPool _sliderPool;
    [SerializeField] private TickPool _tickPool;
    [SerializeField] private NoteSpawner _noteSpawner;
    [SerializeField] private JudgementEngine _judgementEngine;
    [SerializeField] private AudioConductor _audioConductor;

    [Header("Your Prefabs (Drag them here!)")]
    [SerializeField] private GameObject _tapPrefab;
    [SerializeField] private GameObject _sliderPrefab;
    [SerializeField] private GameObject _tickPrefab;


    // THE FIX: Explicit \n string formatting to bypass IDE auto-indentation bugs
    private const string DummyBeatmap = "osu file format v14\n[HitObjects]\n64,192,3000,1,0,0:0:0:0:\n192,192,4000,128,0,6000:0:0:0:";

    private void Awake()
    {
        Debug.Log("<color=cyan>[1] Bootstrapper Awake Started!</color>");

        EnsureRuntimeObjects();
        EnsureFallbackPrefabs();

        ConvertedMapData convertedMapData = BeatmapParser.Parse(DummyBeatmap);
        Debug.Log($"<color=yellow>[2] Parser finished. Found {convertedMapData.TotalNotes} notes.</color>");

        if (convertedMapData.TotalNotes == 0)
        {
            Debug.LogError("<color=red>FATAL: Parser returned 0 notes. The Spawner will not run.</color>");
            return;
        }
        
        _noteSpawner.Initialize(
            _noteManager,
            _notePool,
            _sliderPool,
            _tickPool,
            convertedMapData,
            _tapPrefab,         
            _sliderPrefab,
            _tickPrefab,
            GetOrCreateContainer("TapNotePoolContainer"),
            GetOrCreateContainer("SliderPoolContainer"),
            GetOrCreateContainer("TickPoolContainer"));

        Debug.Log("<color=green>[3] Spawner and Pools Initialized successfully.</color>");

        _judgementEngine.NoteManager = _noteManager;
        _judgementEngine.NotePool = _notePool;
        _judgementEngine.SliderPool = _sliderPool;
        _judgementEngine.TickPool = _tickPool;

        // Playfield decoration (background, lanes, hitline, lane materials)
        PlayfieldVisuals visuals = new GameObject("PlayfieldVisuals").AddComponent<PlayfieldVisuals>();
        visuals.Initialize();
        _noteSpawner.Visuals = visuals;

        // Visual feedback for hits and holds
        HitFeedbackDisplay feedbackDisplay = new GameObject("HitFeedbackDisplay").AddComponent<HitFeedbackDisplay>();
        feedbackDisplay.Initialize(_judgementEngine);
        _judgementEngine.FeedbackDisplay = feedbackDisplay;

        ConfigureAudioConductor();
        _audioConductor.StartSong();
        
        Debug.Log("<color=magenta>[4] Audio Conductor started the clock!</color>");
    }

    private void EnsureFallbackPrefabs()
    {
        // Always use our procedurally generated prefabs (overrides old scene prefabs)
        _tapPrefab = CreateTapPrefab();
        _sliderPrefab = CreateSliderPrefab();
        _tickPrefab = CreateTickPrefab();
    }

    private void EnsureRuntimeObjects()
    {
        if (_noteManager == null) _noteManager = new GameObject("NoteManager").AddComponent<NoteManager>();
        if (_notePool == null) _notePool = new GameObject("NotePool").AddComponent<NotePool>();
        if (_sliderPool == null) _sliderPool = new GameObject("SliderPool").AddComponent<SliderPool>();
        if (_tickPool == null) _tickPool = new GameObject("TickPool").AddComponent<TickPool>();
        if (_noteSpawner == null) _noteSpawner = new GameObject("NoteSpawner").AddComponent<NoteSpawner>();
        if (_judgementEngine == null) _judgementEngine = new GameObject("JudgementEngine").AddComponent<JudgementEngine>();
        if (_audioConductor == null) _audioConductor = new GameObject("AudioConductor").AddComponent<AudioConductor>();
    }

    private void ConfigureAudioConductor()
    {
        AudioSource audioSource = _audioConductor.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = _audioConductor.gameObject.AddComponent<AudioSource>();

        if (audioSource.clip == null)
        {
            AudioClip silentClip = AudioClip.Create("Phase3BootstrapperSilence", 44100, 1, 44100, false);
            audioSource.clip = silentClip;
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.mute = true;
    }

    private Transform GetOrCreateContainer(string objectName)
    {
        GameObject containerObject = new GameObject(objectName);
        containerObject.transform.SetParent(transform, false);
        return containerObject.transform;
    }


    private static GameObject CreateTapPrefab()
    {
        GameObject prefab = new GameObject("FallbackTapPrefab");
        SpriteRenderer sr = prefab.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateMinimalBarSprite(512, 128);
        sr.sortingOrder = 3;
        // Solid, thicker bar for minimalist tap note
        float targetWidth = PlayfieldVisuals.LaneSpacing * 0.95f;
        prefab.transform.localScale = new Vector3(targetWidth, 1.0f, 1f);
        prefab.SetActive(false);
        return prefab;
    }

    private static GameObject CreateSliderPrefab()
    {
        GameObject prefab = new GameObject("FallbackSliderPrefab");
        LineRenderer lr = prefab.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        // 95% of lane width for the ribbon body
        float sliderWidth = PlayfieldVisuals.LaneSpacing * 0.95f;
        lr.startWidth = sliderWidth;
        lr.endWidth = sliderWidth;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;
        lr.textureMode = LineTextureMode.Stretch;
        lr.sortingOrder = 2;

        // Clean bordered ribbon: crisp edges, faint interior
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.color = Color.white;
            mat.mainTexture = GenerateMinimalRibbonTexture(4, 512);
            lr.sharedMaterial = mat;
        }

        // --- Slider head cap (child 0) — sits at slider start ---
        Sprite capSprite = GenerateMinimalBarSprite(512, 128);
        float capWidth = PlayfieldVisuals.LaneSpacing * 0.95f;

        GameObject head = new GameObject("SliderHead");
        SpriteRenderer headSR = head.AddComponent<SpriteRenderer>();
        headSR.sprite = capSprite;
        headSR.sortingOrder = 5;
        head.transform.SetParent(prefab.transform, false);
        head.transform.localPosition = Vector3.zero;
        head.transform.localScale = new Vector3(capWidth, 1.0f, 1f);

        // --- Slider tail cap (child 1) — position set during spawn ---
        GameObject tail = new GameObject("SliderTail");
        SpriteRenderer tailSR = tail.AddComponent<SpriteRenderer>();
        tailSR.sprite = capSprite;
        tailSR.sortingOrder = 5;
        tail.transform.SetParent(prefab.transform, false);
        tail.transform.localPosition = Vector3.zero;
        tail.transform.localScale = new Vector3(capWidth, 1.0f, 1f);

        prefab.SetActive(false);
        return prefab;
    }

    private static GameObject CreateTickPrefab()
    {
        GameObject prefab = new GameObject("FallbackTickPrefab");
        SpriteRenderer sr = prefab.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateMinimalTickSprite(256, 32);
        sr.sortingOrder = 4;
        // Clean thin line spanning most of the lane width
        float tickWidth = PlayfieldVisuals.LaneSpacing * 0.8f;
        prefab.transform.localScale = new Vector3(tickWidth, 0.08f, 1f);
        prefab.SetActive(false);
        return prefab;
    }

    // --- PROCEDURAL TEXTURE GENERATION (init-time only) ---

    /// <summary>
    /// Generates a purely minimalist rectangular tap note sprite.
    /// Flat, crisp, solid 100% white shape with almost no rounding.
    /// Perfect for tinting with single SpriteRenderer colors.
    /// </summary>
    private static Sprite GenerateMinimalBarSprite(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float barHalfW = halfW * 0.98f;
        float barHalfH = halfH * 0.90f;
        float cornerR = 8f; // Minimal rounding for anti-aliasing

        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                float x = px - halfW + 0.5f;
                float y = py - halfH + 0.5f;

                float dx = Mathf.Max(0f, Mathf.Abs(x) - (barHalfW - cornerR));
                float dy = Mathf.Max(0f, Mathf.Abs(y) - (barHalfH - cornerR));
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - cornerR;

                float alpha = Mathf.Clamp01(-dist + 1f);

                if (alpha < 0.01f)
                {
                    pixels[py * width + px] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }

                // Flat 100% white core (takes exact lane color via tint)
                pixels[py * width + px] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 512f);
    }

    /// <summary>
    /// Generates a minimalist ribbon cross-section texture.
    /// Y axis maps across the LineRenderer width (V = cross-section).
    /// Crisp opaque outer borders with a flat, faint transparent interior.
    /// </summary>
    private static Texture2D GenerateMinimalRibbonTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        float halfH = height * 0.5f;

        for (int py = 0; py < height; py++)
        {
            float normY = Mathf.Abs(py - halfH + 0.5f) / halfH;
            float alpha;

            if (normY > 0.95f)
            {
                alpha = Mathf.Lerp(1f, 0f, (normY - 0.95f) / 0.05f);
            }
            else if (normY > 0.88f)
            {
                alpha = 1f;
            }
            else
            {
                alpha = 0.12f;
            }

            for (int px = 0; px < width; px++)
            {
                pixels[py * width + px] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Generates a clean, sharp horizontal bar sprite for slider ticks.
    /// Solid white fill with crisp anti-aliased edges.
    /// </summary>
    private static Sprite GenerateMinimalTickSprite(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float barHalfW = halfW * 0.95f;
        float barHalfH = halfH * 0.90f;

        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                float x = px - halfW + 0.5f;
                float y = py - halfH + 0.5f;

                float dx = Mathf.Max(0f, Mathf.Abs(x) - barHalfW);
                float dy = Mathf.Max(0f, Mathf.Abs(y) - barHalfH);
                float dist = Mathf.Max(dx, dy);

                // Crisp edge, 60% opacity for subtle but clean appearance
                float alpha = Mathf.Clamp01(-dist + 1f) * 0.6f;

                if (alpha < 0.01f)
                {
                    pixels[py * width + px] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }

                pixels[py * width + px] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 256f);
    }

// --- DIAGNOSTIC TELEMETRY ---
    private void Update()
    {
        if (_audioConductor == null || _noteManager == null) return;

        // Print to the console roughly once per second (every 60 frames)
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"<color=yellow>TELEMETRY -> Clock: {_audioConductor.CurrentSongTime:F2}s | Active Notes: {_noteManager.ActiveNoteCount}</color>");
        }
    }
}
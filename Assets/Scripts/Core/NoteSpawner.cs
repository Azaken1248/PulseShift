using UnityEngine;

[DefaultExecutionOrder(-200)]
public class NoteSpawner : MonoBehaviour
{
    public NoteManager NoteManager;
    public NotePool NotePool;
    public SliderPool SliderPool;
    public TickPool TickPool;
    public ConvertedMapData ConvertedMapData;
    public GameObject NotePrefab;
    public GameObject SliderPrefab;
    public GameObject TickPrefab;
    public Transform NotePoolContainer;
    public Transform SliderPoolContainer;
    public Transform TickPoolContainer;

    private int _nextSpawnIndex = 0;
    private bool _initialized;

    public void Initialize(
        NoteManager noteManager,
        NotePool notePool,
        SliderPool sliderPool,
        TickPool tickPool,
        ConvertedMapData convertedMapData,
        GameObject notePrefab,
        GameObject sliderPrefab,
        GameObject tickPrefab,
        Transform notePoolContainer,
        Transform sliderPoolContainer,
        Transform tickPoolContainer)
    {
        NoteManager = noteManager;
        NotePool = notePool;
        SliderPool = sliderPool;
        TickPool = tickPool;
        ConvertedMapData = convertedMapData;
        NotePrefab = notePrefab;
        SliderPrefab = sliderPrefab;
        TickPrefab = tickPrefab;
        NotePoolContainer = notePoolContainer;
        SliderPoolContainer = sliderPoolContainer;
        TickPoolContainer = tickPoolContainer;

        InitializePools();
    }

    private void Start()
    {
        if (!_initialized)
        {
            InitializePools();
        }
    }

    private void Update()
    {
        AudioConductor conductor = AudioConductor.Instance;
        if (conductor == null || !_initialized || NoteManager == null || NotePool == null || SliderPool == null || TickPool == null || ConvertedMapData == null)
        {
            return;
        }

        GameNote[] notes = ConvertedMapData.Notes;
        if (notes == null)
        {
            return;
        }

        int totalNotes = ConvertedMapData.TotalNotes;
        if (totalNotes > notes.Length)
        {
            totalNotes = notes.Length;
        }

        if (_nextSpawnIndex >= totalNotes)
        {
            return;
        }

        double currentSongTime = conductor.CurrentSongTime;
        double spawnThreshold = currentSongTime + ConvertedMapData.ApproachTime;

        while (_nextSpawnIndex < totalNotes)
        {
            GameNote noteData = notes[_nextSpawnIndex];

            if (spawnThreshold < noteData.Timestamp)
            {
                break;
            }

            if (noteData.Type == NoteType.Tap)
            {
                Transform noteTransform = NotePool.GetNote();
                if (noteTransform == null) break;
                ConfigureTapSpawn(noteTransform, noteData, currentSongTime);
                NoteManager.AddActiveNote(noteTransform, noteData);
            }
            else if (noteData.Type == NoteType.SlideStart) 
            {
                LineRenderer slider = SliderPool.GetSlider();
                if (slider == null) break;
                ConfigureSliderSpawn(slider, noteData, currentSongTime);
                NoteManager.AddActiveNote(slider.transform, noteData);

                // Force-spawn all ticks and the SlideEnd for this slide NOW,
                // so the SlideEnd is in active notes when SlideStart is judged.
                int slideId = noteData.SlideId;
                int peekIndex = _nextSpawnIndex + 1;
                while (peekIndex < totalNotes)
                {
                    GameNote peekNote = notes[peekIndex];
                    if (peekNote.SlideId != slideId) break;

                    if (peekNote.Type == NoteType.SlideTick)
                    {
                        Transform tickTransform = TickPool.GetTick();
                        if (tickTransform != null)
                        {
                            ConfigureTickSpawn(tickTransform, peekNote, currentSongTime);
                        }
                        NoteManager.AddActiveNote(tickTransform, peekNote);
                    }
                    else
                    {
                        NoteManager.AddActiveNote(null, peekNote);
                    }

                    peekIndex++;
                    if (peekNote.Type == NoteType.SlideEnd) break;
                }
                _nextSpawnIndex = peekIndex;
                continue; // skip the _nextSpawnIndex++ below
            }
            else
            {
                NoteManager.AddActiveNote(null, noteData);
            }

            _nextSpawnIndex++;
        }
    }

    private void InitializePools()
    {
        ConvertedMapData mapData = ConvertedMapData;
        NoteManager noteManager = NoteManager;
        NotePool notePool = NotePool;
        SliderPool sliderPool = SliderPool;
        TickPool tickPool = TickPool;
        GameObject notePrefab = NotePrefab;
        GameObject sliderPrefab = SliderPrefab;
        GameObject tickPrefab = TickPrefab;

        if (mapData == null || noteManager == null || notePool == null || sliderPool == null || tickPool == null || notePrefab == null || sliderPrefab == null || tickPrefab == null)
        {
            return;
        }

        noteManager.Initialize(mapData.TotalNotes);

        int tapCount = 0;
        int slideStartCount = 0;
        int slideTickCount = 0;
        GameNote[] notes = mapData.Notes;
        int totalNotes = mapData.TotalNotes;

        if (notes != null && totalNotes > 0)
        {
            if (totalNotes > notes.Length)
            {
                totalNotes = notes.Length;
            }

            for (int i = 0; i < totalNotes; i++)
            {
                if (notes[i].Type == NoteType.Tap)
                {
                    tapCount++;
                }
                else if (notes[i].Type == NoteType.SlideStart)
                {
                    slideStartCount++;
                }
                else if (notes[i].Type == NoteType.SlideTick)
                {
                    slideTickCount++;
                }
            }
        }

        notePool.Initialize(tapCount, notePrefab, NotePoolContainer);
        sliderPool.Initialize(slideStartCount, sliderPrefab, SliderPoolContainer);
        tickPool.Initialize(slideTickCount, tickPrefab, TickPoolContainer);
        _nextSpawnIndex = 0;
        _initialized = true;
    }
    public PlayfieldVisuals Visuals;

    private void ConfigureTapSpawn(Transform noteTransform, GameNote noteData, double currentSongTime)
    {
        float laneX = PlayfieldVisuals.GetLaneX(noteData.LaneIndex);
        float spawnY = (float)((noteData.Timestamp - currentSongTime) * NoteManager.ScrollSpeed);
        noteTransform.localPosition = new Vector3(laneX, spawnY, 0f);

        // Apply lane color (value-type assignment, zero allocation)
        SpriteRenderer sr = noteTransform.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = PlayfieldVisuals.LaneColors[noteData.LaneIndex];
        }
    }

    private void ConfigureSliderSpawn(LineRenderer slider, GameNote noteData, double currentSongTime)
    {
        float laneX = PlayfieldVisuals.GetLaneX(noteData.LaneIndex);
        float spawnY = (float)((noteData.Timestamp - currentSongTime) * NoteManager.ScrollSpeed);

        Transform sliderTransform = slider.transform;
        sliderTransform.localPosition = new Vector3(laneX, spawnY, 0f);

        float lengthY = (float)(noteData.Duration * NoteManager.ScrollSpeed);
        slider.positionCount = 2;
        slider.SetPosition(0, Vector3.zero);
        slider.SetPosition(1, new Vector3(0f, lengthY, 0f));

        // Apply lane color via LineRenderer color (value-type, zero allocation)
        Color laneColor = PlayfieldVisuals.LaneColors[noteData.LaneIndex];
        laneColor.a = 0.7f;
        slider.startColor = laneColor;
        slider.endColor = laneColor;

        // Position and color head/tail caps (children 0 and 1)
        Color capColor = PlayfieldVisuals.LaneColors[noteData.LaneIndex];
        if (sliderTransform.childCount >= 2)
        {
            // Head cap: stays at slider start (local origin)
            Transform head = sliderTransform.GetChild(0);
            SpriteRenderer headSR = head.GetComponent<SpriteRenderer>();
            if (headSR != null) headSR.color = capColor;

            // Tail cap: positioned at slider end
            Transform tail = sliderTransform.GetChild(1);
            tail.localPosition = new Vector3(0f, lengthY, 0f);
            SpriteRenderer tailSR = tail.GetComponent<SpriteRenderer>();
            if (tailSR != null) tailSR.color = capColor;
        }
    }

    private void ConfigureTickSpawn(Transform tickTransform, GameNote noteData, double currentSongTime)
    {
        float laneX = PlayfieldVisuals.GetLaneX(noteData.LaneIndex);
        float spawnY = (float)((noteData.Timestamp - currentSongTime) * NoteManager.ScrollSpeed);
        tickTransform.localPosition = new Vector3(laneX, spawnY, 0f);

        // Apply lane color with slight transparency for tick markers
        SpriteRenderer sr = tickTransform.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color tickColor = PlayfieldVisuals.LaneColors[noteData.LaneIndex];
            tickColor.a = 0.85f;
            sr.color = tickColor;
        }
    }
}
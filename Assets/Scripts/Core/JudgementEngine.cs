using System;
using UnityEngine;

[DefaultExecutionOrder(-150)]
public class JudgementEngine : MonoBehaviour
{
    private const double HoldHitThreshold = 0.016d; // ~1 frame tolerance for ticks/end

    public NoteManager NoteManager;
    public NotePool NotePool;
    public SliderPool SliderPool;
    public TickPool TickPool;
    public HitFeedbackDisplay FeedbackDisplay;

    public readonly bool[] LaneIsHeld = new bool[4];
    private readonly bool[] _laneBeganThisFrame = new bool[4];
    private readonly bool[] _slideBroken = new bool[4]; // true if user released mid-hold

    // Combo state — ticks, taps, slide starts/ends all count
    public int Combo;
    public int MaxCombo;

    private Camera _cachedCamera;

    private void Update()
    {
        AudioConductor conductor = AudioConductor.Instance;
        if (conductor == null || NoteManager == null || NotePool == null || SliderPool == null)
        {
            return;
        }

        if (_cachedCamera == null)
        {
            _cachedCamera = Camera.main;
        }

        ClearLaneState();
        ReadTouchState();
        ReadMouseState();

        double currentSongTime = conductor.CurrentSongTime;

        for (byte laneIndex = 0; laneIndex < 4; laneIndex++)
        {
            ProcessLane(laneIndex, currentSongTime);
        }
    }

    private void ClearLaneState()
    {
        for (int i = 0; i < 4; i++)
        {
            LaneIsHeld[i] = false;
            _laneBeganThisFrame[i] = false;
        }
    }

    private void ReadTouchState()
    {
        int touchCount = Input.touchCount;
        for (int i = 0; i < touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            byte laneIndex = GetLaneIndexFromScreenPosition(touch.position);

            if (touch.phase == TouchPhase.Began)
            {
                _laneBeganThisFrame[laneIndex] = true;
                LaneIsHeld[laneIndex] = true;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                LaneIsHeld[laneIndex] = true;
            }
        }
    }

    private void ReadMouseState()
    {
        if (!Input.GetMouseButton(0))
        {
            return;
        }

        byte laneIndex = GetLaneIndexFromScreenPosition(Input.mousePosition);
        LaneIsHeld[laneIndex] = true;

        if (Input.GetMouseButtonDown(0))
        {
            _laneBeganThisFrame[laneIndex] = true;
        }
    }

    private void ProcessLane(byte laneIndex, double currentSongTime)
    {
        while (TryGetEarliestNoteInLane(laneIndex, out int noteIndex, out GameNote noteData))
        {
            double signedDelta = noteData.Timestamp - currentSongTime;
            double absDelta = Math.Abs(signedDelta);

            // Auto-miss: note passed the hitline by more than 200ms
            if (signedDelta <= -0.200d)
            {
                if (FeedbackDisplay != null)
                {
                    if (noteData.Type == NoteType.Tap || noteData.Type == NoteType.SlideStart || noteData.Type == NoteType.SlideEnd)
                    {
                        FeedbackDisplay.ShowMiss(laneIndex);
                    }
                    else if (noteData.Type == NoteType.SlideTick)
                    {
                        FeedbackDisplay.ShowTickMiss(laneIndex);
                    }
                }
                // A missed tick also breaks the slide
                if (noteData.Type == NoteType.SlideTick || noteData.Type == NoteType.SlideEnd)
                {
                    _slideBroken[laneIndex] = true;
                }
                ResetCombo();
                RemoveResolvedNote(noteIndex, noteData);
                continue;
            }

            // Tap / SlideStart: require a new press within the hit window
            if ((noteData.Type == NoteType.Tap || noteData.Type == NoteType.SlideStart) && _laneBeganThisFrame[laneIndex])
            {
                int tier = -1;
                if (absDelta <= 0.050d) tier = 0;      // PERFECT
                else if (absDelta <= 0.100d) tier = 1;  // GREAT
                else if (absDelta <= 0.150d) tier = 2;  // GOOD

                if (tier >= 0)
                {
                    IncrementCombo();
                    if (FeedbackDisplay != null) FeedbackDisplay.ShowJudgement(laneIndex, tier);
                    // Reset broken flag when a new slide starts
                    if (noteData.Type == NoteType.SlideStart)
                    {
                        _slideBroken[laneIndex] = false;
                    }
                    RemoveResolvedNote(noteIndex, noteData);
                    continue;
                }
                // Outside 150ms — too early, ignore the press
            }

            // Ticks/End: only consume when at or past the hitline (~1 frame tolerance)
            if (signedDelta <= HoldHitThreshold)
            {
                if (noteData.Type == NoteType.SlideTick)
                {
                    if (LaneIsHeld[laneIndex] && !_slideBroken[laneIndex])
                    {
                        // Held through successfully — tick counts in combo
                        IncrementCombo();
                        if (FeedbackDisplay != null) FeedbackDisplay.ShowTickHit(laneIndex);
                        RemoveResolvedNote(noteIndex, noteData);
                        continue;
                    }
                    else
                    {
                        // Released — break the slide, miss this tick
                        _slideBroken[laneIndex] = true;
                        ResetCombo();
                        if (FeedbackDisplay != null) FeedbackDisplay.ShowTickMiss(laneIndex);
                        RemoveResolvedNote(noteIndex, noteData);
                        continue;
                    }
                }

                if (noteData.Type == NoteType.SlideEnd)
                {
                    if (LaneIsHeld[laneIndex] && !_slideBroken[laneIndex])
                    {
                        // Completed the entire hold
                        IncrementCombo();
                        if (FeedbackDisplay != null) FeedbackDisplay.ShowJudgement(laneIndex, 0);
                        RemoveResolvedNote(noteIndex, noteData);
                        continue;
                    }
                    else
                    {
                        // Broken hold — always miss
                        ResetCombo();
                        if (FeedbackDisplay != null) FeedbackDisplay.ShowMiss(laneIndex);
                        RemoveResolvedNote(noteIndex, noteData);
                        continue;
                    }
                }
            }

            break;
        }
    }

    private void IncrementCombo()
    {
        Combo++;
        if (Combo > MaxCombo) MaxCombo = Combo;
    }

    private void ResetCombo()
    {
        Combo = 0;
    }

    private void RemoveResolvedNote(int index, GameNote noteData)
    {
        Transform noteTransform = null;
        if (index >= 0 && index < NoteManager.ActiveNoteCount)
        {
            noteTransform = NoteManager.ActiveNoteTransforms[index];
        }

        NoteManager.RemoveActiveNoteAt(index);

        if (noteData.Type == NoteType.Tap)
        {
            if (noteTransform != null) NotePool.ReturnNote(noteTransform);
        }
        else if (noteData.Type == NoteType.SlideStart)
        {
            // Transfer the visual to the SlideEnd so it persists through the hold
            if (noteTransform != null)
            {
                TransferTransformToSlideEnd(noteTransform, noteData.SlideId);
            }
        }
        else if (noteData.Type == NoteType.SlideEnd)
        {
            // Hold is complete — return the visual to the pool
            if (noteTransform != null)
            {
                LineRenderer slider = noteTransform.GetComponent<LineRenderer>();
                if (slider != null)
                {
                    SliderPool.ReturnSlider(slider);
                }
            }
        }
        else if (noteData.Type == NoteType.SlideTick)
        {
            if (noteTransform != null && TickPool != null) TickPool.ReturnTick(noteTransform);
        }
    }

    /// <summary>
    /// When a SlideStart is consumed, hand its LineRenderer Transform to the
    /// matching SlideEnd so the ribbon keeps scrolling until the hold finishes.
    /// </summary>
    private void TransferTransformToSlideEnd(Transform sliderTransform, int slideId)
    {
        GameNote[] activeNotes = NoteManager.ActiveNoteData;
        Transform[] activeTransforms = NoteManager.ActiveNoteTransforms;
        int activeCount = NoteManager.ActiveNoteCount;

        for (int i = 0; i < activeCount; i++)
        {
            if (activeNotes[i].SlideId == slideId && activeNotes[i].Type == NoteType.SlideEnd)
            {
                activeTransforms[i] = sliderTransform;
                return;
            }
        }

        // Fallback: no SlideEnd found, return to pool to avoid leak
        LineRenderer slider = sliderTransform.GetComponent<LineRenderer>();
        if (slider != null)
        {
            SliderPool.ReturnSlider(slider);
        }
    }

    private bool TryGetEarliestNoteInLane(byte laneIndex, out int noteIndex, out GameNote noteData)
    {
        noteIndex = -1;
        noteData = new GameNote();

        Transform[] activeTransforms = NoteManager.ActiveNoteTransforms;
        GameNote[] activeNotes = NoteManager.ActiveNoteData;
        int activeCount = NoteManager.ActiveNoteCount;

        if (activeTransforms == null || activeNotes == null || activeCount <= 0)
        {
            return false;
        }

        if (activeCount > activeNotes.Length)
        {
            activeCount = activeNotes.Length;
        }

        double earliestTimestamp = double.MaxValue;

        for (int i = 0; i < activeCount; i++)
        {
            GameNote candidate = activeNotes[i];
            if (candidate.LaneIndex != laneIndex)
            {
                continue;
            }

            if (candidate.Timestamp < earliestTimestamp)
            {
                earliestTimestamp = candidate.Timestamp;
                noteIndex = i;
                noteData = candidate;
            }
        }

        return noteIndex >= 0;
    }

    /// <summary>
    /// Convert a screen position to a lane index using the camera's world projection.
    /// Lanes use PlayfieldVisuals spacing. Find the nearest lane center.
    /// </summary>
    private byte GetLaneIndexFromScreenPosition(Vector2 screenPosition)
    {
        if (_cachedCamera != null)
        {
            Vector3 worldPos = _cachedCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, 0f));

            // Find closest lane by distance to each lane center
            float worldX = worldPos.x;
            int bestLane = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                float dist = Mathf.Abs(worldX - PlayfieldVisuals.GetLaneX(i));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestLane = i;
                }
            }
            return (byte)bestLane;
        }

        // Fallback: divide screen evenly
        float screenX = screenPosition.x;
        int screenWidth = Screen.width;
        if (screenWidth <= 0) return 0;

        int fallbackLane = (int)((screenX / screenWidth) * 4f);
        if (fallbackLane < 0) fallbackLane = 0;
        else if (fallbackLane > 3) fallbackLane = 3;
        return (byte)fallbackLane;
    }
}
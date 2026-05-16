using System;
using UnityEngine;

[DefaultExecutionOrder(-150)]
public class JudgementEngine : MonoBehaviour
{
    private const double HitWindowSeconds = 0.120d;
    private const double MissWindowSeconds = 0.150d;

    public NoteManager NoteManager;
    public NotePool NotePool;

    private void Update()
    {
        AudioConductor conductor = AudioConductor.Instance;
        if (conductor == null || NoteManager == null || NotePool == null)
        {
            return;
        }

        double currentSongTime = conductor.CurrentSongTime;

        HandleTouchInput(currentSongTime);
        HandleMouseInput(currentSongTime);
        ApplyAutoMisses(currentSongTime);
    }

    private void HandleTouchInput(double currentSongTime)
    {
        int touchCount = Input.touchCount;
        if (touchCount <= 0)
        {
            return;
        }

        for (int i = 0; i < touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began)
            {
                byte laneIndex = GetLaneIndexFromScreenX(touch.position.x);
                TryRegisterHit(laneIndex, currentSongTime);
            }
        }
    }

    private void HandleMouseInput(double currentSongTime)
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        byte laneIndex = GetLaneIndexFromScreenX(Input.mousePosition.x);
        TryRegisterHit(laneIndex, currentSongTime);
    }

    private void TryRegisterHit(byte laneIndex, double currentSongTime)
    {
        int noteIndex;
        GameNote noteData;

        if (!TryGetEarliestNoteInLane(laneIndex, out noteIndex, out noteData))
        {
            return;
        }

        double timeDifference = Math.Abs(noteData.Timestamp - currentSongTime);
        if (timeDifference > HitWindowSeconds)
        {
            return;
        }

        Transform noteTransform = NoteManager.ActiveNoteTransforms[noteIndex];
        NoteManager.RemoveActiveNoteAt(noteIndex);
        NotePool.ReturnNote(noteTransform);
    }

    private void ApplyAutoMisses(double currentSongTime)
    {
        for (int i = NoteManager.ActiveNoteCount - 1; i >= 0; i--)
        {
            GameNote note = NoteManager.ActiveNoteData[i];
            
            if (currentSongTime - note.Timestamp > MissWindowSeconds)
            {
                Transform noteTransform = NoteManager.ActiveNoteTransforms[i];
                NoteManager.RemoveActiveNoteAt(i);
                NotePool.ReturnNote(noteTransform);
                
                Debug.Log("Note Missed in Lane " + note.LaneIndex);
            }
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

    private byte GetLaneIndexFromScreenX(float screenX)
    {

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenX, 0, 0));
        float x = worldPos.x;


        if (x < -1f) return 0;
        if (x < 0f)  return 1;
        if (x < 1f)  return 2;
        
        return 3;
    }
}
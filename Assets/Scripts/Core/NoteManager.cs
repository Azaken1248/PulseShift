using UnityEngine;

[DefaultExecutionOrder(-100)]
public class NoteManager : MonoBehaviour
{
    public Transform[] ActiveNoteTransforms = new Transform[0];
    public GameNote[] ActiveNoteData = new GameNote[0];
    public int ActiveNoteCount;
    public float ScrollSpeed = 4f;

    public void Initialize(int capacity)
    {
        if (capacity < 0)
        {
            capacity = 0;
        }

        if (ActiveNoteTransforms != null && ActiveNoteData != null && ActiveNoteTransforms.Length >= capacity && ActiveNoteData.Length >= capacity)
        {
            ActiveNoteCount = 0;
            return;
        }

        ActiveNoteTransforms = new Transform[capacity];
        ActiveNoteData = new GameNote[capacity];
        ActiveNoteCount = 0;
    }

    public void AddActiveNote(Transform noteTransform, GameNote noteData)
    {
        if (ActiveNoteTransforms == null || ActiveNoteData == null)
        {
            return;
        }

        if (ActiveNoteCount >= ActiveNoteTransforms.Length || ActiveNoteCount >= ActiveNoteData.Length)
        {
            return;
        }

        ActiveNoteTransforms[ActiveNoteCount] = noteTransform;
        ActiveNoteData[ActiveNoteCount] = noteData;
        ActiveNoteCount++;
    }

    public void RemoveActiveNoteAt(int index)
    {
        if (index < 0 || index >= ActiveNoteCount)
        {
            return;
        }

        int lastIndex = ActiveNoteCount - 1;
        if (index != lastIndex)
        {
            ActiveNoteTransforms[index] = ActiveNoteTransforms[lastIndex];
            ActiveNoteData[index] = ActiveNoteData[lastIndex];
        }

        ActiveNoteTransforms[lastIndex] = null;
        ActiveNoteData[lastIndex] = new GameNote();
        ActiveNoteCount = lastIndex;
    }

    private void Update()
    {
        AudioConductor conductor = AudioConductor.Instance;
        if (conductor == null || ActiveNoteCount <= 0)
        {
            return;
        }

        Transform[] activeNoteTransforms = ActiveNoteTransforms;
        GameNote[] activeNoteData = ActiveNoteData;
        if (activeNoteTransforms == null || activeNoteData == null)
        {
            return;
        }

        int activeCount = ActiveNoteCount;
        if (activeCount > activeNoteTransforms.Length)
        {
            activeCount = activeNoteTransforms.Length;
        }

        if (activeCount > activeNoteData.Length)
        {
            activeCount = activeNoteData.Length;
        }

        double currentSongTime = conductor.CurrentSongTime;

        for (int i = 0; i < activeCount; i++)
        {
            Transform noteTransform = activeNoteTransforms[i];
            if (noteTransform == null)
            {
                continue;
            }

            GameNote note = activeNoteData[i];

            // SlideEnd may carry the slider visual (transferred from SlideStart).
            // Anchor it to the hold's start time so the ribbon bottom scrolls correctly.
            double anchorTime = note.Timestamp;
            if (note.Type == NoteType.SlideEnd)
            {
                anchorTime = note.Timestamp - note.Duration;
            }

            double timeRemaining = anchorTime - currentSongTime;

            float yPosition = (float)(timeRemaining * ScrollSpeed);
            if (timeRemaining == 0d)
            {
                yPosition = 0f;
            }

            Vector3 localPosition = noteTransform.localPosition;
            localPosition.y = yPosition;
            noteTransform.localPosition = localPosition;
        }
    }
}
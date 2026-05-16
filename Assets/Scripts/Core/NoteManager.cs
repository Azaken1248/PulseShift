using UnityEngine;

public class NoteManager : MonoBehaviour
{
    public Transform[] ActiveNoteTransforms;
    public GameNote[] ActiveNoteData;
    public int ActiveNoteCount;
    public float ScrollSpeed = 4f;

    private void Update()
    {
        
        if (AudioConductor.Instance == null || ActiveNoteCount == 0) return;

        double currentSongTime = AudioConductor.Instance.CurrentSongTime;

        for (int i = 0; i < ActiveNoteCount; i++)
        {
            Transform noteTransform = ActiveNoteTransforms[i];
            GameNote note = ActiveNoteData[i];

            double timeRemaining = note.Timestamp - currentSongTime;
            
            Vector3 pos = noteTransform.localPosition;
            pos.y = (float)(timeRemaining * ScrollSpeed);
            noteTransform.localPosition = pos;
        }
    }
}
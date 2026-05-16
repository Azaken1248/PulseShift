using UnityEngine;

[DefaultExecutionOrder(-200)]
public class NoteSpawner : MonoBehaviour
{
    public NoteManager NoteManager;
    public NotePool NotePool;
    public ConvertedMapData ConvertedMapData;
    public GameObject NotePrefab;
    public Transform NotePoolContainer;

    private int _nextSpawnIndex = 0;

    private void Start()
    {
        if (NoteManager != null && ConvertedMapData != null)
        {
            NoteManager.Initialize(ConvertedMapData.TotalNotes);
        }

        if (NotePool != null && NotePrefab != null && ConvertedMapData != null)
        {
            NotePool.Initialize(ConvertedMapData.TotalNotes, NotePrefab, NotePoolContainer);
        }

        _nextSpawnIndex = 0;
    }

    // Inside NoteSpawner.cs
    private void Update()
    {
        AudioConductor conductor = AudioConductor.Instance;
        if (conductor == null || NoteManager == null || NotePool == null || ConvertedMapData == null) return;

        GameNote[] notes = ConvertedMapData.Notes;
        if (notes == null) return;

        int totalNotes = ConvertedMapData.TotalNotes;
        double spawnThreshold = conductor.CurrentSongTime + ConvertedMapData.ApproachTime;

        while (_nextSpawnIndex < totalNotes && spawnThreshold >= notes[_nextSpawnIndex].Timestamp)
        {
            GameNote noteData = notes[_nextSpawnIndex];
            
            Transform noteTransform = NotePool.GetNote();
            if (noteTransform != null)
            {

                float laneX = -1.5f + noteData.LaneIndex;
                float spawnY = (float)(ConvertedMapData.ApproachTime * NoteManager.ScrollSpeed);
                noteTransform.localPosition = new Vector3(laneX, spawnY, 0f);

                NoteManager.AddActiveNote(noteTransform, noteData);
            }
            
            // ALWAYS INCREMENT
            _nextSpawnIndex++;
        }
    }
}
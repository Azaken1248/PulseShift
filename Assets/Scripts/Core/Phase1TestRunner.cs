using UnityEngine;

public class Phase1TestRunner : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private NoteManager _noteManager;
    
    [Header("Spawning")]
    [SerializeField] private GameObject _tapNotePrefab;
    [SerializeField] private Transform _gameplayWorldContainer;
    [SerializeField] private float _scrollSpeed = 4f;

    private const string DummyBeatmap = @"osu file format v14

[HitObjects]
64,192,500,1,0,0:0:0:0:
192,192,1000,1,0,0:0:0:0:
320,192,1500,2,0,B|384:192|448:192,1,128
448,192,2000,8,0,2500,0:0:0:0:
384,192,2500,1,0,0:0:0:0:";

    private void Start()
    {

        ConvertedMapData mapData = BeatmapParser.Parse(DummyBeatmap);
        Transform[] activeTransforms = new Transform[mapData.TotalNotes];

        for (int i = 0; i < mapData.TotalNotes; i++)
        {
            GameNote note = mapData.Notes[i];
            
            GameObject noteObj = Instantiate(_tapNotePrefab, _gameplayWorldContainer);
            noteObj.name = $"Note_{i}";
            
            float laneX = -1.5f + note.LaneIndex;
            noteObj.transform.localPosition = new Vector3(laneX, 0, 0);
            
            activeTransforms[i] = noteObj.transform;
        }

        _noteManager.ActiveNoteTransforms = activeTransforms;
        _noteManager.ActiveNoteData = mapData.Notes;
        _noteManager.ActiveNoteCount = mapData.TotalNotes;
        _noteManager.ScrollSpeed = _scrollSpeed;

        AudioConductor.Instance.StartSong();
    }
}
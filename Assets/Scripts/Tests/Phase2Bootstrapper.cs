using UnityEngine;

[DefaultExecutionOrder(-300)]
public class Phase2Bootstrapper : MonoBehaviour
{
    public NoteSpawner Spawner;

    private const string DummyBeatmap = @"osu file format v14

    [HitObjects]
    64,192,1000,1,0,0:0:0:0:
    192,192,1500,1,0,0:0:0:0:
    320,192,2000,1,0,0:0:0:0:
    448,192,2500,1,0,0:0:0:0:";

    private void Awake()
    {
        ConvertedMapData mapData = BeatmapParser.Parse(DummyBeatmap);
        
        // This will print to the Unity Console so we KNOW it worked
        Debug.Log($"Successfully parsed {mapData.TotalNotes} notes!"); 
        
        Spawner.ConvertedMapData = mapData;
    }

    private void Start()
    {
        AudioConductor.Instance.StartSong();
    }
}

using UnityEngine;

public class AudioConductor : MonoBehaviour
{
    public static AudioConductor Instance { get; private set; }

    [Header("Audio Configuration")]
    [Tooltip("Offset in seconds to align the first beat.")]
    public double songOffset = 0.0;

    private float _unityStartTime;
    private bool _isPlaying;

    public double CurrentSongTime
    {
        get
        {
            if (!_isPlaying) return 0.0;
            
            return Time.time - _unityStartTime - songOffset;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void StartSong()
    {
        _unityStartTime = Time.time;
        _isPlaying = true;
    }
}
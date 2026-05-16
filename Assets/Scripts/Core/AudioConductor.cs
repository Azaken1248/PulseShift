using UnityEngine;

public class AudioConductor : MonoBehaviour
{
    public static AudioConductor Instance { get; private set; }

    [Header("Audio Configuration")]
    [SerializeField] private AudioSource _audioSource;

    [Tooltip("Offset in seconds to align the first beat if the mp3 has lead-in silence.")]
    public double songOffset = 0.0;

    private double _dspStartTime;

    private bool _isPlaying;

    public double CurrentSongTime
    {
        get
        {
            if (!_isPlaying)
            {
                return 0.0;
            }

            return AudioSettings.dspTime - _dspStartTime - songOffset;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }
    }


    public void StartSong()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        _dspStartTime = AudioSettings.dspTime;
        _isPlaying = true;

        if (_audioSource != null)
        {
            _audioSource.Play();
        }
    }
}

using UnityEngine;

public class NotePool : MonoBehaviour
{
    private Transform[] _notes = new Transform[0];
    private int[] _freeIndices = new int[0];
    private bool[] _inUse = new bool[0];
    private int _freeCount;

    public void Initialize(int poolSize, GameObject prefab, Transform container)
    {
        if (poolSize <= 0 || prefab == null)
        {
            _notes = new Transform[0];
            _freeIndices = new int[0];
            _inUse = new bool[0];
            _freeCount = 0;
            return;
        }

        if (_notes != null && _notes.Length > 0)
        {
            _freeCount = _notes.Length;

            for (int i = 0; i < _notes.Length; i++)
            {
                _freeIndices[i] = i;
                _inUse[i] = false;

                Transform noteTransform = _notes[i];
                if (noteTransform != null)
                {
                    noteTransform.gameObject.SetActive(false);
                }
            }

            return;
        }

        _notes = new Transform[poolSize];
        _freeIndices = new int[poolSize];
        _inUse = new bool[poolSize];
        _freeCount = poolSize;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject noteObject = Object.Instantiate(prefab);
            if (container != null)
            {
                noteObject.transform.SetParent(container, false);
            }

            noteObject.SetActive(false);

            NotePoolItem notePoolItem = noteObject.GetComponent<NotePoolItem>();
            if (notePoolItem == null)
            {
                notePoolItem = noteObject.AddComponent<NotePoolItem>();
            }

            notePoolItem.PoolIndex = i;
            _notes[i] = noteObject.transform;
            _freeIndices[i] = i;
            _inUse[i] = false;
        }
    }

    public Transform GetNote()
    {
        if (_freeCount <= 0)
        {
            return null;
        }

        _freeCount--;
        int poolIndex = _freeIndices[_freeCount];
        _inUse[poolIndex] = true;

        Transform noteTransform = _notes[poolIndex];
        if (noteTransform != null)
        {
            noteTransform.gameObject.SetActive(true);
        }

        return noteTransform;
    }

    public void ReturnNote(Transform note)
    {
        if (note == null || _notes == null || _inUse == null)
        {
            return;
        }

        NotePoolItem notePoolItem = note.GetComponent<NotePoolItem>();
        if (notePoolItem == null)
        {
            return;
        }

        int poolIndex = notePoolItem.PoolIndex;
        if (poolIndex < 0 || poolIndex >= _notes.Length)
        {
            return;
        }

        if (!_inUse[poolIndex])
        {
            return;
        }

        note.gameObject.SetActive(false);
        _inUse[poolIndex] = false;

        if (_freeCount < _freeIndices.Length)
        {
            _freeIndices[_freeCount] = poolIndex;
            _freeCount++;
        }
    }
}

public sealed class NotePoolItem : MonoBehaviour
{
    public int PoolIndex;
}
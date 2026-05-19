using UnityEngine;

public class TickPool : MonoBehaviour
{
    private Transform[] _ticks = new Transform[0];
    private int[] _freeIndices = new int[0];
    private bool[] _inUse = new bool[0];
    private int _freeCount;

    public void Initialize(int poolSize, GameObject prefab, Transform container)
    {
        if (poolSize <= 0 || prefab == null)
        {
            _ticks = new Transform[0];
            _freeIndices = new int[0];
            _inUse = new bool[0];
            _freeCount = 0;
            return;
        }

        _ticks = new Transform[poolSize];
        _freeIndices = new int[poolSize];
        _inUse = new bool[poolSize];
        _freeCount = poolSize;

        Transform parent = container != null ? container : transform;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject tickObject = Object.Instantiate(prefab, parent);
            tickObject.SetActive(false);

            TickPoolItem poolItem = tickObject.GetComponent<TickPoolItem>();
            if (poolItem == null)
            {
                poolItem = tickObject.AddComponent<TickPoolItem>();
            }

            poolItem.PoolIndex = i;
            _ticks[i] = tickObject.transform;
            _freeIndices[i] = i;
            _inUse[i] = false;
        }
    }

    public Transform GetTick()
    {
        if (_freeCount <= 0)
        {
            return null;
        }

        _freeCount--;
        int poolIndex = _freeIndices[_freeCount];
        _inUse[poolIndex] = true;

        Transform tickTransform = _ticks[poolIndex];
        if (tickTransform != null)
        {
            tickTransform.gameObject.SetActive(true);
        }

        return tickTransform;
    }

    public void ReturnTick(Transform tick)
    {
        if (tick == null || _ticks == null || _inUse == null)
        {
            return;
        }

        TickPoolItem poolItem = tick.GetComponent<TickPoolItem>();
        if (poolItem == null)
        {
            return;
        }

        int poolIndex = poolItem.PoolIndex;
        if (poolIndex < 0 || poolIndex >= _ticks.Length)
        {
            return;
        }

        if (!_inUse[poolIndex])
        {
            return;
        }

        tick.gameObject.SetActive(false);
        _inUse[poolIndex] = false;

        if (_freeCount < _freeIndices.Length)
        {
            _freeIndices[_freeCount] = poolIndex;
            _freeCount++;
        }
    }
}

public sealed class TickPoolItem : MonoBehaviour
{
    public int PoolIndex;
}

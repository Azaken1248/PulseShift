using UnityEngine;

public class SliderPool : MonoBehaviour
{
    private LineRenderer[] _sliders = new LineRenderer[0];
    private int[] _freeIndices = new int[0];
    private bool[] _inUse = new bool[0];
    private int _freeCount;

    public void Initialize(int poolSize, GameObject prefab, Transform container)
    {
        if (poolSize <= 0 || prefab == null) return;

        _sliders = new LineRenderer[poolSize];
        _freeIndices = new int[poolSize];
        _inUse = new bool[poolSize];
        _freeCount = poolSize;

        Transform parent = container != null ? container : transform;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject sliderObject = Object.Instantiate(prefab, parent);
            sliderObject.SetActive(false);

            LineRenderer lineRenderer = sliderObject.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = sliderObject.AddComponent<LineRenderer>();
            }

            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = 2;
            if (lineRenderer.startWidth <= 0f)
            {
                lineRenderer.startWidth = 0.18f;
            }

            if (lineRenderer.endWidth <= 0f)
            {
                lineRenderer.endWidth = 0.18f;
            }

            SliderPoolItem poolItem = sliderObject.AddComponent<SliderPoolItem>();
            poolItem.PoolIndex = i;

            _sliders[i] = lineRenderer;
            _freeIndices[i] = i;
            _inUse[i] = false;
        }
    }

    public LineRenderer GetSlider()
    {
        if (_freeCount <= 0) return null;

        _freeCount--;
        int index = _freeIndices[_freeCount];
        _inUse[index] = true;

        LineRenderer slider = _sliders[index];
        if (slider != null) slider.gameObject.SetActive(true);

        return slider;
    }

    public void ReturnSlider(LineRenderer slider)
    {
        if (slider == null) return;

        SliderPoolItem poolItem = slider.GetComponent<SliderPoolItem>();
        if (poolItem == null) return;

        int poolIndex = poolItem.PoolIndex;
        if (poolIndex < 0 || poolIndex >= _sliders.Length || !_inUse[poolIndex]) return;

        _inUse[poolIndex] = false;
        slider.gameObject.SetActive(false);

        if (_freeCount < _freeIndices.Length)
        {
            _freeIndices[_freeCount] = poolIndex;
            _freeCount++;
        }
    }
}

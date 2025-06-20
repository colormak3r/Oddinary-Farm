using UnityEngine;
using System.Collections;

public class GraphicsUI : MonoBehaviour
{
    //for hot air balloon graphics in main menu
    [Header("Graphics UI Settings")]
    public float xDriftRange = 0.2f;
    public float yDriftRange = 0.3f;
    public float driftSpeed = 1.0f;

    private Vector3 originalLocalPos;
    private Coroutine driftRoutine;

    private void OnEnable()
    {
        originalLocalPos = transform.localPosition;
        driftRoutine = StartCoroutine(FloatDrift());
    }

    private void OnDisable()
    {
        if (driftRoutine != null)
        {
            StopCoroutine(driftRoutine);
            driftRoutine = null;
        }
    }

    private IEnumerator FloatDrift()
    {
        float timeOffset = Random.Range(0f, 100f);

        while (true)
        {
            float time = Time.time + timeOffset;
            float offsetX = Mathf.PerlinNoise(time * driftSpeed, 0f) * 2f - 1f;
            float offsetY = Mathf.PerlinNoise(0f, time * driftSpeed) * 2f - 1f;

            transform.localPosition = originalLocalPos + new Vector3(offsetX * xDriftRange, offsetY * yDriftRange, 0);
            yield return null;
        }
    }
}

using System.Collections;
using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private LocalObjectController controller;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        controller = GetComponent<LocalObjectController>();
    }

    public void SetLaserBeam(Vector2 startPosition, Vector2 targetPosition, float range, float width, float duration)
    {
        var direction = (targetPosition - startPosition).normalized;
        var endPosition = startPosition + direction * range;

        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);

        StartCoroutine(FadeCoroutine(duration));
    }

    private IEnumerator FadeCoroutine(float duration)
    {
        lineRenderer.widthMultiplier = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1.0f, 0.0f, elapsedTime / duration);
            lineRenderer.widthMultiplier = alpha;
            yield return null;
        }

        lineRenderer.widthMultiplier = 0.0f;
        lineRenderer.positionCount = 0;

        controller.LocalDespawn(true);
    }
}

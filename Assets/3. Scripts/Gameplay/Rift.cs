using System.Collections;
using UnityEngine;

public class Rift : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void StartRift(Vector2 startPosition, Vector2 endPosition, float duration)
    {
        if (riftCoroutine != null) StopCoroutine(riftCoroutine);
        riftCoroutine = StartCoroutine(RiftCoroutine(startPosition, endPosition, duration));
    }

    private Coroutine riftCoroutine;
    private IEnumerator RiftCoroutine(Vector2 startPosition, Vector2 endPosition, float duration)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.startWidth = 1f;
        lineRenderer.endWidth = 1f;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            Vector2 shootingPos = Vector2.Lerp(startPosition, endPosition, t);
            lineRenderer.SetPosition(1, shootingPos);
            yield return null;
        }

        lineRenderer.SetPosition(1, endPosition);

        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            lineRenderer.startWidth = Mathf.Lerp(1, 0f, t);
            lineRenderer.endWidth = Mathf.Lerp(1, 0f, t);
            yield return null;
        }

        // Optionally reset or disable the rift after the duration
        lineRenderer.positionCount = 0;

        yield return new WaitForSeconds(duration); // Small delay before despawning

        LocalObjectPooling.Main.Despawn(gameObject);
    }
}

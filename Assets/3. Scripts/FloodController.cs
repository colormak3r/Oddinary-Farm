using System.Collections;
using UnityEngine;

public class FloodController : MonoBehaviour
{
    // Cache the shader property ID for performance.
    private static readonly int FloodedId = Shader.PropertyToID("_Flooded");

    [Header("Settings")]
    [SerializeField]
    private bool alwaysFlood = false;
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private SpriteRenderer[] alphaRenderer;

    private float floodDuration = 3f;

    private float elevation;
    private float currentFloodLevel = 0;

    private bool isInitialized = false;

    private MaterialPropertyBlock mpb;
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        mpb = new MaterialPropertyBlock();

        if (!alwaysFlood)
            FloodManager.Main.OnFloodLevelChanged += HandleFloodLevelChange;
        else
            SetFloodedMaterial(1);
    }

    private void OnDestroy()
    {
        if (!alwaysFlood)
            FloodManager.Main.OnFloodLevelChanged -= HandleFloodLevelChange;
    }

    private void HandleFloodLevelChange(float floodLevel, float waterLevel)
    {
        if (!isInitialized || !gameObject.activeInHierarchy) return;

        EvaluateFloodLevel(false);
    }

    public void SetFloodThreshhold(float value)
    {
        elevation = value;

        EvaluateFloodLevel(true);

        isInitialized = true;
    }

    private void EvaluateFloodLevel(bool instant)
    {
        var waterLevel = FloodManager.Main.CurrentWaterLevel;
        var upperBound = waterLevel + 2 * FloodManager.Main.FloodLevelChangePerHour;
        var newFloodLevel = 0f;
        if (elevation <= waterLevel)
            newFloodLevel = 1f;
        else if (elevation < upperBound)
            newFloodLevel = 1 - (elevation - waterLevel) / (upperBound - waterLevel);
        else
            newFloodLevel = -1f;

        if (instant)
            SetFloodedMaterial(newFloodLevel);
        else
            StartCoroutine(FloodedCoroutine(currentFloodLevel, newFloodLevel, floodDuration));

        currentFloodLevel = newFloodLevel;
    }


    [ContextMenu("Flood")]
    private void Flood()
    {
        StartCoroutine(FloodedCoroutine(mpb.GetFloat(FloodedId), 1, floodDuration));
    }

    [ContextMenu("Half Flood")]
    private void HalfFlood()
    {
        StartCoroutine(FloodedCoroutine(mpb.GetFloat(FloodedId), 0.5f, floodDuration));
    }

    [ContextMenu("Dry")]
    private void Dry()
    {
        StartCoroutine(FloodedCoroutine(mpb.GetFloat(FloodedId), 0, floodDuration));
    }

    private IEnumerator FloodedCoroutine(float start, float end, float duration)
    {
        float t = 0;
        while (t < 1)
        {
            float value = Mathf.Lerp(start, end, t);
            SetFloodedMaterial(value);
            t += Time.deltaTime / duration;
            yield return null;
        }
        SetFloodedMaterial(end);

    }

    private void SetFloodedMaterial(float value)
    {
        foreach (var renderer in alphaRenderer)
            renderer.color = new Color(1, 1, 1, 1 - value);

        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(FloodedId, value);
        spriteRenderer.SetPropertyBlock(mpb);
    }
}

using System.Collections;
using UnityEngine;

public class FloodController : MonoBehaviour
{
    // Cache the shader property ID for performance.
    private static readonly int FLOOD_ID = Shader.PropertyToID("_Flooded");
    private static readonly int DEPTH_ID = Shader.PropertyToID("_Depth");

    [Header("Settings")]
    [SerializeField]
    private bool alwaysFlood = false;
    [SerializeField]
    private float floodDuration = 3f;
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private SpriteRenderer[] alphaRenderer;

    [Header("Debugs")]
    [SerializeField]
    private float elevation;
    [SerializeField]
    private float previousFloodLevel = 0;
    [SerializeField]
    private float previousDepthLevel = 0;

    private bool isInitialized = false;

    private MaterialPropertyBlock mpb;
    private FloodManager floodManager;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        mpb = new MaterialPropertyBlock();
        floodManager = FloodManager.Main;

        if (!alwaysFlood)
            floodManager.OnFloodLevelChanged += HandleFloodLevelChange;
        else
            SetFloodedMaterial(1, 0.5f);
    }

    private void OnDestroy()
    {
        if (!alwaysFlood)
            floodManager.OnFloodLevelChanged -= HandleFloodLevelChange;
    }

    private void HandleFloodLevelChange(float floodLevel, float waterLevel, float depthLevel)
    {
        if (!isInitialized || !gameObject.activeInHierarchy) return;

        EvaluateFloodLevel(false, floodLevel, waterLevel, depthLevel);
    }

    public void SetFloodThreshhold(float value)
    {
        elevation = value;

        EvaluateFloodLevel(true, floodManager.CurrentFloodLevelValue, floodManager.CurrentSafeLevel, floodManager.CurrentDepthLevel);

        isInitialized = true;
    }

    private void EvaluateFloodLevel(bool instant, float floodLevel, float safeLevel, float depthLevel)
    {
        var flooded = elevation > safeLevel ? 0 : Mathf.Clamp01((safeLevel - elevation) / (safeLevel - depthLevel));
        var depth = Mathf.Clamp01((safeLevel - elevation) / (safeLevel - depthLevel));

        if (instant)
        {
            SetFloodedMaterial(flooded, depth);
        }
        else
        {
            StartCoroutine(FloodedCoroutine(previousFloodLevel, flooded, previousDepthLevel, depth, floodDuration));
        }

        previousFloodLevel = flooded;
        previousDepthLevel = depth;
    }

    private IEnumerator FloodedCoroutine(float floodStart, float floatEnd, float depthStart, float depthEnd, float duration)
    {
        float t = 0;
        while (t < 1)
        {
            var flood = Mathf.Lerp(floodStart, floatEnd, t);
            var depth = Mathf.Lerp(depthStart, depthEnd, t);
            SetFloodedMaterial(flood, depth);
            t += Time.deltaTime / duration;
            yield return null;
        }
        SetFloodedMaterial(floatEnd, depthEnd);

    }

    private void SetFloodedMaterial(float flooded, float depth)
    {
        foreach (var renderer in alphaRenderer)
            renderer.color = new Color(1, 1, 1, 1 - flooded);

        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(FLOOD_ID, flooded);
        mpb.SetFloat(DEPTH_ID, depth);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    [ContextMenu("Flood")]
    private void Flood()
    {
        StartCoroutine(FloodedCoroutine(mpb.GetFloat(FLOOD_ID), 1, 0.5f, 0.5f, floodDuration));
    }

    [ContextMenu("Half Flood")]
    private void HalfFlood()
    {
        StartCoroutine(FloodedCoroutine(mpb.GetFloat(FLOOD_ID), 0.5f, 0.5f, 0.5f, floodDuration));
    }

    [ContextMenu("Dry")]
    private void Dry()
    {
        StartCoroutine(FloodedCoroutine(mpb.GetFloat(FLOOD_ID), 0, 0.5f, 0.5f, floodDuration));
    }
}

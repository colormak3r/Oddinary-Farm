using System.Collections;
using UnityEngine;

public class FloodController : MonoBehaviour
{
    // Cache the shader property ID for performance.
    private static readonly int SPRITE_HEIGHT_ID = Shader.PropertyToID("_SpriteHeight");
    private static readonly int WATER_LEVEL_XY_ID = Shader.PropertyToID("_WaterLevel_XY");
    private static readonly int WATER_LEVEL_Z_ID = Shader.PropertyToID("_WaterLevel_Z");
    private static readonly int WATER_DEPTH_ID = Shader.PropertyToID("_WaterDepth");

    [Header("Settings")]
    [SerializeField]
    private bool alwaysFlood = false;
    [SerializeField]
    private bool floodVertical = false;
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
    private float previousWaterLevel = 0;
    [SerializeField]
    private float previousDepth = 0;

    private Material defaultMaterial;

    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        //Debug.Log(spriteRenderer.bounds.max.y - spriteRenderer.bounds.min.y, this);
        defaultMaterial = spriteRenderer.material;
        mpb = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (!alwaysFlood)
        {
            FloodManager.Main.OnFloodLevelChanged += HandleFloodLevelChange;
        }
        else
        {
            SetFloodMaterial(1, 0.5f);
        }

    }

    private void OnDestroy()
    {
        if (!alwaysFlood)
            FloodManager.Main.OnFloodLevelChanged -= HandleFloodLevelChange;
    }

    private void HandleFloodLevelChange(float floodLevel, float safeLevel, float depthLevel)
    {
        if (!WorldGenerator.Main || !WorldGenerator.Main.IsInitialized || !gameObject.activeInHierarchy) return;

        elevation = WorldGenerator.Main.GetElevation(transform.position.x, transform.position.y);
        EvaluateWaterLevel(false, floodLevel, safeLevel, depthLevel);
    }

    public void SetElevation(float elevation)
    {
        this.elevation = elevation;
        EvaluateWaterLevel(true, FloodManager.Main.CurrentFloodLevelValue, FloodManager.Main.CurrentSafeLevel, FloodManager.Main.CurrentDepthLevel);
    }

    private void EvaluateWaterLevel(bool instant, float floodLevel, float safeLevel, float depthLevel)
    {
        var waterLevel = elevation > safeLevel ? 0 : Mathf.Clamp01((safeLevel - elevation) / (safeLevel - depthLevel));
        var depth = Mathf.Clamp01((safeLevel - elevation) / (safeLevel - depthLevel));

        if (instant)
        {
            SetFloodMaterial(waterLevel, depth);
        }
        else
        {
            StartCoroutine(FloodCoroutine(previousWaterLevel, waterLevel, previousDepth, depth, floodDuration));
        }

        previousWaterLevel = waterLevel;
        previousDepth = depth;
    }

    private IEnumerator FloodCoroutine(float waterLevelStart, float waterLevelEnd, float depthStart, float depthEnd, float duration)
    {
        if (spriteRenderer.material != AssetManager.Main.WaterMaterial)
            spriteRenderer.material = AssetManager.Main.WaterMaterial;

        float t = 0;
        while (t < 1)
        {
            var waterLevel = Mathf.Lerp(waterLevelStart, waterLevelEnd, t);
            var depth = Mathf.Lerp(depthStart, depthEnd, t);
            SetFloodMaterial(waterLevel, depth);
            t += Time.deltaTime / duration;
            yield return null;
        }
        SetFloodMaterial(waterLevelEnd, depthEnd);

        if (waterLevelEnd == 0 && spriteRenderer.material != defaultMaterial)
            spriteRenderer.material = defaultMaterial;
    }

    private void SetFloodMaterial(float waterLevel, float depth)
    {
        foreach (var renderer in alphaRenderer)
            renderer.color = new Color(1, 1, 1, 1 - waterLevel);

        spriteRenderer.GetPropertyBlock(mpb);

        mpb.SetFloat(SPRITE_HEIGHT_ID, spriteRenderer.sprite.rect.height);
        mpb.SetFloat(WATER_DEPTH_ID, depth);
        if (floodVertical)
        {
            mpb.SetFloat(WATER_LEVEL_Z_ID, waterLevel);
        }
        else
        {
            mpb.SetFloat(WATER_LEVEL_XY_ID, waterLevel);
            mpb.SetFloat(WATER_LEVEL_Z_ID, 0);
        }

        if (waterLevel != 0)
            spriteRenderer.SetPropertyBlock(mpb);
        else
            spriteRenderer.SetPropertyBlock(null);
    }

    [ContextMenu("Flood")]
    private void Flood()
    {
        StartCoroutine(FloodCoroutine(mpb.GetFloat(floodVertical ? WATER_LEVEL_Z_ID : WATER_LEVEL_XY_ID), 1, 0.5f, 0.5f, floodDuration));
    }

    [ContextMenu("Half Flood")]
    private void HalfFlood()
    {
        StartCoroutine(FloodCoroutine(mpb.GetFloat(floodVertical ? WATER_LEVEL_Z_ID : WATER_LEVEL_XY_ID), 0.5f, 0.5f, 0.5f, floodDuration));
    }

    [ContextMenu("Dry")]
    private void Dry()
    {
        StartCoroutine(FloodCoroutine(mpb.GetFloat(floodVertical ? WATER_LEVEL_Z_ID : WATER_LEVEL_XY_ID), 0, 0.5f, 0.5f, floodDuration));
    }
}

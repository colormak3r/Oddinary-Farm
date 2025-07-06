using System.Collections;
using Unity.VisualScripting;
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

    /*private void EvaluateWaterLevel(bool instant, float floodLevel, float safeLevel, float depthLevel)
    {
        var waterLevel = elevation > safeLevel ? 0 : Mathf.Clamp01((safeLevel - elevation) / (safeLevel - depthLevel));
        var depth = Mathf.Clamp01((safeLevel - elevation) / (safeLevel - depthLevel));

        if (instant)
        {
            SetFloodMaterial(waterLevel, depth);
        }
        else
        {
            //SetFloodMaterial(waterLevel, depth);
            StartCoroutine(FloodCoroutine(previousWaterLevel, waterLevel, previousDepth, depth, floodDuration));
        }

        previousWaterLevel = waterLevel;
        previousDepth = depth;
    }

    *//*private IEnumerator FloodCoroutine(float waterLevelStart, float waterLevelEnd, float depthStart, float depthEnd, float duration)
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

        yield return null;

        if (waterLevelEnd == 0 && spriteRenderer.material != defaultMaterial)
            spriteRenderer.material = defaultMaterial;
    }*//*

    private IEnumerator FloodCoroutine(float waterLevelStart, float waterLevelEnd, float depthStart, float depthEnd, float duration)
    {
        // Make sure we are using the water material
        if (spriteRenderer.material != AssetManager.Main.WaterMaterial)
            spriteRenderer.material = AssetManager.Main.WaterMaterial;

        const int updates = 3;                     // total SetFloodMaterial calls we want
        float interval = duration / (updates - 1); // seconds between updates (4 gaps to 5 hits)
        var wait = new WaitForSeconds(interval);   // cached to avoid per-loop allocations

        for (int i = 0; i < updates; ++i)
        {
            float t = i / (float)(updates - 1);    // 0, 0.25, 0.5, 0.75, 1
            float waterLevel = Mathf.Lerp(waterLevelStart, waterLevelEnd, t);
            float depth = Mathf.Lerp(depthStart, depthEnd, t);

            SetFloodMaterial(waterLevel, depth);

            if (i < updates - 1)                   // don’t wait after the last update
                yield return wait;
        }

        // Swap back to the default material if the water is gone
        if (Mathf.Approximately(waterLevelEnd, 0f) &&
            spriteRenderer.material != defaultMaterial)
        {
            spriteRenderer.material = defaultMaterial;
        }
    }*/

    private bool floodAnimating;
    private int floodStepIndex;
    private const int FLOOD_STEPS = 5;        // total SetFloodMaterial calls

    private float floodTimer;                 // seconds since animation started
    private float floodInterval;              // duration / (steps-1)

    private float flStart, flEnd;             // water-level start / end
    private float dpStart, dpEnd;             // depth      start / end

    #region Update Loop With Steps
    private void EvaluateWaterLevel(bool instant,
                                   float floodLevel, float safeLevel, float depthLevel)
    {
        float waterLevel = elevation > safeLevel
                           ? 0
                           : Mathf.Clamp01((safeLevel - elevation) /
                                           (safeLevel - depthLevel));
        float depth = Mathf.Clamp01((safeLevel - elevation) /
                                         (safeLevel - depthLevel));

        if (instant)
        {
            SetFloodMaterial(waterLevel, depth);
        }
        else
        {
            // prime the animation state
            floodAnimating = true;
            floodStepIndex = 0;
            floodTimer = 0f;
            floodInterval = floodDuration / (FLOOD_STEPS - 1);

            flStart = previousWaterLevel; flEnd = waterLevel;
            dpStart = previousDepth; dpEnd = depth;

            // first step immediately
            SetFloodMaterial(flStart, dpStart);
        }

        previousWaterLevel = waterLevel;
        previousDepth = depth;
    }

    private void Update()
    {
        if (!floodAnimating) return;

        floodTimer += Time.deltaTime;

        // do we need to fire the next step?
        if (floodTimer >= floodStepIndex * floodInterval)
        {
            float t = floodStepIndex / (float)(FLOOD_STEPS - 1); // 0, 0.5, 1  (for 3 steps)
            float waterLevel = Mathf.Lerp(flStart, flEnd, t);
            float depth = Mathf.Lerp(dpStart, dpEnd, t);

            SetFloodMaterial(waterLevel, depth);
            floodStepIndex++;

            if (floodStepIndex >= FLOOD_STEPS)                    // finished
            {
                floodAnimating = false;

                if (Mathf.Approximately(flEnd, 0f) &&
                    spriteRenderer.material != defaultMaterial)
                {
                    spriteRenderer.material = defaultMaterial;
                }
            }
        }
    }
    #endregion

   /* private float floodDurationCurrent;   // keep the target duration

    private void EvaluateWaterLevel(bool instant,
                                    float floodLevel, float safeLevel, float depthLevel)
    {
        float waterLevel = elevation > safeLevel
                           ? 0
                           : Mathf.Clamp01((safeLevel - elevation) /
                                           (safeLevel - depthLevel));
        float depth = Mathf.Clamp01((safeLevel - elevation) /
                                    (safeLevel - depthLevel));

        if (instant)
        {
            SetFloodMaterial(waterLevel, depth);
        }
        else
        {
            floodAnimating = true;
            floodTimer = 0f;
            floodDurationCurrent = floodDuration;   // cache once in case the field changes

            flStart = previousWaterLevel; flEnd = waterLevel;
            dpStart = previousDepth; dpEnd = depth;
        }

        previousWaterLevel = waterLevel;
        previousDepth = depth;
    }

    private void Update()
    {
        if (!floodAnimating) return;

        floodTimer += Time.deltaTime;
        float t = Mathf.Clamp01(floodTimer / floodDurationCurrent);

        SetFloodMaterial(Mathf.Lerp(flStart, flEnd, t),
                         Mathf.Lerp(dpStart, dpEnd, t));

        if (t >= 1f)                       // finished
        {
            floodAnimating = false;

            if (Mathf.Approximately(flEnd, 0f) &&
                spriteRenderer.material != defaultMaterial)
            {
                spriteRenderer.material = defaultMaterial;
            }
        }
    }*/

    private void SetFloodMaterial(float waterLevel, float depth)
    {
        foreach (var renderer in alphaRenderer)
            renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 1 - waterLevel);

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

        if (waterLevel == 0 && spriteRenderer.material != defaultMaterial)
            spriteRenderer.material = defaultMaterial;
    }

    /*[ContextMenu("Flood")]
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
    }*/
}

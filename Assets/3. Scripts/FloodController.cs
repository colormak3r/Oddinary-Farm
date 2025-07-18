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

    #region Debugs

    [ContextMenu("Flood 1")]
    private void FloodAll()
    {
        SetFloodMaterial(1, 0.5f);
    }

    [ContextMenu("Flood 0.5")]
    private void FloodHalf()
    {
        SetFloodMaterial(0.5f, 0.5f);
    }

    [ContextMenu("Flood 0.25")]
    private void FloodQuarter()
    {
        SetFloodMaterial(0.25f, 0.5f);
    }

    [ContextMenu("Flood 0")]
    private void FloodNone()
    {
        SetFloodMaterial(0, 0.5f);
    }

    #endregion
}

using ColorMak3r.Utility;
using UnityEngine;

public class DrownGraphic : MonoBehaviour
{
    [Header("Drown Graphic")]
    [SerializeField]
    private bool canBeDrown = true;
    [SerializeField]
    private bool canBeWet = true;
    [SerializeField]
    private float wetPercentage = 0.1f;
    [SerializeField]
    private Transform waterTransform;
    [SerializeField]
    private float speed = 2f;

    private float maxWaterY = -1.25f;
    private float minWaterY = -2.5f;

    private float range;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        range = maxWaterY - minWaterY;
        spriteRenderer = waterTransform.GetComponent<SpriteRenderer>();
    }

    [Header("Debugs")]
    [SerializeField]
    private float waterLevel = 0.5f;
    private void Update()
    {
        if (WorldGenerator.Main == null || FloodManager.Main == null || !WorldGenerator.Main.IsInitialized) return;
        var floodManager = FloodManager.Main;
        var elevation = WorldGenerator.Main.GetElevation(transform.position.x, transform.position.y);

        if (canBeWet)
        {
            if (canBeDrown)
            {
                if (elevation > floodManager.CurrentSafeLevel)
                {
                    waterLevel = 0;
                }
                else if (elevation > floodManager.CurrentFloodLevelValue && elevation <= floodManager.CurrentSafeLevel)
                {
                    waterLevel = Mathf.Clamp01((floodManager.CurrentSafeLevel - elevation) / wetPercentage);
                }
                else
                {
                    waterLevel = Mathf.Clamp((floodManager.CurrentFloodLevelValue - elevation) / (floodManager.DepthRange), wetPercentage, 1f);
                }
            }
            else
            {
                waterLevel = wetPercentage;
            }
        }
        else
        {
            waterLevel = 0;
        }

        waterTransform.localPosition = Vector2.Lerp(waterTransform.localPosition, new Vector2(0, waterLevel * range + minWaterY), speed * Time.deltaTime);
        spriteRenderer.color = Color.Lerp(spriteRenderer.color, spriteRenderer.color.SetAlpha(waterLevel), speed * Time.deltaTime);
    }

    [ContextMenu("Test Min")]
    private void TestMin()
    {
        waterTransform.localPosition = new Vector2(0, minWaterY);
    }

    [ContextMenu("Test Max")]
    private void TestMax()
    {
        waterTransform.localPosition = new Vector2(0, maxWaterY);
    }

    public void SetCanBeWet(bool canBeWet)
    {
        this.canBeWet = canBeWet;
        if (!canBeWet)
        {
            var renderers = GetComponentsInChildren<SpriteMask>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }
        }
    }
}

using ColorMak3r.Utility;
using UnityEngine;

public class DrownGraphic : MonoBehaviour
{

    [Header("Drown Graphic")]
    [SerializeField]
    private bool canBeDrowned = true;
    [SerializeField]
    private Transform waterTransform;
    [SerializeField]
    private float speed = 2f;
    [SerializeField]
    private float maxWaterY = -1.25f;
    [SerializeField]
    private float minWaterY = -3.5f;

    private float range;

    private void Awake()
    {
        range = maxWaterY - minWaterY;
    }

    private void Update()
    {
        if (!canBeDrowned)
        {
            waterTransform.localPosition = Vector2.Lerp(waterTransform.localPosition, new Vector2(0, minWaterY), speed * Time.deltaTime);
        }
        else
        {
            if (WorldGenerator.Main == null || FloodManager.Main == null || !WorldGenerator.Main.IsInitialized) return;

            var snappedPos = ((Vector2)transform.position).SnapToGrid().ToInt();
            var closetElevation = WorldGenerator.Main.GetElevation(snappedPos.x, snappedPos.y);
            var offsetX = Mathf.RoundToInt(Mathf.Sign(transform.position.x - snappedPos.x));
            var offsetY = Mathf.RoundToInt(Mathf.Sign(transform.position.y - snappedPos.y));
            var offsetElevation = WorldGenerator.Main.GetElevation(snappedPos.x + offsetX, snappedPos.y + offsetY);
            var elevation = Mathf.Lerp(closetElevation, offsetElevation, 1 - Vector2.Distance(transform.position, snappedPos));
            var waterLevel = Mathf.Clamp01((FloodManager.Main.CurrentWaterLevel - elevation) / (FloodManager.Main.CurrentFloodLevelValue * 0.1f));
            waterTransform.localPosition = Vector2.Lerp(waterTransform.localPosition, new Vector2(0, waterLevel * range + minWaterY), speed * Time.deltaTime);
        }
    }

    public void SetCanBeDrowned(bool canBeDrowned)
    {
        this.canBeDrowned = canBeDrowned;
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
}

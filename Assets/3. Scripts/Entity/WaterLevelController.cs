using ColorMak3r.Utility;
using System.Collections;
using UnityEngine;

public class WaterLevelController : MonoBehaviour
{
    [SerializeField]
    private Transform waterTransform;
    [SerializeField]
    private float speed = 2f;
    [SerializeField]
    private float maxWaterY = -1.25f;
    [SerializeField]
    private float minWaterY = -2.5f;

    private float range;

    void Awake()
    {
        range = maxWaterY - minWaterY;
    }

    private void Update()
    {
        var pos = ((Vector2)transform.position).SnapToGrid();
        var elevation = WorldGenerator.Main.GetElevation((int)pos.x, (int)pos.y);
        var waterLevel = Mathf.Clamp01((FloodManager.Main.CurrentWaterLevel - elevation) / (FloodManager.Main.CurrentFloodLevelValue * 0.1f));
        waterTransform.localPosition = Vector2.Lerp(waterTransform.localPosition, new Vector2(0, waterLevel * range + minWaterY), speed * Time.deltaTime);
    }
}

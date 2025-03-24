using System.Collections;
using UnityEngine;

public class FloodController : MonoBehaviour
{
    private static string COORDINATE_ID = "_Coordinate";
    private static string FLOODED_ID = "_Flooded";

    [Header("Settings")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    private float floodDuration = 3f;
    private MaterialPropertyBlock material;
    private float floodThreshhold;
    private bool isFlooded = false;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        material = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(material);
    }

    private void OnEnable()
    {
        FloodManager.Main.OnFloodLevelChanged += HandleFloodLevelChange;
    }

    private void OnDisable()
    {
        FloodManager.Main.OnFloodLevelChanged -= HandleFloodLevelChange;
    }

    private void HandleFloodLevelChange(float floodLevel)
    {
        if (floodThreshhold <= floodLevel)
        {
            Flood();
        }
        else
        {
            Dry();
        }
    }

    public void SetFloodThreshhold(float value)
    {
        floodThreshhold = value;

        // Set flood instantly
        var floodLevel = FloodManager.Main.CurrentFloodLevelValue;
        isFlooded = floodThreshhold < floodLevel;
        material.SetVector(COORDINATE_ID, transform.position);
        material.SetFloat(FLOODED_ID, isFlooded ? 1 : 0);
        spriteRenderer.SetPropertyBlock(material);
    }


    [ContextMenu("Flood")]
    public void Flood()
    {
        if (!isFlooded)
        {
            isFlooded = true;
            StartCoroutine(FloodedCoroutine(0, 1, floodDuration));
        }
    }

    [ContextMenu("Dry")]
    public void Dry()
    {
        if (isFlooded)
        {
            isFlooded = false;
            StartCoroutine(FloodedCoroutine(1, 0, floodDuration));
        }
    }

    private IEnumerator FloodedCoroutine(float start, float end, float duration)
    {
        material.SetVector(COORDINATE_ID, transform.position);
        float t = 0;
        float value = start;
        while (t < 1)
        {
            material.SetFloat(FLOODED_ID, value);
            spriteRenderer.SetPropertyBlock(material);
            value = Mathf.Lerp(start, end, t);
            t += Time.deltaTime / duration;
            yield return null;
        }
        material.SetFloat(FLOODED_ID, end);
        spriteRenderer.SetPropertyBlock(material);
    }
}

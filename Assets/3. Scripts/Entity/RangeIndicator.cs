/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/21/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections;
using UnityEngine;

public class RangeIndicator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool showRange = true;
    [SerializeField]
    private Transform circleTransform;

    private SpriteRenderer spriteRenderer;

    private Coroutine showCoroutine;

    private void Awake()
    {
        spriteRenderer = circleTransform.GetComponent<SpriteRenderer>();
        spriteRenderer.color = Color.clear;
    }

    private void Start()
    {
        if (GameplayUI.Main == null)
        {
            Debug.LogError("GameplayUI.Main is not initialized. Ensure GameplayUI is loaded before RangeIndicator.");
            return;
        }

        showRange = GameplayUI.Main.ShowItemRange; // Get the initial value from GameplayUI
        GameplayUI.Main.OnShowItemRangeChanged += SetShowRange;
    }

    private void OnDestroy()
    {
        if (GameplayUI.Main != null)
        {
            GameplayUI.Main.OnShowItemRangeChanged -= SetShowRange;
        }
    }

    private void SetShowRange(bool showRange)
    {
        this.showRange = showRange;
    }

    public void Show(float range, float tweenDuration = 1f, float showDuration = 1f)
    {
        if (!showRange) return;
        if (showCoroutine != null) StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(ShowCoroutine(range, tweenDuration, showDuration));
    }

    private IEnumerator ShowCoroutine(float range, float tweenDuration = 1f, float showDuration = 1f)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / tweenDuration;
            circleTransform.localScale = Vector3.Lerp(circleTransform.localScale, Vector3.one * 1.1f * range, t);
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.white, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / showDuration;
            spriteRenderer.color = new Color(1, 1, 1, 1 - t);
            yield return null;
        }
    }
}

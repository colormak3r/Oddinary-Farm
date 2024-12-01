using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float readoutDuration = 3f;
    [SerializeField]
    private Vector2 defaultSize = new Vector2(1.375f, 0.5f);
    [SerializeField]
    private Color fullColor;
    [SerializeField]
    private Color fullColorBg;
    [SerializeField]
    private Color hurtColor;
    [SerializeField]
    private Color hurtColorBg;
    [SerializeField]
    private Color criticalColor;
    [SerializeField] 
    private Color criticalColorBg;
    [SerializeField]
    private Color flashColor;

    [SerializeField]
    private SpriteRenderer displayRenderer;
    [SerializeField]
    private SpriteRenderer backgroundRenderer;

    [SerializeField]
    private SpriteRenderer[] renderers;

    private Coroutine colorCoroutine;
    private Coroutine sizeCoroutine;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    public void SetValue(float health, float maxHealth)
    {
        var ratio = health / maxHealth;
        var newSize = new Vector2(ratio * defaultSize.x, defaultSize.y);
        if (sizeCoroutine != null) StopCoroutine(sizeCoroutine);
        sizeCoroutine = StartCoroutine(ShrinkCoroutine(displayRenderer, newSize, 1.5f));

        if (ratio > 0.66f)
        {
            displayRenderer.color = fullColor;
            backgroundRenderer.color = fullColorBg;
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            colorCoroutine = StartCoroutine(AutoHideCoroutine());
        }
        else if (ratio > 0.33f)
        {
            displayRenderer.color = hurtColor;
            backgroundRenderer.color = hurtColorBg;
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            colorCoroutine = StartCoroutine(AutoHideCoroutine());
        }
        else
        {
            displayRenderer.color = criticalColor;
            backgroundRenderer.color = criticalColorBg;
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            colorCoroutine = StartCoroutine(FlashingCoroutine());
        }


    }

    private IEnumerator ShrinkCoroutine(SpriteRenderer renderer, Vector2 newSize, float duration = 0.25f)
    {
        var elapsed = 0f;
        while (elapsed < 1f)
        {
            renderer.size = Vector2.Lerp(renderer.size, newSize, elapsed);
            elapsed += Time.deltaTime / duration;
            yield return null;
        }
        renderer.size = newSize;
    }

    private IEnumerator AutoHideCoroutine()
    {
        foreach (var renderer in renderers)
        {
            renderer.color = renderer.color.SetAlpha(1);
        }

        yield return new WaitForSeconds(readoutDuration);

        foreach (var renderer in renderers)
        {
            StartCoroutine(renderer.SpriteFadeCoroutine(1, 0, 0.5f));
        }
    }

    private IEnumerator FlashingCoroutine()
    {
        foreach (var renderer in renderers)
        {
            renderer.color = renderer.color.SetAlpha(1);
        }

        while (true)
        {
            displayRenderer.color = flashColor;

            yield return displayRenderer.SpriteColorCoroutine(flashColor, criticalColor, 0.5f);
        }
    }
}

using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField]
    private float autoHideDuration = 3f;
    [SerializeField]
    private float flashDuration = 0.5f;
    [SerializeField]
    private float shrinkDuration = 1f;
    [SerializeField]
    private Vector2 defaultSize = new Vector2(1.375f, 0.5f);

    [Header("Color Settings")]
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
    private Color highlightColor;

    [Header("Required Components")]
    [SerializeField]
    private SpriteRenderer outlineRenderer;
    [SerializeField]
    private SpriteRenderer displayRenderer;
    [SerializeField]
    private SpriteRenderer backgroundRenderer;
    [SerializeField]
    private SpriteRenderer highlightRenderer;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;

    private Coroutine colorCoroutine;
    private Coroutine sizeCoroutine;

    private void Start()
    {
        outlineRenderer.color = outlineRenderer.color.SetAlpha(0);
        displayRenderer.color = displayRenderer.color.SetAlpha(0);
        backgroundRenderer.color = backgroundRenderer.color.SetAlpha(0);
        highlightRenderer.color = highlightRenderer.color.SetAlpha(0);
    }

    public void SetValue(float health, float maxHealth)
    {
        if (showDebugs) Debug.Log($"{transform.root.gameObject.name} health: {health} / {maxHealth}", this);

        var ratio = health / maxHealth;
        var newSize = new Vector2(ratio * defaultSize.x, defaultSize.y);
        if (sizeCoroutine != null) StopCoroutine(sizeCoroutine);
        sizeCoroutine = StartCoroutine(ShrinkCoroutine(displayRenderer, newSize, shrinkDuration));
        highlightRenderer.color = highlightColor;

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
        ResetColor();

        yield return new WaitForSeconds(autoHideDuration);

        StartCoroutine(displayRenderer.SpriteFadeCoroutine(1, 0, 0.5f));
        StartCoroutine(backgroundRenderer.SpriteFadeCoroutine(1, 0, 0.5f));
        StartCoroutine(highlightRenderer.SpriteFadeCoroutine(1, 0, 0.5f));
        StartCoroutine(outlineRenderer.SpriteFadeCoroutine(1, 0, 0.5f));
    }

    private IEnumerator FlashingCoroutine()
    {
        ResetColor();

        while (true)
        {
            displayRenderer.color = flashColor;

            yield return displayRenderer.SpriteColorCoroutine(flashColor, criticalColor, flashDuration);
        }
    }

    private void ResetColor()
    {
        outlineRenderer.color = outlineRenderer.color.SetAlpha(1);
        displayRenderer.color = displayRenderer.color.SetAlpha(1);
        backgroundRenderer.color = backgroundRenderer.color.SetAlpha(1);
        highlightRenderer.color = highlightColor;
    }
}

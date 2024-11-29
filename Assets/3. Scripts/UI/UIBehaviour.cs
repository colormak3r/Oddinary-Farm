using System.Collections;
using UnityEngine;
using ColorMak3r.Utility;
using UnityEngine.Events;
using System.Collections.Generic;

public class UIBehaviour : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField]
    protected GameObject container;
    [SerializeField]
    protected float fadeDuration = 0.25f;
    [SerializeField]
    protected bool delayShow;
    [SerializeField]
    protected GameObject[] delayShowFadeFirstObjects;

    [Header("Debugs")]
    [SerializeField]
    private bool isShowing;
    [SerializeField]
    private bool isAnimating;
    [SerializeField]
    private CanvasRenderer[] renderers;
    [SerializeField]
    private CanvasRenderer[] dsffoRenderers;

    [HideInInspector]
    public UnityEvent<bool> OnVisibilityChanged;

    public bool IsShowing => isShowing;
    public bool IsAnimating => isAnimating;

    private void OnEnable()
    {
        isShowing = container.activeSelf;

        var renderers = new List<CanvasRenderer>();
        renderers.AddRange(container.GetComponentsInChildren<CanvasRenderer>(true));
        var dsffoRenderers = new List<CanvasRenderer>();

        if (delayShowFadeFirstObjects != null)
        {
            foreach (GameObject obj in delayShowFadeFirstObjects)
            {
                dsffoRenderers.AddRange(obj.GetComponentsInChildren<CanvasRenderer>(true));
            }
        }

        renderers.RemoveAll(renderer => dsffoRenderers.Contains(renderer));
        this.renderers = renderers.ToArray();
        this.dsffoRenderers = dsffoRenderers.ToArray();
    }

    public IEnumerator ShowCoroutine(bool fade = true)
    {
        if (isShowing) yield break;

        if (delayShow) yield return new WaitForSeconds(fadeDuration);

        isShowing = true;

        container.SetActive(true);

        isAnimating = true;
        yield return renderers.UIFadeCoroutine(0, 1, fade ? fadeDuration : 0);
        yield return dsffoRenderers.UIFadeCoroutine(0, 1, fade ? fadeDuration : 0);
        isAnimating = false;

        OnVisibilityChanged?.Invoke(isShowing);
    }

    public IEnumerator HideCoroutine(bool fade = true)
    {
        if (!isShowing) yield break;

        isAnimating = true;
        yield return dsffoRenderers.UIFadeCoroutine(1, 0, fade ? fadeDuration : 0);
        yield return renderers.UIFadeCoroutine(1, 0, fade ? fadeDuration : 0);
        isAnimating = false;

        container.SetActive(false);

        isShowing = false;

        OnVisibilityChanged?.Invoke(isShowing);
    }

    public void Show()
    {
        if (IsShowing || isAnimating) return;

        StartCoroutine(ShowCoroutine());
    }

    public void Hide()
    {
        if (!IsShowing || isAnimating) return;

        StartCoroutine(HideCoroutine());
    }

    public void ShowNoFade()
    {
        if (IsShowing || isAnimating) return;
        StartCoroutine(ShowCoroutine(false));
    }

    public void HideNoFade()
    {
        if (!IsShowing || isAnimating) return;
        StartCoroutine(HideCoroutine(false));
    }

    [ContextMenu("Toggle Show")]
    public virtual void ToggleShow()
    {
        if (isAnimating) return;

        if (isShowing)
            StartCoroutine(HideCoroutine());
        else
            StartCoroutine(ShowCoroutine());

        // isShowing = !isShowing;
    }
}

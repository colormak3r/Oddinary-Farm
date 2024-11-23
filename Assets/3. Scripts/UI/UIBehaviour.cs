using System.Collections;
using UnityEngine;
using ColorMak3r.Utility;
using UnityEngine.Events;

public class UIBehaviour : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField]
    protected GameObject container;
    [SerializeField]
    protected float fadeDuration = 0.25f;

    [Header("Debugs")]
    [SerializeField]
    private bool isShowing;
    [SerializeField]
    private bool isAnimating;

    [HideInInspector]
    public UnityEvent<bool> OnVisibilityChanged;

    public bool IsShowing => isShowing;
    public bool IsAnimating => isAnimating;

    private void Awake()
    {
        isShowing = container.activeSelf;
    }

    public IEnumerator ShowCoroutine(bool fade = true)
    {
        if (isShowing) yield break;
        isShowing = true;

        container.SetActive(true);

        isAnimating = true;
        yield return container.UIFadeCoroutine(0, 1, fade ? fadeDuration : 0);
        isAnimating = false;

        OnVisibilityChanged?.Invoke(isShowing);
    }

    public IEnumerator UnShowCoroutine(bool fade = true)
    {
        if (!isShowing) yield break;

        isAnimating = true;
        yield return container.UIFadeCoroutine(1, 0, fade ? fadeDuration : 0);
        isAnimating = false;

        container.SetActive(false);

        isShowing = false;

        OnVisibilityChanged?.Invoke(isShowing);
    }

    [ContextMenu("Toggle Show")]
    public virtual void ToggleShow()
    {
        if (isAnimating) return;

        if (isShowing)
            StartCoroutine(UnShowCoroutine());
        else
            StartCoroutine(ShowCoroutine());

        // isShowing = !isShowing;
    }
}

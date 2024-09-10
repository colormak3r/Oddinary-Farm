using System.Collections;
using UnityEngine;
using ColorMak3r.Utility;

public class UIBehaviour : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField]
    protected GameObject container;
    [SerializeField]
    protected float fadeDuration = 0.25f;

    [Header("Debugs")]
    [SerializeField]
    public bool isShowing;

    private void Awake()
    {
        isShowing = container.activeSelf;
    }

    public IEnumerator ShowCoroutine()
    {
        if (isShowing) yield break;
        isShowing = true;

        container.SetActive(true);
        yield return container.UIFadeCoroutine(0, 1, fadeDuration);
    }

    public IEnumerator UnShowCoroutine()
    {
        if (!isShowing) yield break;

        yield return container.UIFadeCoroutine(1, 0, fadeDuration);
        container.SetActive(false);
        
        isShowing = false;
    }

    [ContextMenu("Toggle Show")]
    public void ToggleShow()
    {
        if(isShowing)
            StartCoroutine(UnShowCoroutine());
        else
            StartCoroutine(ShowCoroutine());

        // isShowing = !isShowing;
    }

    public void ShowNoFade()
    {
        StartCoroutine(transform.UIFadeCoroutine(1, 1, 0));
        container.SetActive(true);
    }

    public void UnShowNoFade()
    {
        StartCoroutine(transform.UIFadeCoroutine(0, 0, 0));
        container.SetActive(false);
    }
}
